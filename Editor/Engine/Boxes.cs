using System;
using System.Collections.Generic;

namespace MathMesher.Editor.Engine
{
    internal enum DrawKind { Glyph, Rect, Polygon }

    /// <summary>A positioned primitive in em units (y up, baseline at 0).</summary>
    internal struct DrawItem
    {
        public DrawKind Kind;
        public GlyphId Glyph;
        public float X, Y, ScaleX, ScaleY;   // glyph origin and scale; rect min corner
        public float X1, Y1;                 // rect max corner
        public Polygon Polygon;              // local polygon (absolute coordinates are X/Y + points)
        public Rgba Color;
        public int Layer;                    // 0: content, 1: background behind content
    }

    /// <summary>A filled shape made of closed contours, combined with the non-zero rule.</summary>
    internal sealed class Polygon
    {
        public readonly List<List<Vec2>> Contours = new List<List<Vec2>>();
        private string key;

        /// <summary>Content key used by the geometry cache.</summary>
        public string Key
        {
            get
            {
                if (key != null) return key;
                var sb = new System.Text.StringBuilder();
                foreach (var c in Contours)
                {
                    foreach (var p in c) sb.Append(Math.Round(p.X, 5)).Append(',').Append(Math.Round(p.Y, 5)).Append(';');
                    sb.Append('|');
                }
                return key = sb.ToString();
            }
        }

        public static Polygon Quad(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        {
            var p = new Polygon();
            p.Contours.Add(new List<Vec2> { a, b, c, d });
            return p;
        }

        /// <summary>A straight stroke of the given thickness.</summary>
        public static Polygon Line(float x1, float y1, float x2, float y2, float thickness)
        {
            float dx = x2 - x1, dy = y2 - y1;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6f) return null;
            float nx = -dy / len * thickness * 0.5f, ny = dx / len * thickness * 0.5f;
            return Quad(new Vec2(x1 + nx, y1 + ny), new Vec2(x2 + nx, y2 + ny), new Vec2(x2 - nx, y2 - ny), new Vec2(x1 - nx, y1 - ny));
        }
    }

    internal sealed class DrawList
    {
        public readonly List<DrawItem> Items = new List<DrawItem>();
    }

    internal class Box
    {
        public float Width, Height, Depth;
        /// <summary>Italic correction of the final glyph (already included in Width for math italic letters).</summary>
        public float Italic;
        public bool IsCharBox;
        public List<(Box box, float x, float y)> Children;

        public Box() { }
        public Box(float width, float height, float depth) { Width = width; Height = height; Depth = depth; }

        public void Add(Box child, float x, float y)
        {
            if (child == null) return;
            if (Children == null) Children = new List<(Box, float, float)>();
            Children.Add((child, x, y));
        }

        /// <summary>Recomputes Height/Depth (and Width when growWidth) from children.</summary>
        public void Fit(bool growWidth = true)
        {
            if (Children == null) return;
            float h = float.NegativeInfinity, d = float.NegativeInfinity, w = 0;
            foreach (var (b, x, y) in Children)
            {
                h = Math.Max(h, b.Height + y);
                d = Math.Max(d, b.Depth - y);
                w = Math.Max(w, x + b.Width);
            }
            Height = Children.Count == 0 ? 0 : h;
            Depth = Children.Count == 0 ? 0 : d;
            if (growWidth) Width = Math.Max(Width, w);
        }

        public virtual void Emit(DrawList list, float x, float y)
        {
            if (Children == null) return;
            foreach (var (b, cx, cy) in Children) b.Emit(list, x + cx, y + cy);
        }

        /// <summary>Horizontal list: places children left to right at the given vertical shifts.</summary>
        public static Box HBox(IEnumerable<Box> boxes)
        {
            var box = new Box();
            float x = 0;
            foreach (var b in boxes)
            {
                if (b == null) continue;
                box.Add(b, x, 0);
                x += b.Width;
            }
            box.Width = x;
            box.Fit(false);
            if (box.Children == null) { box.Height = 0; box.Depth = 0; }
            return box;
        }

        public static Box Kern(float width) => new Box(width, 0, 0);
    }

    internal sealed class GlyphBox : Box
    {
        public readonly GlyphId Glyph;
        public readonly float Scale;
        public float ScaleX = 1, ScaleY = 1; // extra stretch for fallback constructions
        public readonly Rgba Color;
        public int Layer;

        public GlyphBox(GlyphId glyph, float scale, Rgba color)
        {
            Glyph = glyph;
            Scale = scale;
            Color = color;
            var m = glyph.Metrics;
            Width = m.Advance * scale;
            Height = m.Height * scale;
            Depth = m.Depth * scale;
            Italic = m.Italic * scale;
            IsCharBox = true;
        }

        public override void Emit(DrawList list, float x, float y)
        {
            list.Items.Add(new DrawItem
            {
                Kind = DrawKind.Glyph, Glyph = Glyph, X = x, Y = y, ScaleX = Scale * ScaleX, ScaleY = Scale * ScaleY, Color = Color, Layer = Layer
            });
        }
    }

    internal sealed class RuleBox : Box
    {
        public readonly Rgba Color;
        public int Layer;
        public RuleBox(float width, float height, float depth, Rgba color) : base(width, height, depth) { Color = color; }

        public override void Emit(DrawList list, float x, float y)
        {
            if (Width <= 0 || Height + Depth <= 0) return;
            list.Items.Add(new DrawItem { Kind = DrawKind.Rect, X = x, Y = y - Depth, X1 = x + Width, Y1 = y + Height, Color = Color, Layer = Layer });
        }
    }

    internal sealed class PolygonBox : Box
    {
        public readonly Polygon Shape;
        public readonly Rgba Color;
        public PolygonBox(Polygon shape, Rgba color, float width, float height, float depth) : base(width, height, depth)
        {
            Shape = shape;
            Color = color;
        }

        public override void Emit(DrawList list, float x, float y)
        {
            if (Shape == null) return;
            list.Items.Add(new DrawItem { Kind = DrawKind.Polygon, Polygon = Shape, X = x, Y = y, ScaleX = 1, ScaleY = 1, Color = Color });
        }
    }

    /// <summary>Keeps the metrics of its child but draws nothing (\phantom).</summary>
    internal sealed class PhantomBox : Box
    {
        public PhantomBox(Box inner, bool keepWidth, bool keepHeight)
        {
            Width = keepWidth ? inner.Width : 0;
            Height = keepHeight ? inner.Height : 0;
            Depth = keepHeight ? inner.Depth : 0;
        }
        public override void Emit(DrawList list, float x, float y) { }
    }
}
