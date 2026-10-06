using System;
using System.Collections.Generic;
using System.Globalization;

namespace MathMesher.Editor.Engine
{
    /// <summary>Lays out a node tree following TeX's math typesetting rules with OpenType MATH metrics.</summary>
    internal sealed partial class Typesetter
    {
        private sealed class Atom
        {
            public Box Box;
            public AtomClass Class;
            public bool Glue;          // explicit space: invisible to inter-atom spacing
            public Options O;
            public Node Source;
        }

        private readonly FontSet fonts;
        private readonly MathConstantsEm C;
        private int recursion;

        public Typesetter(FontSet fonts)
        {
            this.fonts = fonts;
            C = fonts.Constants;
        }

        public Options RootOptions(bool display, Rgba color) => new Options
        {
            Fonts = fonts, C = C, Style = display ? MathStyle.Display : MathStyle.Text, Color = color,
        };

        public Box Layout(List<Node> nodes, List<Node> tag, Options o)
        {
            var body = BuildList(nodes, o);
            if (tag == null) return body;
            var tagBox = BuildText(tag, o.WithText(true));
            var row = Box.HBox(new[] { body, Box.Kern(2f * o.Scale), tagBox });
            return row;
        }

        // ------------------------------------------------------------------ lists and spacing

        private static readonly int[,] spacing =
        {
            //          Ord  Op  Bin  Rel Open Close Punct Inner
            /* Ord   */ { 0,  1,  -2,  -3,  0,   0,    0,   -1 },
            /* Op    */ { 1,  1,   0,  -3,  0,   0,    0,   -1 },
            /* Bin   */ { -2, -2,  0,   0, -2,   0,    0,   -2 },
            /* Rel   */ { -3, -3,  0,   0, -3,   0,    0,   -3 },
            /* Open  */ { 0,  0,   0,   0,  0,   0,    0,    0 },
            /* Close */ { 0,  1,  -2,  -3,  0,   0,    0,   -1 },
            /* Punct */ { -1, -1,  0,  -1, -1,  -1,   -1,   -1 },
            /* Inner */ { -1, 1,  -2,  -3, -1,   0,   -1,   -1 },
        };

        public Box BuildList(List<Node> nodes, Options o, bool openBefore = false)
        {
            if (o.TextMode) return BuildText(nodes, o);
            var atoms = new List<Atom>();
            BuildAtoms(nodes, o, atoms);
            return Assemble(atoms, o, openBefore);
        }

        private void Enter()
        {
            if (++recursion > 400) throw new TeXException("数式の入れ子が深すぎます。");
        }

        private void BuildAtoms(List<Node> nodes, Options o, List<Atom> atoms)
        {
            if (nodes == null) return;
            Enter();
            foreach (var node in nodes)
            {
                switch (node)
                {
                    case null: break;
                    case StyleNode s:
                        BuildAtoms(s.Body, o.WithStyle(new MathStyle(s.Style * 2)), atoms);
                        break;
                    case SizeNode z:
                        {
                            var so = o.Clone(); so.SizeScale = z.Scale;
                            if (so.TextMode) atoms.Add(new Atom { Box = BuildText(z.Body, so), Class = AtomClass.Ord, O = o, Source = z });
                            else BuildAtoms(z.Body, so, atoms);
                            break;
                        }
                    case ColorNode c:
                        if (o.TextMode) atoms.Add(new Atom { Box = BuildText(c.Body, o.WithColor(c.Color)), Class = AtomClass.Ord, O = o, Source = c });
                        else BuildAtoms(c.Body, o.WithColor(c.Color), atoms);
                        break;
                    case FontNode f when !f.PoorMansBold:
                        {
                            var fo = o.Clone(); fo.Variant = f.Variant; fo.BoldSymbol = f.BoldSymbol;
                            BuildAtoms(f.Body, fo, atoms);
                            break;
                        }
                    case GroupNode g when !g.IsOrd:
                        BuildAtoms(g.Body, o, atoms);
                        break;
                    case MathChoiceNode m:
                        BuildAtoms(o.Style.Size switch { 0 => m.Display, 1 => m.Text, 2 => m.Script, _ => m.ScriptScript }, o, atoms);
                        break;
                    case SpaceNode sp:
                        atoms.Add(new Atom { Box = Box.Kern(SpaceWidth(sp, o)), Glue = true, O = o, Source = sp });
                        break;
                    default:
                        atoms.Add(MakeAtom(node, o));
                        break;
                }
            }
            recursion--;
        }

        private float SpaceWidth(SpaceNode sp, Options o) => sp.Mu ? sp.Amount * o.Mu : sp.Amount * o.Scale;

        private Box Assemble(List<Atom> atoms, Options o, bool openBefore)
        {
            // TeX rules 5 and 6: binary operators in non-binary positions become ordinary
            Atom prev = null;
            if (openBefore) prev = new Atom { Class = AtomClass.Open };
            foreach (var a in atoms)
            {
                if (a.Glue) continue;
                if (a.Class == AtomClass.Bin && (prev == null || prev.Class == AtomClass.Bin || prev.Class == AtomClass.Op ||
                    prev.Class == AtomClass.Rel || prev.Class == AtomClass.Open || prev.Class == AtomClass.Punct))
                    a.Class = AtomClass.Ord;
                if (prev != null && prev.Class == AtomClass.Bin && (a.Class == AtomClass.Rel || a.Class == AtomClass.Close || a.Class == AtomClass.Punct))
                    prev.Class = AtomClass.Ord;
                prev = a;
            }
            if (prev != null && prev.Class == AtomClass.Bin) prev.Class = AtomClass.Ord;

            var boxes = new List<Box>(atoms.Count * 2);
            Atom last = openBefore ? new Atom { Class = AtomClass.Open } : null;
            foreach (var a in atoms)
            {
                if (!a.Glue)
                {
                    if (last != null)
                    {
                        int code = spacing[(int)last.Class, (int)a.Class];
                        if (code != 0 && !(code < 0 && a.O.Style.IsScript))
                        {
                            int mu = Math.Abs(code) switch { 1 => 3, 2 => 4, _ => 5 };
                            boxes.Add(Box.Kern(mu * a.O.Mu));
                        }
                    }
                    last = a;
                }
                boxes.Add(a.Box);
            }
            var box = Box.HBox(boxes);
            if (atoms.Count == 1 && !atoms[0].Glue)
            {
                box.IsCharBox = atoms[0].Box.IsCharBox;
                box.Italic = atoms[0].Box.Italic;
            }
            return box;
        }

        /// <summary>The class an expression has when it stands alone (used by \overset etc.).</summary>
        private Atom MakeAtom(Node node, Options o)
        {
            Enter();
            try
            {
                switch (node)
                {
                    case SymbolNode s: return new Atom { Box = MakeSymbol(s, o), Class = s.Class, O = o, Source = s };
                    case GroupNode g: return new Atom { Box = BuildList(g.Body, o), Class = AtomClass.Ord, O = o, Source = g };
                    case SupSubNode ss: return MakeSupSub(ss, o);
                    case FracNode f: return new Atom { Box = MakeFraction(f, o), Class = AtomClass.Inner, O = o, Source = f };
                    case SqrtNode r: return new Atom { Box = MakeRadical(r, o), Class = AtomClass.Ord, O = o, Source = r };
                    case OpNode op: return new Atom { Box = MakeOperator(op, o, out _), Class = AtomClass.Op, O = o, Source = op };
                    case AccentNode a: return new Atom { Box = MakeAccent(a, o), Class = AtomClass.Ord, O = o, Source = a };
                    case LineNode l: return new Atom { Box = MakeLine(l, o), Class = AtomClass.Ord, O = o, Source = l };
                    case HorizBraceNode hb: return new Atom { Box = MakeHorizBrace(hb, o), Class = AtomClass.Op, O = o, Source = hb };
                    case StackNode st: return new Atom { Box = MakeStack(st.Base, st.Over, st.Under, o, false), Class = st.Class, O = o, Source = st };
                    case LeftRightNode lr: return new Atom { Box = MakeLeftRight(lr, o), Class = AtomClass.Inner, O = o, Source = lr };
                    case MiddleNode m: return new Atom { Box = MakeDelimiter(m.Delim, middleHeight, o, true), Class = AtomClass.Ord, O = o, Source = m };
                    case DelimSizingNode d: return new Atom { Box = MakeBigDelimiter(d, o), Class = d.Class, O = o, Source = d };
                    case TextNode t:
                        {
                            var to = o.Clone(); to.TextMode = true; to.TextFont = t.Font == TextFont.Normal ? (o.TextMode ? o.TextFont : TextFont.Normal) : t.Font;
                            return new Atom { Box = BuildText(t.Body, to), Class = AtomClass.Ord, O = o, Source = t };
                        }
                    case MathInTextNode mt:
                        {
                            var mo = o.Clone(); mo.TextMode = false;
                            if (!mo.Style.IsScript) mo.Style = MathStyle.Text;
                            return new Atom { Box = BuildList(mt.Body, mo), Class = AtomClass.Ord, O = o, Source = mt };
                        }
                    case PhantomNode p:
                        {
                            var inner = BuildList(p.Body, o);
                            return new Atom { Box = new PhantomBox(inner, p.KeepWidth, p.KeepHeight), Class = AtomClass.Ord, O = o, Source = p };
                        }
                    case SmashNode sm:
                        {
                            var inner = BuildList(sm.Body, o);
                            var box = new Box(inner.Width, sm.Top ? 0 : inner.Height, sm.Bottom ? 0 : inner.Depth);
                            box.Add(inner, 0, 0);
                            return new Atom { Box = box, Class = AtomClass.Ord, O = o, Source = sm };
                        }
                    case LapNode lap:
                        {
                            var inner = BuildList(lap.Body, o);
                            var box = new Box(0, inner.Height, inner.Depth);
                            box.Add(inner, lap.Align < 0 ? -inner.Width : lap.Align == 0 ? -inner.Width / 2 : 0, 0);
                            return new Atom { Box = box, Class = AtomClass.Ord, O = o, Source = lap };
                        }
                    case RaiseNode rn:
                        {
                            var inner = BuildList(rn.Body, o);
                            float dy = rn.Dy * o.Scale;
                            var box = new Box(inner.Width, 0, 0);
                            box.Add(inner, 0, dy);
                            box.Fit();
                            return new Atom { Box = box, Class = AtomClass.Ord, O = o, Source = rn };
                        }
                    case RuleNode ru:
                        {
                            float w = ru.Width * o.Scale, h = ru.Height * o.Scale, sh = ru.Shift * o.Scale;
                            var box = new Box(w, Math.Max(0, sh + h), Math.Max(0, -sh));
                            if (w > 0 && h > 0) box.Add(new RuleBox(w, h, 0, o.Color), 0, sh);
                            return new Atom { Box = box, Class = AtomClass.Ord, O = o, Source = ru };
                        }
                    case VCenterNode vc:
                        {
                            var inner = BuildList(vc.Body, o);
                            float shift = C.AxisHeight * o.Scale - (inner.Height - inner.Depth) / 2;
                            var box = new Box(inner.Width, 0, 0);
                            box.Add(inner, 0, shift);
                            box.Fit();
                            return new Atom { Box = box, Class = AtomClass.Ord, O = o, Source = vc };
                        }
                    case EncloseNode en: return new Atom { Box = MakeEnclose(en, o), Class = AtomClass.Ord, O = o, Source = en };
                    case ArrayNode ar: return new Atom { Box = MakeArray(ar, o), Class = AtomClass.Ord, O = o, Source = ar };
                    case XArrowNode xa: return new Atom { Box = MakeXArrow(xa, o), Class = AtomClass.Rel, O = o, Source = xa };
                    case ClassNode cn:
                        {
                            var inner = BuildList(cn.Body, o);
                            return new Atom { Box = inner, Class = cn.Class, O = o, Source = cn };
                        }
                    case NotNode nn:
                        {
                            var baseAtom = nn.Base == null ? new Atom { Box = new Box(), Class = AtomClass.Rel } : MakeAtom(nn.Base, o);
                            return new Atom { Box = MakeNegation(baseAtom.Box, o), Class = baseAtom.Class, O = o, Source = nn };
                        }
                    case SideSetNode sd: return new Atom { Box = MakeSideSet(sd, o, null, null, out _, out _), Class = AtomClass.Op, O = o, Source = sd };
                    case FontNode pmb when pmb.PoorMansBold: return new Atom { Box = PoorMansBold(BuildList(pmb.Body, o), o), Class = AtomClass.Ord, O = o, Source = pmb };
                    case FontNode _:
                    case ColorNode _:
                    case StyleNode _:
                    case SizeNode _:
                    case MathChoiceNode _:
                        {
                            // transparent wrappers used as a script base (\mathbb{R}^n, \textcolor{red}{x}^2):
                            // one atom that keeps the class and character box of a single inner atom
                            var inner = new List<Atom>();
                            BuildAtoms(new List<Node> { node }, o, inner);
                            if (inner.Count == 1 && !inner[0].Glue) return new Atom { Box = inner[0].Box, Class = inner[0].Class, O = o, Source = node };
                            return new Atom { Box = Assemble(inner, o, false), Class = AtomClass.Ord, O = o, Source = node };
                        }
                    case TextSpaceNode _: return new Atom { Box = Box.Kern(TextSpace(o)), Glue = true, O = o };
                    case CDArrowNode cd: return new Atom { Box = MakeCDArrow(cd, o, 0, 0), Class = AtomClass.Ord, O = o, Source = cd };
                    case HDotsForNode hd: return new Atom { Box = new Box(), Class = AtomClass.Ord, O = o, Source = hd };
                    case TagNode tg: return new Atom { Box = Layout(new List<Node> { tg.Body }, tg.Tag, o), Class = AtomClass.Ord, O = o, Source = tg };
                    default:
                        throw new TeXException("内部エラー: 未対応のノード " + node.GetType().Name, node.Pos);
                }
            }
            finally { recursion--; }
        }

        // ------------------------------------------------------------------ glyphs

        private static string Describe(int cp) => char.ConvertFromUtf32(cp) + $" (U+{cp:X4})";

        private TeXException Missing(int cp, Node at = null) =>
            new TeXException($"文字 {Describe(cp)} のグリフが同梱フォントにありません。日本語などを使う場合は Mesh 設定の「代替フォント」に対応フォントを指定してください。", at?.Pos ?? -1);

        public GlyphId FindGlyph(int cp, FontPref pref = FontPref.Default, bool text = false, Node at = null)
        {
            var g = fonts.Find(cp, pref, text);
            if (!g.IsValid) throw Missing(cp, at);
            return g;
        }

        private GlyphBox GlyphOf(int cp, Options o, FontPref pref = FontPref.Default, bool text = false, Node at = null) =>
            new GlyphBox(FindGlyph(cp, pref, text, at), o.Scale, o.Color);

        private static List<int> Codepoints(string text)
        {
            var list = new List<int>(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                int cp = char.ConvertToUtf32(text, i);
                if (char.IsHighSurrogate(text[i])) i++;
                list.Add(cp);
            }
            return list;
        }

        private static bool IsCombining(int cp) =>
            CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(cp), 0) == UnicodeCategory.NonSpacingMark;

        private MathVariant EffectiveVariant(int cp, Options o, bool isLetter, bool isDigit)
        {
            if (o.InOperatorName && (isLetter || isDigit)) return o.Variant == MathVariant.Default ? MathVariant.Upright : o.Variant;
            var v = o.Variant;
            if (o.BoldSymbol)
            {
                bool upperGreek = cp >= 0x391 && cp <= 0x3A9;
                if (isDigit || upperGreek) return MathVariant.Bold;
                return MathVariant.BoldItalic;
            }
            if (v == MathVariant.Default) return isLetter || isDigit ? MathVariant.Normal : MathVariant.Default;
            if (v == MathVariant.Upright || !(isLetter || isDigit)) return v == MathVariant.Upright ? MathVariant.Upright : MathVariant.Default;
            return v;
        }

        private static FontPref PrefFor(MathVariant v) =>
            v == MathVariant.Script || v == MathVariant.BoldScript ? FontPref.Stix :
            v == MathVariant.DoubleStruck ? FontPref.Blackboard : FontPref.Default;

        private Box MakeSymbol(SymbolNode s, Options o)
        {
            if (s.TextMode || o.TextMode) return TextRun(s.Text, o, s);
            var cps = Codepoints(s.Text);
            var boxes = new List<Box>();
            bool fakeBold = false;
            GlyphBox lastGlyph = null;
            for (int i = 0; i < cps.Count; i++)
            {
                int cp = cps[i];
                if (IsCombining(cp) && lastGlyph != null)
                {
                    boxes.RemoveAt(boxes.Count - 1);
                    boxes.Add(Overlay(lastGlyph, cp, o));
                    lastGlyph = null;
                    continue;
                }
                bool letter = s.IsLetter || Symbols.IsLatinLetter(cp) && s.IsLetter;
                var variant = EffectiveVariant(cp, o, s.IsLetter, s.IsDigit);
                int mapped = Alphabets.Map(cp, variant);
                GlyphId g;
                if (o.BoldSymbol && !s.IsLetter && !s.IsDigit)
                {
                    int bold = Alphabets.Map(cp, MathVariant.Bold);
                    g = bold != cp ? fonts.Find(bold) : default;
                    if (!g.IsValid) { g = FindGlyph(cp, FontPref.Default, false, s); fakeBold = true; }
                }
                else
                {
                    g = fonts.Find(mapped, PrefFor(variant));
                    if (!g.IsValid && mapped != cp) g = fonts.Find(cp);
                    if (!g.IsValid) throw Missing(cp, s);
                }
                var gb = new GlyphBox(g, o.Scale, o.Color);
                lastGlyph = gb;
                boxes.Add(gb);
                _ = letter;
            }
            Box result;
            if (boxes.Count == 1 && lastGlyph != null)
            {
                result = lastGlyph;
                if (lastGlyph.Italic > 0)
                {
                    // math italic letters carry their italic correction in the width (TeX rule 17)
                    var withItalic = new Box(lastGlyph.Width + lastGlyph.Italic, lastGlyph.Height, lastGlyph.Depth) { Italic = lastGlyph.Italic, IsCharBox = true };
                    withItalic.Add(lastGlyph, 0, 0);
                    result = withItalic;
                }
            }
            else result = Box.HBox(boxes);
            if (fakeBold) result = PoorMansBold(result, o);
            return result;
        }

        /// <summary>Overlays a combining mark centered on (or above) a base glyph.</summary>
        private Box Overlay(GlyphBox baseGlyph, int mark, Options o)
        {
            var g = fonts.Find(mark, FontPref.Default, o.TextMode);
            if (!g.IsValid) throw Missing(mark);
            var mb = new GlyphBox(g, o.Scale, o.Color);
            var m = g.Metrics;
            var box = new Box(baseGlyph.Width, 0, 0);
            box.Add(baseGlyph, 0, 0);
            float markCenter = (m.XMin + m.XMax) / 2 * o.Scale;
            float x = baseGlyph.Width / 2 - markCenter;
            float y = 0;
            if (mark == 0x0338 || mark == 0x20D2) { }
            else if (m.YMin * o.Scale > 0.2f * o.Scale) y = Math.Max(0, baseGlyph.Height - C.AccentBaseHeight * o.Scale);
            box.Add(mb, x, y);
            box.Fit(false);
            return box;
        }

        private Box PoorMansBold(Box inner, Options o)
        {
            float d = 0.025f * o.Scale;
            var box = new Box(inner.Width + d, inner.Height + d * 0.6f, inner.Depth);
            box.Add(inner, 0, 0);
            box.Add(inner, d, 0);
            box.Add(inner, d * 0.5f, d * 0.6f);
            box.Italic = inner.Italic;
            return box;
        }

        private Box MakeNegation(Box inner, Options o)
        {
            var g = fonts.Find(0x0338);
            var box = new Box(inner.Width, inner.Height, inner.Depth);
            box.Add(inner, 0, 0);
            if (g.IsValid)
            {
                var slash = new GlyphBox(g, o.Scale, o.Color);
                var m = g.Metrics;
                float center = (m.XMin + m.XMax) / 2 * o.Scale;
                float yCenter = (m.YMin + m.YMax) / 2 * o.Scale;
                float targetY = (inner.Height - inner.Depth) / 2;
                if (inner.Height + inner.Depth < 0.01f) targetY = C.AxisHeight * o.Scale;
                box.Add(slash, inner.Width / 2 - center, targetY - yCenter);
            }
            else
            {
                float t = 0.05f * o.Scale;
                var shape = Polygon.Line(inner.Width * 0.2f, -inner.Depth - 0.1f * o.Scale, inner.Width * 0.8f, inner.Height + 0.1f * o.Scale, t);
                box.Add(new PolygonBox(shape, o.Color, 0, 0, 0), 0, 0);
            }
            box.Fit(false);
            return box;
        }

        // ------------------------------------------------------------------ text

        private float TextSpace(Options o)
        {
            if (o.TextFont == TextFont.Mono) return 0.525f * o.Scale;
            var g = fonts.Math.Lookup(' ');
            return (g != 0 ? fonts.Math.Metrics(g).Advance : 0.333f) * o.Scale;
        }

        private static MathVariant TextVariant(TextFont f) => f switch
        {
            TextFont.Italic => MathVariant.Italic, TextFont.Slanted => MathVariant.Italic, TextFont.Bold => MathVariant.Bold,
            TextFont.BoldItalic => MathVariant.BoldItalic, TextFont.Sans => MathVariant.SansSerif, TextFont.SansBold => MathVariant.SansBold,
            TextFont.SansItalic => MathVariant.SansItalic, TextFont.Mono => MathVariant.Monospace, _ => MathVariant.Upright,
        };

        private Box BuildText(List<Node> nodes, Options o)
        {
            if (nodes == null) return new Box();
            Enter();
            var boxes = new List<Box>();
            foreach (var n in nodes)
            {
                switch (n)
                {
                    case SymbolNode s: boxes.Add(TextRun(s.Text, o, s)); break;
                    case TextSpaceNode _: boxes.Add(Box.Kern(TextSpace(o))); break;
                    case SpaceNode sp: boxes.Add(Box.Kern(SpaceWidth(sp, o))); break;
                    case GroupNode g: boxes.Add(BuildText(g.Body, o)); break;
                    case TextNode t:
                        {
                            var to = o.Clone(); to.TextMode = true; if (t.Font != TextFont.Normal) to.TextFont = t.Font;
                            boxes.Add(BuildText(t.Body, to));
                            break;
                        }
                    case ColorNode c: boxes.Add(BuildText(c.Body, o.WithColor(c.Color))); break;
                    case SizeNode z: { var so = o.Clone(); so.SizeScale = z.Scale; boxes.Add(BuildText(z.Body, so)); break; }
                    case MathInTextNode m:
                        {
                            var mo = o.Clone(); mo.TextMode = false;
                            if (!mo.Style.IsScript) mo.Style = MathStyle.Text;
                            boxes.Add(BuildList(m.Body, mo));
                            break;
                        }
                    default:
                        {
                            var mo = o.Clone(); mo.TextMode = false;
                            boxes.Add(MakeAtom(n, o).Box);
                            break;
                        }
                }
            }
            recursion--;
            return Box.HBox(boxes);
        }

        private Box TextRun(string text, Options o, Node at)
        {
            var boxes = new List<Box>();
            GlyphBox last = null;
            var font = o.TextMode ? o.TextFont : TextFont.Upright;
            var variant = TextVariant(font);
            foreach (var cp in Codepoints(text))
            {
                if (cp == ' ') { boxes.Add(Box.Kern(TextSpace(o))); last = null; continue; }
                if (IsCombining(cp) && last != null)
                {
                    boxes.RemoveAt(boxes.Count - 1);
                    boxes.Add(Overlay(last, cp, o));
                    last = null;
                    continue;
                }
                int c = cp;
                float scale = o.Scale;
                if (font == TextFont.SmallCaps && char.IsLower((char)Math.Min(cp, 0xFFFF)) && cp <= 0xFFFF)
                {
                    c = char.ToUpperInvariant((char)cp);
                    scale *= 0.8f;
                }
                int mapped = Alphabets.Map(c, variant);
                var g = fonts.Find(mapped, FontPref.Default, true);
                if (!g.IsValid && mapped != c) g = fonts.Find(c, FontPref.Default, true);
                if (!g.IsValid) throw Missing(c, at);
                last = new GlyphBox(g, scale, o.Color);
                boxes.Add(last);
            }
            return Box.HBox(boxes);
        }

        // ------------------------------------------------------------------ stretchy glyph construction

        /// <summary>Builds a glyph at least <paramref name="target"/> long along the stretch axis (em at scale 1 * o.Scale).</summary>
        private Box Stretch(GlyphId g, float target, bool horizontal, Options o, out bool exact)
        {
            exact = false;
            float scale = o.Scale;
            var variants = fonts.Variants(g, horizontal);
            (GlyphId glyph, float size) best = variants[0];
            foreach (var v in variants)
            {
                best = v;
                if (v.size * scale >= target) return new GlyphBox(v.glyph, scale, o.Color);
            }
            var parts = fonts.Assembly(g, horizontal, out _);
            if (parts != null && parts.Count > 0)
            {
                exact = true;
                return Assemble(parts, target / scale, horizontal, g, o);
            }
            // fallback: scale the largest variant
            var gb = new GlyphBox(best.glyph, scale, o.Color);
            float natural = horizontal ? gb.Width : gb.Height + gb.Depth;
            if (natural > 1e-4f && target > natural)
            {
                float k = target / natural;
                if (horizontal) { gb.ScaleX = k; gb.Width *= k; }
                else { gb.ScaleY = k; gb.Height *= k; gb.Depth *= k; }
            }
            return gb;
        }

        private Box Assemble(List<GlyphPart> parts, float target, bool horizontal, GlyphId baseGlyph, Options o)
        {
            float minOverlap = fonts.MinConnectorOverlap(baseGlyph);
            int extenders = 0;
            foreach (var p in parts) if (p.IsExtender) extenders++;
            for (int repeat = 0; repeat < 2000; repeat++)
            {
                var seq = new List<GlyphPart>();
                foreach (var p in parts)
                {
                    if (p.IsExtender) for (int i = 0; i < repeat; i++) seq.Add(p);
                    else seq.Add(p);
                }
                if (seq.Count == 0) { if (extenders == 0) break; continue; }
                float full = 0;
                foreach (var p in seq) full += p.FullAdvance;
                // Overlap per joint is between minOverlap and min(prev.End, next.Start)
                float maxTotal = full - (seq.Count - 1) * minOverlap;
                float minTotal = full;
                var maxOverlaps = new float[Math.Max(0, seq.Count - 1)];
                for (int i = 1; i < seq.Count; i++)
                {
                    maxOverlaps[i - 1] = Math.Max(minOverlap, Math.Min(seq[i - 1].EndConnector, seq[i].StartConnector));
                    minTotal -= maxOverlaps[i - 1];
                }
                if (maxTotal < target && extenders > 0 && repeat < 1999) continue;
                // choose a uniform shrink t in [0,1] between max and min overlap to hit the target
                float total = Math.Max(minTotal, Math.Min(maxTotal, target));
                float t = maxTotal - minTotal > 1e-6f ? (maxTotal - total) / (maxTotal - minTotal) : 0;
                var box = new Box();
                float pos = 0;
                for (int i = 0; i < seq.Count; i++)
                {
                    if (i > 0) pos -= minOverlap + t * (maxOverlaps[i - 1] - minOverlap);
                    var gb = new GlyphBox(seq[i].Glyph, o.Scale, o.Color);
                    // horizontal parts are placed by origin (advance), vertical parts by their ink bottom (as XeTeX does)
                    if (horizontal) box.Add(gb, pos * o.Scale, 0);
                    else box.Add(gb, 0, (pos - seq[i].Glyph.Metrics.YMin) * o.Scale);
                    pos += seq[i].FullAdvance;
                }
                if (horizontal)
                {
                    box.Fit();
                    box.Width = pos * o.Scale;
                }
                else
                {
                    // vertical parts are stacked from the bottom; put the bottom at the baseline
                    float w = 0;
                    foreach (var (b, _, _) in box.Children) w = Math.Max(w, b.Width);
                    box.Width = w;
                    box.Fit(false);
                }
                return box;
            }
            return new GlyphBox(baseGlyph, o.Scale, o.Color);
        }
    }
}
