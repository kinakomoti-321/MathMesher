using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MathMesher.Editor.Engine
{
    /// <summary>TeX / LaTeX math parser producing a node tree. User-defined macros are intentionally unsupported.</summary>
    internal sealed partial class Parser
    {
        [Flags]
        private enum Stop
        {
            None = 0, Brace = 1, Bracket = 2, Align = 4, Row = 8, End = 16, Right = 32, Dollar = 64, ParenMath = 128, BracketMath = 256, AtChar = 512,
        }

        private const Stop StopAtChar = Stop.AtChar;

        private enum Tok { EOF, Command, Char, Begin, End, Sup, Sub, Align, Space, MathShift, Param }

        private struct Token
        {
            public Tok Kind;
            public string Text;
            public int Pos;
            public override string ToString() => Kind + ":" + Text;
        }

        public const int MaxLength = 16384;
        private const int MaxDepth = 160;
        private const int MaxNodes = 40000;

        private readonly string src;
        private int pos;
        private bool textMode;
        private int depth;
        private int nodeCount;
        private readonly Dictionary<string, Rgba> definedColors = new Dictionary<string, Rgba>(StringComparer.Ordinal);
        private readonly Stack<ArrayContext> arrays = new Stack<ArrayContext>();
        private List<Node> pendingTag;

        private sealed class ArrayContext
        {
            public ArrayNode Array;
            public List<Node> RowTag;
            public bool RowNoTag;
            public char? PendingAlign;
        }

        private Parser(string source) { src = source; }

        public static List<Node> Parse(string source, out List<Node> tag)
        {
            source = source ?? "";
            if (source.Length > MaxLength) throw new TeXException($"数式は {MaxLength} 文字以内にしてください。");
            var p = new Parser(source);
            var result = p.ParseTop();
            tag = p.pendingTag;
            return result;
        }

        private TeXException Error(string message, int at = -1)
        {
            if (at < 0) at = pos;
            return new TeXException(message + $"（位置 {Math.Min(at, src.Length) + 1}）", at);
        }

        private T Count<T>(T node) where T : Node
        {
            if (++nodeCount > MaxNodes) throw Error("数式が大きすぎます。数式を分割してください。");
            return node;
        }

        // ------------------------------------------------------------------ lexer

        private void SkipComment()
        {
            while (pos < src.Length && src[pos] != '\n' && src[pos] != '\r') pos++;
        }

        private void SkipWhitespace()
        {
            while (pos < src.Length)
            {
                char c = src[pos];
                if (c == '%') { SkipComment(); continue; }
                if (char.IsWhiteSpace(c)) { pos++; continue; }
                break;
            }
        }

        private Token ReadToken(bool skipSpace)
        {
            while (true)
            {
                if (skipSpace) SkipWhitespace();
                if (pos >= src.Length) return new Token { Kind = Tok.EOF, Pos = pos };
                int start = pos;
                char c = src[pos];
                if (c == '%') { SkipComment(); continue; }
                if (char.IsWhiteSpace(c))
                {
                    while (pos < src.Length && (char.IsWhiteSpace(src[pos]) || src[pos] == '%'))
                    {
                        if (src[pos] == '%') SkipComment(); else pos++;
                    }
                    return new Token { Kind = Tok.Space, Text = " ", Pos = start };
                }
                pos++;
                switch (c)
                {
                    case '{': return new Token { Kind = Tok.Begin, Text = "{", Pos = start };
                    case '}': return new Token { Kind = Tok.End, Text = "}", Pos = start };
                    case '^': return new Token { Kind = Tok.Sup, Text = "^", Pos = start };
                    case '_': return new Token { Kind = Tok.Sub, Text = "_", Pos = start };
                    case '&': return new Token { Kind = Tok.Align, Text = "&", Pos = start };
                    case '$': return new Token { Kind = Tok.MathShift, Text = "$", Pos = start };
                    case '#': return new Token { Kind = Tok.Param, Text = "#", Pos = start };
                    case '~': return new Token { Kind = Tok.Command, Text = "~", Pos = start };
                    case '\\':
                        if (pos >= src.Length) return new Token { Kind = Tok.Command, Text = "\\", Pos = start };
                        if (IsAsciiLetter(src[pos]))
                        {
                            int s = pos;
                            while (pos < src.Length && IsAsciiLetter(src[pos])) pos++;
                            var name = "\\" + src.Substring(s, pos - s);
                            // TeX skips spaces after a control word
                            while (pos < src.Length && (src[pos] == ' ' || src[pos] == '\t' || src[pos] == '\n' || src[pos] == '\r')) pos++;
                            return new Token { Kind = Tok.Command, Text = name, Pos = start };
                        }
                        int len = char.IsHighSurrogate(src[pos]) && pos + 1 < src.Length ? 2 : 1;
                        var sym = "\\" + src.Substring(pos, len);
                        pos += len;
                        return new Token { Kind = Tok.Command, Text = sym, Pos = start };
                    default:
                        if (char.IsHighSurrogate(c) && pos < src.Length && char.IsLowSurrogate(src[pos]))
                        {
                            pos++;
                            return new Token { Kind = Tok.Char, Text = src.Substring(start, 2), Pos = start };
                        }
                        return new Token { Kind = Tok.Char, Text = c.ToString(), Pos = start };
                }
            }
        }

        private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private Token Peek(bool skipSpace)
        {
            int save = pos;
            var t = ReadToken(skipSpace);
            pos = save;
            return t;
        }

        private Token PeekMath() => Peek(true);

        private bool PeekChar(char c)
        {
            SkipWhitespace();
            return pos < src.Length && src[pos] == c;
        }

        private bool TryReadChar(char c)
        {
            if (!PeekChar(c)) return false;
            pos++;
            return true;
        }

        private bool ReadStar()
        {
            if (pos < src.Length && src[pos] == '*') { pos++; return true; }
            int save = pos;
            SkipWhitespace();
            if (pos < src.Length && src[pos] == '*') { pos++; return true; }
            pos = save;
            return false;
        }

        private void Expect(Tok kind, string what)
        {
            var t = ReadToken(!textMode);
            if (t.Kind != kind) throw Error(what + " がありません。", t.Pos);
        }

        /// <summary>Raw text of a balanced {...} group (or a single token if no brace).</summary>
        private string ReadRawGroup(string what)
        {
            SkipWhitespace();
            if (pos >= src.Length) throw Error(what + " の引数がありません。");
            if (src[pos] != '{')
            {
                var t = ReadToken(true);
                if (t.Kind == Tok.EOF) throw Error(what + " の引数がありません。");
                return t.Text;
            }
            int start = ++pos, level = 1;
            while (pos < src.Length)
            {
                char c = src[pos];
                if (c == '\\') { pos += 2; continue; }
                if (c == '{') level++;
                else if (c == '}' && --level == 0) { pos++; return src.Substring(start, pos - 1 - start); }
                pos++;
            }
            throw Error(what + " の } がありません。", start - 1);
        }

        private string ReadRawOptional()
        {
            SkipWhitespace();
            if (pos >= src.Length || src[pos] != '[') return null;
            int start = ++pos, level = 0;
            while (pos < src.Length)
            {
                char c = src[pos];
                if (c == '\\') { pos += 2; continue; }
                if (c == '{') level++;
                else if (c == '}') level--;
                else if (c == ']' && level == 0) { pos++; return src.Substring(start, pos - 1 - start); }
                pos++;
            }
            throw Error("] がありません。", start - 1);
        }

        // ------------------------------------------------------------------ lengths

        private static readonly Dictionary<string, float> units = new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["pt"] = 0.1f, ["mm"] = 0.284527559f, ["cm"] = 2.84527559f, ["in"] = 7.227f, ["bp"] = 0.10037500f, ["pc"] = 1.2f,
            ["dd"] = 0.107000865f, ["cc"] = 1.28401038f, ["sp"] = 0.1f / 65536f, ["px"] = 0.10037500f, ["nd"] = 0.106875f, ["nc"] = 1.2825f,
            ["em"] = 1f,
        };

        /// <summary>Parses a TeX dimension. Returns em, or mu when isMu is set.</summary>
        private float ParseLength(string text, out bool isMu, int at)
        {
            isMu = false;
            text = text.Trim();
            int i = 0;
            float sign = 1;
            while (i < text.Length && (text[i] == '-' || text[i] == '+' || char.IsWhiteSpace(text[i]))) { if (text[i] == '-') sign = -sign; i++; }
            int s = i;
            while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.' || text[i] == ',')) i++;
            var number = text.Substring(s, i - s).Replace(',', '.');
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (number.Length == 0)
            {
                if (text.Substring(i).StartsWith(@"\fill", StringComparison.Ordinal) || text.Substring(i).StartsWith(@"\stretch", StringComparison.Ordinal)) return 0;
                number = "1";
            }
            if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) throw Error("長さの指定が不正です: " + text, at);
            var unit = i + 2 <= text.Length ? text.Substring(i, 2) : text.Substring(i);
            if (unit == "mu") { isMu = true; return sign * value; }
            if (unit == "ex") return sign * value * 0.431f;
            if (units.TryGetValue(unit, out var factor)) return sign * value * factor;
            if (text.Substring(i).StartsWith(@"\quad", StringComparison.Ordinal)) return sign * value;
            throw Error("長さの単位が不正です: " + text, at);
        }

        private float ReadLengthGroup(string what, out bool isMu)
        {
            int at = pos;
            var raw = ReadRawGroup(what);
            return ParseLength(raw, out isMu, at);
        }

        /// <summary>Reads an unbraced dimension such as \kern3pt or \mkern-2mu.</summary>
        private float ReadInlineLength(out bool isMu)
        {
            SkipWhitespace();
            if (PeekChar('{')) return ReadLengthGroup("長さ", out isMu);
            int start = pos;
            while (pos < src.Length && (char.IsDigit(src[pos]) || src[pos] == '.' || src[pos] == ',' || src[pos] == '-' || src[pos] == '+' || src[pos] == ' ')) pos++;
            while (pos < src.Length && src[pos] == ' ') pos++;
            if (pos < src.Length && src[pos] == '\\')
            {
                var t = ReadToken(false);
                return ParseLength(src.Substring(start, t.Pos - start) + t.Text, out isMu, start);
            }
            if (pos + 2 <= src.Length) pos += 2;
            return ParseLength(src.Substring(start, pos - start), out isMu, start);
        }

        // ------------------------------------------------------------------ expressions

        private List<Node> ParseTop()
        {
            var body = ParseExpression(Stop.Align | Stop.Row);
            var next = PeekMath();
            if (next.Kind == Tok.EOF) return body;
            // Top-level & or \\: treat the formula as aligned / gathered lines.
            var array = new ArrayNode { Env = "toplevel", Pos = 0, ColumnMode = "align", AddJot = true, OuterColSep = false, ColSep = 0 };
            arrays.Push(new ArrayContext { Array = array });
            ParseArrayRows(array, body, null);
            arrays.Pop();
            bool hasAlign = false;
            foreach (var row in array.Rows) if (row.Count > 1) hasAlign = true;
            if (!hasAlign) { array.ColumnMode = "gather"; array.DefaultAlign = 'c'; }
            return new List<Node> { array };
        }

        private List<Node> ParseExpression(Stop stop)
        {
            if (++depth > MaxDepth) throw Error("数式の入れ子が深すぎます。");
            var list = new List<Node>();
            int infixAt = -1;
            FracNode infix = null;
            while (true)
            {
                var t = Peek(!textMode);
                if (t.Kind == Tok.EOF)
                {
                    if ((stop & Stop.Brace) != 0) throw Error("} がありません。", t.Pos);
                    if ((stop & Stop.End) != 0 && (stop & Stop.Row) != 0 && arrays.Count > 0 && arrays.Peek().Array.Env != "toplevel")
                        throw Error(@"\end{" + arrays.Peek().Array.Env + "} がありません。", t.Pos);
                    if ((stop & Stop.Right) != 0) throw Error(@"\right がありません。", t.Pos);
                    if ((stop & Stop.Bracket) != 0) throw Error("] がありません。", t.Pos);
                    if ((stop & Stop.Dollar) != 0) throw Error("$ が閉じていません。", t.Pos);
                    break;
                }
                if (t.Kind == Tok.End)
                {
                    if ((stop & Stop.Brace) != 0) break;
                    throw Error("対応する { がない } があります。", t.Pos);
                }
                if (t.Kind == Tok.Char && t.Text == "]" && (stop & Stop.Bracket) != 0) break;
                if (t.Kind == Tok.Char && (stop & Stop.AtChar) != 0 && t.Text.Length == 1 && t.Text[0] == cdStop) break;
                if (t.Kind == Tok.Align)
                {
                    if ((stop & Stop.Align) != 0) break;
                    throw Error("& は表や整列の環境の中で使ってください。", t.Pos);
                }
                if (t.Kind == Tok.MathShift && (!textMode || (stop & Stop.Dollar) != 0))
                {
                    if ((stop & Stop.Dollar) != 0) break;
                    throw Error("数式の中に $ があります。", t.Pos);
                }
                if (t.Kind == Tok.Space && textMode) { ReadToken(false); list.Add(Count(new TextSpaceNode { Pos = t.Pos, TextMode = true })); continue; }
                if (t.Kind == Tok.Command)
                {
                    var name = t.Text;
                    if (name == @"\\" || name == @"\cr" || name == @"\newline" || name == @"\crcr")
                    {
                        if ((stop & Stop.Row) != 0) break;
                        if (textMode) { ReadToken(false); list.Add(Count(new TextSpaceNode { Pos = t.Pos, TextMode = true })); continue; }
                        throw Error(@"\\ は表や整列の環境の中で使ってください。", t.Pos);
                    }
                    if (name == @"\end")
                    {
                        if ((stop & Stop.End) != 0) break;
                        throw Error(@"対応する \begin がない \end があります。", t.Pos);
                    }
                    if (name == @"\right")
                    {
                        if ((stop & Stop.Right) != 0) break;
                        throw Error(@"対応する \left がない \right があります。", t.Pos);
                    }
                    if (name == @"\)" && (stop & Stop.ParenMath) != 0) break;
                    if (name == @"\]" && (stop & Stop.BracketMath) != 0) break;
                    if (name == @"\middle")
                    {
                        if ((stop & Stop.Right) == 0) throw Error(@"\middle は \left と \right の間で使ってください。", t.Pos);
                        ReadToken(true);
                        list.Add(Count(new MiddleNode { Pos = t.Pos, Delim = ReadDelimiter(@"\middle") }));
                        continue;
                    }
                    if (!textMode && IsInfix(name))
                    {
                        ReadToken(true);
                        if (infix != null) throw Error("1 つのグループに分数命令（\\over など）が複数あります。", t.Pos);
                        infix = ReadInfix(name, t.Pos);
                        infixAt = list.Count;
                        continue;
                    }
                    if (TryParseSwitch(name, stop, list)) continue;
                }
                var node = textMode ? ParseTextAtom() : ParseAtom();
                if (node == null) continue;
                if (node is GroupNode g && !g.IsOrd) list.AddRange(g.Body);
                else list.Add(node);
            }
            depth--;
            if (infix != null)
            {
                infix.Num = list.GetRange(0, infixAt);
                infix.Den = list.GetRange(infixAt, list.Count - infixAt);
                return new List<Node> { infix };
            }
            return list;
        }

        private static bool IsInfix(string name) =>
            name == @"\over" || name == @"\atop" || name == @"\choose" || name == @"\brack" || name == @"\brace" || name == @"\above" ||
            name == @"\abovewithdelims" || name == @"\overwithdelims" || name == @"\atopwithdelims";

        private FracNode ReadInfix(string name, int at)
        {
            var f = new FracNode { Pos = at };
            switch (name)
            {
                case @"\atop": f.HasBar = false; break;
                case @"\choose": f.HasBar = false; f.LeftDelim = "("; f.RightDelim = ")"; break;
                case @"\brack": f.HasBar = false; f.LeftDelim = "["; f.RightDelim = "]"; break;
                case @"\brace": f.HasBar = false; f.LeftDelim = "{"; f.RightDelim = "}"; break;
                case @"\above": f.Thickness = ReadInlineLength(out _); break;
                case @"\abovewithdelims":
                    f.LeftDelim = ReadDelimiter(name); f.RightDelim = ReadDelimiter(name); f.Thickness = ReadInlineLength(out _); break;
                case @"\overwithdelims": f.LeftDelim = ReadDelimiter(name); f.RightDelim = ReadDelimiter(name); break;
                case @"\atopwithdelims": f.HasBar = false; f.LeftDelim = ReadDelimiter(name); f.RightDelim = ReadDelimiter(name); break;
            }
            return Count(f);
        }

        /// <summary>Declarations that affect the rest of the current group.</summary>
        private bool TryParseSwitch(string name, Stop stop, List<Node> list)
        {
            int at = Peek(!textMode).Pos;
            int style = name switch { @"\displaystyle" => 0, @"\textstyle" => 1, @"\scriptstyle" => 2, @"\scriptscriptstyle" => 3, _ => -1 };
            if (style >= 0 && !textMode)
            {
                ReadToken(true);
                list.Add(Count(new StyleNode { Pos = at, Style = style, Body = ParseExpression(stop) }));
                return true;
            }
            float size = name switch
            {
                @"\tiny" => 0.5f, @"\scriptsize" => 0.7f, @"\footnotesize" => 0.8f, @"\small" => 0.9f, @"\normalsize" => 1f,
                @"\large" => 1.2f, @"\Large" => 1.44f, @"\LARGE" => 1.728f, @"\huge" => 2.074f, @"\Huge" => 2.488f, _ => 0f
            };
            if (size > 0)
            {
                ReadToken(!textMode);
                list.Add(Count(new SizeNode { Pos = at, Scale = size, Body = ParseExpression(stop), TextMode = textMode }));
                return true;
            }
            if (name == @"\color")
            {
                ReadToken(!textMode);
                var color = ReadColor();
                list.Add(Count(new ColorNode { Pos = at, Color = color, Body = ParseExpression(stop), TextMode = textMode }));
                return true;
            }
            if (!textMode)
            {
                MathVariant? v = name switch
                {
                    @"\rm" => MathVariant.Upright, @"\bf" => MathVariant.Bold, @"\it" => MathVariant.Italic, @"\sf" => MathVariant.SansSerif,
                    @"\tt" => MathVariant.Monospace, @"\cal" => MathVariant.Calligraphic, @"\mit" => MathVariant.Italic,
                    @"\frak" when !IsArgumentNext() => MathVariant.Fraktur, @"\Bbb" when !IsArgumentNext() => MathVariant.DoubleStruck,
                    @"\boldmath" => MathVariant.Bold, @"\unboldmath" => MathVariant.Normal, _ => (MathVariant?)null
                };
                if (v.HasValue)
                {
                    ReadToken(true);
                    list.Add(Count(new FontNode { Pos = at, Variant = v.Value, Body = ParseExpression(stop) }));
                    return true;
                }
            }
            else
            {
                TextFont? f = name switch
                {
                    @"\rm" => TextFont.Upright, @"\upshape" => TextFont.Upright, @"\rmfamily" => TextFont.Upright, @"\normalfont" => TextFont.Normal,
                    @"\bf" => TextFont.Bold, @"\bfseries" => TextFont.Bold, @"\it" => TextFont.Italic, @"\itshape" => TextFont.Italic,
                    @"\sf" => TextFont.Sans, @"\sffamily" => TextFont.Sans, @"\tt" => TextFont.Mono, @"\ttfamily" => TextFont.Mono,
                    @"\sl" => TextFont.Slanted, @"\slshape" => TextFont.Slanted, @"\sc" => TextFont.SmallCaps, @"\scshape" => TextFont.SmallCaps,
                    @"\em" => TextFont.Italic, @"\mdseries" => TextFont.Normal, _ => (TextFont?)null
                };
                if (f.HasValue)
                {
                    ReadToken(false);
                    var body = new TextNode { Pos = at, Font = f.Value, TextMode = true, Body = ParseExpression(stop) };
                    list.Add(Count(body));
                    return true;
                }
            }
            return false;
        }

        private bool IsArgumentNext()
        {
            var t = PeekMath();
            return t.Kind == Tok.Begin || t.Kind == Tok.Char;
        }

        /// <summary>A base followed by scripts.</summary>
        private Node ParseAtom()
        {
            var first = PeekMath();
            Node baseNode = null;
            if (first.Kind != Tok.Sup && first.Kind != Tok.Sub && !(first.Kind == Tok.Char && (first.Text == "'" || IsUnicodeScript(first.Text, out _, out _))))
            {
                baseNode = ParseBase();
                if (baseNode is GroupNode g && !g.IsOrd) return baseNode; // spliced material takes no scripts
                if (baseNode == null) return null;
            }
            List<Node> sup = null, sub = null;
            while (true)
            {
                var t = PeekMath();
                if (t.Kind == Tok.Sup)
                {
                    ReadToken(true);
                    if (sup != null && !(sup.Count > 0 && IsPrimeList(sup))) throw Error("上付き文字が重複しています。{} で区切ってください。", t.Pos);
                    var arg = ParseScriptArgument("^");
                    if (sup == null) sup = arg; else sup.AddRange(arg);
                }
                else if (t.Kind == Tok.Sub)
                {
                    ReadToken(true);
                    if (sub != null) throw Error("下付き文字が重複しています。{} で区切ってください。", t.Pos);
                    sub = ParseScriptArgument("_");
                }
                else if (t.Kind == Tok.Char && t.Text == "'")
                {
                    ReadToken(true);
                    int n = 1;
                    while (pos < src.Length && src[pos] == '\'') { pos++; n++; }
                    var primes = n switch { 1 => "′", 2 => "″", 3 => "‴", 4 => "⁗", _ => new string('′', n) };
                    if (sup == null) sup = new List<Node>();
                    sup.Add(Count(new SymbolNode { Pos = t.Pos, Text = primes, Class = AtomClass.Ord }));
                }
                else if (t.Kind == Tok.Char && IsUnicodeScript(t.Text, out var isSup, out var plain))
                {
                    ReadToken(true);
                    var target = isSup ? (sup ?? (sup = new List<Node>())) : (sub ?? (sub = new List<Node>()));
                    target.Add(MakeCharNode(plain, t.Pos));
                }
                else if (t.Kind == Tok.Command && (t.Text == @"\limits" || t.Text == @"\nolimits" || t.Text == @"\displaylimits"))
                {
                    ReadToken(true);
                    bool? limits = t.Text == @"\limits" ? true : t.Text == @"\nolimits" ? (bool?)false : null;
                    switch (baseNode)
                    {
                        case OpNode op: op.Limits = limits; break;
                        case SideSetNode s when s.Base is OpNode sop: sop.Limits = limits; break;
                        case ClassNode c when c.Class == AtomClass.Op: break;
                        case HorizBraceNode _: break;
                        default: throw Error(t.Text + " は演算子（\\sum など）の直後で使ってください。", t.Pos);
                    }
                }
                else break;
            }
            if (sup == null && sub == null) return baseNode;
            return Count(new SupSubNode { Pos = first.Pos, Base = baseNode, Sup = sup, Sub = sub });
        }

        private static bool IsPrimeList(List<Node> list)
        {
            foreach (var n in list) if (!(n is SymbolNode s && (s.Text.Length > 0 && (s.Text[0] == '′' || s.Text[0] == '″' || s.Text[0] == '‴' || s.Text[0] == '⁗')))) return false;
            return true;
        }

        private const string SuperChars = "⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻⁼⁽⁾ⁿⁱ";
        private const string SuperPlain = "0123456789+-=()ni";
        private const string SubChars = "₀₁₂₃₄₅₆₇₈₉₊₋₌₍₎ₐₑₒₓₕₖₗₘₙₚₛₜ";
        private const string SubPlain = "0123456789+-=()aeoxhklmnpst";

        private static bool IsUnicodeScript(string text, out bool isSup, out string plain)
        {
            isSup = false; plain = null;
            if (text.Length != 1) return false;
            int i = SuperChars.IndexOf(text[0]);
            if (i >= 0) { isSup = true; plain = SuperPlain[i].ToString(); return true; }
            i = SubChars.IndexOf(text[0]);
            if (i >= 0) { plain = SubPlain[i].ToString(); return true; }
            return false;
        }

        private List<Node> ParseScriptArgument(string what)
        {
            var t = PeekMath();
            if (t.Kind == Tok.EOF || t.Kind == Tok.End || t.Kind == Tok.Align)
                throw Error(what + " の後に文字がありません。", t.Pos);
            if (t.Kind == Tok.Sup || t.Kind == Tok.Sub) throw Error(what + " の後に ^ / _ が続いています。{} で囲んでください。", t.Pos);
            return ParseArgument();
        }

        /// <summary>A mandatory argument: {group} or a single token (with its own arguments when it is a command).</summary>
        private List<Node> ParseArgument()
        {
            var t = Peek(true);
            if (t.Kind == Tok.Begin)
            {
                ReadToken(true);
                bool savedText = textMode;
                var body = ParseExpression(Stop.Brace);
                textMode = savedText;
                Expect(Tok.End, "}");
                return body;
            }
            if (t.Kind == Tok.EOF) throw Error("引数がありません。", t.Pos);
            if (t.Kind == Tok.End || t.Kind == Tok.Align) throw Error("引数がありません。", t.Pos);
            if (textMode && t.Kind == Tok.Space) { ReadToken(false); return ParseArgument(); }
            var node = textMode ? ParseTextAtom() : ParseBase();
            var list = new List<Node>();
            if (node is GroupNode g && !g.IsOrd) list.AddRange(g.Body);
            else if (node != null) list.Add(node);
            return list;
        }

        private List<Node> ParseMathArgument()
        {
            bool saved = textMode;
            textMode = false;
            try { return ParseArgument(); }
            finally { textMode = saved; }
        }

        private List<Node> ParseTextArgument()
        {
            bool saved = textMode;
            textMode = true;
            try
            {
                SkipWhitespace();
                if (pos < src.Length && src[pos] == '{')
                {
                    pos++;
                    var body = ParseExpression(Stop.Brace);
                    var end = ReadToken(false);
                    if (end.Kind != Tok.End) throw Error("} がありません。", end.Pos);
                    return body;
                }
                return ParseArgument();
            }
            finally { textMode = saved; }
        }

        private List<Node> ParseOptionalMath()
        {
            SkipWhitespace();
            if (pos >= src.Length || src[pos] != '[') return null;
            pos++;
            bool saved = textMode;
            textMode = false;
            var body = ParseExpression(Stop.Bracket);
            textMode = saved;
            var close = ReadToken(true);
            if (close.Kind != Tok.Char || close.Text != "]") throw Error("] がありません。", close.Pos);
            return body;
        }

        private Node ParseBase()
        {
            var t = ReadToken(true);
            switch (t.Kind)
            {
                case Tok.Begin:
                    {
                        var body = ParseExpression(Stop.Brace);
                        Expect(Tok.End, "}");
                        return Count(new GroupNode { Pos = t.Pos, Body = body });
                    }
                case Tok.Char:
                    return MakeCharNode(t.Text, t.Pos);
                case Tok.Command:
                    return ParseCommand(t.Text, t.Pos);
                case Tok.MathShift:
                    throw Error("数式の中に $ があります。", t.Pos);
                case Tok.Param:
                    throw Error("# はマクロ定義用の文字のため使えません。\\# を使ってください。", t.Pos);
                case Tok.Sup:
                case Tok.Sub:
                    throw Error(t.Text + " の位置が不正です。", t.Pos);
                default:
                    throw Error("予期しない文字です: " + t.Text, t.Pos);
            }
        }

        private Node MakeCharNode(string text, int at)
        {
            int cp = char.ConvertToUtf32(text, 0);
            if (cp >= '0' && cp <= '9') return Count(new SymbolNode { Pos = at, Text = text, Class = AtomClass.Ord, IsDigit = true });
            if (Symbols.IsLatinLetter(cp) || (Symbols.IsGreekLetter(cp) && cp != 0x2202 && cp != 0x2207))
                return Count(new SymbolNode { Pos = at, Text = text, Class = AtomClass.Ord, IsLetter = true });
            if (Symbols.CharBigOps.TryGetValue(cp, out var op))
                return Count(new OpNode { Pos = at, Symbol = op.Text, Limits = op.Limits, Integral = op.Integral });
            switch (cp)
            {
                case '-': return Count(new SymbolNode { Pos = at, Text = "−", Class = AtomClass.Bin });
                case '*': return Count(new SymbolNode { Pos = at, Text = "∗", Class = AtomClass.Bin });
                case '\'': return Count(new SymbolNode { Pos = at, Text = "′", Class = AtomClass.Ord });
                case '`': return Count(new SymbolNode { Pos = at, Text = "‘", Class = AtomClass.Ord });
                case '"': return Count(new SymbolNode { Pos = at, Text = "″", Class = AtomClass.Ord });
                case ' ': return Count(new SpaceNode { Pos = at, Amount = 0.3333f });
                case '⁡': case '⁢': case '⁣': case '​': return null;
            }
            return Count(new SymbolNode { Pos = at, Text = text, Class = Symbols.ClassOf(cp) });
        }

        private Rgba ReadColor()
        {
            var model = ReadRawOptional();
            int at = pos;
            var spec = ReadRawGroup("色");
            try { return Colors.Parse(model, spec, definedColors); }
            catch (TeXException e) { throw Error(e.Message, at); }
        }

        private string ReadDelimiter(string command)
        {
            var t = ReadToken(true);
            if (t.Kind == Tok.EOF) throw Error(command + " の区切り記号がありません。", t.Pos);
            string key = t.Kind == Tok.Begin ? null : t.Text;
            if (t.Kind == Tok.Begin)
            {
                // \left{ is not valid TeX, but {\langle} style braces around a delimiter are accepted
                var inner = ReadToken(true);
                var close = ReadToken(true);
                if (close.Kind != Tok.End) throw Error(command + " の区切り記号が不正です。", t.Pos);
                key = inner.Text;
            }
            if (key != null && Symbols.Delimiters.TryGetValue(key, out var d)) return d;
            if (key != null && key.Length <= 2 && key[0] != '\\')
            {
                int cp = char.ConvertToUtf32(key, 0);
                var cls = Symbols.ClassOf(cp);
                if (cls == AtomClass.Open || cls == AtomClass.Close) return key;
            }
            throw Error(command + " に使えない区切り記号です: " + key, t.Pos);
        }

        // ------------------------------------------------------------------ text mode

        private Node ParseTextAtom()
        {
            var t = ReadToken(false);
            switch (t.Kind)
            {
                case Tok.Space: return Count(new TextSpaceNode { Pos = t.Pos, TextMode = true });
                case Tok.Begin:
                    {
                        var body = ParseExpression(Stop.Brace);
                        var end = ReadToken(false);
                        if (end.Kind != Tok.End) throw Error("} がありません。", end.Pos);
                        return Count(new GroupNode { Pos = t.Pos, Body = body, IsOrd = false, TextMode = true });
                    }
                case Tok.MathShift:
                    {
                        bool display = pos < src.Length && src[pos] == '$';
                        if (display) pos++;
                        textMode = false;
                        var body = ParseExpression(Stop.Dollar);
                        textMode = true;
                        var close = ReadToken(true);
                        if (close.Kind != Tok.MathShift) throw Error("$ が閉じていません。", t.Pos);
                        if (display && pos < src.Length && src[pos] == '$') pos++;
                        if (display) body = new List<Node> { new StyleNode { Style = 0, Body = body, Pos = t.Pos } };
                        return Count(new MathInTextNode { Pos = t.Pos, Body = body, TextMode = true });
                    }
                case Tok.Char:
                    return TextChar(t);
                case Tok.Command:
                    return ParseTextCommand(t.Text, t.Pos);
                case Tok.Sup:
                case Tok.Sub:
                    throw Error("テキストの中では ^ と _ は使えません。$...$ で数式にしてください。", t.Pos);
                case Tok.Align:
                    throw Error("テキストの中に & があります。", t.Pos);
                case Tok.Param:
                    throw Error("# はマクロ定義用の文字のため使えません。", t.Pos);
                default:
                    throw Error("予期しない文字です。", t.Pos);
            }
        }

        private Node TextChar(Token t)
        {
            string text = t.Text;
            // TeX ligatures in text fonts
            if (text == "-" && pos < src.Length && src[pos] == '-')
            {
                pos++;
                if (pos < src.Length && src[pos] == '-') { pos++; text = "—"; }
                else text = "–";
            }
            else if (text == "`" && pos < src.Length && src[pos] == '`') { pos++; text = "“"; }
            else if (text == "'" && pos < src.Length && src[pos] == '\'') { pos++; text = "”"; }
            else if (text == "`") text = "‘";
            else if (text == "'") text = "’";
            else if (text == "!" && pos < src.Length && src[pos] == '`') { pos++; text = "¡"; }
            else if (text == "?" && pos < src.Length && src[pos] == '`') { pos++; text = "¿"; }
            return Count(new SymbolNode { Pos = t.Pos, Text = text, Class = AtomClass.Ord, TextMode = true });
        }

        private static string Compose(string baseText, int accent)
        {
            if (baseText == "i") baseText = "ı";
            if (baseText == "j") baseText = "ȷ";
            var composed = (baseText + char.ConvertFromUtf32(accent)).Normalize(NormalizationForm.FormC);
            return composed;
        }

        // ------------------------------------------------------------------ arrays

        private void ParseArrayRows(ArrayNode array, List<Node> firstCell, string envName)
        {
            var ctx = arrays.Peek();
            var row = new List<ArrayCell>();
            array.HLines.Add(ReadHLines(array, 0));
            bool first = true;
            while (true)
            {
                var cell = new ArrayCell();
                if (first && firstCell != null) cell.Body = firstCell;
                else
                {
                    if (row.Count == 0 && TryReadIntertext(array, ctx)) continue;
                    ReadMulticolumn(cell);
                    bool savedMode = textMode;
                    if (array.TextColumns != null && array.TextColumns.Contains(row.Count)) textMode = true;
                    try { cell.Body.AddRange(ParseExpression(Stop.Align | Stop.Row | Stop.End)); }
                    finally { textMode = savedMode; }
                    if (array.TextColumns != null && array.TextColumns.Contains(row.Count))
                        cell.Body = new List<Node> { Count(new TextNode { Font = TextFont.Normal, Body = cell.Body, Pos = cell.Body.Count > 0 ? cell.Body[0].Pos : pos }) };
                }
                if (ctx.PendingAlign.HasValue) { cell.Align = ctx.PendingAlign; ctx.PendingAlign = null; }
                first = false;
                row.Add(cell);
                var t = PeekMath();
                if (t.Kind == Tok.Align) { ReadToken(true); continue; }
                bool rowEnd = t.Kind == Tok.Command && (t.Text == @"\\" || t.Text == @"\cr" || t.Text == @"\newline" || t.Text == @"\crcr");
                if (rowEnd)
                {
                    ReadToken(true);
                    ReadStar();
                    float gap = 0;
                    int save = pos;
                    SkipWhitespace();
                    if (pos < src.Length && src[pos] == '[')
                    {
                        int at = pos;
                        var raw = ReadRawOptional();
                        gap = ParseLength(raw, out _, at);
                    }
                    else pos = save;
                    FinishRow(array, ctx, row, gap);
                    row = new List<ArrayCell>();
                    array.HLines.Add(ReadHLines(array, array.Rows.Count));
                    continue;
                }
                if (t.Kind == Tok.Command && t.Text == @"\end")
                {
                    if (envName == null) throw Error(@"対応する \begin がない \end があります。", t.Pos);
                    ReadToken(true);
                    int at = pos;
                    var name = ReadEnvironmentName();
                    if (name != envName) throw Error(@"\begin{" + envName + @"} と \end{" + name + "} が対応していません。", at);
                    FinishRow(array, ctx, row, 0);
                    break;
                }
                if (t.Kind == Tok.EOF)
                {
                    if (envName != null) throw Error(@"\end{" + envName + "} がありません。", t.Pos);
                    FinishRow(array, ctx, row, 0);
                    break;
                }
                throw Error("表の解析中に予期しない文字があります: " + t.Text, t.Pos);
            }
            while (array.HLines.Count < array.Rows.Count + 1) array.HLines.Add(new List<HLineKind>());
            while (array.CLines.Count < array.HLines.Count) array.CLines.Add(new List<(int, int, HLineKind)>());
            // A trailing \\ produces an empty last row: drop it (with its gap) like amsmath.
            int last = array.Rows.Count - 1;
            if (last > 0 && array.Rows[last].Count == 1 && array.Rows[last][0].Body.Count == 0 && array.Tags[last] == null)
            {
                bool lineBelow = array.HLines[last + 1].Count > 0;
                array.Rows.RemoveAt(last);
                array.Tags.RemoveAt(last);
                array.RowGaps.RemoveAt(last);
                var below = array.HLines[last + 1];
                array.HLines.RemoveAt(last + 1);
                if (lineBelow) array.HLines[last].AddRange(below);
                array.CLines.RemoveAt(last + 1);
            }
            while (array.CLines.Count < array.HLines.Count) array.CLines.Add(new List<(int, int, HLineKind)>());
        }

        private bool TryReadIntertext(ArrayNode array, ArrayContext ctx)
        {
            var t = PeekMath();
            if (t.Kind != Tok.Command || (t.Text != @"\intertext" && t.Text != @"\shortintertext")) return false;
            ReadToken(true);
            var body = ParseTextArgument();
            var row = new List<ArrayCell> { new ArrayCell { Span = 1000, Align = 'l', Body = new List<Node> { Count(new TextNode { Font = TextFont.Normal, Body = body, Pos = t.Pos }) } } };
            FinishRow(array, ctx, row, 0);
            array.HLines.Add(new List<HLineKind>());
            while (array.CLines.Count < array.HLines.Count) array.CLines.Add(new List<(int, int, HLineKind)>());
            return true;
        }

        private void FinishRow(ArrayNode array, ArrayContext ctx, List<ArrayCell> row, float gap)
        {
            array.Rows.Add(row);
            array.RowGaps.Add(gap);
            array.Tags.Add(ctx.RowNoTag ? null : ctx.RowTag);
            ctx.RowTag = null;
            ctx.RowNoTag = false;
        }

        private List<HLineKind> ReadHLines(ArrayNode array, int boundary)
        {
            var lines = new List<HLineKind>();
            while (array.CLines.Count <= boundary) array.CLines.Add(new List<(int, int, HLineKind)>());
            while (true)
            {
                var t = PeekMath();
                if (t.Kind != Tok.Command) break;
                if (t.Text == @"\hline") { ReadToken(true); lines.Add(HLineKind.Solid); }
                else if (t.Text == @"\hdashline") { ReadToken(true); lines.Add(HLineKind.Dashed); }
                else if (t.Text == @"\cline" || t.Text == @"\cdashline")
                {
                    ReadToken(true);
                    int at = pos;
                    var spec = ReadRawGroup(t.Text);
                    var parts = spec.Split('-');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out var a) || !int.TryParse(parts[1], out var b) || a < 1 || b < a)
                        throw Error(t.Text + " の範囲指定が不正です: " + spec, at);
                    array.CLines[boundary].Add((a - 1, b - 1, t.Text == @"\cline" ? HLineKind.Solid : HLineKind.Dashed));
                }
                else if (t.Text == @"\noalign")
                {
                    ReadToken(true);
                    ReadRawGroup(@"\noalign");
                }
                else break;
            }
            return lines;
        }

        private void ReadMulticolumn(ArrayCell cell)
        {
            var t = PeekMath();
            if (t.Kind != Tok.Command) return;
            if (t.Text == @"\multicolumn")
            {
                ReadToken(true);
                int at = pos;
                var n = ReadRawGroup(@"\multicolumn");
                if (!int.TryParse(n.Trim(), out var span) || span < 1) throw Error(@"\multicolumn の列数が不正です。", at);
                var spec = ReadRawGroup(@"\multicolumn").Replace("|", "").Trim();
                cell.Span = span;
                cell.Align = spec.Length > 0 ? spec[0] : 'c';
                cell.Body.AddRange(ParseMathArgument());
            }
            else if (t.Text == @"\hdotsfor")
            {
                ReadToken(true);
                var spacing = ReadRawOptional();
                int at = pos;
                var n = ReadRawGroup(@"\hdotsfor");
                if (!int.TryParse(n.Trim(), out var span) || span < 1) throw Error(@"\hdotsfor の列数が不正です。", at);
                cell.Span = span;
                cell.Body.Add(Count(new HDotsForNode { Pos = t.Pos, Columns = span, Spacing = spacing != null && float.TryParse(spacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var sp) ? sp : 1f }));
            }
        }

        private string ReadEnvironmentName()
        {
            SkipWhitespace();
            int at = pos;
            if (!TryReadChar('{')) throw Error("環境名がありません。", at);
            int start = pos;
            while (pos < src.Length && src[pos] != '}') pos++;
            if (pos >= src.Length) throw Error("環境名の } がありません。", at);
            var name = src.Substring(start, pos - start).Trim();
            pos++;
            return name;
        }

        private List<ColumnSpec> ParseColumnSpec(string spec, ArrayNode array, int at)
        {
            var cols = new List<ColumnSpec>();
            var pendingRules = new List<HLineKind>();
            List<Node> pendingBefore = null;
            int i = 0;
            // expand *{n}{spec}
            while (spec.Contains("*{"))
            {
                int s = spec.IndexOf("*{", StringComparison.Ordinal);
                int nEnd = spec.IndexOf('}', s);
                if (nEnd < 0 || !int.TryParse(spec.Substring(s + 2, nEnd - s - 2), out var count) || count < 0 || count > 100) throw Error("列指定 *{n}{...} が不正です。", at);
                if (nEnd + 1 >= spec.Length || spec[nEnd + 1] != '{') throw Error("列指定 *{n}{...} が不正です。", at);
                int level = 0, j = nEnd + 1;
                for (; j < spec.Length; j++) { if (spec[j] == '{') level++; else if (spec[j] == '}' && --level == 0) break; }
                if (j >= spec.Length) throw Error("列指定 *{n}{...} が不正です。", at);
                var inner = spec.Substring(nEnd + 2, j - nEnd - 2);
                var sb = new StringBuilder();
                for (int k = 0; k < count; k++) sb.Append(inner);
                spec = spec.Substring(0, s) + sb + spec.Substring(j + 1);
            }
            while (i < spec.Length)
            {
                char c = spec[i++];
                switch (c)
                {
                    case 'l': case 'c': case 'r':
                        cols.Add(new ColumnSpec { Align = c, LeftRules = pendingRules, Before = pendingBefore });
                        pendingRules = new List<HLineKind>(); pendingBefore = null;
                        break;
                    case 'p': case 'm': case 'b': case 'X':
                        {
                            float? width = null;
                            if (i < spec.Length && spec[i] == '{')
                            {
                                int end = spec.IndexOf('}', i);
                                if (end < 0) throw Error("列指定の } がありません。", at);
                                width = ParseLength(spec.Substring(i + 1, end - i - 1), out _, at);
                                i = end + 1;
                            }
                            cols.Add(new ColumnSpec { Align = 'l', Width = width, LeftRules = pendingRules, Before = pendingBefore });
                            pendingRules = new List<HLineKind>(); pendingBefore = null;
                            break;
                        }
                    case '|': pendingRules.Add(HLineKind.Solid); break;
                    case ':': pendingRules.Add(HLineKind.Dashed); break;
                    case '@': case '!':
                        {
                            if (i >= spec.Length || spec[i] != '{') throw Error("列指定 @{...} が不正です。", at);
                            int level = 0, j = i;
                            for (; j < spec.Length; j++) { if (spec[j] == '{') level++; else if (spec[j] == '}' && --level == 0) break; }
                            if (j >= spec.Length) throw Error("列指定 @{...} の } がありません。", at);
                            var inner = spec.Substring(i + 1, j - i - 1);
                            pendingBefore = Parser.ParseFragment(inner, definedColors);
                            if (c == '!') pendingBefore.Insert(0, new SpaceNode { Amount = array.ColSep });
                            i = j + 1;
                            break;
                        }
                    case '>': case '<':
                        {
                            // >{decl} <{decl}: skip
                            if (i < spec.Length && spec[i] == '{')
                            {
                                int level = 0, j = i;
                                for (; j < spec.Length; j++) { if (spec[j] == '{') level++; else if (spec[j] == '}' && --level == 0) break; }
                                i = j + 1;
                            }
                            break;
                        }
                    case ' ': break;
                    default: throw Error("列指定に使えない文字です: " + c, at);
                }
            }
            array.TrailingRules = pendingRules;
            if (pendingBefore != null) cols.Add(new ColumnSpec { Align = 'c', Before = pendingBefore, Width = 0 });
            return cols;
        }

        internal static List<Node> ParseFragment(string text, Dictionary<string, Rgba> colors)
        {
            var p = new Parser(text);
            if (colors != null) foreach (var kv in colors) p.definedColors[kv.Key] = kv.Value;
            return p.ParseExpression(Stop.None);
        }
    }
}
