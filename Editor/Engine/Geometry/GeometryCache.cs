using System;
using System.Collections.Generic;
using MathMesher.Internal.LibTessDotNet;

namespace MathMesher.Editor.Engine
{
    /// <summary>Triangulated outline of one glyph or polygon in its local em coordinates.</summary>
    internal sealed class ShapeGeometry
    {
        public Vec2[] Vertices;
        public int[] Triangles;
        /// <summary>Union boundary loops (outer counter-clockwise, holes clockwise).</summary>
        public Vec2[][] Contours;
        public float MinX, MinY, MaxX, MaxY;
        public int Id;
    }

    /// <summary>Small LRU cache.</summary>
    internal sealed class LruCache<TKey, TValue>
    {
        private readonly int capacity;
        private readonly Dictionary<TKey, LinkedListNode<(TKey key, TValue value)>> map;
        private readonly LinkedList<(TKey key, TValue value)> order = new LinkedList<(TKey, TValue)>();
        public int Hits, Misses;

        public LruCache(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            this.capacity = capacity;
            map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(comparer ?? EqualityComparer<TKey>.Default);
        }

        public int Count => map.Count;

        public bool TryGet(TKey key, out TValue value)
        {
            if (map.TryGetValue(key, out var node))
            {
                order.Remove(node);
                order.AddFirst(node);
                value = node.Value.value;
                Hits++;
                return true;
            }
            Misses++;
            value = default;
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            if (map.TryGetValue(key, out var existing)) { order.Remove(existing); map.Remove(key); }
            var node = order.AddFirst((key, value));
            map[key] = node;
            while (map.Count > capacity)
            {
                var last = order.Last;
                order.RemoveLast();
                map.Remove(last.Value.key);
            }
        }

        public void Clear() { map.Clear(); order.Clear(); Hits = 0; Misses = 0; }
    }

    /// <summary>Caches glyph and polygon triangulations. Shared by every MathMesh in the Editor session.</summary>
    internal static class GeometryCache
    {
        private static readonly LruCache<(int face, ushort glyph, int bucket), ShapeGeometry> glyphs =
            new LruCache<(int, ushort, int), ShapeGeometry>(6000);
        private static readonly LruCache<string, ShapeGeometry> polygons = new LruCache<string, ShapeGeometry>(1000, StringComparer.Ordinal);
        private static int nextId;
        public const int MaxOutlinePoints = 400000;

        public static int GlyphCount => glyphs.Count;
        public static int GlyphHits => glyphs.Hits;
        public static int GlyphMisses => glyphs.Misses;

        public static void Clear()
        {
            glyphs.Clear();
            polygons.Clear();
        }

        /// <summary>Quantizes a tolerance to a power-of-2^(1/4) bucket that is never coarser than requested.</summary>
        public static int Bucket(float tolerance) => (int)Math.Floor(Math.Log(Math.Max(1e-6, tolerance), 2) * 4);
        public static float BucketTolerance(int bucket) => (float)Math.Pow(2, bucket / 4.0);

        public static ShapeGeometry Glyph(GlyphId g, int bucket)
        {
            var key = (g.Face.Id, g.Index, bucket);
            if (glyphs.TryGet(key, out var geometry)) return geometry;
            var contours = g.Face.ReadOutline(g.Index, BucketTolerance(bucket));
            int points = 0;
            foreach (var c in contours) points += c.Count;
            if (points > MaxOutlinePoints) throw new TeXException("文字の輪郭が複雑すぎます。曲線の許容誤差を大きくしてください。");
            geometry = Build(contours);
            glyphs.Add(key, geometry);
            return geometry;
        }

        public static ShapeGeometry Polygon(Polygon p)
        {
            var key = p.Key;
            if (polygons.TryGet(key, out var geometry)) return geometry;
            geometry = Build(p.Contours);
            polygons.Add(key, geometry);
            return geometry;
        }

        private static Tess Tessellate(IEnumerable<IList<Vec2>> contours, ElementType type)
        {
            var tess = new Tess();
            foreach (var contour in contours)
            {
                var points = new ContourVertex[contour.Count];
                for (int i = 0; i < points.Length; i++) points[i].Position = new Vec3(contour[i].X, contour[i].Y, 0);
                tess.AddContour(points);
            }
            tess.Tessellate(WindingRule.NonZero, type, 3, null, new Vec3(0, 0, 1));
            return tess;
        }

        private static ShapeGeometry Build(List<List<Vec2>> source)
        {
            var geometry = new ShapeGeometry { Id = System.Threading.Interlocked.Increment(ref nextId) };
            if (source.Count == 0)
            {
                geometry.Vertices = Array.Empty<Vec2>();
                geometry.Triangles = Array.Empty<int>();
                geometry.Contours = Array.Empty<Vec2[]>();
                return geometry;
            }
            var lists = new List<IList<Vec2>>();
            foreach (var c in source) lists.Add(c);
            var boundary = Tessellate(lists, ElementType.BoundaryContours);
            var contours = new List<Vec2[]>();
            for (int i = 0; i < boundary.ElementCount; i++)
            {
                int start = boundary.Elements[i * 2], count = boundary.Elements[i * 2 + 1];
                var loop = new Vec2[count];
                for (int j = 0; j < count; j++)
                {
                    var p = boundary.Vertices[start + j].Position;
                    loop[j] = new Vec2(p.X, p.Y);
                }
                contours.Add(loop);
            }
            geometry.Contours = contours.ToArray();
            if (contours.Count == 0)
            {
                geometry.Vertices = Array.Empty<Vec2>();
                geometry.Triangles = Array.Empty<int>();
                return geometry;
            }
            var faces = Tessellate(contours.ConvertAll(c => (IList<Vec2>)c), ElementType.Polygons);
            geometry.Vertices = new Vec2[faces.VertexCount];
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < faces.VertexCount; i++)
            {
                var p = faces.Vertices[i].Position;
                geometry.Vertices[i] = new Vec2(p.X, p.Y);
                minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y); maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y);
            }
            var tris = new List<int>(faces.ElementCount * 3);
            for (int i = 0; i < faces.ElementCount; i++)
            {
                int a = faces.Elements[i * 3], b = faces.Elements[i * 3 + 1], c = faces.Elements[i * 3 + 2];
                if (a == Tess.Undef || b == Tess.Undef || c == Tess.Undef) continue;
                tris.Add(a); tris.Add(b); tris.Add(c);
            }
            geometry.Triangles = tris.ToArray();
            geometry.MinX = minX; geometry.MinY = minY; geometry.MaxX = maxX; geometry.MaxY = maxY;
            return geometry;
        }
    }
}
