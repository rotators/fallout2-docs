"""Incremental publisher; Python 3.10+, standard library only."""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import urllib.request
import urllib.parse

EXTENSIONS = {'html', 'css', 'js', 'png', 'gif', 'jpg', 'jpeg', 'svg', 'ico', 'cur', 'webp', 'psd', 'sym', 'txt', 'pdf', 'woff', 'woff2'}


def valid_path(path):
    return (len(path) <= 512 and all(
        re.fullmatch(r'[A-Za-z0-9_-][A-Za-z0-9_. ()-]*', p)
        and not p.endswith(('.', ' '))
        and not re.search(r'\.(php[0-9]*|phtml|phar|cgi|pl|py|sh|shtml)(\.|$)', p, re.I)
        for p in path.split('/')) and path.rsplit('.', 1)[-1].lower() in EXTENSIONS)


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise RuntimeError('Endpoint redirected; use its exact HTTPS URL')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--endpoint', default=os.environ.get('FODOCS_PUBLISH_URL'))
    parser.add_argument('--site', type=Path, default=Path(__file__).resolve().parents[2] / '_site')
    parser.add_argument('--apply', action='store_true', help='Perform changes (default: dry run)')
    parser.add_argument('--delete', action='store_true', help='Also remove remote static files absent locally')
    args = parser.parse_args()
    url = urllib.parse.urlsplit(args.endpoint or '')
    if url.scheme != 'https' or not url.hostname or url.username or url.password or url.query or url.fragment:
        parser.error('Provide an exact HTTPS endpoint without credentials, query, or fragment')
    key = os.environ.get('FODOCS_PUBLISH_KEY', '')
    if len(key) < 64:
        parser.error('Set FODOCS_PUBLISH_KEY to the shared random key (at least 64 characters)')
    if args.site.is_symlink() or not (args.site / 'index.html').is_file():
        parser.error('Site must be a real directory containing index.html; build first')
    local = {}
    for root, dirs, files in os.walk(args.site, followlinks=False):
        for name in dirs + files:
            if (Path(root) / name).is_symlink():
                raise RuntimeError('Symlinks are forbidden in the output site')
        for name in files:
            file = Path(root) / name
            relative = file.relative_to(args.site).as_posix()
            if not valid_path(relative):
                raise RuntimeError(f'Unsupported static path: {relative}')
            data = file.read_bytes()
            if len(data) > 24 * 1024 * 1024 - 4096:
                raise RuntimeError(f'File exceeds endpoint request limit: {relative}')
            local[relative] = (data, hashlib.sha256(data).hexdigest())
    opener = urllib.request.build_opener(NoRedirect())

    def request(payload):
        req = urllib.request.Request(args.endpoint, json.dumps(payload).encode(), {
            'Authorization': 'Bearer ' + key, 'Content-Type': 'application/json'})
        try:
            with opener.open(req, timeout=120) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            raise RuntimeError(
                f"{payload['op']} {payload.get('path', '')}: HTTP {error.code}; "
                'check the VPS Apache/PHP error log'
            ) from error

    remote = request({'op': 'list'})['files']
    if not isinstance(remote, dict) or any(not valid_path(p) for p in remote):
        raise RuntimeError('Invalid server manifest')
    puts = sorted(p for p, (_, h) in local.items() if remote.get(p, {}).get('sha256') != h)
    deletes = sorted(set(remote) - set(local)) if args.delete else []
    for p in puts:
        print('PLAN PUT   ', p)
    for p in deletes:
        print('PLAN DELETE', p)
    print(f'{len(puts)} uploads, {len(deletes)} deletions' + ('' if args.apply else ' (dry run)'))
    if not args.apply:
        return
    # Upload everything successfully before attempting deletions.
    for p in puts:
        data, digest = local[p]
        request({'op': 'put', 'path': p, 'expected': remote.get(p, {}).get('sha256'),
                 'sha256': digest, 'data': base64.b64encode(data).decode('ascii')})
        print('Uploaded', p, flush=True)
    for p in deletes:
        request({'op': 'delete', 'path': p, 'expected': remote[p]['sha256']})
        print('Deleted', p, flush=True)
    verified = request({'op': 'list'})['files']
    if any(verified.get(p, {}).get('sha256') != h for p, (_, h) in local.items()) or any(p in verified for p in deletes):
        raise RuntimeError('Post-publish verification failed; rerun to reconcile')
    print('Published and verified.')


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print(f'Publish failed: {error}', file=sys.stderr)
        sys.exit(1)
