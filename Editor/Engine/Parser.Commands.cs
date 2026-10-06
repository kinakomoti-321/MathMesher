using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MathMesher.Editor.Engine
{
    internal sealed partial class Parser
    {
        private static readonly HashSet<string> forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            @"\newcommand", @"\renewcommand", @"\providecommand", @"\def", @"\gdef", @"\edef", @"\xdef", @"\let", @"\futurelet",
            @"\newenvironment", @"\renewenvironment", @"\DeclareMathOperator", @"\DeclarePairedDelimiter", @"\DeclareRobustCommand",
            @"\documentclass", @"\usepackage", @"\input", @"\include", @"\begin{document}", @"\catcode", @"\csname", @"\expandafter",
            @"\newcount", @"\newdimen", @"\setlength", @"\addtolength", @"\renewcommand*", @"\newcommand*",
        };

        private static readonly Dictionary<string, MathVariant> fontCommands = new Dictionary<string, MathVariant>(StringComparer.Ordinal)
        {
            [@"\mathrm"] = MathVariant.Upright, [@"\symrm"] = MathVariant.Upright, [@"\mathup"] = MathVariant.Upright, [@"\symup"] = MathVariant.Upright,
            [@"\mathit"] = MathVariant.Italic, [@"\symit"] = MathVariant.Italic, [@"\mathbf"] = MathVariant.Bold, [@"\symbf"] = MathVariant.Bold,
            [@"\bold"] = MathVariant.Bold, [@"\mathbfup"] = MathVariant.Bold, [@"\mathsf"] = MathVariant.SansSerif, [@"\symsf"] = MathVariant.SansSerif,
            [@"\mathsfup"] = MathVariant.SansSerif, [@"\mathtt"] = MathVariant.Monospace, [@"\symtt"] = MathVariant.Monospace,
            [@"\mathcal"] = MathVariant.Calligraphic, [@"\symcal"] = MathVariant.Calligraphic, [@"\mathscr"] = MathVariant.Script,
            [@"\symscr"] = MathVariant.Script, [@"\mathfrak"] = MathVariant.Fraktur, [@"\symfrak"] = MathVariant.Fraktur, [@"\frak"] = MathVariant.Fraktur,
            [@"\mathbb"] = MathVariant.DoubleStruck, [@"\symbb"] = MathVariant.DoubleStruck, [@"\Bbb"] = MathVariant.DoubleStruck,
            [@"\mathds"] = MathVariant.DoubleStruck, [@"\mathbbm"] = MathVariant.DoubleStruck, [@"\mathnormal"] = MathVariant.Normal,
            [@"\mathbfit"] = MathVariant.BoldItalic, [@"\symbfit"] = MathVariant.BoldItalic, [@"\mathbfcal"] = MathVariant.BoldCalligraphic,
            [@"\mathbfscr"] = MathVariant.BoldScript, [@"\mathbffrak"] = MathVariant.BoldFraktur, [@"\mathsfit"] = MathVariant.SansItalic,
            [@"\mathbfsf"] = MathVariant.SansBold, [@"\mathsfbf"] = MathVariant.SansBold, [@"\mathbfsfit"] = MathVariant.SansBoldItalic,
            [@"\mathsfbfit"] = MathVariant.SansBoldItalic, [@"\symbfsf"] = MathVariant.SansBold, [@"\symsfit"] = MathVariant.SansItalic,
        };

        private static readonly Dictionary<string, TextFont> textCommands = new Dictionary<string, TextFont>(StringComparer.Ordinal)
        {
            [@"\text"] = TextFont.Normal, [@"\textnormal"] = TextFont.Upright, [@"\textrm"] = TextFont.Upright, [@"\textup"] = TextFont.Upright,
            [@"\textmd"] = TextFont.Normal, [@"\mbox"] = TextFont.Normal, [@"\hbox"] = TextFont.Normal, [@"\textit"] = TextFont.Italic,
            [@"\emph"] = TextFont.Italic, [@"\textsl"] = TextFont.Slanted, [@"\textbf"] = TextFont.Bold, [@"\textsf"] = TextFont.Sans,
            [@"\texttt"] = TextFont.Mono, [@"\textsc"] = TextFont.SmallCaps, [@"\textbfit"] = TextFont.BoldItalic, [@"\textsfbf"] = TextFont.SansBold,
            [@"\textsfit"] = TextFont.SansItalic, [@"\textsuperscript"] = TextFont.Normal, [@"\textsubscript"] = TextFont.Normal,
        };

        private static readonly Dictionary<string, (int cp, string kind)> xArrows = new Dictionary<string, (int, string)>(StringComparer.Ordinal)
        {
            [@"\xleftarrow"] = (0x2190, null), [@"\xrightarrow"] = (0x2192, null), [@"\xLeftarrow"] = (0x21D0, null), [@"\xRightarrow"] = (0x21D2, null),
            [@"\xleftrightarrow"] = (0x2194, null), [@"\xLeftrightarrow"] = (0x21D4, null), [@"\xhookleftarrow"] = (0x21A9, null),
            [@"\xhookrightarrow"] = (0x21AA, null), [@"\xmapsto"] = (0x21A6, null), [@"\xmapsfrom"] = (0x21A4, null),
            [@"\xrightharpoondown"] = (0x21C1, null), [@"\xrightharpoonup"] = (0x21C0, null), [@"\xleftharpoondown"] = (0x21BD, null),
            [@"\xleftharpoonup"] = (0x21BC, null), [@"\xrightleftharpoons"] = (0x21CC, null), [@"\xleftrightharpoons"] = (0x21CB, null),
            [@"\xtwoheadrightarrow"] = (0x21A0, null), [@"\xtwoheadleftarrow"] = (0x219E, null), [@"\xlongequal"] = ('=', null),
            [@"\xtofrom"] = (0x21C4, null), [@"\xrightleftarrows"] = (0x21C4, null), [@"\xleftrightarrows"] = (0x21C6, null),
            [@"\xLongrightarrow"] = (0x21D2, null), [@"\xLongleftarrow"] = (0x21D0, null), [@"\xlongrightarrow"] = (0x2192, null),
            [@"\xlongleftarrow"] = (0x2190, null), [@"\xequal"] = ('=', null), [@"\xrightrightarrows"] = (0x21C9, null),
            [@"\xleftleftarrows"] = (0x21C7, null), [@"\xRrightarrow"] = (0x21DB, null), [@"\xLleftarrow"] = (0x21DA, null),
        };

        private static readonly Dictionary<string, (int cp, bool over)> braces = new Dictionary<string, (int, bool)>(StringComparer.Ordinal)
        {
            [@"\overbrace"] = (0x23DE, true), [@"\underbrace"] = (0x23DF, false), [@"\overbracket"] = (0x23B4, true),
            [@"\underbracket"] = (0x23B5, false), [@"\overparen"] = (0x23DC, true), [@"\underparen"] = (0x23DD, false),
            [@"\overshell"] = (0x23E0, true), [@"\undershell"] = (0x23E1, false),
        };

        private static GroupNode Splice(params Node[] nodes)
        {
            var g = new GroupNode { IsOrd = false };
            foreach (var n in nodes) if (n != null) g.Body.Add(n);
            return g;
        }

        private static GroupNode Splice(List<Node> nodes) => new GroupNode { IsOrd = false, Body = nodes };

        private SymbolNode Sym(string text, AtomClass cls, int at, bool letter = false) =>
            Count(new SymbolNode { Pos = at, Text = text, Class = cls, IsLetter = letter, TextMode = textMode });

        private List<Node> Letters(string text, int at)
        {
            var list = new List<Node>();
            foreach (var c in text) list.Add(Sym(c.ToString(), AtomClass.Ord, at, true));
            return list;
        }

        private Node Upright(string text, int at) =>
            Count(new FontNode { Pos = at, Variant = MathVariant.Upright, Body = Letters(text, at) });

        // ------------------------------------------------------------------ math commands

        private Node ParseCommand(string name, int at)
        {
            if (forbidden.Contains(name))
                throw Error("数式記法のみを使用してください。ユーザー定義マクロや文書・ファイル命令（" + name + "）には対応していません。", at);
            var common = ParseCommonCommand(name, at, out bool handled);
            if (handled) return common;

            switch (name)
            {
                case @"\frac": return Count(new FracNode { Pos = at, Num = ParseMathArgument(), Den = ParseMathArgument() });
                case @"\dfrac": return Count(new FracNode { Pos = at, Num = ParseMathArgument(), Den = ParseMathArgument(), Style = 0 });
                case @"\tfrac": return Count(new FracNode { Pos = at, Num = ParseMathArgument(), Den = ParseMathArgument(), Style = 1 });
                case @"\cfrac":
                    {
                        var align = ReadRawOptional();
                        return Count(new FracNode { Pos = at, Continued = true, ContinuedAlign = string.IsNullOrEmpty(align) ? 'c' : align.Trim()[0], Num = ParseMathArgument(), Den = ParseMathArgument() });
                    }
                case @"\binom": case @"\dbinom": case @"\tbinom":
                    return Count(new FracNode
                    {
                        Pos = at, HasBar = false, LeftDelim = "(", RightDelim = ")", Num = ParseMathArgument(), Den = ParseMathArgument(),
                        Style = name == @"\dbinom" ? 0 : name == @"\tbinom" ? 1 : (int?)null
                    });
                case @"\genfrac":
                    {
                        var l = ReadRawGroup(name).Trim();
                        var r = ReadRawGroup(name).Trim();
                        int tAt = pos;
                        var thick = ReadRawGroup(name).Trim();
                        var style = ReadRawGroup(name).Trim();
                        var f = new FracNode { Pos = at, LeftDelim = DelimFromRaw(l, at), RightDelim = DelimFromRaw(r, at) };
                        if (thick.Length > 0) { f.Thickness = ParseLength(thick, out _, tAt); f.HasBar = f.Thickness > 0; }
                        if (style.Length > 0) f.Style = int.TryParse(style, out var s) && s >= 0 && s <= 3 ? s : throw Error(@"\genfrac のスタイル指定は 0～3 です。", tAt);
                        f.Num = ParseMathArgument(); f.Den = ParseMathArgument();
                        return Count(f);
                    }
                case @"\sfrac": case @"\nicefrac": case @"\xfrac":
                    {
                        ReadRawOptional(); ReadRawOptional();
                        return Count(new FracNode { Pos = at, Skewed = true, Num = ParseMathArgument(), Den = ParseMathArgument() });
                    }
                case @"\sqrt":
                    {
                        var index = ParseOptionalMath();
                        return Count(new SqrtNode { Pos = at, Index = index, Body = ParseMathArgument() });
                    }
                case @"\root":
                    {
                        int of = src.IndexOf(@"\of", pos, StringComparison.Ordinal);
                        if (of < 0) throw Error(@"\root には \of が必要です。", at);
                        var index = ParseFragment(src.Substring(pos, of - pos), definedColors);
                        pos = of + 3;
                        return Count(new SqrtNode { Pos = at, Index = index, Body = ParseMathArgument() });
                    }
                case @"\left":
                    {
                        var left = ReadDelimiter(name);
                        var body = ParseExpression(Stop.Right);
                        var t = ReadToken(true);
                        if (t.Kind != Tok.Command || t.Text != @"\right") throw Error(@"\left に対応する \right がありません。", at);
                        var right = ReadDelimiter(@"\right");
                        return Count(new LeftRightNode { Pos = at, Left = left, Right = right, Body = body });
                    }
                case @"\overline": return Count(new LineNode { Pos = at, Over = true, Body = ParseMathArgument() });
                case @"\underline": case @"\underbar": return Count(new LineNode { Pos = at, Over = false, Body = ParseMathArgument() });
                case @"\overset": { var over = ParseMathArgument(); var b = ParseMathArgument(); return Count(new StackNode { Pos = at, Over = over, Base = b, Class = ClassOfList(b) }); }
                case @"\underset": { var under = ParseMathArgument(); var b = ParseMathArgument(); return Count(new StackNode { Pos = at, Under = under, Base = b, Class = ClassOfList(b) }); }
                case @"\stackrel": { var over = ParseMathArgument(); var b = ParseMathArgument(); return Count(new StackNode { Pos = at, Over = over, Base = b, Class = AtomClass.Rel }); }
                case @"\stackbin": { var over = ParseMathArgument(); var b = ParseMathArgument(); return Count(new StackNode { Pos = at, Over = over, Base = b, Class = AtomClass.Bin }); }
                case @"\overunderset": { var over = ParseMathArgument(); var under = ParseMathArgument(); var b = ParseMathArgument(); return Count(new StackNode { Pos = at, Over = over, Under = under, Base = b, Class = ClassOfList(b) }); }
                case @"\underoverset": { var under = ParseMathArgument(); var over = ParseMathArgument(); var b = ParseMathArgument(); return Count(new StackNode { Pos = at, Over = over, Under = under, Base = b, Class = ClassOfList(b) }); }
                case @"\boldsymbol": case @"\bm": case @"\mathbold":
                    return Count(new FontNode { Pos = at, Variant = MathVariant.BoldItalic, BoldSymbol = true, Body = ParseMathArgument() });
                case @"\pmb": return Count(new FontNode { Pos = at, Variant = MathVariant.Default, PoorMansBold = true, Body = ParseMathArgument() });
                case @"\operatorname": case @"\operatornamewithlimits": case @"\mathop":
                    {
                        bool star = name != @"\mathop" && ReadStar();
                        var body = ParseMathArgument();
                        if (name == @"\mathop")
                        {
                            bool single = body.Count == 1 && body[0] is SymbolNode;
                            return Count(new OpNode { Pos = at, Body = body, Limits = null, Integral = single && false });
                        }
                        var op = new OpNode { Pos = at, Body = new List<Node> { Count(new FontNode { Variant = MathVariant.Upright, Body = OperatorText(body) }) } };
                        op.Limits = star || name == @"\operatornamewithlimits" ? (bool?)null : false;
                        return Count(op);
                    }
                case @"\mathbin": return Count(new ClassNode { Pos = at, Class = AtomClass.Bin, Body = ParseMathArgument() });
                case @"\mathrel": return Count(new ClassNode { Pos = at, Class = AtomClass.Rel, Body = ParseMathArgument() });
                case @"\mathopen": return Count(new ClassNode { Pos = at, Class = AtomClass.Open, Body = ParseMathArgument() });
                case @"\mathclose": return Count(new ClassNode { Pos = at, Class = AtomClass.Close, Body = ParseMathArgument() });
                case @"\mathpunct": return Count(new ClassNode { Pos = at, Class = AtomClass.Punct, Body = ParseMathArgument() });
                case @"\mathinner": return Count(new ClassNode { Pos = at, Class = AtomClass.Inner, Body = ParseMathArgument() });
                case @"\mathord": return Count(new ClassNode { Pos = at, Class = AtomClass.Ord, Body = ParseMathArgument() });
                case @"\mathchoice":
                    return Count(new MathChoiceNode { Pos = at, Display = ParseMathArgument(), Text = ParseMathArgument(), Script = ParseMathArgument(), ScriptScript = ParseMathArgument() });
                case @"\not":
                    {
                        var target = ParseBase();
                        if (target is SymbolNode s)
                        {
                            var composed = (s.Text + "̸").Normalize(NormalizationForm.FormC);
                            if (composed.Length < s.Text.Length + 1) return Count(new SymbolNode { Pos = at, Text = composed, Class = s.Class });
                        }
                        return Count(new NotNode { Pos = at, Base = target });
                    }
                case @"\pmod":
                    {
                        var arg = ParseMathArgument();
                        var list = new List<Node> { Count(new SpaceNode { Amount = 18, Mu = true }), Sym("(", AtomClass.Open, at), Upright("mod", at), Count(new SpaceNode { Amount = 6, Mu = true }) };
                        list.Add(Count(new GroupNode { Body = arg })); list.Add(Sym(")", AtomClass.Close, at));
                        return Splice(list);
                    }
                case @"\pod":
                    {
                        var arg = ParseMathArgument();
                        return Splice(Count(new SpaceNode { Amount = 18, Mu = true }), Sym("(", AtomClass.Open, at), Count(new GroupNode { Body = arg }), Sym(")", AtomClass.Close, at));
                    }
                case @"\mod":
                    {
                        var arg = ParseMathArgument();
                        return Splice(Count(new SpaceNode { Amount = 18, Mu = true }), Upright("mod", at), Count(new SpaceNode { Amount = 6, Mu = true }), Count(new GroupNode { Body = arg }));
                    }
                case @"\bmod":
                    return Count(new ClassNode { Pos = at, Class = AtomClass.Bin, Body = new List<Node> { Upright("mod", at) } });
                case @"\iff": return Splice(Count(new SpaceNode { Amount = 5, Mu = true }), Sym("⟺", AtomClass.Rel, at), Count(new SpaceNode { Amount = 5, Mu = true }));
                case @"\implies": return Splice(Count(new SpaceNode { Amount = 5, Mu = true }), Sym("⟹", AtomClass.Rel, at), Count(new SpaceNode { Amount = 5, Mu = true }));
                case @"\impliedby": return Splice(Count(new SpaceNode { Amount = 5, Mu = true }), Sym("⟸", AtomClass.Rel, at), Count(new SpaceNode { Amount = 5, Mu = true }));
                case @"\idotsint": return Count(new OpNode { Pos = at, Symbol = "∫⋯∫", Limits = false, Integral = true });
                case @"\smallint": return Count(new OpNode { Pos = at, Symbol = "∫", Limits = false, Integral = true, Small = true });
                case @"\substack":
                    {
                        var array = new ArrayNode { Env = "subarray", Pos = at, ColumnMode = "array", ColSep = 0, OuterColSep = false, ArrayStretch = 0.5f, CellStyle = 2, DefaultAlign = 'c' };
                        ParseBracedRows(array);
                        return Count(array);
                    }
                case @"\sideset":
                    {
                        var left = ParseMathArgument();
                        var right = ParseMathArgument();
                        var s = new SideSetNode { Pos = at };
                        ExtractScripts(left, out s.LeftSup, out s.LeftSub, name);
                        ExtractScripts(right, out s.RightSup, out s.RightSub, name);
                        s.Base = ParseBase();
                        return Count(s);
                    }
                case @"\prescript":
                    {
                        var sup = ParseMathArgument();
                        var sub = ParseMathArgument();
                        var b = ParseMathArgument();
                        return Count(new SideSetNode { Pos = at, LeftSup = sup.Count > 0 ? sup : null, LeftSub = sub.Count > 0 ? sub : null, Base = Count(new GroupNode { Body = b }) });
                    }
                case @"\begin": return ParseEnvironment(at);
                case @"\bra": { var a = ParseMathArgument(); a.Insert(0, Sym("⟨", AtomClass.Open, at)); a.Add(Sym("|", AtomClass.Close, at)); return Count(new GroupNode { Pos = at, Body = a }); }
                case @"\ket": { var a = ParseMathArgument(); a.Insert(0, Sym("|", AtomClass.Open, at)); a.Add(Sym("⟩", AtomClass.Close, at)); return Count(new GroupNode { Pos = at, Body = a }); }
                case @"\braket": { var a = ParseMathArgument(); a.Insert(0, Sym("⟨", AtomClass.Open, at)); a.Add(Sym("⟩", AtomClass.Close, at)); return Count(new GroupNode { Pos = at, Body = a }); }
                case @"\set": { var a = ParseMathArgument(); a.Insert(0, Sym("{", AtomClass.Open, at)); a.Add(Sym("}", AtomClass.Close, at)); return Count(new GroupNode { Pos = at, Body = a }); }
                case @"\Bra": return Count(new LeftRightNode { Pos = at, Left = "⟨", Right = "|", Body = ParseMathArgument() });
                case @"\Ket": return Count(new LeftRightNode { Pos = at, Left = "|", Right = "⟩", Body = ParseMathArgument() });
                case @"\Braket": return Count(new LeftRightNode { Pos = at, Left = "⟨", Right = "⟩", Body = BarsToMiddle(ParseMathArgument()) });
                case @"\Set": return Count(new LeftRightNode { Pos = at, Left = "{", Right = "}", Body = BarsToMiddle(ParseMathArgument()) });
                case @"\displaylines":
                    {
                        var array = new ArrayNode { Env = "displaylines", Pos = at, ColumnMode = "gather", AddJot = true, CellStyle = 0, OuterColSep = false, ColSep = 0 };
                        ParseBracedRows(array);
                        return Count(array);
                    }
                case @"\matrix": case @"\pmatrix": case @"\bordermatrix":
                    {
                        var array = new ArrayNode { Env = "matrix", Pos = at, ColumnMode = "matrix", OuterColSep = false, DefaultAlign = 'c' };
                        ParseBracedRows(array);
                        if (name == @"\pmatrix") return Count(new LeftRightNode { Pos = at, Left = "(", Right = ")", Body = new List<Node> { array } });
                        return Count(array);
                    }
                case @"\cases":
                    {
                        var array = new ArrayNode { Env = "cases", Pos = at, ColumnMode = "cases", OuterColSep = false, ColSep = 0, ArrayStretch = 1.2f, CellStyle = 1, DefaultAlign = 'l' };
                        ParseBracedRows(array);
                        return Count(new LeftRightNode { Pos = at, Left = "{", Right = "", Body = new List<Node> { array } });
                    }
                case @"\lefteqn": return Count(new LapNode { Pos = at, Align = 1, Body = ParseMathArgument() });
                case @"\ensuremath": return Count(new GroupNode { Pos = at, Body = ParseMathArgument() });
                case @"\middle": throw Error(@"\middle は \left と \right の間で使ってください。", at);
                case @"\limits": case @"\nolimits": case @"\displaylimits": throw Error(name + " は演算子（\\sum など）の直後で使ってください。", at);
                case @"\hline": case @"\hdashline": case @"\cline": throw Error(name + " は表環境（array など）の中で使ってください。", at);
                case @"\multicolumn": case @"\hdotsfor": throw Error(name + " は表のセルの先頭で使ってください。", at);
                case @"\shoveleft": case @"\shoveright":
                    {
                        var body = ParseMathArgument();
                        if (arrays.Count > 0) arrays.Peek().PendingAlign = name == @"\shoveleft" ? 'l' : 'r';
                        return Splice(body);
                    }
                case @"\intertext": case @"\shortintertext": throw Error(name + @" は align 環境の \\ の直後で使ってください。", at);
            }

            if (fontCommands.TryGetValue(name, out var variant))
                return Count(new FontNode { Pos = at, Variant = variant, Body = ParseMathArgument() });
            if (braces.TryGetValue(name, out var brace))
                return Count(new HorizBraceNode { Pos = at, Codepoint = brace.cp, Over = brace.over, Body = ParseMathArgument() });
            if (xArrows.TryGetValue(name, out var xa))
            {
                var below = ParseOptionalMath();
                var above = ParseMathArgument();
                return Count(new XArrowNode { Pos = at, Codepoint = xa.cp, Kind = xa.kind, Above = above, Below = below });
            }
            if (TryBigDelimiter(name, at, out var big)) return big;
            if (Symbols.Accents.TryGetValue(name, out var accent))
                return Count(new AccentNode { Pos = at, Accent = accent, Base = ParseMathArgument() });
            if (Symbols.BigOps.TryGetValue(name, out var bigop))
                return Count(new OpNode { Pos = at, Symbol = bigop.Text, Limits = bigop.Limits, Integral = bigop.Integral });
            if (Symbols.OperatorNames.TryGetValue(name, out var opname))
                return Count(new OpNode { Pos = at, Name = opname.Name, Limits = opname.Limits ? (bool?)null : false, Decoration = opname.Decoration });
            if (Symbols.Math.TryGetValue(name, out var sym) && sym.MathMode)
                return Count(new SymbolNode { Pos = at, Text = sym.Text, Class = sym.Class, IsLetter = sym.IsLetter });
            if (Symbols.Text.TryGetValue(name, out var tsym))
                return Count(new TextNode { Pos = at, Font = TextFont.Upright, Body = new List<Node> { new SymbolNode { Pos = at, Text = tsym.Text, TextMode = true } } });
            if (Symbols.TextAccents.ContainsKey(name))
                throw Error(name + " は文字用のアクセントです。数式では \\hat や \\acute などを使ってください。", at);
            throw Error("未対応の命令です: " + name, at);
        }

        private static List<Node> OperatorText(List<Node> body)
        {
            // amsmath: '-' and '*' inside \operatorname are text characters
            var list = new List<Node>();
            foreach (var n in body)
            {
                if (n is SymbolNode s && (s.Text == "−" || s.Text == "∗"))
                    list.Add(new SymbolNode { Pos = s.Pos, Text = s.Text == "−" ? "-" : "*", Class = AtomClass.Ord });
                else list.Add(n);
            }
            return list;
        }

        private static AtomClass ClassOfList(List<Node> body)
        {
            if (body.Count == 1)
            {
                switch (body[0])
                {
                    case SymbolNode s when s.Class == AtomClass.Bin || s.Class == AtomClass.Rel: return s.Class;
                    case ClassNode c when c.Class == AtomClass.Bin || c.Class == AtomClass.Rel: return c.Class;
                    case StackNode st: return st.Class;
                }
            }
            return AtomClass.Ord;
        }

        private List<Node> BarsToMiddle(List<Node> body)
        {
            var list = new List<Node>();
            foreach (var n in body)
            {
                if (n is SymbolNode s && (s.Text == "|" || s.Text == "∣")) list.Add(Count(new MiddleNode { Pos = s.Pos, Delim = "|" }));
                else if (n is SymbolNode d && d.Text == "‖") list.Add(Count(new MiddleNode { Pos = d.Pos, Delim = "‖" }));
                else list.Add(n);
            }
            return list;
        }

        private void ExtractScripts(List<Node> group, out List<Node> sup, out List<Node> sub, string command)
        {
            sup = null; sub = null;
            foreach (var n in group)
            {
                if (n is SupSubNode s && s.Base == null)
                {
                    if (s.Sup != null) sup = s.Sup;
                    if (s.Sub != null) sub = s.Sub;
                }
                else throw Error(command + " の引数は _ と ^ だけで指定してください（例: {_a^b}）。", n.Pos);
            }
        }

        private string DelimFromRaw(string raw, int at)
        {
            raw = raw.Trim();
            if (raw.Length == 0 || raw == ".") return null;
            if (Symbols.Delimiters.TryGetValue(raw, out var d)) return d;
            throw Error("区切り記号が不正です: " + raw, at);
        }

        private bool TryBigDelimiter(string name, int at, out Node node)
        {
            node = null;
            string stem = name;
            AtomClass cls = AtomClass.Ord;
            if (stem.EndsWith("l", StringComparison.Ordinal) && stem.Length > 4 && (stem.StartsWith(@"\big", StringComparison.Ordinal) || stem.StartsWith(@"\Big", StringComparison.Ordinal))) { cls = AtomClass.Open; stem = stem.Substring(0, stem.Length - 1); }
            else if (stem.EndsWith("r", StringComparison.Ordinal) && stem.Length > 4 && (stem.StartsWith(@"\big", StringComparison.Ordinal) || stem.StartsWith(@"\Big", StringComparison.Ordinal))) { cls = AtomClass.Close; stem = stem.Substring(0, stem.Length - 1); }
            else if (stem.EndsWith("m", StringComparison.Ordinal) && stem.Length > 4 && (stem.StartsWith(@"\big", StringComparison.Ordinal) || stem.StartsWith(@"\Big", StringComparison.Ordinal))) { cls = AtomClass.Rel; stem = stem.Substring(0, stem.Length - 1); }
            int size = stem switch { @"\big" => 1, @"\Big" => 2, @"\bigg" => 3, @"\Bigg" => 4, _ => 0 };
            if (size == 0) return false;
            node = Count(new DelimSizingNode { Pos = at, Size = size, Class = cls, Delim = ReadDelimiter(name) });
            return true;
        }

        // ------------------------------------------------------------------ commands valid in both modes

        private Node ParseCommonCommand(string name, int at, out bool handled)
        {
            handled = true;
            if (Symbols.MuSpaces.TryGetValue(name, out var mu))
            {
                if (textMode) return Count(new SpaceNode { Pos = at, Amount = mu / 18f, TextMode = true });
                return Count(new SpaceNode { Pos = at, Amount = mu, Mu = true });
            }
            if (Symbols.EmSpaces.TryGetValue(name, out var em) && !float.IsNaN(em))
                return em == 0 ? null : Count(new SpaceNode { Pos = at, Amount = em, TextMode = textMode });
            if (textCommands.TryGetValue(name, out var tf))
            {
                var body = ParseTextArgument();
                var text = Count(new TextNode { Pos = at, Font = tf, Body = body, TextMode = textMode });
                if (name == @"\textsuperscript") return Count(new RaiseNode { Pos = at, Dy = 0.36f, Body = new List<Node> { Count(new SizeNode { Scale = 0.7f, Body = new List<Node> { text } }) } });
                if (name == @"\textsubscript") return Count(new RaiseNode { Pos = at, Dy = -0.15f, Body = new List<Node> { Count(new SizeNode { Scale = 0.7f, Body = new List<Node> { text } }) } });
                return text;
            }
            switch (name)
            {
                case @"\hspace": case @"\hspace*":
                    {
                        ReadStar();
                        var amount = ReadLengthGroup(name, out bool isMu);
                        return Count(new SpaceNode { Pos = at, Amount = amount, Mu = isMu, TextMode = textMode });
                    }
                case @"\kern": case @"\hskip": case @"\mkern": case @"\mskip": case @"\mspace":
                    {
                        var amount = name == @"\mspace" ? ReadLengthGroup(name, out bool m1) : ReadInlineLength(out m1);
                        if ((name == @"\mkern" || name == @"\mskip" || name == @"\mspace") && !m1) throw Error(name + " の単位は mu で指定してください。", at);
                        if ((name == @"\kern" || name == @"\hskip") && m1) throw Error(name + " では mu 単位は使えません。\\mkern を使ってください。", at);
                        return Count(new SpaceNode { Pos = at, Amount = amount, Mu = m1, TextMode = textMode });
                    }
                case @"\vspace": case @"\vspace*": case @"\vskip": ReadStar(); ReadRawGroup(name); return null;
                case @"\phantom": return Count(new PhantomNode { Pos = at, Body = ModeArgument() });
                case @"\hphantom": return Count(new PhantomNode { Pos = at, Body = ModeArgument(), KeepHeight = false });
                case @"\vphantom": return Count(new PhantomNode { Pos = at, Body = ModeArgument(), KeepWidth = false });
                case @"\smash":
                    {
                        var opt = ReadRawOptional();
                        var s = new SmashNode { Pos = at, Body = ModeArgument() };
                        if (opt != null) { s.Top = opt.Contains("t"); s.Bottom = opt.Contains("b"); if (!s.Top && !s.Bottom) s.Top = s.Bottom = true; }
                        return Count(s);
                    }
                case @"\mathstrut": return Count(new PhantomNode { Pos = at, KeepWidth = false, Body = new List<Node> { new SymbolNode { Text = "(", Class = AtomClass.Open } } });
                case @"\strut": return Count(new RuleNode { Pos = at, Width = 0, Height = 1.2f, Shift = -0.36f });
                case @"\llap": case @"\rlap": case @"\clap":
                    return Count(new LapNode { Pos = at, Align = name == @"\llap" ? -1 : name == @"\rlap" ? 1 : 0, Body = new List<Node> { Count(new TextNode { Font = TextFont.Normal, Body = ParseTextArgument(), TextMode = textMode }) } });
                case @"\mathllap": case @"\mathrlap": case @"\mathclap":
                    return Count(new LapNode { Pos = at, Align = name == @"\mathllap" ? -1 : name == @"\mathrlap" ? 1 : 0, Body = ParseMathArgument() });
                case @"\raisebox":
                    {
                        var dy = ReadLengthGroup(name, out _);
                        ReadRawOptional(); ReadRawOptional();
                        return Count(new RaiseNode { Pos = at, Dy = dy, Body = new List<Node> { Count(new TextNode { Font = TextFont.Normal, Body = ParseTextArgument(), TextMode = textMode }) } });
                    }
                case @"\rule":
                    {
                        var shiftRaw = ReadRawOptional();
                        float shift = shiftRaw != null ? ParseLength(shiftRaw, out _, at) : 0;
                        var w = ReadLengthGroup(name, out _);
                        var h = ReadLengthGroup(name, out _);
                        return Count(new RuleNode { Pos = at, Width = w, Height = h, Shift = shift });
                    }
                case @"\vcenter": return Count(new VCenterNode { Pos = at, Body = ParseMathArgument() });
                case @"\textcolor":
                    {
                        var color = ReadColor();
                        return Count(new ColorNode { Pos = at, Color = color, Body = ModeArgument(), TextMode = textMode });
                    }
                case @"\colorbox":
                    {
                        var color = ReadColor();
                        return Count(new EncloseNode { Pos = at, Kind = "colorbox", Background = color, Body = TextBox(), TextBody = true });
                    }
                case @"\fcolorbox":
                    {
                        var frame = ReadColor();
                        var back = ReadColor();
                        return Count(new EncloseNode { Pos = at, Kind = "fcolorbox", Frame = frame, Background = back, Body = TextBox(), TextBody = true });
                    }
                case @"\definecolor":
                    {
                        var cname = ReadRawGroup(name).Trim();
                        var model = ReadRawGroup(name).Trim();
                        int cAt = pos;
                        var spec = ReadRawGroup(name);
                        try { definedColors[cname] = Colors.Parse(model, spec, definedColors); }
                        catch (TeXException e) { throw Error(e.Message, cAt); }
                        return null;
                    }
                case @"\boxed": return Count(new EncloseNode { Pos = at, Kind = "box", Body = ParseMathArgument() });
                case @"\fbox": return Count(new EncloseNode { Pos = at, Kind = "box", Body = TextBox(), TextBody = true });
                case @"\framebox": ReadRawOptional(); ReadRawOptional(); return Count(new EncloseNode { Pos = at, Kind = "box", Body = TextBox(), TextBody = true });
                case @"\cancel": return Count(new EncloseNode { Pos = at, Kind = "cancel", Body = ModeArgument(), TextBody = textMode });
                case @"\bcancel": return Count(new EncloseNode { Pos = at, Kind = "bcancel", Body = ModeArgument(), TextBody = textMode });
                case @"\xcancel": return Count(new EncloseNode { Pos = at, Kind = "xcancel", Body = ModeArgument(), TextBody = textMode });
                case @"\sout": return Count(new EncloseNode { Pos = at, Kind = "sout", Body = ModeArgument(), TextBody = textMode });
                case @"\cancelto":
                    {
                        var to = ParseMathArgument();
                        return Count(new EncloseNode { Pos = at, Kind = "cancelto", CancelTo = to, Body = ParseMathArgument() });
                    }
                case @"\angl": return Count(new EncloseNode { Pos = at, Kind = "angl", Body = ParseMathArgument() });
                case @"\phase": return Count(new EncloseNode { Pos = at, Kind = "phase", Body = ParseMathArgument() });
                case @"\enclose":
                    {
                        int nAt = pos;
                        var notation = ReadRawGroup(name);
                        ReadRawOptional();
                        var set = new HashSet<string>(notation.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
                        foreach (var n in set)
                            if (!enclosures.Contains(n)) throw Error(@"\enclose の未対応の指定です: " + n, nAt);
                        return Count(new EncloseNode { Pos = at, Kind = "enclose", Notations = set, Body = ParseMathArgument() });
                    }
                case @"\bbox":
                    {
                        var opt = ReadRawOptional();
                        var node = new EncloseNode { Pos = at, Kind = "bbox", Body = ParseMathArgument() };
                        if (opt != null) ParseBBoxOptions(opt, node, at);
                        return Count(node);
                    }
                case @"\tag":
                    {
                        bool star = ReadStar();
                        var body = ParseTextArgument();
                        var tag = new List<Node>();
                        if (!star) tag.Add(new SymbolNode { Text = "(", TextMode = true });
                        tag.Add(new TextNode { Font = TextFont.Upright, Body = body, TextMode = true });
                        if (!star) tag.Add(new SymbolNode { Text = ")", TextMode = true });
                        if (arrays.Count > 0) arrays.Peek().RowTag = tag;
                        else if (pendingTag != null) throw Error(@"\tag が複数あります。", at);
                        else pendingTag = tag;
                        return null;
                    }
                case @"\notag": case @"\nonumber": if (arrays.Count > 0) arrays.Peek().RowNoTag = true; return null;
                case @"\label": ReadRawGroup(name); return null;
                case @"\TeX": return TeXLogo(at, false);
                case @"\LaTeX": return TeXLogo(at, true);
                case @"\char":
                    {
                        int cp = ReadCharCode(at);
                        return CodepointNode(cp, at);
                    }
                case @"\unicode":
                    {
                        var raw = ReadRawGroup(name).Trim();
                        string digits = raw;
                        var style = NumberStyles.Integer;
                        if (raw.StartsWith("U+", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) { digits = raw.Substring(2); style = NumberStyles.HexNumber; }
                        else if (raw.StartsWith("x", StringComparison.OrdinalIgnoreCase)) { digits = raw.Substring(1); style = NumberStyles.HexNumber; }
                        bool ok = int.TryParse(digits, style, CultureInfo.InvariantCulture, out int cp);
                        if (!ok || cp <= 0 || cp > 0x10FFFF || (cp >= 0xD800 && cp <= 0xDFFF)) throw Error(@"\unicode の値が不正です: " + raw, at);
                        return CodepointNode(cp, at);
                    }
                case @"\verb":
                    {
                        bool visible = pos < src.Length && src[pos] == '*';
                        if (visible) pos++;
                        if (pos >= src.Length) throw Error(@"\verb の区切り文字がありません。", at);
                        char delim = src[pos++];
                        int end = src.IndexOf(delim, pos);
                        if (end < 0) throw Error(@"\verb が閉じていません。", at);
                        var raw = src.Substring(pos, end - pos);
                        pos = end + 1;
                        var t = new TextNode { Pos = at, Font = TextFont.Mono, TextMode = textMode };
                        foreach (var ch in raw)
                            t.Body.Add(ch == ' ' ? (visible ? new SymbolNode { Text = "␣", TextMode = true } : (Node)new SpaceNode { Amount = 0.525f, TextMode = true }) : new SymbolNode { Text = ch.ToString(), TextMode = true });
                        return Count(t);
                    }
                case @"\(": case @"\[":
                    {
                        if (!textMode) throw Error(name + " は数式の中では使えません。", at);
                        textMode = false;
                        var body = ParseExpression(name == @"\(" ? Stop.ParenMath : Stop.BracketMath);
                        var close = ReadToken(true);
                        textMode = true;
                        if (close.Kind != Tok.Command || close.Text != (name == @"\(" ? @"\)" : @"\]")) throw Error(name + " が閉じていません。", at);
                        if (name == @"\[") body = new List<Node> { new StyleNode { Style = 0, Body = body } };
                        return Count(new MathInTextNode { Pos = at, Body = body, TextMode = true });
                    }
                case @"\)": case @"\]": throw Error(name + " に対応する開き記号がありません。", at);
            }
            handled = false;
            return null;
        }

        private static readonly HashSet<string> enclosures = new HashSet<string>(StringComparer.Ordinal)
        {
            "box", "roundedbox", "circle", "left", "right", "top", "bottom", "updiagonalstrike", "downdiagonalstrike", "verticalstrike",
            "horizontalstrike", "updiagonalarrow", "actuarial", "madruwb", "radical", "longdiv", "phasorangle",
        };

        private void ParseBBoxOptions(string opt, EncloseNode node, int at)
        {
            foreach (var raw in opt.Split(','))
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;
                if (part.StartsWith("border", StringComparison.OrdinalIgnoreCase))
                {
                    var value = part.Substring(part.IndexOf(':') + 1).Trim();
                    node.Frame = new Rgba(1, 1, 1);
                    foreach (var w in value.Split(' '))
                    {
                        if (w.Length == 0) continue;
                        if (char.IsDigit(w[0]) || w[0] == '.') { try { node.LineWidth = ParseLength(w, out _, at); } catch (TeXException) { } }
                        else if (w != "solid" && w != "dashed") { try { node.Frame = Colors.ParseExpression(w, definedColors); } catch (TeXException) { } }
                    }
                }
                else if (char.IsDigit(part[0]) || part[0] == '.') node.Padding = ParseLength(part, out _, at);
                else node.Background = Colors.ParseExpression(part, definedColors);
            }
            if (node.Frame == null && node.Background != null) node.Kind = "bbox";
        }

        private List<Node> ModeArgument() => textMode ? ParseTextArgument() : ParseMathArgument();

        private List<Node> TextBox()
        {
            var body = ParseTextArgument();
            return new List<Node> { Count(new TextNode { Font = TextFont.Normal, Body = body, TextMode = textMode }) };
        }

        private int ReadCharCode(int at)
        {
            SkipWhitespace();
            if (pos >= src.Length) throw Error(@"\char の値がありません。", at);
            char c = src[pos];
            int value;
            if (c == '"')
            {
                int s = ++pos;
                while (pos < src.Length && Uri.IsHexDigit(src[pos])) pos++;
                if (!int.TryParse(src.Substring(s, pos - s), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)) throw Error(@"\char の値が不正です。", at);
            }
            else if (c == '\'')
            {
                int s = ++pos;
                while (pos < src.Length && src[pos] >= '0' && src[pos] <= '7') pos++;
                value = 0;
                foreach (var ch in src.Substring(s, pos - s)) value = value * 8 + (ch - '0');
            }
            else if (c == '`')
            {
                pos++;
                if (pos < src.Length && src[pos] == '\\') pos++;
                if (pos >= src.Length) throw Error(@"\char の値が不正です。", at);
                value = char.ConvertToUtf32(src, pos);
                pos += char.IsHighSurrogate(src[pos]) ? 2 : 1;
            }
            else
            {
                int s = pos;
                while (pos < src.Length && char.IsDigit(src[pos])) pos++;
                if (!int.TryParse(src.Substring(s, pos - s), out value)) throw Error(@"\char の値が不正です。", at);
            }
            if (value <= 0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF)) throw Error(@"\char の値が範囲外です。", at);
            return value;
        }

        private Node CodepointNode(int cp, int at)
        {
            var text = char.ConvertFromUtf32(cp);
            if (textMode) return Count(new SymbolNode { Pos = at, Text = text, TextMode = true });
            return MakeCharNode(text, at);
        }

        private Node TeXLogo(int at, bool latex)
        {
            var t = new TextNode { Pos = at, Font = TextFont.Upright, TextMode = textMode };
            if (latex)
            {
                t.Body.Add(new SymbolNode { Text = "L", TextMode = true });
                t.Body.Add(new SpaceNode { Amount = -0.36f, TextMode = true });
                t.Body.Add(new RaiseNode { Dy = 0.2f, Body = new List<Node> { new SizeNode { Scale = 0.7f, Body = new List<Node> { new SymbolNode { Text = "A", TextMode = true } } } } });
                t.Body.Add(new SpaceNode { Amount = -0.15f, TextMode = true });
            }
            t.Body.Add(new SymbolNode { Text = "T", TextMode = true });
            t.Body.Add(new SpaceNode { Amount = -0.1667f, TextMode = true });
            t.Body.Add(new RaiseNode { Dy = -0.2155f, Body = new List<Node> { new SymbolNode { Text = "E", TextMode = true } } });
            t.Body.Add(new SpaceNode { Amount = -0.125f, TextMode = true });
            t.Body.Add(new SymbolNode { Text = "X", TextMode = true });
            return Count(t);
        }

        // ------------------------------------------------------------------ text commands

        private Node ParseTextCommand(string name, int at)
        {
            if (forbidden.Contains(name))
                throw Error("ユーザー定義マクロや文書・ファイル命令（" + name + "）には対応していません。", at);
            var common = ParseCommonCommand(name, at, out bool handled);
            if (handled) return common;
            if (Symbols.TextAccents.TryGetValue(name, out var accent))
            {
                var arg = ParseTextArgument();
                string baseText = arg.Count == 0 ? " " : arg.Count == 1 && arg[0] is SymbolNode s ? s.Text : null;
                if (baseText == null) throw Error(name + " は 1 文字に付けてください。", at);
                return Count(new SymbolNode { Pos = at, Text = Compose(baseText, accent), TextMode = true });
            }
            if (Symbols.Text.TryGetValue(name, out var sym))
                return Count(new SymbolNode { Pos = at, Text = sym.Text, TextMode = true });
            switch (name)
            {
                case @"\\": case @"\newline": case @"\cr": return Count(new TextSpaceNode { Pos = at, TextMode = true });
                case @"\ensuremath": return Count(new MathInTextNode { Pos = at, Body = ParseMathArgument(), TextMode = true });
                case @"\$": return Count(new SymbolNode { Pos = at, Text = "$", TextMode = true });
            }
            if (Symbols.Math.TryGetValue(name, out var msym) && !msym.IsLetter && msym.Class == AtomClass.Ord)
                return Count(new SymbolNode { Pos = at, Text = msym.Text, TextMode = true });
            if (Symbols.Math.ContainsKey(name) || Symbols.BigOps.ContainsKey(name) || fontCommands.ContainsKey(name) || name == @"\frac")
                throw Error(name + " は数式用の命令です。テキストの中では $...$ で囲んでください。", at);
            throw Error("未対応のテキスト命令です: " + name, at);
        }

        // ------------------------------------------------------------------ environments

        private void ParseBracedRows(ArrayNode array)
        {
            SkipWhitespace();
            if (!TryReadChar('{')) throw Error("{ がありません。");
            var ctx = new ArrayContext { Array = array };
            arrays.Push(ctx);
            var row = new List<ArrayCell>();
            array.HLines.Add(ReadHLines(array, 0));
            while (true)
            {
                var cell = new ArrayCell();
                cell.Body.AddRange(ParseExpression(Stop.Align | Stop.Row | Stop.Brace));
                row.Add(cell);
                var t = PeekMath();
                if (t.Kind == Tok.Align) { ReadToken(true); continue; }
                if (t.Kind == Tok.Command && (t.Text == @"\\" || t.Text == @"\cr"))
                {
                    ReadToken(true);
                    FinishRow(array, ctx, row, 0);
                    row = new List<ArrayCell>();
                    array.HLines.Add(ReadHLines(array, array.Rows.Count));
                    continue;
                }
                if (t.Kind == Tok.End) { ReadToken(true); FinishRow(array, ctx, row, 0); break; }
                throw Error("} がありません。", t.Pos);
            }
            arrays.Pop();
            int last = array.Rows.Count - 1;
            if (last > 0 && array.Rows[last].Count == 1 && array.Rows[last][0].Body.Count == 0)
            {
                array.Rows.RemoveAt(last); array.Tags.RemoveAt(last); array.RowGaps.RemoveAt(last); array.HLines.RemoveAt(last + 1);
            }
            while (array.CLines.Count < array.HLines.Count) array.CLines.Add(new List<(int, int, HLineKind)>());
        }

        private Node ParseEnvironment(int at)
        {
            int nameAt = pos;
            var env = ReadEnvironmentName();
            bool star = env.EndsWith("*", StringComparison.Ordinal);
            var stem = star ? env.Substring(0, env.Length - 1) : env;
            ArrayNode array = null;
            string left = null, right = null;
            switch (stem)
            {
                case "matrix": case "pmatrix": case "bmatrix": case "Bmatrix": case "vmatrix": case "Vmatrix":
                    {
                        array = new ArrayNode { Env = env, ColumnMode = "matrix", OuterColSep = false, DefaultAlign = 'c' };
                        if (star) { var a = ReadRawOptional(); if (!string.IsNullOrEmpty(a)) array.DefaultAlign = a.Trim()[0]; }
                        (left, right) = MatrixDelims(stem);
                        break;
                    }
                case "smallmatrix": case "psmallmatrix": case "bsmallmatrix": case "Bsmallmatrix": case "vsmallmatrix": case "Vsmallmatrix":
                    {
                        array = new ArrayNode { Env = env, ColumnMode = "matrix", OuterColSep = false, ArrayStretch = 0.5f, CellStyle = 2, ColSep = 0.1389f, DefaultAlign = 'c' };
                        if (star) { var a = ReadRawOptional(); if (!string.IsNullOrEmpty(a)) array.DefaultAlign = a.Trim()[0]; }
                        (left, right) = MatrixDelims(stem.Replace("small", ""));
                        break;
                    }
                case "array": case "darray": case "subarray":
                    {
                        array = new ArrayNode { Env = env, ColumnMode = "array" };
                        var valign = ReadRawOptional();
                        if (!string.IsNullOrEmpty(valign)) array.VAlign = valign.Trim()[0];
                        int specAt = pos;
                        var spec = ReadRawGroup("列指定");
                        if (stem == "subarray") { array.ColSep = 0; array.OuterColSep = false; array.ArrayStretch = 0.5f; array.CellStyle = 2; }
                        if (stem == "darray") array.CellStyle = 0;
                        array.Columns = ParseColumnSpec(spec, array, specAt);
                        break;
                    }
                case "cases": case "dcases": case "rcases": case "drcases":
                    {
                        array = new ArrayNode { Env = env, ColumnMode = "cases", OuterColSep = false, ColSep = 0, ArrayStretch = 1.2f, DefaultAlign = 'l', CellStyle = stem.StartsWith("d", StringComparison.Ordinal) ? 0 : 1 };
                        if (star) array.TextColumns = new HashSet<int> { 1 };
                        if (stem.Contains("rcases")) { left = ""; right = "}"; } else { left = "{"; right = ""; }
                        break;
                    }
                case "aligned": case "split": case "align": case "flalign": case "eqnarray": case "alignat": case "alignedat": case "xalignat": case "xxalignat":
                    {
                        array = new ArrayNode { Env = env, ColumnMode = stem == "eqnarray" ? "eqnarray" : "align", AddJot = true, CellStyle = 0, OuterColSep = false, ColSep = stem == "eqnarray" ? 0.5f : 0 };
                        if (stem == "alignat" || stem == "alignedat" || stem.EndsWith("xalignat", StringComparison.Ordinal)) { ReadRawGroup("列数"); array.AlignGap = stem == "xxalignat" ? 2f : 0f; }
                        if (stem == "aligned" || stem == "alignedat") { var v = ReadRawOptional(); if (!string.IsNullOrEmpty(v)) array.VAlign = v.Trim()[0]; }
                        break;
                    }
                case "gather": case "gathered":
                    {
                        array = new ArrayNode { Env = env, ColumnMode = "gather", AddJot = true, CellStyle = 0, OuterColSep = false, ColSep = 0, DefaultAlign = 'c' };
                        if (stem == "gathered") { var v = ReadRawOptional(); if (!string.IsNullOrEmpty(v)) array.VAlign = v.Trim()[0]; }
                        break;
                    }
                case "multline":
                    array = new ArrayNode { Env = env, ColumnMode = "multline", AddJot = true, CellStyle = 0, OuterColSep = false, ColSep = 0 };
                    break;
                case "equation": case "displaymath": case "math":
                    {
                        var body = ParseExpression(Stop.End);
                        var t = ReadToken(true);
                        if (t.Kind != Tok.Command || t.Text != @"\end") throw Error(@"\end{" + env + "} がありません。", t.Pos);
                        var name = ReadEnvironmentName();
                        if (name != env) throw Error(@"\begin{" + env + @"} と \end{" + name + "} が対応していません。", t.Pos);
                        return Count(new StyleNode { Pos = at, Style = 0, Body = body });
                    }
                case "CD":
                    return ParseCD(at);
                default:
                    throw Error("未対応の環境です: " + env, nameAt);
            }
            array.Pos = at;
            var ctx = new ArrayContext { Array = array };
            arrays.Push(ctx);
            ParseArrayRows(array, null, env);
            arrays.Pop();
            Count(array);
            if (left != null || right != null)
                return Count(new LeftRightNode { Pos = at, Left = left ?? "", Right = right ?? "", Body = new List<Node> { array } });
            return array;
        }

        private static (string, string) MatrixDelims(string stem) => stem switch
        {
            "pmatrix" => ("(", ")"), "bmatrix" => ("[", "]"), "Bmatrix" => ("{", "}"), "vmatrix" => ("|", "|"), "Vmatrix" => ("‖", "‖"),
            _ => (null, null)
        };

        // amscd: rows of objects and arrows written as @>a>b>, @<<<, @VVV, @AAA, @=, @|, @.
        private Node ParseCD(int at)
        {
            var array = new ArrayNode { Env = "CD", Pos = at, ColumnMode = "CD", OuterColSep = false, ColSep = 0, DefaultAlign = 'c', CellStyle = 0 };
            var ctx = new ArrayContext { Array = array };
            arrays.Push(ctx);
            var inputRows = new List<List<(List<Node> obj, CDArrowNode arrow)>>();
            var current = new List<(List<Node>, CDArrowNode)>();
            while (true)
            {
                var obj = ParseExpressionUntil('@', Stop.Row | Stop.End);
                var t = PeekMath();
                if (t.Kind == Tok.Char && t.Text == "@")
                {
                    ReadToken(true);
                    if (pos >= src.Length) throw Error("@ の後に矢印の種類がありません。", t.Pos);
                    char kind = src[pos++];
                    var arrow = new CDArrowNode { Pos = t.Pos, Kind = kind };
                    switch (kind)
                    {
                        case '>': case '<': case 'V': case 'A':
                            arrow.Above = ParseExpressionUntil(kind, Stop.None);
                            ExpectChar(kind);
                            arrow.Below = ParseExpressionUntil(kind, Stop.None);
                            ExpectChar(kind);
                            break;
                        case '=': case '|': case '.': break;
                        default: throw Error("CD 環境の矢印の種類が不正です: @" + kind, t.Pos);
                    }
                    current.Add((obj, arrow));
                    continue;
                }
                current.Add((obj, null));
                if (t.Kind == Tok.Command && (t.Text == @"\\" || t.Text == @"\cr"))
                {
                    ReadToken(true);
                    inputRows.Add(current);
                    current = new List<(List<Node>, CDArrowNode)>();
                    continue;
                }
                if (t.Kind == Tok.Command && t.Text == @"\end")
                {
                    ReadToken(true);
                    var name = ReadEnvironmentName();
                    if (name != "CD") throw Error(@"\begin{CD} と \end{" + name + "} が対応していません。", t.Pos);
                    inputRows.Add(current);
                    break;
                }
                throw Error(@"\end{CD} がありません。", t.Pos);
            }
            arrays.Pop();
            // Build the grid: object rows alternate objects and horizontal arrows; arrow rows hold vertical arrows.
            int maxObjects = 1;
            foreach (var r in inputRows)
            {
                int n = 0;
                foreach (var e in r) if (e.arrow == null || e.arrow.Kind == '>' || e.arrow.Kind == '<' || e.arrow.Kind == '=' || e.arrow.Kind == '.') n++;
                maxObjects = Math.Max(maxObjects, n);
            }
            foreach (var r in inputRows)
            {
                bool vertical = false, hasObject = false;
                foreach (var e in r)
                {
                    if (e.obj.Count > 0) hasObject = true;
                    if (e.arrow != null && (e.arrow.Kind == 'V' || e.arrow.Kind == 'A' || e.arrow.Kind == '|')) vertical = true;
                }
                if (!hasObject && r.TrueForAll(e => e.arrow == null || e.arrow.Kind == '.') && r.Count > 1) vertical = true;
                var cells = new List<ArrayCell>();
                if (vertical)
                {
                    foreach (var e in r)
                    {
                        if (e.arrow == null) continue;
                        if (cells.Count > 0) cells.Add(new ArrayCell());
                        cells.Add(new ArrayCell { Body = new List<Node> { e.arrow } });
                    }
                }
                else
                {
                    for (int i = 0; i < r.Count; i++)
                    {
                        cells.Add(new ArrayCell { Body = r[i].obj });
                        if (r[i].arrow != null) cells.Add(new ArrayCell { Body = new List<Node> { r[i].arrow } });
                    }
                }
                if (r.Count == 1 && r[0].obj.Count == 0 && r[0].arrow == null && inputRows.Count > 1 && r == inputRows[inputRows.Count - 1]) continue;
                array.Rows.Add(cells);
                array.RowGaps.Add(0);
                array.Tags.Add(null);
            }
            for (int i = 0; i <= array.Rows.Count; i++) { array.HLines.Add(new List<HLineKind>()); array.CLines.Add(new List<(int, int, HLineKind)>()); }
            return Count(array);
        }

        private char cdStop;

        private List<Node> ParseExpressionUntil(char stopChar, Stop extra)
        {
            var saved = cdStop;
            cdStop = stopChar;
            try { return ParseExpression(extra | StopAtChar); }
            finally { cdStop = saved; }
        }

        private void ExpectChar(char c)
        {
            var t = ReadToken(true);
            if (t.Kind != Tok.Char || t.Text != c.ToString()) throw Error(c + " がありません。", t.Pos);
        }
    }
}
