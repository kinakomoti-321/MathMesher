using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MathMesher.Editor.Engine
{
    internal sealed class EngineResult
    {
        public MeshBuffers Buffers;
        public float Width, Height, Depth; // em
        public int ShapeCount;
        public double ParseMs, LayoutMs, MeshMs;
        public bool FromCache;
        public int NewGlyphs;
    }

    /// <summary>Unity-independent pipeline: TeX → nodes → boxes → draw list → mesh buffers, with caching at every level.</summary>
    internal static class TeXEngine
    {
        private static readonly LruCache<string, EngineResult> results = new LruCache<string, EngineResult>(48, StringComparer.Ordinal);

        public static void ClearCaches()
        {
            results.Clear();
            GeometryCache.Clear();
            MeshAssembler.ClearCache();
        }

        public static string Normalize(string source)
        {
            source = (source ?? "").Trim();
            foreach (var pair in new[] { ("$$", "$$"), ("$", "$"), (@"\[", @"\]"), (@"\(", @"\)") })
            {
                if (source.Length >= pair.Item1.Length + pair.Item2.Length && source.StartsWith(pair.Item1, StringComparison.Ordinal) &&
                    source.EndsWith(pair.Item2, StringComparison.Ordinal))
                {
                    var inner = source.Substring(pair.Item1.Length, source.Length - pair.Item1.Length - pair.Item2.Length);
                    if (pair.Item1 == "$" && inner.Contains("$")) continue;
                    return inner.Trim();
                }
            }
            return source;
        }

        private static string Key(string latex, bool display, AssemblySettings s, IReadOnlyList<string> fonts)
        {
            var sb = new StringBuilder(latex.Length + 96);
            sb.Append(display ? 'D' : 'T').Append('|').Append((int)s.Mode).Append('|').Append((int)s.Alignment).Append('|')
              .Append(s.EmSize.ToString("R")).Append('|').Append(s.Depth.ToString("R")).Append('|').Append(s.CurveTolerance.ToString("R")).Append('|')
              .Append(s.DoubleSided ? 1 : 0).Append('|').Append(s.Color.R).Append(',').Append(s.Color.G).Append(',').Append(s.Color.B).Append(',').Append(s.Color.A).Append('|');
            if (fonts != null) foreach (var f in fonts) sb.Append(f).Append(';');
            sb.Append('|').Append(latex);
            return sb.ToString();
        }

        /// <summary>Lays out TeX without building a mesh.</summary>
        public static (DrawList list, Box box) LayoutOnly(string latex, bool display, IReadOnlyList<string> userFonts = null)
        {
            var fonts = MathFonts.Get(userFonts);
            var nodes = Parser.Parse(Normalize(latex), out var tag);
            var ts = new Typesetter(fonts);
            var box = ts.Layout(nodes, tag, ts.RootOptions(display, Rgba.White));
            var list = new DrawList();
            box.Emit(list, 0, 0);
            return (list, box);
        }

        public static EngineResult Generate(string latex, bool display, AssemblySettings settings, IReadOnlyList<string> userFonts = null)
        {
            latex = latex ?? "";
            var key = Key(latex, display, settings, userFonts);
            if (results.TryGet(key, out var cached))
                return new EngineResult
                {
                    Buffers = cached.Buffers, Width = cached.Width, Height = cached.Height, Depth = cached.Depth, ShapeCount = cached.ShapeCount, FromCache = true,
                };
            var sw = Stopwatch.StartNew();
            var fonts = MathFonts.Get(userFonts);
            var nodes = Parser.Parse(Normalize(latex), out var tag);
            double parse = sw.Elapsed.TotalMilliseconds;
            var ts = new Typesetter(fonts);
            var box = ts.Layout(nodes, tag, ts.RootOptions(display, Rgba.White));
            var list = new DrawList();
            box.Emit(list, 0, 0);
            double layout = sw.Elapsed.TotalMilliseconds - parse;
            int missesBefore = GeometryCache.GlyphMisses;
            var buffers = MeshAssembler.Build(list, settings);
            double mesh = sw.Elapsed.TotalMilliseconds - parse - layout;
            var result = new EngineResult
            {
                Buffers = buffers, Width = box.Width, Height = box.Height, Depth = box.Depth, ShapeCount = list.Items.Count,
                ParseMs = parse, LayoutMs = layout, MeshMs = mesh, NewGlyphs = GeometryCache.GlyphMisses - missesBefore,
            };
            if (buffers.VertexCount <= 200000) results.Add(key, result);
            return result;
        }
    }
}
