<?php
declare(strict_types=1);

// Install in the site root. Configure these through PHP-FPM / Apache, not HTTP.
// FODOCS_PUBLISH_KEY: at least 32 random bytes encoded as hex.
// FODOCS_PUBLISH_IPS: optional comma-separated REMOTE_ADDR allowlist.
ini_set('display_errors', '0');
header('Content-Type: application/json');
header('Cache-Control: no-store');
const MAX_BYTES = 33554432;
const EXTENSIONS = ['html', 'css', 'js', 'png', 'gif', 'jpg', 'jpeg', 'svg', 'ico', 'cur', 'webp', 'psd', 'sym', 'txt', 'pdf', 'woff', 'woff2'];

function reply(int $status, array $data): never {
    http_response_code($status);
    echo json_encode($data, JSON_THROW_ON_ERROR | JSON_UNESCAPED_SLASHES);
    exit;
}
function validPath(string $path): bool {
    if (strlen($path) > 512 || !preg_match('~\A[A-Za-z0-9_-][A-Za-z0-9_. /()-]*\z~D', $path)) return false;
    foreach (explode('/', $path) as $part) {
        if (!preg_match('~\A[A-Za-z0-9_-][A-Za-z0-9_. ()-]*\z~D', $part) || str_ends_with($part, '.') || str_ends_with($part, ' ')) return false;
        // Reject compound script names even if the final extension is static.
        if (preg_match('~\.(php[0-9]*|phtml|phar|cgi|pl|py|sh|shtml)(\.|$)~i', $part)) return false;
    }
    return in_array(strtolower(pathinfo($path, PATHINFO_EXTENSION)), EXTENSIONS, true);
}
function target(string $path, bool $create): string {
    if (!validPath($path)) reply(400, ['error' => 'Invalid static file path']);
    $parts = explode('/', $path);
    $current = __DIR__;
    foreach ($parts as $i => $part) {
        $current .= '/' . $part;
        clearstatcache(true, $current);
        if (is_link($current)) reply(400, ['error' => 'Symlinks are forbidden']);
        if ($i < count($parts) - 1) {
            if (!file_exists($current) && $create && !mkdir($current, 0755)) throw new RuntimeException('mkdir failed');
            if (file_exists($current) && !is_dir($current)) reply(409, ['error' => 'Parent is not a directory']);
        } elseif (file_exists($current) && !is_file($current)) reply(409, ['error' => 'Target is not a regular file']);
    }
    return $current;
}

try {
    if (($_SERVER['HTTPS'] ?? '') !== 'on' && ($_SERVER['HTTPS'] ?? '') !== '1') reply(403, ['error' => 'HTTPS required']);
    $key = getenv('FODOCS_PUBLISH_KEY');
    if (!$key || strlen($key) < 64) reply(503, ['error' => 'Publishing is not configured']);
    $ips = array_filter(array_map('trim', explode(',', getenv('FODOCS_PUBLISH_IPS') ?: '')));
    if ($ips && !in_array($_SERVER['REMOTE_ADDR'] ?? '', $ips, true)) reply(403, ['error' => 'Forbidden']);
    if (!hash_equals('Bearer ' . $key, $_SERVER['HTTP_AUTHORIZATION'] ?? '')) reply(401, ['error' => 'Unauthorized']);
    if ($_SERVER['REQUEST_METHOD'] !== 'POST') reply(405, ['error' => 'POST required']);
    // Lock the endpoint itself; no writable state files in the web root.
    $lock = fopen(__FILE__, 'r');
    if (!$lock || !flock($lock, LOCK_EX)) throw new RuntimeException('Lock failed');
    $body = file_get_contents('php://input', false, null, 0, MAX_BYTES + 1);
    if ($body === false || strlen($body) > MAX_BYTES) reply(413, ['error' => 'Request too large']);
    try { $request = json_decode($body, true, 16, JSON_THROW_ON_ERROR); }
    catch (JsonException $e) { reply(400, ['error' => 'Invalid JSON']); }
    if (!is_array($request)) reply(400, ['error' => 'Expected object']);
    $op = $request['op'] ?? '';
    if ($op === 'list') {
        $files = [];
        $dirs = [];
        $tree = new RecursiveCallbackFilterIterator(
            new RecursiveDirectoryIterator(__DIR__, FilesystemIterator::SKIP_DOTS),
            function ($entry): bool {
                if ($entry->isLink()) return false;
                $relative = str_replace(DIRECTORY_SEPARATOR, '/', substr($entry->getPathname(), strlen(__DIR__) + 1));
                return validPath($entry->isDir() ? $relative . '/file.html' : $relative);
            }
        );
        $iterator = new RecursiveIteratorIterator($tree, RecursiveIteratorIterator::SELF_FIRST);
        foreach ($iterator as $entry) {
            if ($entry->isLink()) continue;
            $relative = str_replace(DIRECTORY_SEPARATOR, '/', substr($entry->getPathname(), strlen(__DIR__) + 1));
            if ($entry->isDir()) { $dirs[] = $relative; continue; }
            if (!$entry->isFile() || !validPath($relative)) continue;
            $hash = hash_file('sha256', $entry->getPathname());
            if ($hash === false) throw new RuntimeException('Hash failed');
            $files[$relative] = ['sha256' => $hash, 'size' => $entry->getSize()];
        }
        ksort($files);
        sort($dirs);
        reply(200, ['files' => (object)$files, 'directories' => $dirs]);
    }
    if (!in_array($op, ['put', 'delete'], true) || !is_string($request['path'] ?? null)) reply(400, ['error' => 'Invalid operation']);
    $path = target($request['path'], false);
    $currentHash = is_file($path) ? hash_file('sha256', $path) : null;
    if (!array_key_exists('expected', $request) || $request['expected'] !== $currentHash) reply(409, ['error' => 'File changed; list again']);
    if ($op === 'delete') {
        if (is_file($path) && !unlink($path)) throw new RuntimeException('Delete failed');
        reply(200, ['ok' => true]);
    }
    $data = is_string($request['data'] ?? null) ? base64_decode($request['data'], true) : false;
    if ($data === false || !is_string($request['sha256'] ?? null) || !hash_equals(hash('sha256', $data), $request['sha256'])) reply(400, ['error' => 'Invalid data or hash']);
    $path = target($request['path'], true);
    $temp = tempnam(dirname($path), '.publish-');
    if ($temp === false) throw new RuntimeException('Temp failed');
    try {
        if (file_put_contents($temp, $data) !== strlen($data) || !chmod($temp, 0644) || !rename($temp, $path)) throw new RuntimeException('Write failed');
    } finally { if (is_file($temp)) unlink($temp); }
    reply(200, ['ok' => true]);
} catch (Throwable $e) {
    error_log('FoDocs publish: ' . $e->getMessage());
    reply(500, ['error' => 'Publishing failed; check server log']);
}
