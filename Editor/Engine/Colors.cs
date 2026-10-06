using System;
using System.Collections.Generic;
using System.Globalization;

namespace MathMesher.Editor.Engine
{
    /// <summary>xcolor names (base, dvipsnames), CSS/svgnames, color models and xcolor mix expressions.</summary>
    internal static class Colors
    {
        private static readonly Dictionary<string, Rgba> exact = new Dictionary<string, Rgba>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Rgba> loose = new Dictionary<string, Rgba>(StringComparer.OrdinalIgnoreCase);

        private static void Add(Dictionary<string, Rgba> d, string table)
        {
            foreach (var entry in table.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var p = entry.Split('=');
                int v = int.Parse(p[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                d[p[0]] = Rgba.FromBytes((v >> 16) & 255, (v >> 8) & 255, v & 255);
            }
        }

        static Colors()
        {
            // xcolor base colors
            Add(exact, "black=000000 blue=0000FF brown=BF8040 cyan=00FFFF darkgray=404040 gray=808080 green=00FF00 lightgray=BFBFBF " +
                "lime=BFFF00 magenta=FF00FF olive=808000 orange=FF8000 pink=FFBFBF purple=BF0040 red=FF0000 teal=008080 violet=800080 " +
                "white=FFFFFF yellow=FFFF00");
            // dvipsnames
            Add(exact, "Apricot=FBB982 Aquamarine=00B5BE Bittersweet=C04F17 Black=221E1F Blue=2D2F92 BlueGreen=00B3B8 BlueViolet=473992 " +
                "BrickRed=B6321C Brown=792500 BurntOrange=F7921D CadetBlue=74729A CarnationPink=F282B4 Cerulean=00A2E3 CornflowerBlue=41B0E4 " +
                "Cyan=00AEEF Dandelion=FDBC42 DarkOrchid=A4538A Emerald=00A99D ForestGreen=009B55 Fuchsia=8C368C Goldenrod=FFDF42 Gray=949698 " +
                "Green=00A64F GreenYellow=DFE674 JungleGreen=00A99A Lavender=F49EC4 LimeGreen=8DC73E Magenta=EC008C Mahogany=A9341F " +
                "Maroon=AF3235 Melon=F89E7B MidnightBlue=006795 Mulberry=A93C93 NavyBlue=006EB8 OliveGreen=3C8031 Orange=F58137 " +
                "OrangeRed=ED135A Orchid=AF72B0 Peach=F7965A Periwinkle=7977B8 PineGreen=008B72 Plum=92268F ProcessBlue=00B0F0 " +
                "Purple=99479B RawSienna=974006 Red=ED1B23 RedOrange=F26035 RedViolet=A1246B Rhodamine=EF559F RoyalBlue=0071BC " +
                "RoyalPurple=613F99 RubineRed=ED017D Salmon=F69289 SeaGreen=3FBC9D Sepia=671800 SkyBlue=46C5DD SpringGreen=C6DC67 " +
                "Tan=DA9D76 TealBlue=00AEB3 Thistle=D883B7 Turquoise=00B4CE Violet=58429B VioletRed=EF58A0 White=FFFFFF " +
                "WildStrawberry=EE2967 Yellow=FFF200 YellowGreen=98CC70 YellowOrange=FAA21A");
            // CSS / svgnames (case-insensitive)
            Add(loose, "aliceblue=F0F8FF antiquewhite=FAEBD7 aqua=00FFFF aquamarine=7FFFD4 azure=F0FFFF beige=F5F5DC bisque=FFE4C4 " +
                "black=000000 blanchedalmond=FFEBCD blue=0000FF blueviolet=8A2BE2 brown=A52A2A burlywood=DEB887 cadetblue=5F9EA0 " +
                "chartreuse=7FFF00 chocolate=D2691E coral=FF7F50 cornflowerblue=6495ED cornsilk=FFF8DC crimson=DC143C cyan=00FFFF " +
                "darkblue=00008B darkcyan=008B8B darkgoldenrod=B8860B darkgray=A9A9A9 darkgreen=006400 darkgrey=A9A9A9 darkkhaki=BDB76B " +
                "darkmagenta=8B008B darkolivegreen=556B2F darkorange=FF8C00 darkorchid=9932CC darkred=8B0000 darksalmon=E9967A " +
                "darkseagreen=8FBC8F darkslateblue=483D8B darkslategray=2F4F4F darkslategrey=2F4F4F darkturquoise=00CED1 darkviolet=9400D3 " +
                "deeppink=FF1493 deepskyblue=00BFFF dimgray=696969 dimgrey=696969 dodgerblue=1E90FF firebrick=B22222 floralwhite=FFFAF0 " +
                "forestgreen=228B22 fuchsia=FF00FF gainsboro=DCDCDC ghostwhite=F8F8FF gold=FFD700 goldenrod=DAA520 gray=808080 " +
                "green=008000 greenyellow=ADFF2F grey=808080 honeydew=F0FFF0 hotpink=FF69B4 indianred=CD5C5C indigo=4B0082 ivory=FFFFF0 " +
                "khaki=F0E68C lavender=E6E6FA lavenderblush=FFF0F5 lawngreen=7CFC00 lemonchiffon=FFFACD lightblue=ADD8E6 lightcoral=F08080 " +
                "lightcyan=E0FFFF lightgoldenrodyellow=FAFAD2 lightgray=D3D3D3 lightgreen=90EE90 lightgrey=D3D3D3 lightpink=FFB6C1 " +
                "lightsalmon=FFA07A lightseagreen=20B2AA lightskyblue=87CEFA lightslategray=778899 lightslategrey=778899 " +
                "lightsteelblue=B0C4DE lightyellow=FFFFE0 lime=00FF00 limegreen=32CD32 linen=FAF0E6 magenta=FF00FF maroon=800000 " +
                "mediumaquamarine=66CDAA mediumblue=0000CD mediumorchid=BA55D3 mediumpurple=9370DB mediumseagreen=3CB371 " +
                "mediumslateblue=7B68EE mediumspringgreen=00FA9A mediumturquoise=48D1CC mediumvioletred=C71585 midnightblue=191970 " +
                "mintcream=F5FFFA mistyrose=FFE4E1 moccasin=FFE4B5 navajowhite=FFDEAD navy=000080 oldlace=FDF5E6 olive=808000 " +
                "olivedrab=6B8E23 orange=FFA500 orangered=FF4500 orchid=DA70D6 palegoldenrod=EEE8AA palegreen=98FB98 paleturquoise=AFEEEE " +
                "palevioletred=DB7093 papayawhip=FFEFD5 peachpuff=FFDAB9 peru=CD853F pink=FFC0CB plum=DDA0DD powderblue=B0E0E6 " +
                "purple=800080 rebeccapurple=663399 red=FF0000 rosybrown=BC8F8F royalblue=4169E1 saddlebrown=8B4513 salmon=FA8072 " +
                "sandybrown=F4A460 seagreen=2E8B57 seashell=FFF5EE sienna=A0522D silver=C0C0C0 skyblue=87CEEB slateblue=6A5ACD " +
                "slategray=708090 slategrey=708090 snow=FFFAFA springgreen=00FF7F steelblue=4682B4 tan=D2B48C teal=008080 thistle=D8BFD8 " +
                "tomato=FF6347 turquoise=40E0D0 violet=EE82EE wheat=F5DEB3 white=FFFFFF whitesmoke=F5F5F5 yellow=FFFF00 yellowgreen=9ACD32");
        }

        private static float Num(string s)
        {
            if (!float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                throw new TeXException("色の値が不正です: " + s);
            return v;
        }

        /// <summary>Parses a color given an optional model ([rgb], [RGB], [HTML], [gray], [cmyk], [hsb], [HSB], [wave]).</summary>
        public static Rgba Parse(string model, string spec, IDictionary<string, Rgba> defined = null)
        {
            spec = (spec ?? "").Trim();
            if (string.IsNullOrEmpty(model)) return ParseExpression(spec, defined);
            var parts = spec.Split(',');
            switch (model.Trim())
            {
                case "rgb":
                    if (parts.Length != 3) break;
                    return new Rgba(Clamp(Num(parts[0])), Clamp(Num(parts[1])), Clamp(Num(parts[2])));
                case "RGB":
                    if (parts.Length != 3) break;
                    return new Rgba(Clamp(Num(parts[0]) / 255f), Clamp(Num(parts[1]) / 255f), Clamp(Num(parts[2]) / 255f));
                case "HTML":
                    return Hex(spec) ?? throw new TeXException("HTML 色指定が不正です: " + spec);
                case "gray":
                    var g = Clamp(Num(spec));
                    return new Rgba(g, g, g);
                case "Gray":
                    var g15 = Clamp(Num(spec) / 15f);
                    return new Rgba(g15, g15, g15);
                case "cmyk":
                    if (parts.Length != 4) break;
                    float c = Num(parts[0]), m = Num(parts[1]), y = Num(parts[2]), k = Num(parts[3]);
                    return new Rgba(Clamp((1 - c) * (1 - k)), Clamp((1 - m) * (1 - k)), Clamp((1 - y) * (1 - k)));
                case "cmy":
                    if (parts.Length != 3) break;
                    return new Rgba(Clamp(1 - Num(parts[0])), Clamp(1 - Num(parts[1])), Clamp(1 - Num(parts[2])));
                case "hsb":
                    if (parts.Length != 3) break;
                    return Hsb(Num(parts[0]), Num(parts[1]), Num(parts[2]));
                case "HSB":
                    if (parts.Length != 3) break;
                    return Hsb(Num(parts[0]) / 240f, Num(parts[1]) / 240f, Num(parts[2]) / 240f);
                default:
                    throw new TeXException("未対応の色モデルです: " + model);
            }
            throw new TeXException("色の指定が不正です: [" + model + "]{" + spec + "}");
        }

        private static float Clamp(float v) => Math.Max(0, Math.Min(1, v));

        private static Rgba Hsb(float h, float s, float b)
        {
            h = (h % 1 + 1) % 1 * 6;
            int i = (int)Math.Floor(h);
            float f = h - i, p = b * (1 - s), q = b * (1 - s * f), t = b * (1 - s * (1 - f));
            switch (i)
            {
                case 0: return new Rgba(b, t, p);
                case 1: return new Rgba(q, b, p);
                case 2: return new Rgba(p, b, t);
                case 3: return new Rgba(p, q, b);
                case 4: return new Rgba(t, p, b);
                default: return new Rgba(b, p, q);
            }
        }

        private static Rgba? Hex(string s)
        {
            s = s.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal)) s = s.Substring(1);
            if (s.Length == 3) s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
            if ((s.Length != 6 && s.Length != 8) || !uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) return null;
            if (s.Length == 8) return new Rgba(((v >> 24) & 255) / 255f, ((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
            return Rgba.FromBytes((int)((v >> 16) & 255), (int)((v >> 8) & 255), (int)(v & 255));
        }

        private static Rgba Named(string name, IDictionary<string, Rgba> defined)
        {
            name = name.Trim();
            if (defined != null && defined.TryGetValue(name, out var d)) return d;
            if (exact.TryGetValue(name, out var c)) return c;
            if (loose.TryGetValue(name, out c)) return c;
            if (name.StartsWith("#", StringComparison.Ordinal) && Hex(name) is Rgba h) return h;
            throw new TeXException("未知の色名です: " + name);
        }

        /// <summary>xcolor expressions: name, name!pct, name!pct!name2, chains, and a leading '-' for the complement.</summary>
        public static Rgba ParseExpression(string expr, IDictionary<string, Rgba> defined = null)
        {
            expr = expr.Trim();
            if (expr.Length == 0) throw new TeXException("色が指定されていません。");
            bool complement = expr.StartsWith("-", StringComparison.Ordinal);
            if (complement) expr = expr.Substring(1);
            var parts = expr.Split('!');
            var color = Named(parts[0], defined);
            for (int i = 1; i < parts.Length; i += 2)
            {
                float pct = Clamp(Num(parts[i]) / 100f);
                var other = i + 1 < parts.Length ? Named(parts[i + 1], defined) : Rgba.White;
                color = Rgba.Mix(other, color, pct);
            }
            if (complement) color = new Rgba(1 - color.R, 1 - color.G, 1 - color.B, color.A);
            return color;
        }
    }
}
