using System.Collections.Generic;

namespace MathMesher.Editor.Engine
{
    internal enum TextFont { Normal, Upright, Italic, Bold, BoldItalic, Sans, SansBold, SansItalic, Mono, SmallCaps, Slanted }

    internal abstract class Node
    {
        public int Pos;
        public bool TextMode;
    }

    /// <summary>A character or symbol command.</summary>
    internal sealed class SymbolNode : Node
    {
        public string Text;
        public AtomClass Class;
        public bool IsLetter;
        public bool IsDigit;
        public bool Upright; // e.g. \imath already mapped, \varGamma
    }

    internal sealed class GroupNode : Node
    {
        public List<Node> Body = new List<Node>();
        /// <summary>Explicit {...} groups are ordinary atoms; implicit ones are spliced into the parent.</summary>
        public bool IsOrd = true;
    }

    internal sealed class SupSubNode : Node
    {
        public Node Base;
        public List<Node> Sup, Sub;
    }

    internal sealed class FracNode : Node
    {
        public List<Node> Num, Den;
        public bool HasBar = true;
        public float? Thickness; // em
        public string LeftDelim, RightDelim;
        public int? Style;       // 0 display, 1 text, 2 script, 3 scriptscript
        public bool Continued;   // \cfrac
        public char ContinuedAlign = 'c';
        public bool Skewed;      // \sfrac, \nicefrac
    }

    internal sealed class SqrtNode : Node
    {
        public List<Node> Body, Index;
    }

    internal sealed class OpNode : Node
    {
        public string Symbol;        // big operator glyph text
        public string Name;          // \sin, \operatorname
        public List<Node> Body;      // \mathop{...}, \operatorname{...}
        public bool? Limits;
        public bool AlwaysLimits;    // \operatorname* etc. set via Limits
        public bool Integral;
        public string Decoration;
        public bool Small;           // \smallint
    }

    internal sealed class AccentNode : Node
    {
        public AccentSym Accent;
        public List<Node> Base;
    }

    internal sealed class LineNode : Node
    {
        public bool Over;
        public List<Node> Body;
    }

    internal sealed class HorizBraceNode : Node
    {
        public int Codepoint;
        public bool Over;
        public List<Node> Body;
    }

    /// <summary>\overset, \underset, \stackrel and labels of braces.</summary>
    internal sealed class StackNode : Node
    {
        public List<Node> Base, Over, Under;
        public AtomClass Class = AtomClass.Ord;
        public bool BaseIsScriptStyleOver = true;
    }

    internal sealed class LeftRightNode : Node
    {
        public string Left, Right;
        public List<Node> Body;
    }

    internal sealed class MiddleNode : Node
    {
        public string Delim;
    }

    internal sealed class DelimSizingNode : Node
    {
        public string Delim;
        public int Size;
        public AtomClass Class;
    }

    internal sealed class StyleNode : Node
    {
        public int Style;
        public List<Node> Body;
    }

    internal sealed class SizeNode : Node
    {
        public float Scale;
        public List<Node> Body;
    }

    internal sealed class FontNode : Node
    {
        public MathVariant Variant;
        public bool BoldSymbol;   // \boldsymbol / \bm: bold variants, otherwise fake bold
        public bool PoorMansBold; // \pmb
        public List<Node> Body;
    }

    internal sealed class ColorNode : Node
    {
        public Rgba Color;
        public List<Node> Body;
    }

    internal sealed class TextNode : Node
    {
        public TextFont Font;
        public List<Node> Body = new List<Node>();
    }

    internal sealed class TextSpaceNode : Node { }

    /// <summary>Math inside text ($...$).</summary>
    internal sealed class MathInTextNode : Node
    {
        public List<Node> Body;
    }

    internal sealed class SpaceNode : Node
    {
        public float Amount;
        public bool Mu;
    }

    internal sealed class PhantomNode : Node
    {
        public List<Node> Body;
        public bool KeepWidth = true, KeepHeight = true;
    }

    internal sealed class SmashNode : Node
    {
        public List<Node> Body;
        public bool Top = true, Bottom = true;
    }

    internal sealed class LapNode : Node
    {
        public List<Node> Body;
        public int Align; // -1 llap, 0 clap, 1 rlap
    }

    internal sealed class RaiseNode : Node
    {
        public float Dy; // em
        public List<Node> Body;
    }

    internal sealed class RuleNode : Node
    {
        public float Width, Height, Shift; // em
    }

    internal sealed class VCenterNode : Node
    {
        public List<Node> Body;
    }

    internal sealed class EncloseNode : Node
    {
        public string Kind; // box, colorbox, fcolorbox, cancel, bcancel, xcancel, sout, angl, circle, roundedbox, top, bottom, left, right, phase, longdiv, radical
        public HashSet<string> Notations;
        public List<Node> Body;
        public Rgba? Background, Frame;
        public float? Padding, LineWidth;
        public List<Node> CancelTo;
        public bool TextBody;
    }

    internal sealed class ArrayCell
    {
        public List<Node> Body = new List<Node>();
        public int Span = 1;
        public char? Align;
    }

    internal enum HLineKind { None, Solid, Dashed }

    internal sealed class ColumnSpec
    {
        public char Align = 'c';    // l c r
        public float? Width;        // p{...}
        public List<HLineKind> LeftRules = new List<HLineKind>(); // vertical rules before the column
        public List<Node> Before;   // @{...} / !{...} material before the column (replaces the column gap)
    }

    internal sealed class ArrayNode : Node
    {
        public string Env;
        public List<List<ArrayCell>> Rows = new List<List<ArrayCell>>();
        public List<ColumnSpec> Columns;           // explicit spec (array) or null
        public List<HLineKind> TrailingRules = new List<HLineKind>(); // vertical rules after the last column
        public List<List<HLineKind>> HLines = new List<List<HLineKind>>(); // [rowBoundary] lines above row i (Count = rows + 1)
        public List<List<(int from, int to, HLineKind kind)>> CLines = new List<List<(int, int, HLineKind)>>();
        public List<float> RowGaps = new List<float>();  // extra gap after row i (em)
        public List<List<Node>> Tags = new List<List<Node>>();
        public float ArrayStretch = 1f;
        public float ColSep = 0.5f;        // \arraycolsep (each side)
        public bool OuterColSep = true;     // hskip before and after
        public bool AddJot;
        public int? CellStyle;              // forced style for cells (0 D, 1 T, 2 S)
        public string ColumnMode;           // "align", "alignat", "gather", "multline", "cases", "matrix", "array", "CD"
        public char DefaultAlign = 'c';
        public float AlignGap = 2f;         // gap between column pairs in align-like environments (em)
        public char VAlign = 'c';           // t, c, b
        public bool LeftTagAlign;
        public HashSet<int> TextColumns;
    }

    internal sealed class XArrowNode : Node
    {
        public int Codepoint;
        public string Kind; // special: "longequal", "rightleftharpoons", "leftrightharpoons", "tofrom", ...
        public List<Node> Above, Below;
    }

    internal sealed class ClassNode : Node
    {
        public AtomClass Class;
        public List<Node> Body;
    }

    internal sealed class MathChoiceNode : Node
    {
        public List<Node> Display, Text, Script, ScriptScript;
    }

    internal sealed class NotNode : Node
    {
        public Node Base;
    }

    internal sealed class SideSetNode : Node
    {
        public List<Node> LeftSub, LeftSup, RightSub, RightSup;
        public Node Base;
    }

    /// <summary>A tagged equation: body plus "(tag)".</summary>
    internal sealed class TagNode : Node
    {
        public Node Body;
        public List<Node> Tag;
    }

    /// <summary>Cell content in amscd: horizontal or vertical arrows with labels.</summary>
    internal sealed class CDArrowNode : Node
    {
        public char Kind; // > < V A = | . (empty)
        public List<Node> Above, Below;
    }

    internal sealed class HDotsForNode : Node
    {
        public int Columns;
        public float Spacing = 1f;
    }
}
