using System;
using System.Collections.Generic;

namespace MathMesher.Editor.Engine
{
    internal sealed partial class Typesetter
    {
        private float middleHeight;

        /// <summary>Actual ink extent of a box (Height/Depth are clamped at the baseline, ink is not).</summary>
        private static void Ink(Box b, float y, ref float min, ref float max)
        {
            switch (b)
            {
                case GlyphBox g:
                    {
                        var m = g.Glyph.Metrics;
                        if (m.Empty) return;
                        float sy = g.Scale * g.ScaleY;
                        min = Math.Min(min, y + m.YMin * sy);
                        max = Math.Max(max, y + m.YMax * sy);
                        return;
                    }
                case RuleBox r:
                    min = Math.Min(min, y - r.Depth);
                    max = Math.Max(max, y + r.Height);
                    return;
                case PhantomBox _: return;
            }
            if (b.Children == null) return;
            foreach (var (c, _, cy) in b.Children) Ink(c, y + cy, ref min, ref max);
        }

        private static (float min, float max) InkOf(Box b)
        {
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            Ink(b, 0, ref min, ref max);
            if (float.IsInfinity(min)) return (-b.Depth, b.Height);
            return (min, max);
        }

        // ------------------------------------------------------------------ fractions

        private Box MakeFraction(FracNode f, Options o)
        {
            var fo = f.Style.HasValue ? o.WithStyle(new MathStyle(f.Style.Value * 2)) : o;
            if (f.Continued) fo = o.WithStyle(MathStyle.Display);
            if (f.Skewed) return MakeSkewed(f, fo);
            var numO = fo.WithStyle(f.Continued ? MathStyle.Display : fo.Style.FracNum());
            var denO = fo.WithStyle(f.Continued ? MathStyle.Display.Cramp() : fo.Style.FracDen());
            var num = BuildList(f.Num, numO);
            var den = BuildList(f.Den, denO);
            float s = fo.Scale;
            if (f.Continued)
            {
                // \cfrac puts a strut in both parts
                num = WithMinimum(num, 0.85f * s, 0.35f * s);
                den = WithMinimum(den, 0.85f * s, 0.35f * s);
            }
            bool display = fo.Style.IsDisplay;
            float t = f.HasBar ? (f.Thickness.HasValue ? f.Thickness.Value * fo.SizeScale : C.FractionRuleThickness * s) : 0;
            float axis = C.AxisHeight * s;
            float u, v;
            if (t > 0)
            {
                u = (display ? C.FractionNumeratorDisplayStyleShiftUp : C.FractionNumeratorShiftUp) * s;
                v = (display ? C.FractionDenominatorDisplayStyleShiftDown : C.FractionDenominatorShiftDown) * s;
                float numGap = (display ? C.FractionNumDisplayStyleGapMin : C.FractionNumeratorGapMin) * s;
                float denGap = (display ? C.FractionDenomDisplayStyleGapMin : C.FractionDenominatorGapMin) * s;
                float a = (u - num.Depth) - (axis + t / 2);
                if (a < numGap) u += numGap - a;
                float b = (axis - t / 2) - (den.Height - v);
                if (b < denGap) v += denGap - b;
            }
            else
            {
                u = (display ? C.StackTopDisplayStyleShiftUp : C.StackTopShiftUp) * s;
                v = (display ? C.StackBottomDisplayStyleShiftDown : C.StackBottomShiftDown) * s;
                float gap = (u - num.Depth) - (den.Height - v);
                float min = (display ? C.StackDisplayStyleGapMin : C.StackGapMin) * s;
                if (gap < min) { u += (min - gap) / 2; v += (min - gap) / 2; }
            }
            float w = Math.Max(num.Width, den.Width);
            var frac = new Box(w, 0, 0);
            float numX = f.Continued && f.ContinuedAlign == 'l' ? 0 : f.Continued && f.ContinuedAlign == 'r' ? w - num.Width : (w - num.Width) / 2;
            frac.Add(num, numX, u);
            frac.Add(den, (w - den.Width) / 2, -v);
            if (t > 0) frac.Add(new RuleBox(w, t / 2, t / 2, fo.Color), 0, axis);
            frac.Fit(false);

            float nullDelim = 0.12f * s;
            var parts = new List<Box>();
            float delimSize = (display ? 2.39f : 1.01f) * s;
            parts.Add(string.IsNullOrEmpty(f.LeftDelim) ? Box.Kern(nullDelim) : MakeDelimiter(f.LeftDelim, delimSize, fo, true));
            parts.Add(frac);
            parts.Add(string.IsNullOrEmpty(f.RightDelim) ? Box.Kern(nullDelim) : MakeDelimiter(f.RightDelim, delimSize, fo, true));
            return Box.HBox(parts);
        }

        private static Box WithMinimum(Box b, float height, float depth)
        {
            if (b.Height >= height && b.Depth >= depth) return b;
            var box = new Box(b.Width, Math.Max(b.Height, height), Math.Max(b.Depth, depth));
            box.Add(b, 0, 0);
            return box;
        }

        private Box MakeSkewed(FracNode f, Options o)
        {
            var so = o.WithStyle(o.Style.Sup());
            var num = BuildList(f.Num, so);
            var den = BuildList(f.Den, o.WithStyle(o.Style.Sub()));
            float s = o.Scale;
            var slash = GlyphOf(0x2044, o);
            var (smin, smax) = InkOf(slash);
            float gap = Math.Max(0.05f * s, C.SkewedFractionHorizontalGap * s * 0.25f);
            var box = new Box();
            box.Add(num, 0, smax - num.Height);
            float x = num.Width - gap;
            box.Add(slash, x, 0);
            x += slash.Width - gap;
            box.Add(den, x, smin + den.Depth);
            box.Width = x + den.Width;
            box.Fit(false);
            return box;
        }

        // ------------------------------------------------------------------ radicals

        private Box MakeRadical(SqrtNode r, Options o)
        {
            float s = o.Scale;
            var inner = BuildList(r.Body, o.WithStyle(o.Style.Cramp()));
            float gap = (o.Style.IsDisplay ? C.RadicalDisplayStyleVerticalGap : C.RadicalVerticalGap) * s;
            float t = C.RadicalRuleThickness * s;
            float target = inner.Height + inner.Depth + gap + t;
            var sign = Stretch(FindGlyph(0x221A), target, false, o, out _);
            var (inkMin, inkMax) = InkOf(sign);
            float signTotal = inkMax - inkMin;
            float delta = signTotal - target;
            if (delta > 0) gap += delta / 2;
            float top = inner.Height + gap + t;
            float signY = top - inkMax;
            var box = new Box();
            box.Add(sign, 0, signY);
            box.Add(inner, sign.Width, 0);
            box.Add(new RuleBox(inner.Width, t, 0, o.Color), sign.Width, top - t);
            box.Width = sign.Width + inner.Width;
            box.Fit(false);
            box.Height = Math.Max(box.Height, top + C.RadicalExtraAscender * s);
            if (r.Index == null || r.Index.Count == 0) return box;

            var degree = BuildList(r.Index, o.WithStyle(MathStyle.ScriptScript));
            float before = C.RadicalKernBeforeDegree * s;
            float after = C.RadicalKernAfterDegree * s;
            float bottom = signY + inkMin;
            float raise = C.RadicalDegreeBottomRaisePercent * signTotal;
            float degX = before;
            float radX = Math.Max(0, before + degree.Width + after);
            var result = new Box();
            result.Add(degree, degX, bottom + raise + degree.Depth);
            result.Add(box, radX, 0);
            result.Width = Math.Max(radX + box.Width, degX + degree.Width);
            result.Fit(false);
            return result;
        }

        // ------------------------------------------------------------------ accents and lines

        private static GlyphBox SingleGlyph(Box b)
        {
            while (true)
            {
                if (b is GlyphBox g) return g;
                if (b.Children == null || b.Children.Count != 1) return null;
                b = b.Children[0].box;
            }
        }

        private Box MakeAccent(AccentNode a, Options o)
        {
            var acc = a.Accent;
            var bo = acc.Under ? o : o.WithStyle(o.Style.Cramp());
            var b = BuildList(a.Base, bo);
            float s = o.Scale;
            var g = FindGlyph(acc.Codepoint, FontPref.Default, false, a);
            if (acc.IsArrow || acc.Under) return MakeWideAccent(b, g, acc.Under, o);

            Box accent;
            float accAttach;
            if (acc.Stretchy)
            {
                accent = Stretch(g, b.Width, true, o, out bool assembled);
                if (accent is GlyphBox ag)
                {
                    var am = ag.Glyph.Metrics;
                    accAttach = (float.IsNaN(am.TopAccent) ? (am.XMin + am.XMax) / 2 : am.TopAccent) * ag.Scale * ag.ScaleX;
                }
                else accAttach = accent.Width / 2;
            }
            else
            {
                var gb = new GlyphBox(g, s, o.Color);
                var m = g.Metrics;
                accAttach = (float.IsNaN(m.TopAccent) ? (m.XMin + m.XMax) / 2 : m.TopAccent) * s;
                accent = gb;
            }
            float baseAttach = b.Width / 2;
            var single = b.IsCharBox ? SingleGlyph(b) : null;
            if (single != null)
            {
                var bm = single.Glyph.Metrics;
                baseAttach = (float.IsNaN(bm.TopAccent) ? bm.Advance / 2 : bm.TopAccent) * single.Scale;
            }
            float y = Math.Max(0, b.Height - C.AccentBaseHeight * s);
            var (inkMin, _) = InkOf(accent);
            // keep a small clearance for accents whose ink would collide with tall bases
            if (y + inkMin < b.Height - 0.05f * s && b.Height > C.AccentBaseHeight * s * 1.05f)
                y = Math.Max(y, b.Height + 0.03f * s - inkMin);
            var box = new Box(b.Width, 0, 0);
            box.Add(b, 0, 0);
            box.Add(accent, baseAttach - accAttach, y);
            box.Fit(false);
            box.Italic = b.Italic;
            return box;
        }

        private Box MakeWideAccent(Box b, GlyphId g, bool under, Options o)
        {
            float s = o.Scale;
            var arrow = Stretch(g, b.Width, true, o, out _);
            var (inkMin, inkMax) = InkOf(arrow);
            float gap = 0.08f * s;
            float w = Math.Max(b.Width, arrow.Width);
            var box = new Box(w, 0, 0);
            box.Add(b, (w - b.Width) / 2, 0);
            float y = under ? -(b.Depth + gap) - inkMax : b.Height + gap - inkMin;
            box.Add(arrow, (w - arrow.Width) / 2, y);
            box.Fit(false);
            return box;
        }

        private Box MakeLine(LineNode l, Options o)
        {
            var inner = BuildList(l.Body, l.Over ? o.WithStyle(o.Style.Cramp()) : o);
            return MakeLineBox(inner, o, l.Over);
        }

        private Box MakeLineBox(Box inner, Options o, bool over)
        {
            float s = o.Scale;
            var box = new Box(inner.Width, 0, 0);
            box.Add(inner, 0, 0);
            if (over)
            {
                float t = C.OverbarRuleThickness * s, gap = C.OverbarVerticalGap * s;
                box.Add(new RuleBox(inner.Width, t, 0, o.Color), 0, inner.Height + gap);
                box.Fit(false);
                box.Height = Math.Max(box.Height, inner.Height + gap + t + C.OverbarExtraAscender * s);
            }
            else
            {
                float t = C.UnderbarRuleThickness * s, gap = C.UnderbarVerticalGap * s;
                box.Add(new RuleBox(inner.Width, 0, t, o.Color), 0, -(inner.Depth + gap));
                box.Fit(false);
                box.Depth = Math.Max(box.Depth, inner.Depth + gap + t + C.UnderbarExtraDescender * s);
            }
            return box;
        }

        private Box MakeHorizBrace(HorizBraceNode hb, Options o)
        {
            float s = o.Scale;
            var b = BuildList(hb.Body, o);
            var brace = Stretch(FindGlyph(hb.Codepoint, FontPref.Default, false, hb), b.Width, true, o, out _);
            var (inkMin, inkMax) = InkOf(brace);
            float w = Math.Max(b.Width, brace.Width);
            var box = new Box(w, 0, 0);
            box.Add(b, (w - b.Width) / 2, 0);
            float y = hb.Over ? b.Height + C.StretchStackGapBelowMin * s - inkMin : -(b.Depth + C.StretchStackGapAboveMin * s) - inkMax;
            box.Add(brace, (w - brace.Width) / 2, y);
            box.Fit(false);
            return box;
        }

        private Box MakeStack(List<Node> baseList, List<Node> over, List<Node> under, Options o, bool stretch)
        {
            var b = BuildList(baseList, o);
            return MakeLimits(b, over, under, 0, o, false, false);
        }

        // ------------------------------------------------------------------ delimiters

        private float DelimiterTarget(float height, float depth, Options o)
        {
            float axis = C.AxisHeight * o.Scale;
            float delta = Math.Max(height - axis, depth + axis);
            return Math.Max(delta / 500f * 901f, 2 * delta - 0.5f * o.Scale);
        }

        private Box MakeDelimiter(string delim, float target, Options o, bool center)
        {
            if (string.IsNullOrEmpty(delim)) return Box.Kern(0.12f * o.Scale);
            int cp = char.ConvertToUtf32(delim, 0);
            var g = FindGlyph(cp);
            var glyph = Stretch(g, target, false, o, out _);
            if (!center) return glyph;
            var (inkMin, inkMax) = InkOf(glyph);
            float axis = C.AxisHeight * o.Scale;
            float y = axis - (inkMax + inkMin) / 2;
            var box = new Box(glyph.Width, 0, 0);
            box.Add(glyph, 0, y);
            box.Fit(false);
            return box;
        }

        private Box MakeBigDelimiter(DelimSizingNode d, Options o)
        {
            float size = d.Size switch { 1 => 1.2f, 2 => 1.8f, 3 => 2.4f, _ => 3.0f };
            return MakeDelimiter(d.Delim, size * o.Scale, o, true);
        }

        private static bool ContainsMiddle(List<Node> nodes)
        {
            foreach (var n in nodes)
            {
                if (n is MiddleNode) return true;
                if (n is GroupNode g && !g.IsOrd && ContainsMiddle(g.Body)) return true;
                if (n is StyleNode st && ContainsMiddle(st.Body)) return true;
                if (n is ColorNode c && ContainsMiddle(c.Body)) return true;
            }
            return false;
        }

        private Box MakeLeftRight(LeftRightNode lr, Options o)
        {
            float saved = middleHeight;
            middleHeight = 0;
            var atoms = new List<Atom>();
            BuildAtoms(lr.Body, o, atoms);
            float h = 0, d = 0;
            foreach (var a in atoms) { h = Math.Max(h, a.Box.Height); d = Math.Max(d, a.Box.Depth); }
            float target = DelimiterTarget(h, d, o);
            if (ContainsMiddle(lr.Body))
            {
                middleHeight = target;
                atoms.Clear();
                BuildAtoms(lr.Body, o, atoms);
            }
            middleHeight = saved;
            atoms.Insert(0, new Atom { Box = MakeDelimiter(lr.Left, target, o, true), Class = AtomClass.Open, O = o });
            atoms.Add(new Atom { Box = MakeDelimiter(lr.Right, target, o, true), Class = AtomClass.Close, O = o });
            return Assemble(atoms, o, false);
        }

        // ------------------------------------------------------------------ extensible arrows

        private Box MakeXArrow(XArrowNode x, Options o)
        {
            float s = o.Scale;
            var above = x.Above != null && x.Above.Count > 0 ? BuildList(x.Above, o.WithStyle(o.Style.Sup())) : null;
            var below = x.Below != null && x.Below.Count > 0 ? BuildList(x.Below, o.WithStyle(o.Style.Sub())) : null;
            var g = FindGlyph(x.Codepoint, FontPref.Default, false, x);
            float labels = Math.Max(above?.Width ?? 0, below?.Width ?? 0);
            float natural = g.Metrics.Advance * s;
            float w = Math.Max(Math.Max(natural, 1.6f * s), labels + 0.8f * s);
            return ArrowWithLabels(g, w, above, below, o);
        }

        private Box ArrowWithLabels(GlyphId g, float w, Box above, Box below, Options o)
        {
            float s = o.Scale;
            var arrow = Stretch(g, w, true, o, out _);
            var (inkMin, inkMax) = InkOf(arrow);
            float total = Math.Max(arrow.Width, Math.Max(above?.Width ?? 0, below?.Width ?? 0));
            var box = new Box(total, 0, 0);
            box.Add(arrow, (total - arrow.Width) / 2, 0);
            float gap = 0.11f * s;
            if (above != null) box.Add(above, (total - above.Width) / 2, inkMax + gap + above.Depth);
            if (below != null) box.Add(below, (total - below.Width) / 2, inkMin - gap - below.Height);
            box.Fit(false);
            return box;
        }

        private Box MakeCDArrow(CDArrowNode cd, Options o, float cellWidth, float cellHeight)
        {
            float s = o.Scale;
            var supO = o.WithStyle(o.Style.Sup());
            var above = cd.Above != null && cd.Above.Count > 0 ? BuildList(cd.Above, supO) : null;
            var below = cd.Below != null && cd.Below.Count > 0 ? BuildList(cd.Below, supO) : null;
            switch (cd.Kind)
            {
                case '>': case '<': case '=':
                    {
                        int cp = cd.Kind == '>' ? 0x2192 : cd.Kind == '<' ? 0x2190 : '=';
                        float labels = Math.Max(above?.Width ?? 0, below?.Width ?? 0);
                        float w = Math.Max(Math.Max(cellWidth, 3f * s), labels + 0.8f * s);
                        var box = ArrowWithLabels(FindGlyph(cp), w, above, below, o);
                        var padded = new Box(box.Width + 0.5f * s, box.Height, box.Depth);
                        padded.Add(box, 0.25f * s, 0);
                        return padded;
                    }
                case 'V': case 'A': case '|':
                    {
                        int cp = cd.Kind == 'V' ? 0x2193 : cd.Kind == 'A' ? 0x2191 : 0x2016;
                        float len = Math.Max(cellHeight, 2.2f * s);
                        var arrow = MakeDelimiter(char.ConvertFromUtf32(cp), len, o, true);
                        float gap = 0.15f * s;
                        float axis = C.AxisHeight * s;
                        var box = new Box();
                        float x = 0;
                        if (above != null) { box.Add(above, 0, axis - (above.Height - above.Depth) / 2); x = above.Width + gap; }
                        float arrowX = x;
                        box.Add(arrow, arrowX, 0);
                        x += arrow.Width;
                        if (below != null) { box.Add(below, x + gap, axis - (below.Height - below.Depth) / 2); x += gap + below.Width; }
                        box.Width = x;
                        box.Fit(false);
                        // keep the arrow centred in its column
                        float leftW = arrowX, rightW = x - arrowX - arrow.Width;
                        float pad = Math.Abs(leftW - rightW);
                        var centered = new Box(x + pad, box.Height, box.Depth);
                        centered.Add(box, leftW < rightW ? pad : 0, 0);
                        return centered;
                    }
                default:
                    return new Box();
            }
        }

        // ------------------------------------------------------------------ enclosures

        private Box MakeEnclose(EncloseNode e, Options o)
        {
            float s = o.Scale;
            var inner = BuildList(e.Body, o);
            float t = (e.LineWidth ?? 0.04f) * (e.LineWidth.HasValue ? o.SizeScale : s);
            switch (e.Kind)
            {
                case "box": case "fcolorbox": case "colorbox": case "bbox":
                    {
                        float pad = e.Padding.HasValue ? e.Padding.Value * o.SizeScale : (e.Kind == "bbox" ? 0 : 0.3f * s);
                        bool frame = e.Kind == "box" || e.Kind == "fcolorbox" || (e.Kind == "bbox" && e.Frame.HasValue);
                        float ft = frame ? t : 0;
                        float w = inner.Width + 2 * (pad + ft), h = inner.Height + pad + ft, d = inner.Depth + pad + ft;
                        var box = new Box(w, h, d);
                        if (e.Background.HasValue)
                        {
                            var bg = new RuleBox(w - 2 * ft, h - ft, d - ft, e.Background.Value) { Layer = 1 };
                            box.Add(bg, ft, 0);
                        }
                        box.Add(inner, ft + pad, 0);
                        if (frame) AddFrame(box, w, h, d, ft, e.Frame ?? o.Color, true, true, true, true);
                        return box;
                    }
                case "cancel": case "bcancel": case "xcancel":
                    {
                        var box = new Box(inner.Width, inner.Height, inner.Depth);
                        box.Add(inner, 0, 0);
                        float lt = 0.05f * s, ext = 0.08f * s;
                        float y0 = -inner.Depth - ext, y1 = inner.Height + ext;
                        if (e.Kind != "bcancel") AddStroke(box, 0, y0, inner.Width, y1, lt, o.Color);
                        if (e.Kind != "cancel") AddStroke(box, 0, y1, inner.Width, y0, lt, o.Color);
                        box.Fit(false);
                        return box;
                    }
                case "sout":
                    {
                        var box = new Box(inner.Width, inner.Height, inner.Depth);
                        box.Add(inner, 0, 0);
                        float y = (e.TextBody ? C.XHeight * 0.5f : C.AxisHeight) * s;
                        box.Add(new RuleBox(inner.Width, 0.025f * s, 0.025f * s, o.Color), 0, y);
                        return box;
                    }
                case "cancelto":
                    {
                        var box = new Box(inner.Width, inner.Height, inner.Depth);
                        box.Add(inner, 0, 0);
                        float lt = 0.05f * s;
                        float x0 = 0, y0 = -inner.Depth - 0.05f * s, x1 = inner.Width + 0.15f * s, y1 = inner.Height + 0.25f * s;
                        AddArrowStroke(box, x0, y0, x1, y1, lt, o.Color, s);
                        var label = BuildList(e.CancelTo, o.WithStyle(o.Style.Sup()));
                        box.Add(label, x1 + 0.03f * s, y1 + label.Depth);
                        box.Width = Math.Max(inner.Width, x1 + 0.03f * s + label.Width);
                        box.Fit(false);
                        return box;
                    }
                case "angl":
                    return EncloseNotations(inner, new HashSet<string> { "actuarial" }, o, 0.1f * s);
                case "phase":
                    return EncloseNotations(inner, new HashSet<string> { "phasorangle" }, o, 0.1f * s);
                case "enclose":
                    return EncloseNotations(inner, e.Notations, o, 0.2f * s);
            }
            return inner;
        }

        private static void AddStroke(Box box, float x0, float y0, float x1, float y1, float thickness, Rgba color)
        {
            var shape = Polygon.Line(x0, y0, x1, y1, thickness);
            if (shape != null) box.Add(new PolygonBox(shape, color, 0, 0, 0), 0, 0);
        }

        private static void AddArrowStroke(Box box, float x0, float y0, float x1, float y1, float thickness, Rgba color, float s)
        {
            float dx = x1 - x0, dy = y1 - y0, len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-5f) return;
            float ux = dx / len, uy = dy / len;
            float head = 0.18f * s, half = 0.08f * s;
            float bx = x1 - ux * head, by = y1 - uy * head;
            AddStroke(box, x0, y0, bx, by, thickness, color);
            var tri = new Polygon();
            tri.Contours.Add(new List<Vec2> { new Vec2(x1, y1), new Vec2(bx - uy * half, by + ux * half), new Vec2(bx + uy * half, by - ux * half) });
            box.Add(new PolygonBox(tri, color, 0, 0, 0), 0, 0);
        }

        private static void AddFrame(Box box, float w, float h, float d, float t, Rgba color, bool left, bool right, bool top, bool bottom)
        {
            if (top) box.Add(new RuleBox(w, 0, t, color), 0, h);
            if (bottom) box.Add(new RuleBox(w, t, 0, color), 0, -d);
            if (left) box.Add(new RuleBox(t, h, d, color), 0, 0);
            if (right) box.Add(new RuleBox(t, h, d, color), w - t, 0);
        }

        private Box EncloseNotations(Box inner, HashSet<string> n, Options o, float pad)
        {
            float s = o.Scale;
            float t = 0.05f * s;
            bool has(string k) => n.Contains(k);
            float padL = pad, padR = pad;
            if (has("phasorangle")) padL += (inner.Height + inner.Depth + 2 * pad) * 0.5f;
            if (has("longdiv")) padL += 0.3f * s;
            if (has("radical")) return RadicalAround(inner, o);
            float w = inner.Width + padL + padR, h = inner.Height + pad, d = inner.Depth + pad;
            var box = new Box(w, h + t, d + t);
            box.Add(inner, padL, 0);
            bool box4 = has("box") || has("roundedbox");
            AddFrame(box, w, h, d, t, o.Color, box4 || has("left"), box4 || has("right") || has("actuarial") || has("madruwb"),
                box4 || has("top") || has("actuarial") || has("longdiv"), box4 || has("bottom") || has("madruwb") || has("phasorangle"));
            if (has("updiagonalstrike")) AddStroke(box, 0, -d, w, h, t, o.Color);
            if (has("downdiagonalstrike")) AddStroke(box, 0, h, w, -d, t, o.Color);
            if (has("verticalstrike")) box.Add(new RuleBox(t, h, d, o.Color), w / 2 - t / 2, 0);
            if (has("horizontalstrike")) box.Add(new RuleBox(w, t / 2, t / 2, o.Color), 0, (h - d) / 2);
            if (has("updiagonalarrow")) AddArrowStroke(box, 0, -d, w, h, t, o.Color, s);
            if (has("phasorangle")) AddStroke(box, 0, -d, (h + d) * 0.5f, h, t, o.Color);
            if (has("longdiv"))
            {
                var paren = MakeDelimiter(")", h + d, o, false);
                var (pmin, _) = InkOf(paren);
                box.Add(paren, 0, -d - pmin);
            }
            if (has("circle"))
            {
                var ring = new Polygon();
                float cx = w / 2, cy = (h - d) / 2, rx = w / 2 * 1.12f, ry = (h + d) / 2 * 1.12f;
                var outer = new List<Vec2>(); var innerRing = new List<Vec2>();
                for (int i = 0; i < 72; i++)
                {
                    double a = i * Math.PI * 2 / 72;
                    outer.Add(new Vec2(cx + (float)Math.Cos(a) * (rx + t / 2), cy + (float)Math.Sin(a) * (ry + t / 2)));
                    innerRing.Add(new Vec2(cx + (float)Math.Cos(-a) * (rx - t / 2), cy + (float)Math.Sin(-a) * (ry - t / 2)));
                }
                ring.Contours.Add(outer); ring.Contours.Add(innerRing);
                box.Add(new PolygonBox(ring, o.Color, 0, 0, 0), 0, 0);
                box.Height = Math.Max(box.Height, cy + ry + t);
                box.Depth = Math.Max(box.Depth, ry - cy + t);
            }
            return box;
        }

        private Box RadicalAround(Box inner, Options o)
        {
            // \enclose{radical}: reuse the radical layout around an already built box
            float s = o.Scale;
            float gap = C.RadicalVerticalGap * s, t = C.RadicalRuleThickness * s;
            var sign = Stretch(FindGlyph(0x221A), inner.Height + inner.Depth + gap + t, false, o, out _);
            var (inkMin, inkMax) = InkOf(sign);
            float top = inner.Height + gap + t;
            var box = new Box();
            box.Add(sign, 0, top - inkMax);
            box.Add(inner, sign.Width, 0);
            box.Add(new RuleBox(inner.Width, t, 0, o.Color), sign.Width, top - t);
            box.Width = sign.Width + inner.Width;
            box.Fit(false);
            return box;
        }
    }
}
