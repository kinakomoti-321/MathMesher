"""Generates Editor/Engine/Symbols.UnicodeMath.cs from unicode-math-table.tex of the unicode-math package
(https://github.com/latex3/unicode-math, LPPL 1.3c).
usage: python GenerateUnicodeMathSymbols.py path/to/unicode-math-table.tex [exclude.txt]
exclude.txt lists command names (without backslash) whose glyph is missing in the bundled fonts."""
import re, io, os, sys
table = sys.argv[1]
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
text = io.open(table, encoding='utf-8').read()
exclude = set()
if len(sys.argv) > 2 and os.path.exists(sys.argv[2]):
    exclude = set(l.strip() for l in io.open(sys.argv[2], encoding='utf-8') if l.strip())
code = {'mathord': 'o', 'mathalpha': 'o', 'mathfence': 'o', 'mathbin': 'b', 'mathrel': 'r', 'mathopen': 'O', 'mathclose': 'C',
        'mathpunct': 'p', 'mathaccent': 'A', 'mathaccentwide': 'W', 'mathbotaccent': 'U', 'mathbotaccentwide': 'V',
        'mathover': 'X', 'mathunder': 'Y'}
entries = []
for m in re.finditer(r'\\UnicodeMathSymbol\{"([0-9A-F]+)\}\{\\(\w+)\s*\}\{\\(\w+)\}', text):
    cp, name, cls = int(m.group(1), 16), m.group(2), m.group(3)
    if name in exclude:
        continue
    if cls == 'mathop':
        c = 'I' if 'int' in name else 'L'
    elif cls in code:
        c = code[cls]
    else:
        continue  # mathaccentoverlay
    entries.append('%s %X %s' % (name, cp, c))
lines, line = [], ''
for e in entries:
    if len(line) + len(e) > 130:
        lines.append(line)
        line = ''
    line += e + '|'
lines.append(line)
out = '''using System.Globalization;

namespace MathMesher.Editor.Engine
{
    /// <summary>
    /// Symbol commands generated from unicode-math-table.tex of the unicode-math package (LPPL 1.3c, see ThirdPartyNotices.md),
    /// limited to symbols whose glyphs exist in the bundled fonts. Entries never replace a command already defined by the
    /// classic LaTeX / AMS tables. Regenerate with Tools~/GenerateUnicodeMathSymbols.py.
    /// Format: "name hex class": o ord, b bin, r rel, O open, C close, p punct, L large operator, I integral,
    /// A accent, W wide accent, U bottom accent, V wide bottom accent, X over brace/arrow, Y under brace/arrow.
    /// </summary>
    internal static partial class Symbols
    {
        private const string UnicodeMathTable = @"
''' + '\n'.join(lines) + '''
";

        private static void RegisterUnicodeMath()
        {
            foreach (var raw in UnicodeMathTable.Split('|'))
            {
                var parts = raw.Trim().Split(' ');
                if (parts.Length != 3) continue;
                var name = @"\\" + parts[0];
                if (Math.ContainsKey(name) || BigOps.ContainsKey(name) || OperatorNames.ContainsKey(name) || Accents.ContainsKey(name)) continue;
                int cp = int.Parse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var text = char.ConvertFromUtf32(cp);
                switch (parts[2])
                {
                    case "L": Op(parts[0], text, null); break;
                    case "I": Op(parts[0], text, false, true); break;
                    case "A": Acc(parts[0], cp); break;
                    case "W": Acc(parts[0], cp, true); break;
                    case "U": Acc(parts[0], cp, false, true); break;
                    case "V": Acc(parts[0], cp, true, true); break;
                    case "X": Acc(parts[0], cp, true, false, true); break;
                    case "Y": Acc(parts[0], cp, true, true, true); break;
                    default:
                        var cls = parts[2] switch
                        {
                            "b" => AtomClass.Bin, "r" => AtomClass.Rel, "O" => AtomClass.Open, "C" => AtomClass.Close, "p" => AtomClass.Punct, _ => AtomClass.Ord,
                        };
                        M(cls, parts[0], text);
                        break;
                }
            }
        }
    }
}
'''
io.open(os.path.join(root, 'Editor', 'Engine', 'Symbols.UnicodeMath.cs'), 'w', encoding='utf-8').write(out)
print('entries', len(entries))
