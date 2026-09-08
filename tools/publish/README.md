# Publishing the docs

The endpoint runs on PHP 8.1+ on a Linux VPS. The client uses Python 3.10+
without packages. Only generated `_site/` files are published.

## VPS installation

1. Install `publish.php` in the directory serving `https://fodev.net/files/fo2/`.
2. Generate a shared key with `openssl rand -hex 32`. Store it outside the web
   root and configure `FODOCS_PUBLISH_KEY` in the PHP worker environment. For
   PHP-FPM, a root-owned pool configuration can contain
   `env[FODOCS_PUBLISH_KEY] = <your-key>`. Reload the pool after changing it.
   Do not commit the real key or embed it in the endpoint.
3. Ensure PHP receives the `Authorization` header and `HTTPS=on` from the
   web server. Require TLS at the web server too. For a reverse proxy, set
   HTTPS in trusted server configuration; this script deliberately does not
   trust forwarded headers from the caller.
4. Give the PHP worker write access to the documentation files and their
   parent directories. Keep the endpoint owned by the administrator and not
   writable by PHP. No other untrusted process should write within this tree.
   Use a dedicated PHP pool/user where practical.
5. Configure the web server so only `publish.php` executes as PHP here;
   all other files must be served as static content, including `.html`.
   Disable directory indexes and deny access to dotfiles (including temporary
   `.publish-*` files). Set the request body limit to 32 MiB or less and PHP's
   memory limit to at least 256 MiB. Add rate limiting on this endpoint.
6. Optionally set `FODOCS_PUBLISH_IPS` to comma-separated client IPs. This checks
   `REMOTE_ADDR`, so with a proxy use an allowlist at the trusted edge instead.

### Apache with mod_php (`php_module`)

Use Apache `SetEnv` instead of the PHP-FPM pool setting. Inside the existing
HTTPS virtual host, scope it to the site's actual filesystem directory and
endpoint (replace the example path):

```apache
<Directory "/var/www/fodev.net/files/fo2">
    <Files "publish.php">
        SetEnv FODOCS_PUBLISH_KEY "<64-character key from openssl rand -hex 32>"
        # Optional: use your actual public IP address.
        # SetEnv FODOCS_PUBLISH_IPS "203.0.113.10"
    </Files>
</Directory>
```

Keep this in an administrator-owned Apache configuration file outside the web
root, not `.htaccess`. `mod_env` must be enabled. Run `apachectl configtest`, then
reload Apache using your distribution's service command. The endpoint's existing
`getenv('FODOCS_PUBLISH_KEY')` reads this setting without code changes. The client
still uses the same key in its local environment. Avoid displaying `phpinfo()`
publicly, as it can expose environment settings.

With mod_php, filesystem writes run as the Apache worker user; there is no
separate PHP-FPM pool. The scoped setting limits which requests receive the key,
but does not isolate this publisher from other PHP applications running as the
same OS user.

Reference: [Apache SetEnv](https://httpd.apache.org/docs/2.4/mod/mod_env.html#setenv).

The directory must be dedicated to this site before using `--delete`: every
allowed static file below it is considered publishable. PHP, dotfiles, and
unsupported file types are excluded from the manifest and cannot be modified.
The API never deletes directories. Empty directories can remain after publishing.
It rejects symbolic links and traversal, and allows only listed static file
extensions. Both endpoint and client reject compound script names such as
`shell.php.html`. Adjust both extension lists if adding a new static asset type.

## Client usage

Set these environment variables in the local terminal or CI secret store:

```powershell
$env:FODOCS_PUBLISH_URL = 'https://fodev.net/files/fo2/publish.php'
$env:FODOCS_PUBLISH_KEY = '<your-key>'
```

Build and validate with `gen.bat`. Preview uploads, then apply:

```powershell
python tools/publish/publish.py
python tools/publish/publish.py --apply
```

Add `--delete` to preview/remove remote static files that are missing locally.
Review the first deletion plan carefully if the server has legacy files.
The client refuses missing/empty output without `index.html`, rejects symlinks,
checks all local paths before uploading, refuses HTTP and redirects, and
verifies remote SHA-256 hashes after publishing. It snapshots local file contents
before listing the server, so a concurrent rebuild cannot alter its uploads.

For one-command build, validate, and publish, run `publish.bat` at the repo root.
It forwards options such as `--delete`; use the Python command directly for a dry
run. CI can run the same build/validation and then the client with `--apply`.
Publishing is not implicitly attached to ordinary builds. Serialize publish jobs.

## Protocol and security limits

All requests are JSON POSTs to the endpoint with `Authorization: Bearer <key>`.
HTTPS protects the key and request body in transit. A random 256-bit key and
constant-time comparison are sufficient for this small private publisher; an
IP allowlist provides another restriction. Keep headers out of access logs.
Rotate the key in both places if exposed. No cookies or CORS access are used.

- `{"op":"list"}` returns `files`, a map from relative paths to `sha256` and
  `size`, plus `directories`. Directories themselves have no content hash.
- `put` takes `path`, base64 `data`, `sha256`, and `expected` (the previous file
  hash, or JSON null for a new file).
- `delete` takes `path` and `expected`.

Changes require a matching previous hash or return HTTP 409. Each request is
locked, and each upload uses a same-directory temporary file and atomic rename.
All uploads complete before deletions start. This is not an atomic release:
visitors can see mixed versions during a publish, and a failed run can leave
some updates applied. Rerunning reconciles the remaining differences. Keep VPS
backups for rollback. The hash precondition prevents most stale writes, but does
not replace serializing whole publish jobs. Bearer requests have no separate
nonce/replay cache; TLS and keeping the key secret are the authentication boundary.

The API cannot overwrite its PHP endpoint, but a stolen key can change all
allowed static content, including JavaScript. Server-side filesystem checks
assume no other process races them with symlink changes. This is a narrow
publishing API, not a sandbox against other applications sharing its Unix user.

PHP reference: [timing-safe hash comparison](https://www.php.net/manual/en/function.hash-equals.php).

Run local integration tests with `python tools/publish/test_publish.py` (requires
`php` on PATH). Tests use a temporary site and a localhost PHP server; they never
contact the VPS.
