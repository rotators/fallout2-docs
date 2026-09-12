using System.Text;

internal static class InlineMarkdownRenderer
{
    private sealed class Token(string html)
    {
        public string Html = html;
        public char Marker;
        public int Count;
        public bool Open, Close, Active;
        public string Before = "", After = "";
    }

    public static string Render(string text)
    {
        var tokens = new List<Token>();
        for (var i = 0; i < text.Length;)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length && char.IsAscii(text[i + 1]) &&
                (char.IsPunctuation(text[i + 1]) || char.IsSymbol(text[i + 1])))
            {
                tokens.Add(new(Html.Escape(text[i + 1].ToString())));
                i += 2;
                continue;
            }
            if (c == '`')
            {
                var length = RunLength(text, i);
                var end = i + length;
                while (end < text.Length)
                {
                    if (text[end] != '`') { end++; continue; }
                    var closingLength = RunLength(text, end);
                    if (closingLength == length) break;
                    end += closingLength;
                }
                if (end < text.Length)
                {
                    var code = text[(i + length)..end].Replace('\n', ' ');
                    if (code.StartsWith(' ') && code.EndsWith(' ') && code.Any(ch => ch != ' '))
                        code = code[1..^1];
                    tokens.Add(new("<code>" + Html.Escape(code) + "</code>"));
                    i = end + length;
                    continue;
                }
                tokens.Add(new(new string('`', length)));
                i += length;
                continue;
            }
            if (c == '[')
            {
                var labelEnd = text.IndexOf("](", i, StringComparison.Ordinal);
                var urlEnd = labelEnd < 0 ? -1 : text.IndexOf(')', labelEnd + 2);
                if (urlEnd >= 0)
                {
                    tokens.Add(new("<a href=\"" + Html.EscapeAttribute(text[(labelEnd + 2)..urlEnd]) +
                        "\">" + Render(text[(i + 1)..labelEnd]) + "</a>"));
                    i = urlEnd + 1;
                    continue;
                }
            }
            if (c is '*' or '_')
            {
                var count = RunLength(text, i);
                var previous = i == 0 ? ' ' : text[i - 1];
                var next = i + count == text.Length ? ' ' : text[i + count];
                var previousPunctuation = char.IsPunctuation(previous) || char.IsSymbol(previous);
                var nextPunctuation = char.IsPunctuation(next) || char.IsSymbol(next);
                var left = !char.IsWhiteSpace(next) && (!nextPunctuation || char.IsWhiteSpace(previous) || previousPunctuation);
                var right = !char.IsWhiteSpace(previous) && (!previousPunctuation || char.IsWhiteSpace(next) || nextPunctuation);
                tokens.Add(new("") { Marker = c, Count = count, Active = true,
                    Open = left && (c == '*' || !right || previousPunctuation),
                    Close = right && (c == '*' || !left || nextPunctuation) });
                i += count;
                continue;
            }
            tokens.Add(new(Html.Escape(c.ToString())));
            i++;
        }

        for (var closing = 0; closing < tokens.Count; closing++)
        {
            var closer = tokens[closing];
            while (closer.Active && closer.Close && closer.Count > 0)
            {
                var opening = closing - 1;
                for (; opening >= 0; opening--)
                {
                    var candidate = tokens[opening];
                    if (!candidate.Active || !candidate.Open || candidate.Count == 0 || candidate.Marker != closer.Marker) continue;
                    if ((candidate.Close || closer.Open) && (candidate.Count + closer.Count) % 3 == 0 &&
                        (candidate.Count % 3 != 0 || closer.Count % 3 != 0)) continue;
                    break;
                }
                if (opening < 0) break;
                var opener = tokens[opening];
                var used = opener.Count >= 2 && closer.Count >= 2 ? 2 : 1;
                var tag = used == 2 ? "strong" : "em";
                opener.After = "<" + tag + ">" + opener.After;
                closer.Before += "</" + tag + ">";
                opener.Count -= used;
                closer.Count -= used;
                for (var middle = opening + 1; middle < closing; middle++) tokens[middle].Active = false;
            }
        }
        var output = new StringBuilder();
        foreach (var token in tokens)
            output.Append(token.Before).Append(token.Marker == '\0' ? token.Html : new string(token.Marker, token.Count)).Append(token.After);
        return output.ToString();
    }

    private static int RunLength(string text, int start)
    {
        var end = start + 1;
        while (end < text.Length && text[end] == text[start]) end++;
        return end - start;
    }
}
