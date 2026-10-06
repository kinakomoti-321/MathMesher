using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MathMesher.Editor.Engine;
using Mesh = UnityEngine.Mesh;

namespace MathMesher.Editor
{
    public sealed class MathMeshResult
    {
        public Mesh Mesh { get; internal set; }
        public float AdvanceWidth { get; internal set; }
        public float Ascent { get; internal set; }
        public float Descent { get; internal set; }
        public int ShapeCount { get; internal set; }
        /// <summary>Time spent producing this result, in milliseconds (0 when served from the cache).</summary>
        public double Milliseconds { get; internal set; }
        /// <summary>True when the whole result came from the cache of recent formulas.</summary>
        public bool FromCache { get; internal set; }
    }

    /// <summary>Editor-only TeX math to vector outlines to Unity Mesh.</summary>
    public static class MathMeshGenerator
    {
        private static readonly VertexAttributeDescriptor[] layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        private static bool fontsConfigured;

        private static void ConfigureFonts()
        {
            if (fontsConfigured) return;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(MathMeshGenerator).Assembly);
            string root = info != null ? info.resolvedPath : System.IO.Path.GetFullPath("Packages/com.kinankomoti.mathmesher");
            MathFonts.BundledFontDirectory = System.IO.Path.Combine(root, "Editor", "Fonts");
            fontsConfigured = true;
        }

        public static MathMeshResult Generate(string latex, MathMeshSettings settings = null)
        {
            var result = new MathMeshResult { Mesh = new Mesh { name = "MathMesher Math" } };
            try { GenerateInto(result.Mesh, latex, settings, result); }
            catch { UnityEngine.Object.DestroyImmediate(result.Mesh); throw; }
            return result;
        }

        /// <summary>Generates into an existing Mesh, replacing its contents. Used for live preview without allocating meshes.</summary>
        public static MathMeshResult GenerateInto(Mesh mesh, string latex, MathMeshSettings settings = null, MathMeshResult result = null)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            settings = (settings ?? new MathMeshSettings()).Copy();
            ValidateSettings(settings);
            ConfigureFonts();
            var assembly = new AssemblySettings
            {
                Mode = settings.mode == MathMeshMode.Extruded ? AssemblyMode.Extruded : AssemblyMode.Flat,
                Alignment = (AssemblyAlignment)(int)settings.alignment,
                EmSize = settings.emSize,
                Depth = settings.depth,
                CurveTolerance = settings.curveTolerance,
                DoubleSided = settings.doubleSided,
                Color = new Rgba(settings.color.r, settings.color.g, settings.color.b, settings.color.a),
            };
            EngineResult engine;
            try { engine = TeXEngine.Generate(latex, settings.mathStyle == MathMeshStyle.Display, assembly, FallbackFontPaths(settings)); }
            catch (TeXException e) { throw new ArgumentException(e.Message, nameof(latex)); }
            Apply(mesh, engine.Buffers);
            result = result ?? new MathMeshResult();
            result.Mesh = mesh;
            result.AdvanceWidth = engine.Width * settings.emSize;
            result.Ascent = engine.Height * settings.emSize;
            result.Descent = engine.Depth * settings.emSize;
            result.ShapeCount = engine.ShapeCount;
            result.FromCache = engine.FromCache;
            result.Milliseconds = engine.ParseMs + engine.LayoutMs + engine.MeshMs;
            return result;
        }

        private static List<string> FallbackFontPaths(MathMeshSettings settings)
        {
            if (settings.fallbackFonts == null || settings.fallbackFonts.Length == 0) return null;
            var paths = new List<string>();
            foreach (var font in settings.fallbackFonts)
            {
                if (font == null) continue;
                var path = AssetDatabase.GetAssetPath(font);
                if (string.IsNullOrEmpty(path)) throw new ArgumentException("Fallback fonts must be .ttf / .otf font assets in the project.");
                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".ttf" && ext != ".otf") throw new ArgumentException("Fallback fonts must be .ttf or .otf files (" + System.IO.Path.GetFileName(path) + ").");
                paths.Add(System.IO.Path.GetFullPath(path));
            }
            return paths;
        }

        private static void Apply(Mesh mesh, MeshBuffers b)
        {
            mesh.Clear();
            if (b.VertexCount == 0) return;
            mesh.indexFormat = b.VertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertexBufferParams(b.VertexCount, layout);
            mesh.SetVertexBufferData(b.Vertices, 0, 0, b.VertexCount, 0, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
            mesh.SetIndexBufferParams(b.IndexCount, mesh.indexFormat);
            if (mesh.indexFormat == IndexFormat.UInt16)
            {
                var shorts = new ushort[b.IndexCount];
                for (int i = 0; i < shorts.Length; i++) shorts[i] = (ushort)b.Indices[i];
                mesh.SetIndexBufferData(shorts, 0, 0, b.IndexCount, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
            }
            else mesh.SetIndexBufferData(b.Indices, 0, 0, b.IndexCount, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
            mesh.subMeshCount = 1;
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(b.Min.X, b.Min.Y, b.Min.Z), new Vector3(b.Max.X, b.Max.Y, b.Max.Z));
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, b.IndexCount) { bounds = bounds, vertexCount = b.VertexCount }, MeshUpdateFlags.DontRecalculateBounds);
            mesh.bounds = bounds;
        }

        internal static string Normalize(string source) => TeXEngine.Normalize(source);

        /// <summary>Drops cached glyph geometry and recent results (e.g. after replacing a fallback font file).</summary>
        public static void ClearCache() => TeXEngine.ClearCaches();

        private static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        private static void ValidateSettings(MathMeshSettings settings)
        {
            if (!Finite(settings.emSize) || settings.emSize <= 0 || settings.emSize > 10000)
                throw new ArgumentException("Font size must be greater than 0 and at most 10000.");
            if (!Finite(settings.curveTolerance) || settings.curveTolerance < 0.0001f || settings.curveTolerance > 0.02f)
                throw new ArgumentException("Curve tolerance must be between 0.0001 and 0.02 em.");
            if (settings.mode == MathMeshMode.Extruded && (!Finite(settings.depth) || settings.depth <= 0 || settings.depth > 10000))
                throw new ArgumentException("Depth must be greater than 0 and at most 10000.");
        }
    }
}
