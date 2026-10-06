using System;

namespace MathMesher.Editor.Engine
{
    /// <summary>Unity-independent 2D point in em units (y up).</summary>
    internal struct Vec2 : IEquatable<Vec2>
    {
        public float X, Y;
        public Vec2(float x, float y) { X = x; Y = y; }
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);
        public float SqrMagnitude => X * X + Y * Y;
        public bool Equals(Vec2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => (X, Y).GetHashCode();
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>Linear 0..1 RGBA color.</summary>
    internal struct Rgba : IEquatable<Rgba>
    {
        public float R, G, B, A;
        public Rgba(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
        public static readonly Rgba White = new Rgba(1, 1, 1, 1);
        public static Rgba FromBytes(int r, int g, int b) => new Rgba(r / 255f, g / 255f, b / 255f, 1f);
        public bool Equals(Rgba o) => R == o.R && G == o.G && B == o.B && A == o.A;
        public override bool Equals(object obj) => obj is Rgba c && Equals(c);
        public override int GetHashCode() => (R, G, B, A).GetHashCode();
        public static Rgba Mix(Rgba a, Rgba b, float t) =>
            new Rgba(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t, a.A + (b.A - a.A) * t);
    }

    /// <summary>A user-facing TeX error. Messages are Japanese to match the Inspector.</summary>
    internal sealed class TeXException : ArgumentException
    {
        public int Position { get; }
        public TeXException(string message, int position = -1) : base(message) { Position = position; }
    }
}
