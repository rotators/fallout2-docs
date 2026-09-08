"""Local integration tests; no VPS access or external dependencies."""
import base64
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import tempfile
import time
import unittest
import urllib.error
import urllib.request

HERE = Path(__file__).resolve().parent
KEY = 'a' * 64


class EndpointTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        cls.root = Path(cls.temp.name)
        shutil.copy(HERE / 'publish.php', cls.root)
        # Simulate the trusted TLS termination flag, only in this local fixture.
        (cls.root / 'router.php').write_text("<?php if (!isset($_GET['insecure'])) $_SERVER['HTTPS']='on'; require __DIR__.'/publish.php';")
        with socket.socket() as sock:
            sock.bind(('127.0.0.1', 0))
            port = sock.getsockname()[1]
        cls.url = f'http://127.0.0.1:{port}/'
        cls.server = subprocess.Popen(['php', '-S', f'127.0.0.1:{port}', 'router.php'],
            cwd=cls.root, env={**os.environ, 'FODOCS_PUBLISH_KEY': KEY, 'FODOCS_PUBLISH_IPS': ''},
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        for _ in range(100):
            try:
                with socket.create_connection(('127.0.0.1', port), timeout=.1):
                    break
            except OSError:
                time.sleep(.05)
        else:
            cls.server.terminate()
            cls.server.wait()
            cls.temp.cleanup()
            raise RuntimeError('PHP server did not start')

    @classmethod
    def tearDownClass(cls):
        cls.server.terminate()
        cls.server.wait(timeout=10)
        cls.temp.cleanup()

    def request(self, payload, key=KEY, suffix=''):
        request = urllib.request.Request(self.url + suffix, json.dumps(payload).encode(),
            {'Authorization': 'Bearer ' + key, 'Content-Type': 'application/json'})
        try:
            response = urllib.request.urlopen(request, timeout=5)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            return response.status, json.load(response)

    def put(self, path, data=b'hello', expected=None):
        return self.request({'op': 'put', 'path': path, 'data': base64.b64encode(data).decode(),
            'sha256': hashlib.sha256(data).hexdigest(), 'expected': expected})

    def test_authentication_and_https(self):
        self.assertEqual(self.request({'op': 'list'}, key='wrong')[0], 401)
        self.assertEqual(self.request({'op': 'list'}, suffix='?insecure=1')[0], 403)

    def test_restricted_paths(self):
        for path in ['../escape.html', '/absolute.html', 'a/../../escape.html',
                     'a\\escape.html', 'publish.php', '.htaccess', '.hidden/x.html',
                     'a//x.html', 'a/./x.html', 'shell.php.html', 'x.phtml.css', 'a./x.html']:
            with self.subTest(path=path):
                self.assertEqual(self.put(path)[0], 400)

    def test_upload_list_update_delete(self):
        path = 'assets/test file.html'
        digest = hashlib.sha256(b'hello').hexdigest()
        self.assertEqual(self.put(path)[0], 200)
        status, result = self.request({'op': 'list'})
        self.assertEqual(status, 200)
        self.assertEqual(result['files'][path], {'sha256': digest, 'size': 5})
        self.assertIn('assets', result['directories'])
        self.assertNotIn('publish.php', result['files'])
        self.assertEqual(self.put(path, b'changed')[0], 409)
        self.assertEqual((self.root / path).read_bytes(), b'hello')
        self.assertEqual(self.put(path, b'changed', digest)[0], 200)
        self.assertEqual(self.request({'op': 'delete', 'path': path, 'expected': digest})[0], 409)
        self.assertEqual(self.request({'op': 'delete', 'path': path,
            'expected': hashlib.sha256(b'changed').hexdigest()})[0], 200)
        self.assertFalse((self.root / path).exists())

    def test_bad_hash_and_missing_precondition(self):
        self.assertEqual(self.request({'op': 'put', 'path': 'bad.html', 'expected': None,
            'data': 'aGVsbG8=', 'sha256': '0' * 64})[0], 400)
        self.assertFalse((self.root / 'bad.html').exists())
        self.assertEqual(self.request({'op': 'delete', 'path': 'bad.html'})[0], 409)

    def test_symlink(self):
        outside = self.root / 'outside.txt'
        outside.write_text('keep')
        link = self.root / 'link.txt'
        try:
            link.symlink_to(outside)
        except OSError:
            self.skipTest('Symlink creation requires OS permission')
        try:
            self.assertEqual(self.put('link.txt')[0], 400)
            self.assertNotIn('link.txt', self.request({'op': 'list'})[1]['files'])
            self.assertEqual(outside.read_text(), 'keep')
        finally:
            link.unlink()
            outside.unlink()

    def test_client_sync(self):
        spec = importlib.util.spec_from_file_location('publisher', HERE / 'publish.py')
        publisher = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(publisher)
        from unittest.mock import patch
        site = self.root / '.local'
        site.mkdir(exist_ok=True)
        (site / 'index.html').write_text('client test')
        real_opener = urllib.request.build_opener()

        class LocalTransport:
            def open(inner, request, timeout):
                # Production client still validates HTTPS; redirect only the test transport.
                request.full_url = self.url
                return real_opener.open(request, timeout=timeout)

        with patch.dict(os.environ, {'FODOCS_PUBLISH_KEY': KEY}), \
             patch.object(publisher.urllib.request, 'build_opener', return_value=LocalTransport()):
            with patch('sys.argv', ['publish', '--endpoint', 'https://test.invalid/publish.php', '--site', str(site)]):
                publisher.main()
            self.assertFalse((self.root / 'index.html').exists())
            args = ['publish', '--endpoint', 'https://test.invalid/publish.php', '--site', str(site), '--apply']
            with patch('sys.argv', args):
                publisher.main()
            self.assertEqual((self.root / 'index.html').read_text(), 'client test')
            with patch('sys.argv', args):
                publisher.main()
            self.put('obsolete.txt')
            with patch('sys.argv', args + ['--delete']):
                publisher.main()
            self.assertFalse((self.root / 'obsolete.txt').exists())


if __name__ == '__main__':
    unittest.main()
