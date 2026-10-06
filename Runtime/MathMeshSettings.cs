using System;
using UnityEngine;

namespace MathMesher
{
    public enum MathMeshMode { Flat, Extruded }

    public enum MathMeshAlignment
    {
        [InspectorName("Baseline left")] BaselineLeft,
        [InspectorName("Bottom left")] BottomLeft,
        Center,
        [InspectorName("Top left")] TopLeft,
    }

    public enum MathMeshStyle { Display, Inline }

    [Serializable]
    public sealed class MathMeshSettings
    {
        [Tooltip("Flat: outlines only. Extruded: a solid with front, back and side faces.")]
        public MathMeshMode mode = MathMeshMode.Flat;
        [Tooltip("Display: large operators and limits as in a displayed equation. Inline: compact, as in running text.")]
        public MathMeshStyle mathStyle = MathMeshStyle.Display;
        [Tooltip("Which point of the formula is placed at the object's origin.")]
        public MathMeshAlignment alignment = MathMeshAlignment.Center;
        [Tooltip("Font size in Unity units (the size of 1 em). Ordinary letters are about half of it tall.")]
        [Min(0.001f)] public float emSize = 1f;
        [Tooltip("Thickness of the extruded solid in Unity units.")]
        [Min(0.0001f)] public float depth = 0.15f;
        [Tooltip("Maximum deviation of curves from the true outline, in em. Smaller is smoother but uses more vertices.")]
        [Range(0.0001f, 0.02f)] public float curveTolerance = 0.002f;
        [Tooltip("Also generate the back face of a flat mesh.")]
        public bool doubleSided = true;
        [Tooltip("Vertex color, multiplied with colors in the formula.")]
        public Color color = Color.white;
        [Tooltip(".ttf / .otf fonts used, in order, for characters missing from the bundled fonts (e.g. Japanese). Only used in the Editor.")]
        public Font[] fallbackFonts = new Font[0];

        public MathMeshSettings Copy() => (MathMeshSettings)MemberwiseClone();
    }
}
