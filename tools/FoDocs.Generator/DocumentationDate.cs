using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class DocumentationDate
{
    private sealed record Stamp(string Fingerprint, string History, string Date);

    public static string Resolve(SiteOptions options)
    {
        // Only published documentation inputs count, not tooling or build output.
        string[] paths = [Path.GetRelativePath(options.Root.FullName, options.Content.FullName).Replace('\\', '/'),
            "img", "highslide", "symbols", ":(top,glob)*.css", ":(top,glob)*.sym", ":(top,glob)*.txt"];
        var files = new List<string>();
        foreach (var directory in new[] { options.Content.FullName }.Concat(
                     new[] { "img", "highslide", "symbols" }.Select(p => Path.Combine(options.Root.FullName, p))))
            if (Directory.Exists(directory)) files.AddRange(Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories));
        foreach (var pattern in new[] { "*.css", "*.sym", "*.txt" })
            files.AddRange(Directory.EnumerateFiles(options.Root.FullName, pattern));

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files.Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(options.Root.FullName, file).Replace('\\', '/') + "\0"));
            hash.AppendData(SHA256.HashData(File.ReadAllBytes(file)));
        }
        var fingerprint = Convert.ToHexString(hash.GetHashAndReset());
        var history = Git(options.Root.FullName, ["log", "-1", "--format=%cI", "--", .. paths])?.Trim() ?? "";
        var changed = Git(options.Root.FullName, ["diff", "--name-only", "-z", "HEAD", "--", .. paths]);
        var untracked = Git(options.Root.FullName, ["ls-files", "--others", "--exclude-standard", "-z", "--", .. paths]);
        var hasHistory = DateTimeOffset.TryParse(history, CultureInfo.InvariantCulture, DateTimeStyles.None, out var committed);
        var hasGit = hasHistory && changed is not null && untracked is not null;
        if (hasGit && changed!.Length == 0 && untracked!.Length == 0)
            return committed.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var cacheDirectory = Path.Combine(options.Root.FullName, ".fodocs-cache");
        var cachePath = Path.Combine(cacheDirectory, "documentation-date.json");
        if (File.Exists(cachePath))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<Stamp>(File.ReadAllText(cachePath));
                if (cached?.Fingerprint == fingerprint && cached.History == history &&
                    DateOnly.TryParseExact(cached.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    return cached.Date;
            }
            catch (JsonException) { /* Recreate an invalid cache from source metadata. */ }
        }

        var latest = hasHistory ? committed.UtcDateTime : DateTime.MinValue;
        var modifiedFiles = hasGit
            ? (changed! + untracked!).Split('\0', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => Path.Combine(options.Root.FullName, p))
            : files;
        foreach (var file in modifiedFiles)
        {
            // Git records a deletion, but the removed file no longer has an mtime.
            var modified = File.Exists(file) ? File.GetLastWriteTimeUtc(file) : DateTime.UtcNow;
            if (modified > latest) latest = modified;
        }
        if (latest == DateTime.MinValue)
            throw new InvalidOperationException("Cannot determine update date: no documentation history or source files.");
        if (!hasGit)
            Console.Error.WriteLine("Update date: Git history unavailable; using source timestamps and the local content cache.");
        var date = latest.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Directory.CreateDirectory(cacheDirectory);
        File.WriteAllText(cachePath, JsonSerializer.Serialize(new Stamp(fingerprint, history, date)));
        return date;
    }

    private static string? Git(string root, string[] args)
    {
        try
        {
            var info = new ProcessStartInfo("git")
            {
                WorkingDirectory = root, RedirectStandardOutput = true,
                RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
            };
            foreach (var arg in args) info.ArgumentList.Add(arg);
            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30_000)) { process.Kill(entireProcessTree: true); return null; }
            Task.WaitAll(output, errors);
            return process.ExitCode == 0 ? output.Result : null;
        }
        catch (System.ComponentModel.Win32Exception) { return null; }
    }
}
