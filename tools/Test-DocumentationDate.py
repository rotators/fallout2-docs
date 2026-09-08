"""Exercise update-date behavior in an isolated Git repository (Python 3.10+)."""
import datetime
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / 'tools/FoDocs.Generator/bin/Debug/net10.0/FoDocs.Generator.dll'


def run(*args, cwd, env=None):
    return subprocess.run(args, cwd=cwd, env=env, check=True, capture_output=True, text=True).stdout


def timestamp(path, date):
    value = datetime.datetime.fromisoformat(date + 'T12:00:00+00:00').timestamp()
    os.utime(path, (value, value))


with tempfile.TemporaryDirectory(prefix='fodocs-date-') as temporary:
    root = Path(temporary)
    run('git', 'init', cwd=root)
    run('git', 'config', 'core.autocrlf', 'false', cwd=root)
    pages = root / 'content/pages'
    pages.mkdir(parents=True)
    index = pages / 'index.md'
    index.write_text('---\ntitle: Test\noutput: index.html\n---\n\nUpdated {{ documentationUpdated }}\n')
    (root / '.gitignore').write_text('_site/\n.fodocs-cache/\n')
    commit_env = {**os.environ, 'GIT_AUTHOR_NAME': 'Test', 'GIT_COMMITTER_NAME': 'Test',
                  'GIT_AUTHOR_EMAIL': 'test@example.invalid', 'GIT_COMMITTER_EMAIL': 'test@example.invalid',
                  'GIT_AUTHOR_DATE': '2020-01-02T12:00:00Z', 'GIT_COMMITTER_DATE': '2020-01-02T12:00:00Z'}
    run('git', 'add', '.', cwd=root)
    run('git', 'commit', '-m', 'Fixture', cwd=root, env=commit_env)

    def build():
        run('dotnet', str(DLL), '--root', str(root), '--clean', cwd=root)
        return (root / '_site/index.html').read_bytes()

    def expect(date):
        html = build()
        assert f'Updated {date}'.encode() in html, html
        return html

    original = expect('2020-01-02')  # Checkout timestamps are newer than this commit.
    timestamp(index, '2025-01-01')
    assert expect('2020-01-02') == original  # Touching a clean source is not a change.
    (root / 'README.md').write_text('Not published')
    expect('2020-01-02')
    index.write_text(index.read_text() + '\nLocal edit\n')
    timestamp(index, '2021-02-03')
    edited = expect('2021-02-03')
    timestamp(index, '2025-02-03')
    assert expect('2021-02-03') == edited  # Cached content survives touch and --clean.
    added = pages / 'new.md'
    added.write_text('---\ntitle: New\noutput: new.html\n---\n\nNew page\n')
    timestamp(index, '2021-02-03')
    timestamp(added, '2022-03-04')
    expect('2022-03-04')  # Includes untracked documentation.
    run('git', 'add', 'content', cwd=root)
    commit_env.update(GIT_AUTHOR_DATE='2023-04-05T12:00:00Z', GIT_COMMITTER_DATE='2023-04-05T12:00:00Z')
    run('git', 'commit', '-m', 'Documentation change', cwd=root, env=commit_env)
    expect('2023-04-05')  # Committed state uses Git, not dirty-state cache.
    added.unlink()
    deleted = build()
    assert build() == deleted  # Deletion detection also stabilizes on rebuild.
    run('git', 'restore', 'content', cwd=root)
    expect('2023-04-05')  # Reverting changes restores the committed date.
    run('git', 'add', 'README.md', cwd=root)
    commit_env.update(GIT_AUTHOR_DATE='2024-05-06T12:00:00Z', GIT_COMMITTER_DATE='2024-05-06T12:00:00Z')
    run('git', 'commit', '-m', 'Non-site change', cwd=root, env=commit_env)
    expect('2023-04-05')  # Non-documentation commits do not affect the date.
    print('Documentation date checks passed: clean checkout, touches, edits, additions, deletion, revert, and unrelated commits.')
