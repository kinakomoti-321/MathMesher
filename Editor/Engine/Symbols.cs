using System;
using System.Collections.Generic;
using System.Globalization;

namespace MathMesher.Editor.Engine
{
    internal enum AtomClass { Ord, Op, Bin, Rel, Open, Close, Punct, Inner }

    internal sealed class Sym
    {
        public string Text;
        public AtomClass Class;
        public bool IsLetter;        // affected by math alphabets (\mathbf etc.)
        public bool TextMode;        // usable in text mode
        public bool MathMode = true; // usable in math mode
    }

    internal sealed class BigOpSym
    {
        public string Text;
        public bool? Limits;   // null: limits in display style only
        public bool Integral;
    }

    internal sealed class OperatorName
    {
        public string Name;
        public bool Limits;
        public string Decoration; // varlimsup etc.: "over", "under", "underleft", "underright"
    }

    internal sealed class AccentSym
    {
        public int Codepoint;
        public bool Stretchy;     // choose a horizontal variant/assembly to cover the base
        public bool Under;
        public bool IsArrow;      // stretchy arrow accents: full width, no skew
    }

    /// <summary>Command and character tables for TeX / LaTeX / AMS / mathtools / common package math.</summary>
    internal static partial class Symbols
    {
        public static readonly Dictionary<string, Sym> Math = new Dictionary<string, Sym>(StringComparer.Ordinal);
        public static readonly Dictionary<string, Sym> Text = new Dictionary<string, Sym>(StringComparer.Ordinal);
        public static readonly Dictionary<string, BigOpSym> BigOps = new Dictionary<string, BigOpSym>(StringComparer.Ordinal);
        public static readonly Dictionary<string, OperatorName> OperatorNames = new Dictionary<string, OperatorName>(StringComparer.Ordinal);
        public static readonly Dictionary<string, string> Delimiters = new Dictionary<string, string>(StringComparer.Ordinal);
        public static readonly Dictionary<string, AccentSym> Accents = new Dictionary<string, AccentSym>(StringComparer.Ordinal);
        public static readonly Dictionary<string, int> TextAccents = new Dictionary<string, int>(StringComparer.Ordinal);
        public static readonly Dictionary<string, float> MuSpaces = new Dictionary<string, float>(StringComparer.Ordinal);
        public static readonly Dictionary<string, float> EmSpaces = new Dictionary<string, float>(StringComparer.Ordinal);
        /// <summary>Class of a raw Unicode character typed directly in math mode.</summary>
        public static readonly Dictionary<int, AtomClass> CharClasses = new Dictionary<int, AtomClass>();
        public static readonly Dictionary<int, BigOpSym> CharBigOps = new Dictionary<int, BigOpSym>();

        private static void M(AtomClass cls, string names, string text, bool letter = false, bool textToo = false)
        {
            foreach (var name in names.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var s = new Sym { Text = text, Class = cls, IsLetter = letter, TextMode = textToo };
                Math[@"\" + name] = s;
                if (textToo) Text[@"\" + name] = s;
            }
            if (!letter && !CharClasses.ContainsKey(char.ConvertToUtf32(text, 0)) && TextElementLength(text) == text.Length)
                CharClasses[char.ConvertToUtf32(text, 0)] = cls;
        }

        private static int TextElementLength(string s) => char.IsSurrogatePair(s, 0) ? 2 : 1;

        private static void Ord(string names, string text) => M(AtomClass.Ord, names, text);
        private static void Bin(string names, string text) => M(AtomClass.Bin, names, text);
        private static void Rel(string names, string text) => M(AtomClass.Rel, names, text);
        private static void Open(string names, string text) => M(AtomClass.Open, names, text);
        private static void Close(string names, string text) => M(AtomClass.Close, names, text);
        private static void Punct(string names, string text) => M(AtomClass.Punct, names, text);
        private static void Both(string names, string text, AtomClass cls = AtomClass.Ord) => M(cls, names, text, false, true);
        private static void TextOnly(string names, string text)
        {
            foreach (var name in names.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                Text[@"\" + name] = new Sym { Text = text, Class = AtomClass.Ord, TextMode = true, MathMode = false };
        }
        private static void Op(string names, string text, bool? limits, bool integral = false)
        {
            var op = new BigOpSym { Text = text, Limits = limits, Integral = integral };
            foreach (var name in names.Split(' ')) BigOps[@"\" + name] = op;
            if (TextElementLength(text) == text.Length) CharBigOps[char.ConvertToUtf32(text, 0)] = op;
        }
        private static void Name(string names, string text, bool limits, string decoration = null)
        {
            foreach (var name in names.Split(' '))
                OperatorNames[@"\" + name] = new OperatorName { Name = text ?? name, Limits = limits, Decoration = decoration };
        }
        private static void Acc(string names, int cp, bool stretchy = false, bool under = false, bool arrow = false)
        {
            foreach (var name in names.Split(' '))
                Accents[@"\" + name] = new AccentSym { Codepoint = cp, Stretchy = stretchy, Under = under, IsArrow = arrow };
        }
        private static void Delim(string names, string text)
        {
            foreach (var name in names.Split(' ')) Delimiters[name] = text;
        }

        static Symbols()
        {
            // ---------------- Greek ----------------
            string[] lower = { "alpha α", "beta β", "gamma γ", "delta δ", "epsilon ϵ", "varepsilon ε", "zeta ζ", "eta η", "theta θ", "vartheta ϑ",
                "iota ι", "kappa κ", "varkappa ϰ", "lambda λ", "mu μ", "nu ν", "xi ξ", "omicron ο", "pi π", "varpi ϖ", "rho ρ", "varrho ϱ",
                "sigma σ", "varsigma ς", "tau τ", "upsilon υ", "phi ϕ", "varphi φ", "chi χ", "psi ψ", "omega ω", "digamma ϝ" };
            foreach (var e in lower) { var p = e.Split(' '); M(AtomClass.Ord, p[0], p[1], true); }
            string[] upper = { "Alpha Α", "Beta Β", "Gamma Γ", "Delta Δ", "Epsilon Ε", "Zeta Ζ", "Eta Η", "Theta Θ", "Iota Ι", "Kappa Κ",
                "Lambda Λ", "Mu Μ", "Nu Ν", "Xi Ξ", "Omicron Ο", "Pi Π", "Rho Ρ", "Sigma Σ", "Tau Τ", "Upsilon Υ", "Phi Φ", "Chi Χ",
                "Psi Ψ", "Omega Ω", "Digamma Ϝ" };
            foreach (var e in upper)
            {
                var p = e.Split(' ');
                M(AtomClass.Ord, p[0], p[1], true);
                // \varGamma etc.: italic capitals
                var italic = Alphabets.Map(char.ConvertToUtf32(p[1], 0), MathVariant.Italic);
                Math[@"\var" + p[0]] = new Sym { Text = char.ConvertFromUtf32(italic), Class = AtomClass.Ord };
            }
            // upgreek
            foreach (var e in lower)
            {
                var p = e.Split(' ');
                if (p[0].StartsWith("var", StringComparison.Ordinal)) Math[@"\upvar" + p[0].Substring(3)] = new Sym { Text = p[1], Class = AtomClass.Ord };
                else Math[@"\up" + p[0]] = new Sym { Text = p[1], Class = AtomClass.Ord };
            }

            // ---------------- Ordinary symbols ----------------
            Ord("infty", "∞"); Ord("nabla", "∇"); Ord("partial", "∂"); Ord("emptyset", "∅"); Ord("varnothing", "⌀");
            Ord("aleph", "ℵ"); Ord("beth", "ℶ"); Ord("gimel", "ℷ"); Ord("daleth", "ℸ");
            Ord("hbar", "ℏ"); Ord("hslash", "ℏ"); Ord("ell", "ℓ"); Ord("wp", "℘"); Ord("weierp", "℘"); Ord("Re", "ℜ"); Ord("Im", "ℑ");
            Ord("mho", "℧"); Ord("Finv", "Ⅎ"); Ord("Game", "⅁"); Ord("Bbbk", "𝕜"); Ord("eth", "ð");
            Ord("imath", "𝚤"); Ord("jmath", "𝚥"); Ord("forall", "∀"); Ord("exists", "∃"); Ord("nexists", "∄");
            Ord("neg lnot", "¬"); Ord("top", "⊤"); Ord("bot", "⊥"); Ord("angle", "∠"); Ord("measuredangle", "∡"); Ord("sphericalangle", "∢");
            Ord("triangle vartriangle", "△"); Ord("triangledown", "▽"); Ord("blacktriangle", "▲"); Ord("blacktriangledown", "▼");
            Ord("square Box", "□"); Ord("blacksquare", "■"); Ord("lozenge", "◊"); Ord("Diamond", "◇"); Ord("blacklozenge", "⧫"); Ord("bigstar", "★");
            Ord("clubsuit", "♣"); Ord("diamondsuit", "♢"); Ord("heartsuit", "♡"); Ord("spadesuit", "♠");
            Ord("varclubsuit", "♧"); Ord("vardiamondsuit", "♦"); Ord("varheartsuit", "♥"); Ord("varspadesuit", "♤");
            Ord("flat", "♭"); Ord("natural", "♮"); Ord("sharp", "♯"); Ord("backprime", "‵"); Ord("prime", "′"); Ord("dprime", "″"); Ord("trprime", "‴");
            Ord("surd", "√"); Ord("checkmark", "✓"); Ord("circledS", "Ⓢ"); Ord("maltese", "✠"); Ord("complement", "∁");
            Ord("diagup", "╱"); Ord("diagdown", "╲"); Ord("backslash", "\\"); Ord("Vert", "‖"); Ord("vert", "|");
            Ord("degree", "°"); Ord("lightning", "↯"); Ord("diameter", "⌀"); Ord("varkappa", "ϰ");
            Ord("infin", "∞"); Ord("Dagger", "‡"); Ord("ohm", "Ω"); Ord("micro", "µ"); Ord("celsius", "℃"); Ord("perthousand", "‰");
            Ord("mathdollar", "$"); Ord("mathsterling", "£"); Ord("textyen", "¥");
            Ord("mathellipsis", "…"); Ord("cdots dotsb dotsm dotsi", "⋯"); Ord("vdots", "⋮"); Ord("ddots", "⋱"); Ord("iddots adots", "⋰");
            Ord("dotsc dotso", "…");
            Ord("And", "&"); Ord("Box", "□"); Ord("emptyset", "∅");
            M(AtomClass.Inner, "ldots dots", "…", false, true);
            Both("S", "§"); Both("P", "¶"); Both("dag", "†"); Both("ddag", "‡"); Both("copyright", "©"); Both("pounds", "£"); Both("circledR", "®");
            Both("textdollar $", "$"); Both("%", "%"); Both("&", "&"); Both("#", "#"); Both("_", "_"); Both("yen", "¥"); Both("euro", "€");
            Math[@"\{"] = new Sym { Text = "{", Class = AtomClass.Open, TextMode = true };
            Math[@"\}"] = new Sym { Text = "}", Class = AtomClass.Close, TextMode = true };
            Text[@"\{"] = Math[@"\{"]; Text[@"\}"] = Math[@"\}"];
            Math[@"\|"] = new Sym { Text = "‖", Class = AtomClass.Ord };

            // ---------------- Binary operators ----------------
            Bin("pm", "±"); Bin("mp", "∓"); Bin("times", "×"); Bin("div", "÷"); Bin("ast", "∗"); Bin("star", "⋆"); Bin("circ", "∘");
            Bin("bullet", "∙"); Bin("cdot centerdot", "⋅"); Bin("cap", "∩"); Bin("cup", "∪"); Bin("uplus", "⊎"); Bin("sqcap", "⊓"); Bin("sqcup", "⊔");
            Bin("vee lor", "∨"); Bin("wedge land", "∧"); Bin("setminus", "∖"); Bin("smallsetminus", "∖"); Bin("wr", "≀"); Bin("diamond", "⋄");
            Bin("bigtriangleup", "△"); Bin("bigtriangledown", "▽"); Bin("triangleleft", "◁"); Bin("triangleright", "▷");
            Bin("lhd", "⊲"); Bin("rhd", "⊳"); Bin("unlhd", "⊴"); Bin("unrhd", "⊵");
            Bin("oplus", "⊕"); Bin("ominus", "⊖"); Bin("otimes", "⊗"); Bin("oslash", "⊘"); Bin("odot", "⊙"); Bin("bigcirc", "◯");
            Bin("dagger", "†"); Bin("ddagger", "‡"); Bin("amalg", "⨿"); Bin("dotplus", "∔"); Bin("Cap doublecap", "⋒"); Bin("Cup doublecup", "⋓");
            Bin("barwedge", "⌅"); Bin("veebar", "⊻"); Bin("doublebarwedge", "⩞"); Bin("boxminus", "⊟"); Bin("boxtimes", "⊠"); Bin("boxdot", "⊡");
            Bin("boxplus", "⊞"); Bin("divideontimes", "⋇"); Bin("ltimes", "⋉"); Bin("rtimes", "⋊"); Bin("leftthreetimes", "⋋");
            Bin("rightthreetimes", "⋌"); Bin("curlywedge", "⋏"); Bin("curlyvee", "⋎"); Bin("circleddash", "⊝"); Bin("circledast", "⊛");
            Bin("circledcirc", "⊚"); Bin("intercal", "⊺"); Bin("gtrdot", "⋗"); Bin("lessdot", "⋖");
            Bin("sslash", "⫽"); Bin("talloblong", "⫾"); Bin("interleave", "⫴"); Bin("obar", "⌽"); Bin("oblong", "⎕");
            Bin("boxbar", "◫"); Bin("minuso", "⦵"); Bin("Wedge", "⩓"); Bin("Vee", "⩔"); Bin("bigcupdot", "⨃");
            Bin("cdotp", "·"); Bin("btimes", "⨲"); Bin("smallin", "∊");

            // ---------------- Relations ----------------
            Rel("leq le", "≤"); Rel("geq ge", "≥"); Rel("neq ne", "≠"); Rel("equiv", "≡"); Rel("prec", "≺"); Rel("succ", "≻");
            Rel("preceq", "⪯"); Rel("succeq", "⪰"); Rel("sim", "∼"); Rel("simeq", "≃"); Rel("asymp", "≍"); Rel("approx", "≈"); Rel("cong", "≅");
            Rel("subset", "⊂"); Rel("supset", "⊃"); Rel("subseteq", "⊆"); Rel("supseteq", "⊇"); Rel("sqsubset", "⊏"); Rel("sqsupset", "⊐");
            Rel("sqsubseteq", "⊑"); Rel("sqsupseteq", "⊒"); Rel("in", "∈"); Rel("ni owns", "∋"); Rel("notin", "∉"); Rel("notni", "∌");
            Rel("vdash", "⊢"); Rel("dashv", "⊣"); Rel("models", "⊧"); Rel("mid", "∣"); Rel("parallel", "∥"); Rel("perp", "⟂");
            Rel("smile", "⌣"); Rel("frown", "⌢"); Rel("doteq", "≐"); Rel("propto", "∝"); Rel("bowtie Join", "⋈"); Rel("ll", "≪"); Rel("gg", "≫");
            Rel("lll llless", "⋘"); Rel("ggg gggtr", "⋙"); Rel("vcentcolon ratio", "∶"); Rel("coloneqq colonequals", "≔"); Rel("coloneq", "≔");
            Rel("eqqcolon equalscolon", "≕"); Rel("eqcolon", "≕"); Rel("Coloneqq", "⩴"); Rel("dblcolon Colon", "∷"); Rel("colonsim", "∶∼");
            Rel("Colonsim", "∷∼"); Rel("colonapprox", "∶≈"); Rel("Colonapprox", "∷≈"); Rel("Coloneq", "∷−"); Rel("Eqqcolon", "=∷");
            Rel("Eqcolon", "−∷"); Rel("coloncolon", "∷");
            Rel("leqq", "≦"); Rel("leqslant", "⩽"); Rel("eqslantless", "⪕"); Rel("lesssim", "≲"); Rel("lessapprox", "⪅"); Rel("approxeq", "≊");
            Rel("lessgtr", "≶"); Rel("lesseqgtr", "⋚"); Rel("lesseqqgtr", "⪋"); Rel("doteqdot Doteq", "≑"); Rel("risingdotseq", "≓");
            Rel("fallingdotseq", "≒"); Rel("backsim", "∽"); Rel("backsimeq", "⋍"); Rel("subseteqq", "⫅"); Rel("Subset", "⋐");
            Rel("preccurlyeq", "≼"); Rel("curlyeqprec", "⋞"); Rel("precsim", "≾"); Rel("precapprox", "⪷"); Rel("vartriangleleft", "⊲");
            Rel("trianglelefteq", "⊴"); Rel("vDash", "⊨"); Rel("Vvdash", "⊪"); Rel("smallsmile", "⌣"); Rel("smallfrown", "⌢");
            Rel("bumpeq", "≏"); Rel("Bumpeq", "≎"); Rel("geqq", "≧"); Rel("geqslant", "⩾"); Rel("eqslantgtr", "⪖"); Rel("gtrsim", "≳");
            Rel("gtrapprox", "⪆"); Rel("gtrless", "≷"); Rel("gtreqless", "⋛"); Rel("gtreqqless", "⪌"); Rel("eqcirc", "≖");
            Rel("circeq", "≗"); Rel("triangleq", "≜"); Rel("thicksim", "∼"); Rel("thickapprox", "≈"); Rel("supseteqq", "⫆"); Rel("Supset", "⋑");
            Rel("succcurlyeq", "≽"); Rel("curlyeqsucc", "⋟"); Rel("succsim", "≿"); Rel("succapprox", "⪸"); Rel("vartriangleright", "⊳");
            Rel("trianglerighteq", "⊵"); Rel("Vdash", "⊩"); Rel("VDash", "⊫"); Rel("shortmid", "∣"); Rel("shortparallel", "∥"); Rel("between", "≬");
            Rel("pitchfork", "⋔"); Rel("varpropto", "∝"); Rel("blacktriangleleft", "◀"); Rel("therefore", "∴"); Rel("backepsilon", "϶");
            Rel("blacktriangleright", "▶"); Rel("because", "∵"); Rel("eqsim", "≂"); Rel("measeq", "≞"); Rel("stareq", "≛"); Rel("questeq", "≟");
            Rel("wedgeq", "≙"); Rel("veeeq", "≚"); Rel("eqdef", "≝"); Rel("arceq", "≘"); Rel("approxident", "≋"); Rel("multimap", "⊸");
            Rel("multimapinv", "⟜"); Rel("multimapboth", "⧟"); Rel("origof", "⊶"); Rel("imageof", "⊷"); Rel("Perp", "⫫"); Rel("nVleftarrow", "⇺");
            Rel("leftmodels", "⊨"); Rel("lesseqslantgtr", "⪓"); Rel("gtreqslantless", "⪔"); Rel("lsim", "≲"); Rel("gsim", "≳");
            Rel("subsetplus", "⪿"); Rel("supsetplus", "⫀"); Rel("strictif", "⥽"); Rel("strictfi", "⥼"); Rel("strictiff", "⥿");
            Rel("Lt", "≪"); Rel("Gt", "≫"); Rel("nasymp", "≭"); Rel("lneq", "⪇"); Rel("gneq", "⪈");
            // negations
            Rel("nless", "≮"); Rel("nleq", "≰"); Rel("nleqslant", "⩽̸"); Rel("nleqq", "≦̸"); Rel("lneqq", "≨"); Rel("lvertneqq", "≨");
            Rel("lnsim", "⋦"); Rel("lnapprox", "⪉"); Rel("nprec", "⊀"); Rel("npreceq", "⋠"); Rel("precneqq", "⪵"); Rel("precnsim", "⋨");
            Rel("precnapprox", "⪹"); Rel("nsim", "≁"); Rel("nshortmid nmid", "∤"); Rel("nvdash", "⊬"); Rel("nvDash", "⊭"); Rel("ntriangleleft", "⋪");
            Rel("ntrianglelefteq", "⋬"); Rel("nsubseteq", "⊈"); Rel("nsubseteqq", "⫅̸"); Rel("subsetneq varsubsetneq", "⊊");
            Rel("subsetneqq varsubsetneqq", "⫋"); Rel("ngtr", "≯"); Rel("ngeq", "≱"); Rel("ngeqslant", "⩾̸"); Rel("ngeqq", "≧̸"); Rel("gneqq gvertneqq", "≩");
            Rel("gnsim", "⋧"); Rel("gnapprox", "⪊"); Rel("nsucc", "⊁"); Rel("nsucceq", "⋡"); Rel("succneqq", "⪶"); Rel("succnsim", "⋩");
            Rel("succnapprox", "⪺"); Rel("ncong", "≇"); Rel("nshortparallel nparallel", "∦"); Rel("nVdash", "⊮"); Rel("nVDash", "⊯");
            Rel("ntriangleright", "⋫"); Rel("ntrianglerighteq", "⋭"); Rel("nsupseteq", "⊉"); Rel("nsupseteqq", "⫆̸"); Rel("supsetneq varsupsetneq", "⊋");
            Rel("supsetneqq varsupsetneqq", "⫌"); Rel("nsubset", "⊄"); Rel("nsupset", "⊅"); Rel("nequiv", "≢"); Rel("napprox", "≉");
            Rel("nsimeq", "≄"); Rel("nni", "∌"); Rel("nin", "∉"); Rel("npreccurlyeq", "⋠"); Rel("nsucccurlyeq", "⋡"); Rel("nparallelslant", "∦");
            Rel("nsqsubseteq", "⋢"); Rel("nsqsupseteq", "⋣");
            // arrows
            Rel("leftarrow gets", "←"); Rel("rightarrow to", "→"); Rel("leftrightarrow", "↔"); Rel("Leftarrow", "⇐"); Rel("Rightarrow", "⇒");
            Rel("Leftrightarrow", "⇔"); Rel("longleftarrow", "⟵"); Rel("longrightarrow", "⟶"); Rel("longleftrightarrow", "⟷");
            Rel("Longleftarrow", "⟸"); Rel("Longrightarrow", "⟹"); Rel("Longleftrightarrow", "⟺"); Rel("uparrow", "↑"); Rel("downarrow", "↓");
            Rel("updownarrow", "↕"); Rel("Uparrow", "⇑"); Rel("Downarrow", "⇓"); Rel("Updownarrow", "⇕"); Rel("nearrow", "↗"); Rel("searrow", "↘");
            Rel("swarrow", "↙"); Rel("nwarrow", "↖"); Rel("mapsto", "↦"); Rel("longmapsto", "⟼"); Rel("mapsfrom", "↤"); Rel("longmapsfrom", "⟻");
            Rel("Mapsto", "⤇"); Rel("Mapsfrom", "⤆"); Rel("hookleftarrow", "↩"); Rel("hookrightarrow", "↪"); Rel("leftharpoonup", "↼");
            Rel("leftharpoondown", "↽"); Rel("rightharpoonup", "⇀"); Rel("rightharpoondown", "⇁"); Rel("rightleftharpoons", "⇌");
            Rel("leftrightharpoons", "⇋"); Rel("leadsto rightsquigarrow", "⇝"); Rel("leftsquigarrow", "⇜"); Rel("leftleftarrows", "⇇");
            Rel("rightrightarrows", "⇉"); Rel("leftrightarrows", "⇆"); Rel("rightleftarrows", "⇄"); Rel("Lleftarrow", "⇚"); Rel("Rrightarrow", "⇛");
            Rel("twoheadleftarrow", "↞"); Rel("twoheadrightarrow", "↠"); Rel("leftarrowtail", "↢"); Rel("rightarrowtail", "↣");
            Rel("looparrowleft", "↫"); Rel("looparrowright", "↬"); Rel("curvearrowleft", "↶"); Rel("curvearrowright", "↷");
            Rel("circlearrowleft", "↺"); Rel("circlearrowright", "↻"); Rel("Lsh", "↰"); Rel("Rsh", "↱"); Rel("upuparrows", "⇈");
            Rel("downdownarrows", "⇊"); Rel("upharpoonleft", "↿"); Rel("upharpoonright restriction", "↾"); Rel("downharpoonleft", "⇃");
            Rel("downharpoonright", "⇂"); Rel("leftrightsquigarrow", "↭"); Rel("dashrightarrow dasharrow", "⇢"); Rel("dashleftarrow", "⇠");
            Rel("nleftarrow", "↚"); Rel("nrightarrow", "↛"); Rel("nLeftarrow", "⇍"); Rel("nRightarrow", "⇏"); Rel("nleftrightarrow", "↮");
            Rel("nLeftrightarrow", "⇎"); Rel("Lleftarrow", "⇚"); Rel("leftrightarrowtriangle", "⇿"); Rel("updownarrows", "⇅");
            Rel("downuparrows", "⇵"); Rel("leftarrowtriangle", "⇽"); Rel("rightarrowtriangle", "⇾"); Rel("Nwarrow", "⇖"); Rel("Nearrow", "⇗");
            Rel("Searrow", "⇘"); Rel("Swarrow", "⇙"); Rel("rightarrowbar", "⇥"); Rel("leftarrowbar", "⇤"); Rel("longleftsquigarrow", "⬳");
            Rel("longrightsquigarrow", "⟿"); Rel("Longmapsto", "⟾"); Rel("Longmapsfrom", "⟽"); Rel("uparrowbar", "⤒"); Rel("downarrowbar", "⤓");
            Rel("lightningbolt", "↯"); Rel("leftrightharpoonup", "⥊"); Rel("rightleftharpoonup", "⥋");
            Rel("leftharpoonsupdown", "⥢"); Rel("rightharpoonsupdown", "⥤"); Rel("Uuparrow", "⤊"); Rel("Ddownarrow", "⤋");

            Rel("=", "="); Rel("<", "<"); Rel(">", ">"); Rel("lt", "<"); Rel("gt", ">"); Rel("approxcolon", "≈∶");

            // ---------------- Delimiter atoms ----------------
            Open("lbrace", "{"); Close("rbrace", "}"); Open("lbrack", "["); Close("rbrack", "]"); Open("lparen", "("); Close("rparen", ")");
            Open("langle", "⟨"); Close("rangle", "⟩"); Open("lAngle", "⟪"); Close("rAngle", "⟫"); Open("lceil", "⌈"); Close("rceil", "⌉");
            Open("lfloor", "⌊"); Close("rfloor", "⌋"); Open("lgroup", "⟮"); Close("rgroup", "⟯"); Open("lmoustache", "⎰"); Close("rmoustache", "⎱");
            Open("ulcorner", "⌜"); Close("urcorner", "⌝"); Open("llcorner", "⌞"); Close("lrcorner", "⌟"); Open("llbracket", "⟦"); Close("rrbracket", "⟧");
            Open("lBrace", "⦃"); Close("rBrace", "⦄"); Open("lvert", "|"); Close("rvert", "|"); Open("lVert", "‖"); Close("rVert", "‖");
            Open("llparenthesis", "⦇"); Close("rrparenthesis", "⦈"); Open("lbrbrak", "❲"); Close("rbrbrak", "❳"); Open("lBrack", "⟦"); Close("rBrack", "⟧");

            // ---------------- Punctuation ----------------
            Punct("colon", ":"); Punct("ldotp", "."); Punct("cdotp", "⋅");

            // Characters typed directly
            CharClasses['+'] = AtomClass.Bin; CharClasses['-'] = AtomClass.Bin; CharClasses['*'] = AtomClass.Bin; CharClasses['−'] = AtomClass.Bin;
            CharClasses['='] = AtomClass.Rel; CharClasses['<'] = AtomClass.Rel; CharClasses['>'] = AtomClass.Rel; CharClasses[':'] = AtomClass.Rel;
            CharClasses[','] = AtomClass.Punct; CharClasses[';'] = AtomClass.Punct; CharClasses['.'] = AtomClass.Ord; CharClasses['/'] = AtomClass.Ord;
            CharClasses['!'] = AtomClass.Close; CharClasses['?'] = AtomClass.Close; CharClasses['('] = AtomClass.Open; CharClasses[')'] = AtomClass.Close;
            CharClasses['['] = AtomClass.Open; CharClasses[']'] = AtomClass.Close; CharClasses['|'] = AtomClass.Ord; CharClasses['@'] = AtomClass.Ord;
            CharClasses['"'] = AtomClass.Ord; CharClasses['`'] = AtomClass.Ord; CharClasses['·'] = AtomClass.Bin; CharClasses['×'] = AtomClass.Bin;
            CharClasses['÷'] = AtomClass.Bin; CharClasses['±'] = AtomClass.Bin; CharClasses['¬'] = AtomClass.Ord;
            CharClasses['∶'] = AtomClass.Rel;
            CharClasses['…'] = AtomClass.Inner;

            // ---------------- Large operators ----------------
            Op("sum", "∑", null); Op("prod", "∏", null); Op("coprod", "∐", null); Op("bigcap", "⋂", null); Op("bigcup", "⋃", null);
            Op("bigsqcup", "⨆", null); Op("bigsqcap", "⨅", null); Op("bigvee", "⋁", null); Op("bigwedge", "⋀", null); Op("bigodot", "⨀", null);
            Op("bigoplus", "⨁", null); Op("bigotimes", "⨂", null); Op("biguplus", "⨄", null); Op("bigtimes", "⨉", null); Op("bigcupdot", "⨃", null);
            Op("bigcapdot", "⨀", null); Op("bigbot", "⟘", null); Op("bigtop", "⟙", null); Op("bigsqcupdot", "⨆", null); Op("Bigcap", "⋂", null);
            Op("int", "∫", false, true); Op("iint", "∬", false, true); Op("iiint", "∭", false, true); Op("iiiint", "⨌", false, true);
            Op("oint", "∮", false, true); Op("oiint", "∯", false, true); Op("oiiint", "∰", false, true); Op("intop", "∫", true, true);
            Op("varointclockwise", "∲", false, true); Op("ointctrclockwise", "∳", false, true); Op("intclockwise", "∱", false, true);
            Op("sqint", "⨖", false, true); Op("fint", "⨏", false, true); Op("awint", "⨑", false, true); Op("rppolint", "⨒", false, true);
            Op("scpolint", "⨓", false, true); Op("pointint", "⨕", false, true); Op("upint", "⨛", false, true); Op("lowint", "⨜", false, true);
            Op("intlarhk", "⨗", false, true); Op("intx", "⨘", false, true); Op("intcap", "⨙", false, true); Op("intcup", "⨚", false, true);
            Op("varoint", "∮", false, true); Op("ointclockwise", "∲", false, true);
            Op("bigplus", "⨁", null); Op("conjquant", "⨇", null); Op("disjquant", "⨈", null); Op("modtwosum", "⨊", null);
            Op("bigbox", "⨀", null); Op("bigvarstar", "★", null);

            // ---------------- Operator names ----------------
            foreach (var n in "arccos arcsin arctan arg cos cosh cot coth csc deg dim exp hom ker lg ln log sec sin sinh tan tanh arccot arcsec arccsc sech csch arsinh arcosh artanh arcoth arsech arcsch sgn tg ctg cotg arctg arcctg th cth ch sh cosec".Split(' '))
                Name(n, n, false);
            foreach (var n in "det gcd inf lim max min Pr sup".Split(' ')) Name(n, n, true);
            Name("liminf", "lim inf", true); Name("limsup", "lim sup", true); Name("injlim", "inj lim", true); Name("projlim", "proj lim", true);
            Name("argmax", "arg max", true); Name("argmin", "arg min", true); Name("lcm", "lcm", true);
            Name("varlimsup", "lim", true, "over"); Name("varliminf", "lim", true, "under");
            Name("varinjlim", "lim", true, "underright"); Name("varprojlim", "lim", true, "underleft");
            Name("Tr tr", "tr", false); Name("rank", "rank", false); Name("diag", "diag", false); Name("Res", "Res", false); Name("erf", "erf", false);
            Name("sinc", "sinc", false); Name("span", "span", false); Name("supp", "supp", false); Name("codim", "codim", false);
            Name("Ker", "Ker", false); Name("Hom", "Hom", false); Name("End", "End", false); Name("Aut", "Aut", false); Name("id", "id", false);
            Name("im", "im", false); Name("Arg", "Arg", false); Name("ord", "ord", false); Name("adj", "adj", false); Name("sign", "sign", false);
            Name("cov", "cov", false); Name("Var", "Var", false); Name("Cov", "Cov", false); Name("E", "E", false);
            Name("plim", "plim", true); Name("esssup", "ess sup", true); Name("essinf", "ess inf", true);

            // ---------------- Delimiters (\left, \big, ...) ----------------
            Delim("( \\lparen", "("); Delim(") \\rparen", ")"); Delim("[ \\lbrack", "["); Delim("] \\rbrack", "]");
            Delim("\\{ \\lbrace", "{"); Delim("\\} \\rbrace", "}"); Delim("< \\langle \\lang", "⟨"); Delim("> \\rangle \\rang", "⟩");
            Delim("| \\vert \\lvert \\rvert \\mid \\arrowvert \\bracevert", "|"); Delim("\\| \\Vert \\lVert \\rVert \\Arrowvert \\parallel", "‖");
            Delim("\\lfloor", "⌊"); Delim("\\rfloor", "⌋"); Delim("\\lceil", "⌈"); Delim("\\rceil", "⌉");
            Delim("\\lgroup", "⟮"); Delim("\\rgroup", "⟯"); Delim("\\lmoustache", "⎰"); Delim("\\rmoustache", "⎱");
            Delim("\\ulcorner", "⌜"); Delim("\\urcorner", "⌝"); Delim("\\llcorner", "⌞"); Delim("\\lrcorner", "⌟");
            Delim("/", "/"); Delim("\\backslash", "\\"); Delim("\\uparrow", "↑"); Delim("\\downarrow", "↓"); Delim("\\updownarrow", "↕");
            Delim("\\Uparrow", "⇑"); Delim("\\Downarrow", "⇓"); Delim("\\Updownarrow", "⇕"); Delim("\\llbracket \\lBrack", "⟦"); Delim("\\rrbracket \\rBrack", "⟧");
            Delim("\\lBrace", "⦃"); Delim("\\rBrace", "⦄"); Delim("\\lAngle", "⟪"); Delim("\\rAngle", "⟫"); Delim("\\lt", "⟨"); Delim("\\gt", "⟩");
            Delim("\\llparenthesis", "⦇"); Delim("\\rrparenthesis", "⦈"); Delim("\\lbrbrak", "❲"); Delim("\\rbrbrak", "❳");
            Delim(".", "");
            foreach (var c in "()[]|/⟨⟩⌊⌋⌈⌉‖↑↓↕⇑⇓⇕⟦⟧⟮⟯") Delimiters[c.ToString()] = c.ToString();

            // ---------------- Accents ----------------
            Acc("hat", 0x0302); Acc("widehat", 0x0302, true); Acc("check", 0x030C); Acc("widecheck", 0x030C, true);
            Acc("tilde", 0x0303); Acc("widetilde", 0x0303, true); Acc("acute", 0x0301); Acc("grave", 0x0300); Acc("dot", 0x0307);
            Acc("ddot", 0x0308); Acc("dddot", 0x20DB); Acc("ddddot", 0x20DC); Acc("breve", 0x0306); Acc("bar", 0x0304);
            Acc("vec", 0x20D7); Acc("mathring", 0x030A); Acc("ocirc", 0x030A); Acc("overbar", 0x0305, true);
            Acc("overleftarrow", 0x2190, true, false, true); Acc("overrightarrow", 0x2192, true, false, true);
            Acc("overleftrightarrow", 0x2194, true, false, true); Acc("Overrightarrow", 0x21D2, true, false, true);
            Acc("Overleftarrow", 0x21D0, true, false, true); Acc("Overleftrightarrow", 0x21D4, true, false, true);
            Acc("overleftharpoon", 0x21BC, true, false, true); Acc("overrightharpoon", 0x21C0, true, false, true);
            Acc("underleftarrow", 0x2190, true, true, true); Acc("underrightarrow", 0x2192, true, true, true);
            Acc("underleftrightarrow", 0x2194, true, true, true); Acc("underleftharpoon", 0x21BD, true, true, true);
            Acc("underrightharpoon", 0x21C1, true, true, true); Acc("utilde", 0x0303, true, true); Acc("undertilde", 0x0303, true, true);
            Acc("overgroup", 0x23E0, true, false, true); Acc("undergroup", 0x23E1, true, true, true);
            Acc("overparen wideparen", 0x23DC, true, false, true); Acc("underparen", 0x23DD, true, true, true);
            Acc("leftharpoonaccent", 0x20D0); Acc("rightharpoonaccent", 0x20D1); Acc("widebridgeabove", 0x20E9, true);
            Acc("asteraccent", 0x20F0); Acc("threeunderdot", 0x20E8, false, true); Acc("vertoverlay", 0x20D2);
            Acc("overleftrightarrowaccent", 0x20E1, true);

            // text accents
            TextAccents[@"\'"] = 0x0301; TextAccents[@"\`"] = 0x0300; TextAccents[@"\^"] = 0x0302; TextAccents["\\\""] = 0x0308;
            TextAccents[@"\~"] = 0x0303; TextAccents[@"\="] = 0x0304; TextAccents[@"\."] = 0x0307; TextAccents[@"\u"] = 0x0306;
            TextAccents[@"\v"] = 0x030C; TextAccents[@"\H"] = 0x030B; TextAccents[@"\r"] = 0x030A; TextAccents[@"\c"] = 0x0327;
            TextAccents[@"\k"] = 0x0328; TextAccents[@"\b"] = 0x0331; TextAccents[@"\d"] = 0x0323; TextAccents[@"\t"] = 0x0361;

            // ---------------- Text-mode symbols ----------------
            TextOnly("textbackslash", "\\"); TextOnly("textasciitilde", "~"); TextOnly("textasciicircum", "^"); TextOnly("textbar", "|");
            TextOnly("textbardbl", "‖"); TextOnly("textbraceleft", "{"); TextOnly("textbraceright", "}"); TextOnly("textless", "<");
            TextOnly("textgreater", ">"); TextOnly("textunderscore", "_"); TextOnly("textsterling", "£"); TextOnly("texteuro", "€");
            TextOnly("textdagger", "†"); TextOnly("textdaggerdbl", "‡"); TextOnly("textsection", "§"); TextOnly("textparagraph", "¶");
            TextOnly("textbullet", "•"); TextOnly("textperiodcentered", "·"); TextOnly("textellipsis", "…"); TextOnly("textquoteleft lq", "‘");
            TextOnly("textquoteright rq", "’"); TextOnly("textquotedblleft", "“"); TextOnly("textquotedblright", "”"); TextOnly("textendash", "–");
            TextOnly("textemdash", "—"); TextOnly("textregistered", "®"); TextOnly("texttrademark", "™"); TextOnly("textcopyright", "©");
            TextOnly("textdegree", "°"); TextOnly("textcelsius", "℃"); TextOnly("textmu", "µ"); TextOnly("textohm", "Ω"); TextOnly("textonehalf", "½");
            TextOnly("textonequarter", "¼"); TextOnly("textthreequarters", "¾"); TextOnly("textordfeminine", "ª"); TextOnly("textordmasculine", "º");
            TextOnly("textexclamdown", "¡"); TextOnly("textquestiondown", "¿"); TextOnly("textvisiblespace", "␣"); TextOnly("i", "ı"); TextOnly("j", "ȷ");
            TextOnly("aa", "å"); TextOnly("AA", "Å"); TextOnly("ae", "æ"); TextOnly("AE", "Æ"); TextOnly("oe", "œ"); TextOnly("OE", "Œ");
            TextOnly("o", "ø"); TextOnly("O", "Ø"); TextOnly("ss", "ß"); TextOnly("SS", "SS"); TextOnly("l", "ł"); TextOnly("L", "Ł");
            TextOnly("dh", "ð"); TextOnly("DH", "Ð"); TextOnly("th", "þ"); TextOnly("TH", "Þ"); TextOnly("ng", "ŋ"); TextOnly("NG", "Ŋ");
            TextOnly("dj", "đ"); TextOnly("DJ", "Đ"); TextOnly("guillemotleft guillemetleft", "«"); TextOnly("guillemotright guillemetright", "»");
            TextOnly("guilsinglleft", "‹"); TextOnly("guilsinglright", "›"); TextOnly("quotedblbase", "„"); TextOnly("quotesinglbase", "‚");
            TextOnly("textquotedbl", "\""); TextOnly("textyen", "¥"); TextOnly("textcent", "¢"); TextOnly("textnumero", "№"); TextOnly("textdiv", "÷");
            TextOnly("texttimes", "×"); TextOnly("textpm", "±"); TextOnly("textminus", "−"); TextOnly("textlangle", "⟨"); TextOnly("textrangle", "⟩");
            TextOnly("textasteriskcentered", "∗"); TextOnly("textbigcircle", "◯"); TextOnly("textdollar", "$"); TextOnly("LaTeXe", "LaTeX2ε");
            TextOnly("checkmark", "✓"); TextOnly("ldots dots", "…");

            // ---------------- Spaces ----------------
            MuSpaces[@"\,"] = 3; MuSpaces[@"\thinspace"] = 3; MuSpaces[@"\:"] = 4; MuSpaces[@"\>"] = 4; MuSpaces[@"\medspace"] = 4;
            MuSpaces[@"\;"] = 5; MuSpaces[@"\thickspace"] = 5; MuSpaces[@"\!"] = -3; MuSpaces[@"\negthinspace"] = -3;
            MuSpaces[@"\negmedspace"] = -4; MuSpaces[@"\negthickspace"] = -5;
            EmSpaces[@"\quad"] = 1f; EmSpaces[@"\qquad"] = 2f; EmSpaces[@"\enspace"] = 0.5f; EmSpaces[@"\enskip"] = 0.5f;
            EmSpaces[@"\ "] = 0.3333f; EmSpaces["~"] = 0.3333f; EmSpaces[@"\space"] = 0.3333f; EmSpaces[@"\nobreakspace"] = 0.3333f;
            EmSpaces["\\\t"] = 0.3333f; EmSpaces["\\\n"] = 0.3333f; EmSpaces[@"\nobreak"] = 0; EmSpaces[@"\allowbreak"] = 0;
            EmSpaces[@"\relax"] = 0; EmSpaces[@"\-"] = 0; EmSpaces[@"\/"] = 0; EmSpaces[@"\hfill"] = 0; EmSpaces[@"\hfil"] = 0; EmSpaces[@"\hss"] = 0;
            EmSpaces[@"\displaybreak"] = 0; EmSpaces[@"\noindent"] = 0; EmSpaces[@"\indent"] = 0; EmSpaces[@"\par"] = 0;
            EmSpaces[@"\smallskip"] = 0; EmSpaces[@"\medskip"] = 0; EmSpaces[@"\bigskip"] = 0; EmSpaces[@"\centering"] = 0;
            EmSpaces[@"\protect"] = 0; EmSpaces[@"\limits"] = float.NaN; // handled by the parser; marker only

            RegisterUnicodeMath();

            // Reverse lookup for directly typed symbols
            foreach (var pair in Math)
            {
                var s = pair.Value;
                if (s.IsLetter || string.IsNullOrEmpty(s.Text)) continue;
                int cp = char.ConvertToUtf32(s.Text, 0);
                if (s.Text.Length != TextElementLength(s.Text)) continue;
                if (!CharClasses.ContainsKey(cp)) CharClasses[cp] = s.Class;
            }
        }

        public static bool IsLatinLetter(int cp) => (cp >= 'a' && cp <= 'z') || (cp >= 'A' && cp <= 'Z');
        public static bool IsGreekLetter(int cp) => (cp >= 0x391 && cp <= 0x3A9) || (cp >= 0x3B1 && cp <= 0x3C9) ||
            cp == 0x3D1 || cp == 0x3D5 || cp == 0x3D6 || cp == 0x3F0 || cp == 0x3F1 || cp == 0x3F5 || cp == 0x3DD || cp == 0x3DC || cp == 0x3F4 || cp == 0x2202 || cp == 0x2207;

        /// <summary>Class for a directly typed Unicode character in math mode.</summary>
        public static AtomClass ClassOf(int cp)
        {
            if (CharClasses.TryGetValue(cp, out var c)) return c;
            var cat = CharUnicodeInfo.GetUnicodeCategory(cp <= 0xFFFF ? ((char)cp).ToString() : char.ConvertFromUtf32(cp), 0);
            if (cat == UnicodeCategory.OpenPunctuation) return AtomClass.Open;
            if (cat == UnicodeCategory.ClosePunctuation) return AtomClass.Close;
            if (cp >= 0x2190 && cp <= 0x21FF) return AtomClass.Rel;
            if (cp >= 0x27F0 && cp <= 0x27FF) return AtomClass.Rel;
            if (cp >= 0x2900 && cp <= 0x297F) return AtomClass.Rel;
            return AtomClass.Ord;
        }
    }

    internal enum MathVariant
    {
        Default, Normal, Upright, Italic, Bold, BoldItalic, Script, BoldScript, Fraktur, BoldFraktur, DoubleStruck,
        SansSerif, SansBold, SansItalic, SansBoldItalic, Monospace, Calligraphic, BoldCalligraphic
    }

    /// <summary>Unicode Mathematical Alphanumeric Symbols mapping (plus the letterlike-symbol holes).</summary>
    internal static class Alphabets
    {
        private static readonly Dictionary<MathVariant, (int upper, int lower)> latin = new Dictionary<MathVariant, (int, int)>
        {
            [MathVariant.Bold] = (0x1D400, 0x1D41A), [MathVariant.Italic] = (0x1D434, 0x1D44E), [MathVariant.BoldItalic] = (0x1D468, 0x1D482),
            [MathVariant.Script] = (0x1D49C, 0x1D4B6), [MathVariant.BoldScript] = (0x1D4D0, 0x1D4EA), [MathVariant.Fraktur] = (0x1D504, 0x1D51E),
            [MathVariant.DoubleStruck] = (0x1D538, 0x1D552), [MathVariant.BoldFraktur] = (0x1D56C, 0x1D586), [MathVariant.SansSerif] = (0x1D5A0, 0x1D5BA),
            [MathVariant.SansBold] = (0x1D5D4, 0x1D5EE), [MathVariant.SansItalic] = (0x1D608, 0x1D622), [MathVariant.SansBoldItalic] = (0x1D63C, 0x1D656),
            [MathVariant.Monospace] = (0x1D670, 0x1D68A), [MathVariant.Calligraphic] = (0x1D49C, 0x1D4B6), [MathVariant.BoldCalligraphic] = (0x1D4D0, 0x1D4EA),
        };
        private static readonly Dictionary<MathVariant, int> greek = new Dictionary<MathVariant, int>
        {
            [MathVariant.Bold] = 0x1D6A8, [MathVariant.Italic] = 0x1D6E2, [MathVariant.BoldItalic] = 0x1D71C,
            [MathVariant.SansBold] = 0x1D756, [MathVariant.SansBoldItalic] = 0x1D790,
        };
        private static readonly Dictionary<MathVariant, int> digits = new Dictionary<MathVariant, int>
        {
            [MathVariant.Bold] = 0x1D7CE, [MathVariant.BoldItalic] = 0x1D7CE, [MathVariant.DoubleStruck] = 0x1D7D8, [MathVariant.SansSerif] = 0x1D7E2,
            [MathVariant.SansBold] = 0x1D7EC, [MathVariant.SansBoldItalic] = 0x1D7EC, [MathVariant.SansItalic] = 0x1D7E2, [MathVariant.Monospace] = 0x1D7F6,
        };
        private static readonly Dictionary<(MathVariant, int), int> holes = new Dictionary<(MathVariant, int), int>
        {
            [(MathVariant.Italic, 'h')] = 0x210E,
            [(MathVariant.Script, 'B')] = 0x212C, [(MathVariant.Script, 'E')] = 0x2130, [(MathVariant.Script, 'F')] = 0x2131,
            [(MathVariant.Script, 'H')] = 0x210B, [(MathVariant.Script, 'I')] = 0x2110, [(MathVariant.Script, 'L')] = 0x2112,
            [(MathVariant.Script, 'M')] = 0x2133, [(MathVariant.Script, 'R')] = 0x211B, [(MathVariant.Script, 'e')] = 0x212F,
            [(MathVariant.Script, 'g')] = 0x210A, [(MathVariant.Script, 'o')] = 0x2134,
            [(MathVariant.Calligraphic, 'B')] = 0x212C, [(MathVariant.Calligraphic, 'E')] = 0x2130, [(MathVariant.Calligraphic, 'F')] = 0x2131,
            [(MathVariant.Calligraphic, 'H')] = 0x210B, [(MathVariant.Calligraphic, 'I')] = 0x2110, [(MathVariant.Calligraphic, 'L')] = 0x2112,
            [(MathVariant.Calligraphic, 'M')] = 0x2133, [(MathVariant.Calligraphic, 'R')] = 0x211B, [(MathVariant.Calligraphic, 'e')] = 0x212F,
            [(MathVariant.Calligraphic, 'g')] = 0x210A, [(MathVariant.Calligraphic, 'o')] = 0x2134,
            [(MathVariant.Fraktur, 'C')] = 0x212D, [(MathVariant.Fraktur, 'H')] = 0x210C, [(MathVariant.Fraktur, 'I')] = 0x2111,
            [(MathVariant.Fraktur, 'R')] = 0x211C, [(MathVariant.Fraktur, 'Z')] = 0x2128,
            [(MathVariant.DoubleStruck, 'C')] = 0x2102, [(MathVariant.DoubleStruck, 'H')] = 0x210D, [(MathVariant.DoubleStruck, 'N')] = 0x2115,
            [(MathVariant.DoubleStruck, 'P')] = 0x2119, [(MathVariant.DoubleStruck, 'Q')] = 0x211A, [(MathVariant.DoubleStruck, 'R')] = 0x211D,
            [(MathVariant.DoubleStruck, 'Z')] = 0x2124,
        };

        private static int GreekIndex(int cp)
        {
            if (cp >= 0x391 && cp <= 0x3A9) return cp == 0x3A2 ? -1 : cp - 0x391;
            if (cp == 0x3F4) return 17;
            if (cp == 0x2207) return 25;
            if (cp >= 0x3B1 && cp <= 0x3C9) return 26 + cp - 0x3B1;
            switch (cp)
            {
                case 0x2202: return 51;
                case 0x3F5: return 52;
                case 0x3D1: return 53;
                case 0x3F0: return 54;
                case 0x3D5: return 55;
                case 0x3F1: return 56;
                case 0x3D6: return 57;
            }
            return -1;
        }

        /// <summary>Maps a plain code point to the math alphabet code point, or returns it unchanged.</summary>
        public static int Map(int cp, MathVariant variant)
        {
            if (variant == MathVariant.Default || variant == MathVariant.Upright) return cp;
            if (variant == MathVariant.Normal)
            {
                if (Symbols.IsLatinLetter(cp)) return Map(cp, MathVariant.Italic);
                if ((cp >= 0x3B1 && cp <= 0x3C9) || cp == 0x3D1 || cp == 0x3D5 || cp == 0x3D6 || cp == 0x3F0 || cp == 0x3F1 || cp == 0x3F5)
                    return Map(cp, MathVariant.Italic);
                if (cp == 0x131) return 0x1D6A4;
                if (cp == 0x237) return 0x1D6A5;
                return cp;
            }
            if (Symbols.IsLatinLetter(cp) && latin.TryGetValue(variant, out var l))
            {
                if (holes.TryGetValue((variant, cp), out var hole)) return hole;
                return cp >= 'a' ? l.lower + (cp - 'a') : l.upper + (cp - 'A');
            }
            if (cp >= '0' && cp <= '9' && digits.TryGetValue(variant, out var d)) return d + (cp - '0');
            int gi = GreekIndex(cp);
            if (gi >= 0)
            {
                var gv = variant switch { MathVariant.SansSerif => MathVariant.SansBold, MathVariant.SansItalic => MathVariant.SansBoldItalic, _ => variant };
                if (variant == MathVariant.SansSerif || variant == MathVariant.SansItalic) return cp;
                if (greek.TryGetValue(gv, out var g)) return g + gi;
            }
            if (variant == MathVariant.Italic || variant == MathVariant.BoldItalic)
            {
                if (cp == 0x131) return 0x1D6A4;
                if (cp == 0x237) return 0x1D6A5;
            }
            return cp;
        }
    }
}
