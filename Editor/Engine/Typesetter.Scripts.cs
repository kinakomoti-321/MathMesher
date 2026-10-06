using System;
using System.Collections.Generic;

namespace MathMesher.Editor.Engine
{
    internal sealed partial class Typesetter
    {
        private Atom MakeSupSub(SupSubNode n, Options o)
        {
            switch (n.Base)
            {
                case OpNode op:
                    {
                        var opBox = MakeOperator(op, o, out float opItalic);
                        bool limits = op.Limits ?? o.Style.IsDisplay;
                        var box = limits ? MakeLimits(opBox, n.Sup, n.Sub, opItalic, o, false, false)
                                         : AttachScripts(opBox, opBox.Width, opItalic, false, n.Sup, n.Sub, o);
                        return new Atom { Box = box, Class = AtomClass.Op, O = o, Source = n };
                    }
                case HorizBraceNode hb:
                    {
                        var braceBox = MakeHorizBrace(hb, o);
                        return new Atom { Box = MakeLimits(braceBox, n.Sup, n.Sub, 0, o, hb.Over, !hb.Over), Class = AtomClass.Op, O = o, Source = n };
                    }
                case SideSetNode sd:
                    return new Atom { Box = MakeSideSet(sd, o, n.Sup, n.Sub, out _, out _), Class = AtomClass.Op, O = o, Source = n };
            }
            Atom baseAtom = n.Base == null ? new Atom { Box = new Box(), Class = AtomClass.Ord, O = o } : MakeAtom(n.Base, o);
            var b = baseAtom.Box;
            bool isChar = b.IsCharBox;
            float italic = isChar ? b.Italic : 0;
            var result = AttachScripts(b, b.Width - italic, italic, isChar, n.Sup, n.Sub, o);
            return new Atom { Box = result, Class = baseAtom.Class, O = o, Source = n };
        }

        /// <summary>Computes the TeX superscript shift-up u and subscript shift-down v.</summary>
        private void ScriptShifts(Box b, bool isChar, Box sup, Box sub, Options o, out float u, out float v)
        {
            float s = o.Scale;
            float supScale = o.WithStyle(o.Style.Sup()).Scale;
            float subScale = o.WithStyle(o.Style.Sub()).Scale;
            if (isChar) { u = 0; v = 0; }
            else
            {
                u = b.Height - C.SuperscriptBaselineDropMax * supScale;
                v = b.Depth + C.SubscriptBaselineDropMin * subScale;
            }
            if (sup == null)
            {
                v = Math.Max(v, Math.Max(C.SubscriptShiftDown * s, sub.Height - C.SubscriptTopMax * s));
                return;
            }
            u = Math.Max(u, Math.Max((o.Style.Cramped ? C.SuperscriptShiftUpCramped : C.SuperscriptShiftUp) * s, sup.Depth + C.SuperscriptBottomMin * s));
            if (sub == null) return;
            v = Math.Max(v, C.SubscriptShiftDown * s);
            float gap = (u - sup.Depth) - (sub.Height - v);
            float minGap = C.SubSuperscriptGapMin * s;
            if (gap < minGap)
            {
                v += minGap - gap;
                float psi = C.SuperscriptBottomMaxWithSubscript * s - (u - sup.Depth);
                if (psi > 0) { u += psi; v -= psi; }
            }
        }

        private Box AttachScripts(Box b, float baseWidth, float italic, bool isChar, List<Node> supList, List<Node> subList, Options o)
        {
            // OpenType math primes (U+2032...) are already raised: draw them at full size, not as scripts
            Box primes = null;
            if (supList != null)
            {
                int n = 0;
                while (n < supList.Count && supList[n] is SymbolNode ps && IsPrimeText(ps.Text)) n++;
                if (n > 0)
                {
                    var pb = new List<Box>();
                    for (int i = 0; i < n; i++) pb.Add(MakeSymbol((SymbolNode)supList[i], o));
                    primes = Box.HBox(pb);
                    supList = n == supList.Count ? null : supList.GetRange(n, supList.Count - n);
                }
            }
            if (primes != null)
            {
                var withPrimes = new Box();
                withPrimes.Add(b, 0, 0);
                withPrimes.Add(primes, baseWidth + italic, 0);
                withPrimes.Width = baseWidth + italic + primes.Width;
                withPrimes.Fit(false);
                if (supList == null && subList == null) return withPrimes;
                if (supList != null) { b = withPrimes; baseWidth = withPrimes.Width; italic = 0; isChar = false; }
                else
                {
                    // prime with subscript: the subscript goes under the prime position
                    var subOnly = AttachScripts(b, baseWidth, 0, isChar, null, subList, o);
                    var both = new Box();
                    both.Add(subOnly, 0, 0);
                    both.Add(primes, baseWidth + italic, 0);
                    both.Width = Math.Max(subOnly.Width, withPrimes.Width);
                    both.Fit(false);
                    return both;
                }
            }
            var sup = supList != null ? BuildList(supList, o.WithStyle(o.Style.Sup())) : null;
            var sub = subList != null ? BuildList(subList, o.WithStyle(o.Style.Sub())) : null;
            if (sup == null && sub == null) return b;
            ScriptShifts(b, isChar, sup, sub, o, out float u, out float v);
            var box = new Box();
            box.Add(b, 0, 0);
            float w = b.Width;
            if (sup != null)
            {
                float x = baseWidth + italic;
                box.Add(sup, x, u);
                w = Math.Max(w, x + sup.Width);
            }
            if (sub != null)
            {
                float x = baseWidth;
                box.Add(sub, x, -v);
                w = Math.Max(w, x + sub.Width);
            }
            box.Width = w + C.SpaceAfterScript * o.Scale;
            box.Fit(false);
            return box;
        }

        private static bool IsPrimeText(string t) => t.Length > 0 && (t[0] == '′' || t[0] == '″' || t[0] == '‴' || t[0] == '⁗' || t[0] == '‵');

        private Box MakeLimits(Box op, List<Node> supList, List<Node> subList, float italic, Options o, bool stretchOver, bool stretchUnder)
        {
            var sup = supList != null ? BuildList(supList, o.WithStyle(o.Style.Sup())) : null;
            var sub = subList != null ? BuildList(subList, o.WithStyle(o.Style.Sub())) : null;
            if (sup == null && sub == null) return op;
            float s = o.Scale;
            float w = Math.Max(op.Width, Math.Max(sup?.Width ?? 0, sub?.Width ?? 0));
            var box = new Box(w, 0, 0);
            box.Add(op, (w - op.Width) / 2, 0);
            if (sup != null)
            {
                float y = stretchOver
                    ? op.Height + Math.Max(C.StretchStackGapAboveMin * s + sup.Depth, C.StretchStackTopShiftUp * s)
                    : op.Height + Math.Max(C.UpperLimitGapMin * s, C.UpperLimitBaselineRiseMin * s - sup.Depth) + sup.Depth;
                box.Add(sup, (w - sup.Width) / 2 + italic / 2, y);
            }
            if (sub != null)
            {
                float y = stretchUnder
                    ? op.Depth + Math.Max(C.StretchStackGapBelowMin * s + sub.Height, C.StretchStackBottomShiftDown * s)
                    : op.Depth + Math.Max(C.LowerLimitGapMin * s, C.LowerLimitBaselineDropMin * s - sub.Height) + sub.Height;
                box.Add(sub, (w - sub.Width) / 2 - italic / 2, -y);
            }
            box.Fit(false);
            return box;
        }

        private Box MakeOperator(OpNode op, Options o, out float italic)
        {
            italic = 0;
            float s = o.Scale;
            float axis = C.AxisHeight * s;
            if (op.Symbol != null)
            {
                var cps = Codepoints(op.Symbol);
                var boxes = new List<Box>();
                float lastItalic = 0;
                foreach (var cp in cps)
                {
                    var g = FindGlyph(cp, FontPref.Default, false, op);
                    bool large = o.Style.IsDisplay && !op.Small && cp != 0x22EF;
                    if (large)
                    {
                        float min = C.DisplayOperatorMinHeight;
                        GlyphId chosen = g;
                        foreach (var v in fonts.Variants(g, false))
                        {
                            chosen = v.glyph;
                            if (v.size >= min && !v.glyph.Equals(g)) break;
                        }
                        g = chosen;
                    }
                    var gb = new GlyphBox(g, s, o.Color);
                    lastItalic = gb.Italic;
                    float shift = (gb.Height - gb.Depth) / 2 - axis;
                    var centered = new Box(gb.Width, 0, 0);
                    centered.Add(gb, 0, -shift);
                    centered.Fit(false);
                    if (boxes.Count > 0) boxes.Add(Box.Kern(op.Integral ? -0.15f * s : 0));
                    boxes.Add(centered);
                }
                italic = lastItalic;
                var result = boxes.Count == 1 ? boxes[0] : Box.HBox(boxes);
                result.Italic = italic;
                return result;
            }
            if (op.Name != null)
            {
                var uo = o.Clone(); uo.Variant = MathVariant.Upright; uo.InOperatorName = true;
                var parts = new List<Box>();
                foreach (var word in op.Name.Split(' '))
                {
                    if (parts.Count > 0) parts.Add(Box.Kern(3 * o.Mu));
                    foreach (var cp in Codepoints(word))
                        parts.Add(GlyphOf(cp, uo, FontPref.Default, false, op));
                }
                var name = Box.HBox(parts);
                switch (op.Decoration)
                {
                    case "over": return MakeLineBox(name, o, true);
                    case "under": return MakeLineBox(name, o, false);
                    case "underright": return UnderArrow(name, 0x2192, o);
                    case "underleft": return UnderArrow(name, 0x2190, o);
                }
                return name;
            }
            var bo = o.Clone(); bo.InOperatorName = op.Name != null;
            var body = BuildList(op.Body, bo);
            if (op.Body != null && op.Body.Count == 1 && op.Body[0] is SymbolNode)
            {
                float shift = (body.Height - body.Depth) / 2 - axis;
                var centered = new Box(body.Width, 0, 0);
                centered.Add(body, 0, -shift);
                centered.Fit(false);
                italic = body.Italic;
                return centered;
            }
            return body;
        }

        private Box UnderArrow(Box inner, int cp, Options o)
        {
            var arrow = Stretch(FindGlyph(cp), inner.Width, true, o, out _);
            var am = arrow;
            float gap = 0.08f * o.Scale;
            var box = new Box(Math.Max(inner.Width, arrow.Width), 0, 0);
            box.Add(inner, (box.Width - inner.Width) / 2, 0);
            float arrowTop = ArrowInkTop(arrow);
            box.Add(arrow, (box.Width - arrow.Width) / 2, -inner.Depth - gap - arrowTop);
            box.Fit(false);
            return box;
        }

        private static float ArrowInkTop(Box arrow) => arrow.Height;

        private Box MakeSideSet(SideSetNode sd, Options o, List<Node> outerSup, List<Node> outerSub, out float opX, out float opWidth)
        {
            float italic = 0;
            Box op;
            bool? limits = null;
            if (sd.Base is OpNode opNode) { op = MakeOperator(opNode, o, out italic); limits = opNode.Limits; }
            else op = MakeAtom(sd.Base, o).Box;
            var supO = o.WithStyle(o.Style.Sup());
            var subO = o.WithStyle(o.Style.Sub());
            var ls = sd.LeftSup != null ? BuildList(sd.LeftSup, supO) : null;
            var lb = sd.LeftSub != null ? BuildList(sd.LeftSub, subO) : null;
            var rs = sd.RightSup != null ? BuildList(sd.RightSup, supO) : null;
            var rb = sd.RightSub != null ? BuildList(sd.RightSub, subO) : null;
            var box = new Box();
            float x = 0;
            if (ls != null || lb != null)
            {
                ScriptShifts(op, false, ls, lb, o, out float u, out float v);
                float lw = Math.Max(ls?.Width ?? 0, lb?.Width ?? 0);
                if (ls != null) box.Add(ls, lw - ls.Width, u);
                if (lb != null) box.Add(lb, lw - lb.Width, -v);
                x = lw + C.SpaceAfterScript * o.Scale;
            }
            opX = x;
            opWidth = op.Width;
            bool useLimits = limits ?? o.Style.IsDisplay;
            Box core = op;
            if ((outerSup != null || outerSub != null) && useLimits)
            {
                core = MakeLimits(op, outerSup, outerSub, italic, o, false, false);
                outerSup = null; outerSub = null;
            }
            float coreShift = (core.Width - op.Width) / 2;
            box.Add(core, x - coreShift, 0);
            float right = x + op.Width;
            if (rs != null || rb != null)
            {
                ScriptShifts(op, false, rs, rb, o, out float u, out float v);
                if (rs != null) box.Add(rs, right + italic, u);
                if (rb != null) box.Add(rb, right, -v);
                right += Math.Max((rs?.Width ?? 0) + italic, rb?.Width ?? 0) + C.SpaceAfterScript * o.Scale;
            }
            box.Fit(false);
            box.Width = Math.Max(right, x - coreShift + core.Width);
            if (outerSup != null || outerSub != null)
                return AttachScripts(box, box.Width, 0, false, outerSup, outerSub, o);
            return box;
        }
    }
}
