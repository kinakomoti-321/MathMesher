namespace MathMesher.Editor.Engine
{
    /// <summary>TeX math style: D, D', T, T', S, S', SS, SS'.</summary>
    internal readonly struct MathStyle
    {
        public readonly int Id;
        public MathStyle(int id) { Id = id; }
        public static readonly MathStyle Display = new MathStyle(0);
        public static readonly MathStyle Text = new MathStyle(2);
        public static readonly MathStyle Script = new MathStyle(4);
        public static readonly MathStyle ScriptScript = new MathStyle(6);

        public int Size => Id >> 1;           // 0 display, 1 text, 2 script, 3 scriptscript
        public bool Cramped => (Id & 1) != 0;
        public bool IsDisplay => Size == 0;
        public bool IsScript => Size >= 2;

        private static readonly int[] sup = { 4, 5, 4, 5, 6, 7, 6, 7 };
        private static readonly int[] sub = { 5, 5, 5, 5, 7, 7, 7, 7 };
        private static readonly int[] num = { 2, 3, 4, 5, 6, 7, 6, 7 };
        private static readonly int[] den = { 3, 3, 5, 5, 7, 7, 7, 7 };
        private static readonly int[] text = { 0, 1, 2, 3, 4, 5, 6, 7 };

        public MathStyle Sup() => new MathStyle(sup[Id]);
        public MathStyle Sub() => new MathStyle(sub[Id]);
        public MathStyle FracNum() => new MathStyle(num[Id]);
        public MathStyle FracDen() => new MathStyle(den[Id]);
        public MathStyle Cramp() => new MathStyle(Id | 1);
        public MathStyle WithSize(int size) => new MathStyle(size * 2 + (Id & 1));
        public MathStyle Text_() => new MathStyle(text[Id]);
    }

    internal sealed class Options
    {
        public FontSet Fonts;
        public MathConstantsEm C;
        public MathStyle Style = MathStyle.Display;
        public float SizeScale = 1f;
        public MathVariant Variant = MathVariant.Default;
        public TextFont TextFont = TextFont.Normal;
        public bool BoldSymbol, PoorMansBold;
        public bool TextMode;
        public Rgba Color = Rgba.White;
        public bool InOperatorName;

        public float StyleScale => Style.Size switch { 2 => C.ScriptScale, 3 => C.ScriptScriptScale, _ => 1f };
        public float Scale => SizeScale * StyleScale;
        /// <summary>One math unit (1/18 em) at the current size.</summary>
        public float Mu => Scale / 18f;

        public Options Clone() => (Options)MemberwiseClone();
        public Options WithStyle(MathStyle s) { var o = Clone(); o.Style = s; return o; }
        public Options WithColor(Rgba c) { var o = Clone(); o.Color = c; return o; }
        public Options WithText(bool text) { var o = Clone(); o.TextMode = text; return o; }
    }
}
