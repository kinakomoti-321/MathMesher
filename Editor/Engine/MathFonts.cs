using System;
using System.Collections.Generic;
using System.IO;
using MathMesher.Internal.Typography.OpenFont;
using MathMesher.Internal.Typography.OpenFont.MathGlyphs;
using MathMesher.Internal.Typography.Contours;

namespace MathMesher.Editor.Engine
{
    /// <summary>One loaded OpenType font. All metrics returned by the engine are in em.</summary>
    internal sealed class FontFace
    {
        private static int nextId;
        public readonly int Id;
        public readonly string Name;
        public readonly Typeface Typeface;
        public readonly float Scale;
        private readonly Dictionary<ushort, GlyphMetrics> metrics = new Dictionary<ushort, GlyphMetrics>();
        private OutlineBuilder outlineBuilder;

        public FontFace(string name, Typeface typeface)
        {
            Id = System.Threading.Interlocked.Increment(ref nextId);
            Name = name;
            Typeface = typeface;
            Scale = 1f / typeface.UnitsPerEm;
        }

        public bool HasMath => Typeface.MathConsts != null;
        public ushort Lookup(int codepoint) => codepoint <= 0 ? (ushort)0 : Typeface.GetGlyphIndex(codepoint);

        public GlyphMetrics Metrics(ushort index)
        {
            if (metrics.TryGetValue(index, out var m)) return m;
            var glyph = Typeface.GetGlyph(index);
            var b = glyph.Bounds;
            m = new GlyphMetrics
            {
                Advance = Typeface.GetHAdvanceWidthFromGlyphIndex(index) * Scale,
                XMin = b.XMin * Scale, YMin = b.YMin * Scale, XMax = b.XMax * Scale, YMax = b.YMax * Scale,
            };
            if (b.XMin == 0 && b.XMax == 0 && b.YMin == 0 && b.YMax == 0) m.Empty = true;
            var info = glyph.MathGlyphInfo;
            if (info != null)
            {
                m.Italic = (info.ItalicCorrection?.Value ?? 0) * Scale;
                m.TopAccent = info.TopAccentAttachment.HasValue ? info.TopAccentAttachment.Value.Value * Scale : float.NaN;
                m.IsExtendedShape = info.IsShapeExtensible;
            }
            else m.TopAccent = float.NaN;
            metrics[index] = m;
            return m;
        }

        public MathGlyphInfo MathInfo(ushort index) => Typeface.GetGlyph(index).MathGlyphInfo;

        /// <summary>Reads the outline in em units, flattened to the given tolerance (em).</summary>
        public List<List<Vec2>> ReadOutline(ushort index, float toleranceEm)
        {
            if (outlineBuilder == null) outlineBuilder = new OutlineBuilder(Typeface);
            var translator = new FlatteningTranslator(Scale, toleranceEm);
            var glyph = Typeface.GetGlyph(index);
            if (glyph.TtfWoffInfo == null && glyph.CffInfo == null) return translator.Contours;
            outlineBuilder.BuildFromGlyph(glyph, -1);
            outlineBuilder.ReadShapes(translator);
            translator.CloseContour();
            return translator.Contours;
        }

        private sealed class OutlineBuilder : GlyphOutlineBuilderBase
        {
            public OutlineBuilder(Typeface typeface) : base(typeface) { UseTrueTypeInstructions = false; }
        }
    }

    internal struct GlyphMetrics
    {
        public float Advance, XMin, YMin, XMax, YMax, Italic, TopAccent;
        public bool Empty, IsExtendedShape;
        public float Height => Empty ? 0 : Math.Max(0, YMax);
        public float Depth => Empty ? 0 : Math.Max(0, -YMin);
    }

    internal enum FontPref { Default = 0, Stix = 1, Blackboard = 2 }

    internal readonly struct GlyphId : IEquatable<GlyphId>
    {
        public readonly FontFace Face;
        public readonly ushort Index;
        public GlyphId(FontFace face, ushort index) { Face = face; Index = index; }
        public bool IsValid => Face != null;
        public GlyphMetrics Metrics => Face.Metrics(Index);
        public bool Equals(GlyphId other) => ReferenceEquals(Face, other.Face) && Index == other.Index;
        public override bool Equals(object obj) => obj is GlyphId g && Equals(g);
        public override int GetHashCode() => ((Face?.Id ?? 0) * 397) ^ Index;
    }

    internal struct GlyphPart
    {
        public GlyphId Glyph;
        public float StartConnector, EndConnector, FullAdvance;
        public bool IsExtender;
    }

    /// <summary>Converts outline commands to flattened polylines.</summary>
    internal sealed class FlatteningTranslator : IGlyphTranslator
    {
        public readonly List<List<Vec2>> Contours = new List<List<Vec2>>();
        private readonly float scale, tolerance;
        private List<Vec2> current;
        private Vec2 last;
        public int PointCount { get; private set; }

        public FlatteningTranslator(float scale, float toleranceEm)
        {
            this.scale = scale;
            tolerance = Math.Max(1e-5f, toleranceEm);
        }

        public void BeginRead(int contourCount) { }
        public void EndRead() { }
        public void MoveTo(float x0, float y0)
        {
            CloseContour();
            current = new List<Vec2>();
            Add(new Vec2(x0 * scale, y0 * scale));
        }

        private void Add(Vec2 p)
        {
            if (current == null) { current = new List<Vec2>(); }
            if (current.Count > 0 && (p - current[current.Count - 1]).SqrMagnitude < 1e-14f) { last = p; return; }
            if (float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsInfinity(p.X) || float.IsInfinity(p.Y))
                throw new TeXException("フォントの輪郭座標が不正です。");
            current.Add(p);
            last = p;
            PointCount++;
        }

        public void LineTo(float x1, float y1) => Add(new Vec2(x1 * scale, y1 * scale));
        public void Curve3(float x1, float y1, float x2, float y2) =>
            Quadratic(last, new Vec2(x1 * scale, y1 * scale), new Vec2(x2 * scale, y2 * scale), 0);
        public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3) =>
            Cubic(last, new Vec2(x1 * scale, y1 * scale), new Vec2(x2 * scale, y2 * scale), new Vec2(x3 * scale, y3 * scale), 0);

        internal static float DistanceToSegment(Vec2 p, Vec2 a, Vec2 b)
        {
            var d = b - a;
            float len = d.SqrMagnitude;
            float t = len == 0 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * d.X + (p.Y - a.Y) * d.Y) / len));
            var q = a + d * t - p;
            return (float)Math.Sqrt(q.SqrMagnitude);
        }

        private void Quadratic(Vec2 a, Vec2 b, Vec2 c, int depth)
        {
            if (depth >= 12 || DistanceToSegment(b, a, c) <= tolerance) { Add(c); return; }
            var ab = (a + b) * 0.5f; var bc = (b + c) * 0.5f; var mid = (ab + bc) * 0.5f;
            Quadratic(a, ab, mid, depth + 1);
            Quadratic(mid, bc, c, depth + 1);
        }

        private void Cubic(Vec2 a, Vec2 b, Vec2 c, Vec2 d, int depth)
        {
            if (depth >= 12 || Math.Max(DistanceToSegment(b, a, d), DistanceToSegment(c, a, d)) <= tolerance) { Add(d); return; }
            var ab = (a + b) * 0.5f; var bc = (b + c) * 0.5f; var cd = (c + d) * 0.5f;
            var abc = (ab + bc) * 0.5f; var bcd = (bc + cd) * 0.5f; var mid = (abc + bcd) * 0.5f;
            Cubic(a, ab, abc, mid, depth + 1);
            Cubic(mid, bcd, cd, d, depth + 1);
        }

        public void CloseContour()
        {
            if (current == null) return;
            if (current.Count > 1 && (current[0] - current[current.Count - 1]).SqrMagnitude < 1e-14f) current.RemoveAt(current.Count - 1);
            if (current.Count >= 3) Contours.Add(current);
            current = null;
        }
    }

    /// <summary>Mathematical constants of the primary math font, in em.</summary>
    internal sealed class MathConstantsEm
    {
        public float ScriptScale, ScriptScriptScale, AxisHeight, AccentBaseHeight, FlattenedAccentBaseHeight;
        public float SubscriptShiftDown, SubscriptTopMax, SubscriptBaselineDropMin;
        public float SuperscriptShiftUp, SuperscriptShiftUpCramped, SuperscriptBottomMin, SuperscriptBaselineDropMax;
        public float SubSuperscriptGapMin, SuperscriptBottomMaxWithSubscript, SpaceAfterScript;
        public float UpperLimitGapMin, UpperLimitBaselineRiseMin, LowerLimitGapMin, LowerLimitBaselineDropMin;
        public float StackTopShiftUp, StackTopDisplayStyleShiftUp, StackBottomShiftDown, StackBottomDisplayStyleShiftDown;
        public float StackGapMin, StackDisplayStyleGapMin;
        public float StretchStackTopShiftUp, StretchStackBottomShiftDown, StretchStackGapAboveMin, StretchStackGapBelowMin;
        public float FractionNumeratorShiftUp, FractionNumeratorDisplayStyleShiftUp, FractionDenominatorShiftDown;
        public float FractionDenominatorDisplayStyleShiftDown, FractionNumeratorGapMin, FractionNumDisplayStyleGapMin;
        public float FractionRuleThickness, FractionDenominatorGapMin, FractionDenomDisplayStyleGapMin;
        public float SkewedFractionHorizontalGap, SkewedFractionVerticalGap;
        public float OverbarVerticalGap, OverbarRuleThickness, OverbarExtraAscender;
        public float UnderbarVerticalGap, UnderbarRuleThickness, UnderbarExtraDescender;
        public float RadicalVerticalGap, RadicalDisplayStyleVerticalGap, RadicalRuleThickness, RadicalExtraAscender;
        public float RadicalKernBeforeDegree, RadicalKernAfterDegree, RadicalDegreeBottomRaisePercent;
        public float MinConnectorOverlap, DelimitedSubFormulaMinHeight, DisplayOperatorMinHeight, MathLeading;
        public float XHeight, Quad;

        public static MathConstantsEm From(FontFace face)
        {
            var c = face.Typeface.MathConsts;
            float s = face.Scale;
            float V(MathValueRecord r) => r.Value * s;
            var x = face.Metrics(face.Lookup('x'));
            return new MathConstantsEm
            {
                ScriptScale = c.ScriptPercentScaleDown / 100f,
                ScriptScriptScale = c.ScriptScriptPercentScaleDown / 100f,
                AxisHeight = V(c.AxisHeight),
                AccentBaseHeight = V(c.AccentBaseHeight),
                FlattenedAccentBaseHeight = V(c.FlattenedAccentBaseHeight),
                SubscriptShiftDown = V(c.SubscriptShiftDown),
                SubscriptTopMax = V(c.SubscriptTopMax),
                SubscriptBaselineDropMin = V(c.SubscriptBaselineDropMin),
                SuperscriptShiftUp = V(c.SuperscriptShiftUp),
                SuperscriptShiftUpCramped = V(c.SuperscriptShiftUpCramped),
                SuperscriptBottomMin = V(c.SuperscriptBottomMin),
                SuperscriptBaselineDropMax = V(c.SuperscriptBaselineDropMax),
                SubSuperscriptGapMin = V(c.SubSuperscriptGapMin),
                SuperscriptBottomMaxWithSubscript = V(c.SuperscriptBottomMaxWithSubscript),
                SpaceAfterScript = V(c.SpaceAfterScript),
                UpperLimitGapMin = V(c.UpperLimitGapMin),
                UpperLimitBaselineRiseMin = V(c.UpperLimitBaselineRiseMin),
                LowerLimitGapMin = V(c.LowerLimitGapMin),
                LowerLimitBaselineDropMin = V(c.LowerLimitBaselineDropMin),
                StackTopShiftUp = V(c.StackTopShiftUp),
                StackTopDisplayStyleShiftUp = V(c.StackTopDisplayStyleShiftUp),
                StackBottomShiftDown = V(c.StackBottomShiftDown),
                StackBottomDisplayStyleShiftDown = V(c.StackBottomDisplayStyleShiftDown),
                StackGapMin = V(c.StackGapMin),
                StackDisplayStyleGapMin = V(c.StackDisplayStyleGapMin),
                StretchStackTopShiftUp = V(c.StretchStackTopShiftUp),
                StretchStackBottomShiftDown = V(c.StretchStackBottomShiftDown),
                StretchStackGapAboveMin = V(c.StretchStackGapAboveMin),
                StretchStackGapBelowMin = V(c.StretchStackGapBelowMin),
                FractionNumeratorShiftUp = V(c.FractionNumeratorShiftUp),
                FractionNumeratorDisplayStyleShiftUp = V(c.FractionNumeratorDisplayStyleShiftUp),
                FractionDenominatorShiftDown = V(c.FractionDenominatorShiftDown),
                FractionDenominatorDisplayStyleShiftDown = V(c.FractionDenominatorDisplayStyleShiftDown),
                FractionNumeratorGapMin = V(c.FractionNumeratorGapMin),
                FractionNumDisplayStyleGapMin = V(c.FractionNumDisplayStyleGapMin),
                FractionRuleThickness = V(c.FractionRuleThickness),
                FractionDenominatorGapMin = V(c.FractionDenominatorGapMin),
                FractionDenomDisplayStyleGapMin = V(c.FractionDenomDisplayStyleGapMin),
                SkewedFractionHorizontalGap = V(c.SkewedFractionHorizontalGap),
                SkewedFractionVerticalGap = V(c.SkewedFractionVerticalGap),
                OverbarVerticalGap = V(c.OverbarVerticalGap),
                OverbarRuleThickness = V(c.OverbarRuleThickness),
                OverbarExtraAscender = V(c.OverbarExtraAscender),
                UnderbarVerticalGap = V(c.UnderbarVerticalGap),
                UnderbarRuleThickness = V(c.UnderbarRuleThickness),
                UnderbarExtraDescender = V(c.UnderbarExtraDescender),
                RadicalVerticalGap = V(c.RadicalVerticalGap),
                RadicalDisplayStyleVerticalGap = V(c.RadicalDisplayStyleVerticalGap),
                RadicalRuleThickness = V(c.RadicalRuleThickness),
                RadicalExtraAscender = V(c.RadicalExtraAscender),
                RadicalKernBeforeDegree = V(c.RadicalKernBeforeDegree),
                RadicalKernAfterDegree = V(c.RadicalKernAfterDegree),
                RadicalDegreeBottomRaisePercent = c.RadicalDegreeBottomRaisePercent / 100f,
                MinConnectorOverlap = c.MinConnectorOverlap * s,
                DelimitedSubFormulaMinHeight = c.DelimitedSubFormulaMinHeight * s,
                DisplayOperatorMinHeight = c.DisplayOperatorMinHeight * s,
                MathLeading = V(c.MathLeading),
                XHeight = x.Height > 0 ? x.Height : 0.431f,
                Quad = 1f,
            };
        }
    }

    /// <summary>The font chain used for layout: the primary math font, bundled supplements and user fallbacks.</summary>
    internal sealed class FontSet
    {
        public readonly FontFace Math;
        public readonly FontFace Stix;
        public readonly FontFace Blackboard;
        public readonly FontFace Cyrillic;
        public readonly IReadOnlyList<FontFace> UserFallbacks;
        public readonly MathConstantsEm Constants;
        private readonly Dictionary<long, GlyphId> lookupCache = new Dictionary<long, GlyphId>();

        public FontSet(FontFace math, FontFace stix, FontFace blackboard, FontFace cyrillic, IReadOnlyList<FontFace> userFallbacks)
        {
            Math = math; Stix = stix; Blackboard = blackboard; Cyrillic = cyrillic;
            UserFallbacks = userFallbacks ?? Array.Empty<FontFace>();
            Constants = MathConstantsEm.From(math);
        }

        /// <summary>Finds a glyph for a code point along the fallback chain.</summary>
        public GlyphId Find(int codepoint, FontPref pref = FontPref.Default, bool text = false)
        {
            long key = ((long)codepoint << 3) | ((long)pref << 1) | (text ? 1L : 0L);
            if (lookupCache.TryGetValue(key, out var hit)) return hit;
            GlyphId result = default;
            foreach (var face in Chain(pref, text))
            {
                if (face == null) continue;
                var index = face.Lookup(codepoint);
                if (index != 0) { result = new GlyphId(face, index); break; }
            }
            lookupCache[key] = result;
            return result;
        }

        private IEnumerable<FontFace> Chain(FontPref pref, bool text)
        {
            if (pref == FontPref.Stix) yield return Stix;
            if (pref == FontPref.Blackboard) yield return Blackboard;
            yield return Math;
            if (text) yield return Cyrillic;
            if (pref != FontPref.Stix) yield return Stix;
            if (!text) yield return Cyrillic;
            foreach (var f in UserFallbacks) yield return f;
        }

        public MathGlyphConstruction Construction(GlyphId g, bool horizontal)
        {
            if (!g.IsValid) return null;
            var info = g.Face.MathInfo(g.Index);
            return horizontal ? info?.HoriGlyphConstruction : info?.VertGlyphConstruction;
        }

        /// <summary>Size variants (including the base glyph) and their advance along the stretch direction.</summary>
        public List<(GlyphId glyph, float size)> Variants(GlyphId g, bool horizontal)
        {
            var list = new List<(GlyphId, float)>();
            var c = Construction(g, horizontal);
            if (c?.GlyphVariantRecords != null && c.GlyphVariantRecords.Length > 0)
            {
                foreach (var r in c.GlyphVariantRecords)
                    list.Add((new GlyphId(g.Face, r.VariantGlyph), r.AdvanceMeasurement * g.Face.Scale));
            }
            else
            {
                var m = g.Metrics;
                list.Add((g, horizontal ? m.Advance : m.Height + m.Depth));
            }
            return list;
        }

        public List<GlyphPart> Assembly(GlyphId g, bool horizontal, out float italic)
        {
            italic = 0;
            var c = Construction(g, horizontal);
            if (c?.GlyphAsm == null) return null;
            var asm = c.GlyphAsm.Value;
            if (asm.GlyphPartRecords == null || asm.GlyphPartRecords.Length == 0) return null;
            italic = asm.ItalicCorrection.Value * g.Face.Scale;
            var parts = new List<GlyphPart>();
            foreach (var p in asm.GlyphPartRecords)
                parts.Add(new GlyphPart
                {
                    Glyph = new GlyphId(g.Face, p.GlyphId),
                    StartConnector = p.StartConnectorLength * g.Face.Scale,
                    EndConnector = p.EndConnectorLength * g.Face.Scale,
                    FullAdvance = p.FullAdvance * g.Face.Scale,
                    IsExtender = p.IsExtender,
                });
            return parts;
        }

        public float MinConnectorOverlap(GlyphId g)
        {
            var mc = g.IsValid ? g.Face.Typeface.MathConsts : null;
            return mc != null ? mc.MinConnectorOverlap * g.Face.Scale : Constants.MinConnectorOverlap;
        }

        /// <summary>Math kern (cut-in) at a corner. corner: 0 TR, 1 TL, 2 BR, 3 BL.</summary>
        public float MathKern(GlyphId g, int corner, float heightEm)
        {
            if (!g.IsValid) return 0;
            var info = g.Face.MathInfo(g.Index);
            if (info == null || !info.HasSomeMathKern) return 0;
            MathKern k = corner switch { 0 => info.TopRightMathKern, 1 => info.TopLeftMathKern, 2 => info.BottomRightMathKern, _ => info.BottomLeftMathKern };
            if (k == null || k.KernValues == null || k.KernValues.Length == 0) return 0;
            float s = g.Face.Scale;
            int i = 0;
            while (i < k.HeightCount && i < k.CorrectionHeights.Length && heightEm >= k.CorrectionHeights[i].Value * s) i++;
            i = System.Math.Min(i, k.KernValues.Length - 1);
            return k.KernValues[i].Value * s;
        }
    }

    /// <summary>Loads bundled and user fonts. Thread-affine: call from the Unity main thread.</summary>
    internal static class MathFonts
    {
        private const string ResourcePrefix = "MathMesher.Internal.Fonts.";
        private static FontFace math, blackboard, cyrillic, stix;
        private static string stixPath;
        private static readonly Dictionary<string, (DateTime stamp, FontFace face)> userFonts = new Dictionary<string, (DateTime, FontFace)>();
        private static readonly Dictionary<string, FontSet> sets = new Dictionary<string, FontSet>();

        /// <summary>Directory containing STIXTwoMath-Regular.otf (Editor/Fonts in the package).</summary>
        public static string BundledFontDirectory { get; set; }

        private static FontFace LoadResource(string file)
        {
            var asm = typeof(Typeface).Assembly;
            using (var stream = asm.GetManifestResourceStream(ResourcePrefix + file))
            {
                if (stream == null) throw new InvalidOperationException("同梱フォントが見つかりません: " + file);
                var typeface = new OpenFontReader().Read(stream);
                if (typeface == null) throw new InvalidOperationException("同梱フォントを読み込めません: " + file);
                typeface.UpdateAllCffGlyphBounds();
                return new FontFace(file, typeface);
            }
        }

        private static FontFace LoadFile(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var typeface = new OpenFontReader().Read(stream);
                if (typeface == null) throw new TeXException("フォントを読み込めません: " + Path.GetFileName(path));
                if (typeface.IsCffFont) typeface.UpdateAllCffGlyphBounds();
                return new FontFace(Path.GetFileName(path), typeface);
            }
        }

        public static FontSet Get(IReadOnlyList<string> userFontPaths = null)
        {
            if (math == null)
            {
                math = LoadResource("latinmodern-math.otf");
                blackboard = LoadResource("AMS-Capital-Blackboard-Bold.otf");
                cyrillic = LoadResource("cyrillic-modern-nmr10.otf");
            }
            string wantedStix = BundledFontDirectory == null ? null : Path.Combine(BundledFontDirectory, "STIXTwoMath-Regular.otf");
            if (wantedStix != stixPath)
            {
                stixPath = wantedStix;
                stix = wantedStix != null && File.Exists(wantedStix) ? LoadFile(wantedStix) : null;
                sets.Clear();
            }
            var faces = new List<FontFace>();
            var key = new System.Text.StringBuilder();
            if (userFontPaths != null)
                foreach (var p in userFontPaths)
                {
                    if (string.IsNullOrEmpty(p)) continue;
                    if (!File.Exists(p)) throw new TeXException("代替フォントのファイルが見つかりません: " + p);
                    var stamp = File.GetLastWriteTimeUtc(p);
                    if (!userFonts.TryGetValue(p, out var entry) || entry.stamp != stamp)
                    {
                        entry = (stamp, LoadFile(p));
                        userFonts[p] = entry;
                    }
                    faces.Add(entry.face);
                    key.Append(entry.face.Id).Append(';');
                }
            var k = key.ToString();
            if (!sets.TryGetValue(k, out var set))
            {
                set = new FontSet(math, stix, blackboard, cyrillic, faces);
                sets[k] = set;
            }
            return set;
        }
    }
}
