"""Check inline Markdown and block ordering through the real generator."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / 'tools/FoDocs.Generator/bin/Debug/net10.0/FoDocs.Generator.dll'
CASES = [
    ('**bold** and *italic*', '<strong>bold</strong> and <em>italic</em>'),
    ('__bold__ and _italic_', '<strong>bold</strong> and <em>italic</em>'),
    ('***both***', '<em><strong>both</strong></em>'),
    ('**bold *nested* text**', '<strong>bold <em>nested</em> text</strong>'),
    ('*italic **nested** text*', '<em>italic <strong>nested</strong> text</em>'),
    ('file_name_here.txt and foo__bar__baz', 'file_name_here.txt and foo__bar__baz'),
    (r'\*literal\* and \**bold**', '*literal* and *<em>bold</em>*'),
    ('`**END-DISK**` and `` `**END-PAR**` ``', '<code>**END-DISK**</code> and <code>`**END-PAR**`</code>'),
    ('**[tool](tools.html)**', '<strong><a href="tools.html">tool</a></strong>'),
    ('[**tool**](tools.html)', '<a href="tools.html"><strong>tool</strong></a>'),
    ('**open and * unmatched', '**open and * unmatched'),
    ('**a & b < c**', '<strong>a &amp; b &lt; c</strong>'),
    ('a * b * c', 'a * b * c'),
]

with tempfile.TemporaryDirectory(prefix='fodocs-markdown-') as temporary:
    root = Path(temporary)
    pages = root / 'content/pages'
    pages.mkdir(parents=True)
    source = '\n\n'.join(markdown for markdown, _ in CASES)
    source += '\n\nBefore heading\n## **Heading**\n\nBefore table\n| Label | Value |\n|---|---|\n| **Game flags** | *value* |\n\n- **List label**\n\n```text\n**END-DISK** _literal_\n```\n'
    (pages / 'index.md').write_text('---\ntitle: Rendering\noutput: index.html\ntoc: auto\n---\n\n' + source, encoding='utf-8')
    subprocess.run(['dotnet', str(DLL), '--root', str(root), '--clean'], check=True, capture_output=True)
    html = (root / '_site/index.html').read_text(encoding='utf-8')
    for markdown, expected in CASES:
        assert '<p>' + expected + '</p>' in html, (markdown, expected, html)
    assert '<td><strong>Game flags</strong></td>' in html
    assert '<td><em>value</em></td>' in html
    assert '<strong>List label</strong>' in html
    assert '<code class="language-text">**END-DISK** _literal_</code>' in html
    assert html.index('<p>Before heading</p>') < html.index('<h2 id="heading">')
    assert html.index('<p>Before table</p>') < html.index('<table>')
    assert '<h2 id="heading"><strong>Heading</strong></h2>' in html
    assert '<a href="#heading"><strong>Heading</strong></a>' in html
print('Markdown checks passed: emphasis, nesting, literals, escapes, code, links, tables, lists, headings, TOCs, and block order.')
