using System.Text;

internal static class SiteNavigation
{
    private static readonly (string Title, string Pages)[] FormatCategories =
    [
        ("Graphics and fonts", "frm|FRM artwork;pal|PAL palettes;fo_colors|Colors;rix|RIX images;msk|MSK masks;fon|FON fonts;aaf|AAF fonts"),
        ("Audio and video", "acm|ACM audio;lip|LIP speech;mve|MVE video"),
        ("World and characters", "pro|PRO prototypes;map|MAP maps;gcd|GCD characters;bio|BIO biographies"),
        ("Archives and game data", "dat|DAT archives;lst|LST lists;gam|GAM variables;cfg|CFG / INI configuration;savegame|Savegames;sve|SVE saves")
    ];

    private static readonly (string Title, string Pages)[] Groups =
    [
        ("Formats", string.Join(";", FormatCategories.Select(category => category.Pages))),
        ("Scripting", "ssl|SSL source;int|INT bytecode;msg|MSG messages;scripts_lst|SCRIPTS.LST;ai_txt|Combat AI;party_txt|Party members;pipboy_txt|Pip-Boy;worldmap_config|Worldmap configuration;worldmap_dat|Worldmap data;elevators|Elevators;endings|Endings;books|Skill books;criticals|Critical hits;anim_names|Animation names"),
        ("Engine", "structs|Structures;symbols|Symbols;fallout2_re|Reverse engineering;sfall_refs|sfall references;hrp|High-resolution patch;legacy-reversing|Legacy resources"),
        ("Tools", "tools|All tools;tools#archives|Archive tools;tools#scripting|Script and dialogue tools;tools#critters|Prototype and critter editors;tools#fonts|Font tools;tools#mapping|Mapping tools;tools#worldmap-and-masks|Worldmap and masks;tools#audio|Audio tools;tools#graphics|Graphics tools;tools#video|Video tools;tools#text-and-configuration|Text and configuration;tools#debugging|Debugging;tools#libraries|Libraries;tools#character-templates|Character templates;tools#saves|Save editors"),
        ("Mods", "mods|Mod directory;fo1in2|Fallout et Tu")
    ];

    public static string Render(string output)
    {
        var prefix = string.Concat(Enumerable.Repeat("../", output.Count(c => c == '/')));
        var html = new StringBuilder("<a class=\"skip-link\" href=\"#main\">Skip to content</a><header class=\"site-header\">");
        html.Append($"<a class=\"site-brand\" href=\"{prefix}index.html\">Fallout 1 &amp; 2 Docs</a>");
        html.Append("<button class=\"browse-toggle\" type=\"button\" aria-expanded=\"false\" aria-controls=\"site-nav\">Browse</button><nav id=\"site-nav\" aria-label=\"Main navigation\">");
        foreach (var group in Groups)
        {
            var links = group.Pages.Split(';').Select(entry => entry.Split('|')).ToArray();
            var active = links.Any(link => output == link[0].Split('#')[0] + ".html");
            var isFormats = group.Title == "Formats";
            html.Append($"<details class=\"nav-group{(isFormats ? " nav-formats" : "")}{(active ? " active" : "")}\"><summary>{group.Title}</summary>");
            if (isFormats)
            {
                html.Append("<div class=\"format-menu\">");
                for (var index = 0; index < FormatCategories.Length; index++)
                {
                    var category = FormatCategories[index];
                    html.Append($"<section aria-labelledby=\"format-category-{index}\"><h2 id=\"format-category-{index}\">{category.Title}</h2>");
                    AppendLinks(html, category.Pages, prefix, output);
                    html.Append("</section>");
                }
                html.Append("</div>");
            }
            else
            {
                AppendLinks(html, group.Pages, prefix, output);
            }
            html.Append("</details>");
        }
        return html.Append("</nav></header>").ToString();
    }
    private static void AppendLinks(StringBuilder html, string pages, string prefix, string output)
    {
        html.Append("<ul>");
        foreach (var entry in pages.Split(';'))
        {
            var link = entry.Split('|');
            var target = link[0].Replace("#", ".html#", StringComparison.Ordinal);
            if (!target.Contains('#')) target += ".html";
            var current = output == target ? " aria-current=\"page\"" : "";
            html.Append($"<li><a href=\"{prefix}{target}\"{current}>{Html.Escape(link[1])}</a></li>");
        }
        html.Append("</ul>");
    }}




