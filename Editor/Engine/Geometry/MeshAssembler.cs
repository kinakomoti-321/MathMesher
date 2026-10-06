using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MathMesher.Editor.Engine
{
    internal struct V3
    {
        public float X, Y, Z;
        public V3(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>Interleaved vertex matching the Unity vertex layout Position, Normal, Color (float4), TexCoord0.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MeshVertex
    {
        public V3 Position;
        public V3 Normal;
        public Rgba Color;
        public Vec2 UV;
    }

    internal sealed class MeshBuffers
    {
        public MeshVertex[] Vertices = Array.Empty<MeshVertex>();
        public int[] Indices = Array.Empty<int>();
        public int VertexCount, IndexCount;
        public V3 Min, Max;
        public int Chunks, ChunkHits;
    }

    internal enum AssemblyMode { Flat, Extruded }
    internal enum AssemblyAlignment { BaselineLeft, BottomLeft, Center, TopLeft }

    internal struct AssemblySettings
    {
        public AssemblyMode Mode;
        public AssemblyAlignment Alignment;
        public float EmSize, Depth, CurveTolerance;
        public bool DoubleSided;
        public Rgba Color;
    }

    /// <summary>
    /// Turns a draw list into mesh buffers. Each placed glyph/polygon becomes a reusable chunk (triangles, back face and walls
    /// in local coordinates); chunks are cached, so after an edit only new or changed pieces are triangulated and built.
    /// </summary>
    internal static class MeshAssembler
    {
        public const int MaxVertices = 1000000;

        private sealed class Chunk
        {
            public V3[] Positions;
            public V3[] Normals;
            public int[] Indices;
            public V3 Min, Max;
        }

        private struct ChunkKey : IEquatable<ChunkKey>
        {
            public int Geometry;
            public float ScaleX, ScaleY, Z0, Z1;
            public byte Faces; // 1 front, 2 back, 4 walls
            public bool Equals(ChunkKey o) => Geometry == o.Geometry && ScaleX == o.ScaleX && ScaleY == o.ScaleY && Z0 == o.Z0 && Z1 == o.Z1 && Faces == o.Faces;
            public override bool Equals(object obj) => obj is ChunkKey k && Equals(k);
            public override int GetHashCode()
            {
                unchecked
                {
                    int h = Geometry;
                    h = h * 397 ^ ScaleX.GetHashCode();
                    h = h * 397 ^ ScaleY.GetHashCode();
                    h = h * 397 ^ Z0.GetHashCode();
                    h = h * 397 ^ Z1.GetHashCode();
                    return h * 397 ^ Faces;
                }
            }
        }

        private static readonly LruCache<ChunkKey, Chunk> chunks = new LruCache<ChunkKey, Chunk>(20000);

        public static void ClearCache() => chunks.Clear();
        public static int ChunkCount => chunks.Count;

        private static (float z0, float z1, byte faces) Layering(AssemblySettings s, int layer, bool hasBackground)
        {
            float eps = Math.Min(0.002f * s.EmSize, s.Mode == AssemblyMode.Extruded ? s.Depth * 0.2f : float.MaxValue);
            if (s.Mode == AssemblyMode.Extruded)
            {
                if (layer == 0) return (0, s.Depth, 7);
                return (eps, Math.Max(eps * 1.5f, s.Depth - eps), 7);
            }
            if (layer == 0) return (0, hasBackground ? 2 * eps : 0, (byte)(s.DoubleSided ? 3 : 1));
            return (eps, eps, (byte)(s.DoubleSided ? 3 : 1));
        }

        public static MeshBuffers Build(DrawList list, AssemblySettings s)
        {
            bool hasBackground = false;
            foreach (var item in list.Items) if (item.Layer != 0) { hasBackground = true; break; }
            float em = s.EmSize;
            var placed = new List<(Chunk chunk, float x, float y, Rgba color)>(list.Items.Count);
            int vertexCount = 0, indexCount = 0, hits = 0;
            foreach (var item in list.Items)
            {
                var (z0, z1, faces) = Layering(s, item.Layer, hasBackground);
                Chunk chunk;
                float x = item.X * em, y = item.Y * em;
                switch (item.Kind)
                {
                    case DrawKind.Glyph:
                        {
                            float scale = Math.Max(Math.Abs(item.ScaleX), Math.Abs(item.ScaleY));
                            if (scale <= 0) continue;
                            int bucket = GeometryCache.Bucket(s.CurveTolerance / scale);
                            var geometry = GeometryCache.Glyph(item.Glyph, bucket);
                            if (geometry.Triangles.Length == 0) continue;
                            chunk = GetChunk(geometry, item.ScaleX * em, item.ScaleY * em, z0, z1, faces, ref hits);
                            break;
                        }
                    case DrawKind.Polygon:
                        {
                            var geometry = GeometryCache.Polygon(item.Polygon);
                            if (geometry.Triangles.Length == 0) continue;
                            chunk = GetChunk(geometry, item.ScaleX * em, item.ScaleY * em, z0, z1, faces, ref hits);
                            break;
                        }
                    default:
                        {
                            float w = (item.X1 - item.X) * em, h = (item.Y1 - item.Y) * em;
                            if (w <= 0 || h <= 0) continue;
                            chunk = BuildChunk(new ShapeGeometry
                            {
                                Vertices = new[] { new Vec2(0, 0), new Vec2(w, 0), new Vec2(w, h), new Vec2(0, h) },
                                Triangles = new[] { 0, 1, 2, 0, 2, 3 },
                                Contours = new[] { new[] { new Vec2(0, 0), new Vec2(w, 0), new Vec2(w, h), new Vec2(0, h) } },
                                MinX = 0, MinY = 0, MaxX = w, MaxY = h,
                            }, 1, 1, z0, z1, faces);
                            break;
                        }
                }
                vertexCount += chunk.Positions.Length;
                indexCount += chunk.Indices.Length;
                if (vertexCount > MaxVertices)
                    throw new TeXException("Mesh の頂点数が上限を超えています。数式を分割するか、曲線の許容誤差を大きくしてください。");
                placed.Add((chunk, x, y, Mul(item.Color, s.Color)));
            }

            var result = new MeshBuffers { Vertices = new MeshVertex[vertexCount], Indices = new int[indexCount], Chunks = placed.Count, ChunkHits = hits };
            if (placed.Count == 0) return result;
            V3 min = new V3(float.MaxValue, float.MaxValue, float.MaxValue), max = new V3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var (chunk, x, y, _) in placed)
            {
                min.X = Math.Min(min.X, chunk.Min.X + x); min.Y = Math.Min(min.Y, chunk.Min.Y + y); min.Z = Math.Min(min.Z, chunk.Min.Z);
                max.X = Math.Max(max.X, chunk.Max.X + x); max.Y = Math.Max(max.Y, chunk.Max.Y + y); max.Z = Math.Max(max.Z, chunk.Max.Z);
            }
            V3 offset = default;
            switch (s.Alignment)
            {
                case AssemblyAlignment.BottomLeft: offset = new V3(min.X, min.Y, 0); break;
                case AssemblyAlignment.Center: offset = new V3((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2); break;
                case AssemblyAlignment.TopLeft: offset = new V3(min.X, max.Y, 0); break;
            }
            int v = 0, idx = 0;
            var verts = result.Vertices;
            var indices = result.Indices;
            foreach (var (chunk, x, y, color) in placed)
            {
                float dx = x - offset.X, dy = y - offset.Y, dz = -offset.Z;
                var p = chunk.Positions;
                var n = chunk.Normals;
                for (int i = 0; i < p.Length; i++)
                {
                    float px = p[i].X + dx, py = p[i].Y + dy;
                    verts[v + i] = new MeshVertex { Position = new V3(px, py, p[i].Z + dz), Normal = n[i], Color = color, UV = new Vec2(px, py) };
                }
                var ci = chunk.Indices;
                for (int i = 0; i < ci.Length; i++) indices[idx + i] = ci[i] + v;
                v += p.Length;
                idx += ci.Length;
            }
            result.VertexCount = v;
            result.IndexCount = idx;
            result.Min = new V3(min.X - offset.X, min.Y - offset.Y, min.Z - offset.Z);
            result.Max = new V3(max.X - offset.X, max.Y - offset.Y, max.Z - offset.Z);
            return result;
        }

        private static Rgba Mul(Rgba a, Rgba b) => new Rgba(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);

        private static Chunk GetChunk(ShapeGeometry g, float sx, float sy, float z0, float z1, byte faces, ref int hits)
        {
            var key = new ChunkKey { Geometry = g.Id, ScaleX = sx, ScaleY = sy, Z0 = z0, Z1 = z1, Faces = faces };
            if (chunks.TryGet(key, out var chunk)) { hits++; return chunk; }
            chunk = BuildChunk(g, sx, sy, z0, z1, faces);
            chunks.Add(key, chunk);
            return chunk;
        }

        private static Chunk BuildChunk(ShapeGeometry g, float sx, float sy, float z0, float z1, byte faces)
        {
            var pos = new List<V3>();
            var nrm = new List<V3>();
            var idx = new List<int>();
            var tv = g.Vertices;
            if ((faces & 1) != 0) AddFace(tv, g.Triangles, sx, sy, z0, -1, pos, nrm, idx);
            if ((faces & 2) != 0) AddFace(tv, g.Triangles, sx, sy, z1, 1, pos, nrm, idx);
            if ((faces & 4) != 0)
                foreach (var loop in g.Contours) AddWalls(loop, sx, sy, z0, z1, pos, nrm, idx);
            var chunk = new Chunk { Positions = pos.ToArray(), Normals = nrm.ToArray(), Indices = idx.ToArray() };
            V3 min = new V3(float.MaxValue, float.MaxValue, float.MaxValue), max = new V3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var p in chunk.Positions)
            {
                min.X = Math.Min(min.X, p.X); min.Y = Math.Min(min.Y, p.Y); min.Z = Math.Min(min.Z, p.Z);
                max.X = Math.Max(max.X, p.X); max.Y = Math.Max(max.Y, p.Y); max.Z = Math.Max(max.Z, p.Z);
            }
            if (chunk.Positions.Length == 0) { min = default; max = default; }
            chunk.Min = min; chunk.Max = max;
            return chunk;
        }

        private static void AddFace(Vec2[] v, int[] tris, float sx, float sy, float z, float nz, List<V3> pos, List<V3> nrm, List<int> idx)
        {
            int offset = pos.Count;
            var normal = new V3(0, 0, nz);
            for (int i = 0; i < v.Length; i++) { pos.Add(new V3(v[i].X * sx, v[i].Y * sy, z)); nrm.Add(normal); }
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                float cross = (v[b].X - v[a].X) * sx * (v[c].Y - v[a].Y) * sy - (v[b].Y - v[a].Y) * sy * (v[c].X - v[a].X) * sx;
                if (Math.Abs(cross) < 1e-14f) continue;
                // emitted triangles satisfy cross(b - a, c - a) . normal > 0 (Unity front faces)
                idx.Add(offset + a);
                if (cross * nz > 0) { idx.Add(offset + b); idx.Add(offset + c); }
                else { idx.Add(offset + c); idx.Add(offset + b); }
            }
        }

        private static void AddWalls(Vec2[] loop, float sx, float sy, float z0, float z1, List<V3> pos, List<V3> nrm, List<int> idx)
        {
            int n = loop.Length;
            for (int i = 0; i < n; i++)
            {
                float ax = loop[i].X * sx, ay = loop[i].Y * sy;
                float bx = loop[(i + 1) % n].X * sx, by = loop[(i + 1) % n].Y * sy;
                float dx = bx - ax, dy = by - ay;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-7f) continue;
                var normal = new V3(dy / len, -dx / len, 0);
                int v = pos.Count;
                pos.Add(new V3(ax, ay, z0)); pos.Add(new V3(bx, by, z0)); pos.Add(new V3(bx, by, z1)); pos.Add(new V3(ax, ay, z1));
                nrm.Add(normal); nrm.Add(normal); nrm.Add(normal); nrm.Add(normal);
                Quad(pos, idx, v, normal);
            }
        }

        private static void Quad(List<V3> pos, List<int> idx, int v, V3 normal)
        {
            Tri(pos, idx, v, v + 1, v + 2, normal);
            Tri(pos, idx, v, v + 2, v + 3, normal);
        }

        private static void Tri(List<V3> pos, List<int> idx, int a, int b, int c, V3 n)
        {
            var A = pos[a]; var B = pos[b]; var Cc = pos[c];
            float ux = B.X - A.X, uy = B.Y - A.Y, uz = B.Z - A.Z, vx = Cc.X - A.X, vy = Cc.Y - A.Y, vz = Cc.Z - A.Z;
            float cx = uy * vz - uz * vy, cy = uz * vx - ux * vz, cz = ux * vy - uy * vx;
            float dot = cx * n.X + cy * n.Y + cz * n.Z;
            if (Math.Abs(dot) < 1e-20f) return;
            idx.Add(a);
            if (dot > 0) { idx.Add(b); idx.Add(c); }
            else { idx.Add(c); idx.Add(b); }
        }
    }
}
