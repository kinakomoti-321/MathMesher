# Third-party notices

MathMesher includes the following third-party software and unmodified font files. Copyrights remain with their respective authors. Copies of license terms and source-file notices are in `Third Party Notices`.

| Component | Pinned source | License |
| --- | --- | --- |
| CSharpMath 0.5.1, including math editor and rendering core | [69fb790c40693ae85d2d8679655ee04e88f35aea](https://github.com/verybadcat/CSharpMath/tree/69fb790c40693ae85d2d8679655ee04e88f35aea) | MIT |
| Typography OpenFont, GlyphLayout, TextBreak and GlyphOutlineBuilderBase | [d331968f3ee714332a8ea4fa95a21c15812a8d93](https://github.com/LayoutFarm/Typography/tree/d331968f3ee714332a8ea4fa95a21c15812a8d93) | MIT; source portions include Apache-2.0, FreeType and other notices preserved separately |
| LibTessDotNet | [bdc0b0f94904dd94c3edfb2c25aa358695276e09](https://github.com/speps/LibTessDotNet/tree/bdc0b0f94904dd94c3edfb2c25aa358695276e09) | SGI Free Software License B 2.0 |
| Latin Modern Math | Unmodified font from the pinned CSharpMath source | GUST Font License |
| Cyrillic Modern nmr10 | Unmodified font from the pinned CSharpMath source | SIL Open Font License 1.1 |
| AMS Capital Blackboard Bold, extracted by Happypig375 | Unmodified font from the pinned CSharpMath source | SIL Open Font License 1.1 |
| unicode-math symbol table (command names, code points and classes) | `unicode-math-table.tex` at [50b5278e9e1a8e04be24d0780c89f7070568806d](https://github.com/latex3/unicode-math/blob/50b5278e9e1a8e04be24d0780c89f7070568806d/unicode-math-table.tex), converted to `Editor/Engine/Symbols.UnicodeMath.cs` by `Tools~/GenerateUnicodeMathSymbols.py`. Copyright 2006–2019 Will Robertson, 2010–2017 Philipp Stephani, 2011–2017 Joseph Wright and contributors | LaTeX Project Public License 1.3c (`Third Party Notices/LPPL-1.3c.txt`) |
| STIX Two Math 2.13 b171 | Unmodified `fonts/static_otf/STIXTwoMath-Regular.otf` from [stipub/stixfonts v2.13b171](https://github.com/stipub/stixfonts/tree/v2.13b171), stored as `Editor/Fonts/STIXTwoMath-Regular.otf` | SIL Open Font License 1.1 (`Third Party Notices/STIX-Two-OFL.txt`) |

Since 0.2.0 MathMesher lays out math with its own engine in `Editor/Engine`. The prebuilt DLL is unchanged and still contains the CSharpMath code listed above, but only its Typography font reader and embedded fonts are used.

The fonts' original copyright and family-name metadata is reproduced in their `*-Notice.txt` files. Font data is embedded in `Editor/Plugins/TeXmexsh.MathEngine.dll`.

Changes to the math engine: combined the required managed projects into one .NET Standard 2.1 DLL; prefixed CSharpMath and Typography namespaces with `MathMesher.Internal`; preserved font-resource lookup names with the same prefix; disambiguated `Atom.Range` from `System.Range`; enabled the Unicode prime glyph for `\prime`. No SkiaSharp, native renderer, external TeX executable, or NuGet runtime dependency is included.

LibTessDotNet source namespaces are prefixed with `MathMesher.Internal`. Original copyright/license headers are retained. MathMesher also registers built-in `\dfrac` and `\tfrac` handlers on its private math engine; user-defined macros remain unsupported.

To reproduce the managed DLL and tessellation sources, run `Tools~/BuildMathEngine.ps1` with a .NET SDK capable of targeting .NET Standard 2.1. The script downloads the pinned original sources. Installed package users do not need to run it.
