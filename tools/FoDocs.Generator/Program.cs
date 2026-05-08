using System.Text;
using System.Text.RegularExpressions;

return SiteOptions.TryParse(args, Directory.GetCurrentDirectory(), out var options, out var error)
    ? new SiteGenerator(options).Run()
    : PrintUsage(error);

static int PrintUsage(string? error)
{
    if (!string.IsNullOrWhiteSpace(error))
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine();
    }

    Console.WriteLine("""
    Fallout docs generator

    Usage:
      dotnet run --project tools/FoDocs.Generator -- [options]

    Options:
      --root <path>      Repository root. Defaults to the current directory.
      --content <path>   Content source directory. Defaults to <root>/content.
      --output <path>    Generated site directory. Defaults to <root>/_site.
      --clean            Delete the output directory before generating.
      --verbose          Print copied and generated files.
      --help             Show this help.
    """);

    return string.IsNullOrWhiteSpace(error) ? 0 : 1;
}

internal sealed record SiteOptions(
    DirectoryInfo Root,
    DirectoryInfo Content,
    DirectoryInfo Output,
    bool Clean,
    bool Verbose)
{
    public static bool TryParse(
        string[] args,
        string currentDirectory,
        out SiteOptions options,
        out string? error)
    {
        var rootPath = currentDirectory;
        string? contentPath = null;
        string? outputPath = null;
        var clean = false;
        var verbose = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help" or "-h":
                    options = Default(currentDirectory);
                    error = null;
                    return false;
                case "--clean":
                    clean = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--root":
                    if (!TryReadValue(args, ref i, out rootPath, out error))
                    {
                        options = Default(currentDirectory);
                        return false;
                    }
                    break;
                case "--content":
                    if (!TryReadValue(args, ref i, out contentPath, out error))
                    {
                        options = Default(currentDirectory);
                        return false;
                    }
                    break;
                case "--output":
                    if (!TryReadValue(args, ref i, out outputPath, out error))
                    {
                        options = Default(currentDirectory);
                        return false;
                    }
                    break;
                default:
                    options = Default(currentDirectory);
                    error = $"Unknown argument: {arg}";
                    return false;
            }
        }

        var root = new DirectoryInfo(Path.GetFullPath(rootPath));
        var content = new DirectoryInfo(Path.GetFullPath(contentPath ?? Path.Combine(root.FullName, "content")));
        var output = new DirectoryInfo(Path.GetFullPath(outputPath ?? Path.Combine(root.FullName, "_site")));

        options = new SiteOptions(root, content, output, clean, verbose);
        error = null;
        return true;
    }

    private static bool TryReadValue(string[] args, ref int index, out string value, out string? error)
    {
        if (index + 1 >= args.Length)
        {
            value = "";
            error = $"{args[index]} requires a value.";
            return false;
        }

        value = args[++index];
        error = null;
        return true;
    }

    private static SiteOptions Default(string currentDirectory)
    {
        var root = new DirectoryInfo(Path.GetFullPath(currentDirectory));
        return new SiteOptions(
            root,
            new DirectoryInfo(Path.Combine(root.FullName, "content")),
            new DirectoryInfo(Path.Combine(root.FullName, "_site")),
            Clean: false,
            Verbose: false);
    }
}

internal sealed class SiteGenerator(SiteOptions options)
{
    private static readonly string[] RootFilePatterns = ["*.html", "*.css", "*.sym", "*.txt"];
    private static readonly string[] PassthroughDirectories = ["img", "highslide", "symbols"];
    private static readonly string[] ExcludedDirectoryNames = [".git", "content", "tools", "_site"];

    public int Run()
    {
        if (!options.Root.Exists)
        {
            Console.Error.WriteLine($"Root directory does not exist: {options.Root.FullName}");
            return 1;
        }

        PrepareOutputDirectory();
        CopyLegacySiteFiles();

        var pages = LoadPages();
        var layout = LoadLayout();
        foreach (var page in pages)
        {
            WritePage(page, layout);
        }

        Console.WriteLine($"Generated site: {options.Output.FullName}");
        Console.WriteLine($"Migrated Markdown pages: {pages.Count}");
        return 0;
    }

    private void PrepareOutputDirectory()
    {
        if (options.Clean && options.Output.Exists)
        {
            var rootPath = EnsureTrailingSeparator(options.Root.FullName);
            var outputPath = EnsureTrailingSeparator(options.Output.FullName);
            if (!outputPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rootPath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Refusing to clean output directory outside the repository root: {options.Output.FullName}");
            }

            options.Output.Delete(recursive: true);
        }

        options.Output.Create();
    }

    private void CopyLegacySiteFiles()
    {
        foreach (var pattern in RootFilePatterns)
        {
            foreach (var file in options.Root.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly))
            {
                CopyFile(file.FullName, Path.Combine(options.Output.FullName, file.Name));
            }
        }

        foreach (var directoryName in PassthroughDirectories)
        {
            var source = Path.Combine(options.Root.FullName, directoryName);
            if (Directory.Exists(source))
            {
                CopyDirectory(source, Path.Combine(options.Output.FullName, directoryName));
            }
        }
    }

    private IReadOnlyList<GeneratedPage> LoadPages()
    {
        var pagesDirectory = new DirectoryInfo(Path.Combine(options.Content.FullName, "pages"));
        if (!pagesDirectory.Exists)
        {
            return [];
        }

        return pagesDirectory
            .EnumerateFiles("*.md", SearchOption.AllDirectories)
            .Where(file => !file.Name.StartsWith('_'))
            .Select(file => MarkdownPage.Load(file, pagesDirectory))
            .Select(page => page.Render())
            .ToList();
    }

    private string LoadLayout()
    {
        var layoutPath = Path.Combine(options.Content.FullName, "templates", "layout.html");
        return File.Exists(layoutPath)
            ? File.ReadAllText(layoutPath)
            : """
              <!DOCTYPE html>
              <html lang="en">
              <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>{{ title }}</title>
                  <link rel="stylesheet" href="style.css" type="text/css">
              </head>
              <body>
              <main id="main">
              {{ content }}
              </main>
              </body>
              </html>
              """;
    }

    private void WritePage(GeneratedPage page, string layout)
    {
        var outputPath = GetSafeOutputPath(page.OutputPath);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var html = layout
            .Replace("{{ title }}", Html.Escape(page.Title), StringComparison.Ordinal)
            .Replace("{{ description }}", Html.Escape(page.Description ?? ""), StringComparison.Ordinal)
            .Replace("{{ content }}", page.BodyHtml, StringComparison.Ordinal)
            .Replace("{{ generatedAt }}", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"), StringComparison.Ordinal);

        File.WriteAllText(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Log($"generated {page.OutputPath}");
    }

    private string GetSafeOutputPath(string relativeOutputPath)
    {
        if (Path.IsPathRooted(relativeOutputPath))
        {
            throw new InvalidOperationException($"Page output path must be relative: {relativeOutputPath}");
        }

        var outputPath = Path.GetFullPath(Path.Combine(options.Output.FullName, relativeOutputPath));
        var outputRoot = EnsureTrailingSeparator(options.Output.FullName);
        if (!outputPath.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Page output path escapes the output directory: {relativeOutputPath}");
        }

        return outputPath;
    }

    private void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(directory);
            if (ExcludedDirectoryNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            CopyFile(file, Path.Combine(targetDirectory, relativePath));
        }
    }

    private void CopyFile(string sourceFile, string targetFile)
    {
        var directory = Path.GetDirectoryName(targetFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(sourceFile, targetFile, overwrite: true);
        Log($"copied {Path.GetRelativePath(options.Root.FullName, sourceFile)}");
    }

    private void Log(string message)
    {
        if (options.Verbose)
        {
            Console.WriteLine(message);
        }
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}

internal sealed record MarkdownPage(
    FileInfo Source,
    PageMetadata Metadata,
    string Markdown)
{
    public static MarkdownPage Load(FileInfo file, DirectoryInfo pagesRoot)
    {
        var markdown = File.ReadAllText(file.FullName);
        var (metadata, body) = FrontMatter.Parse(markdown);
        var relativePath = Path.GetRelativePath(pagesRoot.FullName, file.FullName);
        var defaultOutput = Path.ChangeExtension(relativePath, ".html").Replace('\\', '/');
        var title = metadata.TryGetValue("title", out var parsedTitle)
            ? parsedTitle
            : MarkdownRenderer.FindFirstHeading(body) ?? Path.GetFileNameWithoutExtension(file.Name);

        return new MarkdownPage(
            file,
            new PageMetadata(
                Title: title,
                OutputPath: metadata.GetValueOrDefault("output", defaultOutput).Replace('\\', '/'),
                Description: metadata.GetValueOrDefault("description")),
            body);
    }

    public GeneratedPage Render() =>
        new(Metadata.Title, Metadata.OutputPath, Metadata.Description, MarkdownRenderer.Render(Markdown));
}

internal sealed record PageMetadata(string Title, string OutputPath, string? Description);

internal sealed record GeneratedPage(string Title, string OutputPath, string? Description, string BodyHtml);

internal static class FrontMatter
{
    public static (Dictionary<string, string> Metadata, string Body) Parse(string markdown)
    {
        using var reader = new StringReader(markdown);
        if (reader.ReadLine() is not "---")
        {
            return ([], markdown);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line == "---")
            {
                return (metadata, reader.ReadToEnd().TrimStart('\r', '\n'));
            }

            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(key))
            {
                metadata[key] = value;
            }
        }

        return (metadata, markdown);
    }
}

internal static partial class MarkdownRenderer
{
    public static string Render(string markdown)
    {
        var lines = markdown.ReplaceLineEndings("\n").Split('\n');
        var html = new StringBuilder();
        var paragraph = new List<string>();
        var headingIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                FlushParagraph();
                continue;
            }

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph();
                var language = trimmed[3..].Trim();
                var code = new StringBuilder();
                while (++index < lines.Length && !lines[index].Trim().StartsWith("```", StringComparison.Ordinal))
                {
                    code.AppendLine(lines[index]);
                }

                var languageClass = string.IsNullOrWhiteSpace(language)
                    ? ""
                    : $" class=\"language-{Html.EscapeAttribute(language)}\"";
                html.Append("<pre><code").Append(languageClass).Append('>')
                    .Append(Html.Escape(code.ToString().TrimEnd('\r', '\n')))
                    .AppendLine("</code></pre>");
                continue;
            }

            if (TryRenderHeading(trimmed, html, headingIds) || TryRenderTable(lines, ref index, html))
            {
                FlushParagraph();
                continue;
            }

            if (UnorderedListRegex().IsMatch(trimmed) || OrderedListRegex().IsMatch(trimmed))
            {
                FlushParagraph();
                index = RenderList(lines, index, html);
                continue;
            }

            if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
            {
                FlushParagraph();
                html.AppendLine(line);
                continue;
            }

            paragraph.Add(trimmed);
        }

        FlushParagraph();
        return html.ToString();

        void FlushParagraph()
        {
            if (paragraph.Count == 0)
            {
                return;
            }

            html.Append("<p>")
                .Append(RenderInline(string.Join(' ', paragraph)))
                .AppendLine("</p>");
            paragraph.Clear();
        }
    }

    public static string? FindFirstHeading(string markdown)
    {
        foreach (var line in markdown.ReplaceLineEndings("\n").Split('\n'))
        {
            var match = HeadingRegex().Match(line.Trim());
            if (match.Success)
            {
                return match.Groups["text"].Value.Trim();
            }
        }

        return null;
    }

    private static bool TryRenderHeading(string trimmed, StringBuilder html, Dictionary<string, int> headingIds)
    {
        var match = HeadingRegex().Match(trimmed);
        if (!match.Success)
        {
            return false;
        }

        var level = match.Groups["level"].Value.Length;
        var plainText = match.Groups["text"].Value.Trim();
        var id = CreateUniqueHeadingId(plainText, headingIds);
        var text = RenderInline(plainText);
        html.Append('<').Append('h').Append(level).Append(" id=\"").Append(id).Append("\">")
            .Append(text)
            .Append("</h").Append(level).AppendLine(">");
        return true;
    }

    private static string CreateUniqueHeadingId(string headingText, Dictionary<string, int> headingIds)
    {
        var slug = HeadingSlugInvalidCharactersRegex()
            .Replace(headingText.ToLowerInvariant(), "-")
            .Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "section";
        }

        if (!headingIds.TryGetValue(slug, out var count))
        {
            headingIds[slug] = 1;
            return slug;
        }

        headingIds[slug] = count + 1;
        return $"{slug}-{count + 1}";
    }

    private static bool TryRenderTable(string[] lines, ref int index, StringBuilder html)
    {
        if (index + 1 >= lines.Length || !LooksLikeTableRow(lines[index]) || !TableSeparatorRegex().IsMatch(lines[index + 1].Trim()))
        {
            return false;
        }

        var headers = SplitTableCells(lines[index]);
        index += 2;
        var rows = new List<IReadOnlyList<string>>();
        while (index < lines.Length && LooksLikeTableRow(lines[index]))
        {
            rows.Add(SplitTableCells(lines[index]));
            index++;
        }

        index--;
        html.AppendLine("<table>");
        html.Append("<thead><tr>");
        foreach (var header in headers)
        {
            html.Append("<th>").Append(RenderInline(header)).Append("</th>");
        }
        html.AppendLine("</tr></thead>");

        html.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            html.Append("<tr>");
            foreach (var cell in row)
            {
                html.Append("<td>").Append(RenderInline(cell)).Append("</td>");
            }
            html.AppendLine("</tr>");
        }
        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
        return true;
    }

    private static int RenderList(string[] lines, int index, StringBuilder html)
    {
        var ordered = OrderedListRegex().IsMatch(lines[index].Trim());
        var itemRegex = ordered ? OrderedListRegex() : UnorderedListRegex();
        html.AppendLine(ordered ? "<ol>" : "<ul>");

        while (index < lines.Length)
        {
            var trimmed = lines[index].Trim();
            var match = itemRegex.Match(trimmed);
            if (!match.Success)
            {
                break;
            }

            html.Append("<li>").Append(RenderInline(match.Groups["text"].Value.Trim())).AppendLine("</li>");
            index++;
        }

        html.AppendLine(ordered ? "</ol>" : "</ul>");
        return index - 1;
    }

    private static bool LooksLikeTableRow(string line) =>
        line.Trim().Contains('|', StringComparison.Ordinal);

    private static IReadOnlyList<string> SplitTableCells(string line) =>
        line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

    private static string RenderInline(string text)
    {
        var output = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    output.Append("<code>").Append(Html.Escape(text[(i + 1)..end])).Append("</code>");
                    i = end;
                    continue;
                }
            }

            if (text[i] == '[')
            {
                var closeText = text.IndexOf("](", i, StringComparison.Ordinal);
                if (closeText > i)
                {
                    var closeUrl = text.IndexOf(')', closeText + 2);
                    if (closeUrl > closeText)
                    {
                        var label = text[(i + 1)..closeText];
                        var url = text[(closeText + 2)..closeUrl];
                        output.Append("<a href=\"")
                            .Append(Html.EscapeAttribute(url))
                            .Append("\">")
                            .Append(RenderInline(label))
                            .Append("</a>");
                        i = closeUrl;
                        continue;
                    }
                }
            }

            output.Append(Html.Escape(text[i].ToString()));
        }

        return output.ToString();
    }

    [GeneratedRegex("^(?<level>#{1,6})\\s+(?<text>.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex("^[-*]\\s+(?<text>.+)$")]
    private static partial Regex UnorderedListRegex();

    [GeneratedRegex("^\\d+\\.\\s+(?<text>.+)$")]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex("^\\|?\\s*:?-{3,}:?\\s*(\\|\\s*:?-{3,}:?\\s*)+\\|?$")]
    private static partial Regex TableSeparatorRegex();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex HeadingSlugInvalidCharactersRegex();
}

internal static class Html
{
    public static string Escape(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    public static string EscapeAttribute(string value) => Escape(value);
}
