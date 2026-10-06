# Third-party notices

MathMesher includes the following third-party software, data and unmodified font files. Copyrights remain with their respective authors. Copies of license terms and source-file notices are in `Third Party Notices`.

| Component | Pinned source | License |
| --- | --- | --- |
| Typography: OpenFont and GlyphOutlineBuilderBase | [d331968f3ee714332a8ea4fa95a21c15812a8d93](https://github.com/LayoutFarm/Typography/tree/d331968f3ee714332a8ea4fa95a21c15812a8d93) | MIT as a whole; individual source files are Apache-2.0, MIT, BSD (Adobe Glyph List) or the FreeType License, as listed in `Third Party Notices/Original-Source-Headers.txt` |
| LibTessDotNet | [bdc0b0f94904dd94c3edfb2c25aa358695276e09](https://github.com/speps/LibTessDotNet/tree/bdc0b0f94904dd94c3edfb2c25aa358695276e09) | SGI Free Software License B 2.0 |
| unicode-math symbol table (command names, code points and classes) | `unicode-math-table.tex` at [50b5278e9e1a8e04be24d0780c89f7070568806d](https://github.com/latex3/unicode-math/blob/50b5278e9e1a8e04be24d0780c89f7070568806d/unicode-math-table.tex), converted to `Editor/Engine/Symbols.UnicodeMath.cs` by `Tools~/GenerateUnicodeMathSymbols.py`. Copyright 2006–2019 Will Robertson, 2010–2017 Philipp Stephani, 2011–2017 Joseph Wright and contributors | LaTeX Project Public License 1.3c (`Third Party Notices/LPPL-1.3c.txt`) |
| Latin Modern Math | Unmodified font file, obtained from the [CSharpMath repository](https://github.com/verybadcat/CSharpMath/tree/69fb790c40693ae85d2d8679655ee04e88f35aea/CSharpMath.Rendering/Reference%20Fonts) | GUST Font License |
| Cyrillic Modern nmr10 | Unmodified font file, same source | SIL Open Font License 1.1 |
| AMS Capital Blackboard Bold, extracted by Happypig375 | Unmodified font file, same source | SIL Open Font License 1.1 |
| STIX Two Math 2.13 b171 | Unmodified `fonts/static_otf/STIXTwoMath-Regular.otf` from [stipub/stixfonts v2.13b171](https://github.com/stipub/stixfonts/tree/v2.13b171), stored as `Editor/Fonts/STIXTwoMath-Regular.otf` | SIL Open Font License 1.1 (`Third Party Notices/STIX-Two-OFL.txt`) |

Portions of this software are based in part on the work of the FreeType Team (via source files of Typography that carry the FreeType License; see `Third Party Notices/FreeType-FTL.txt` and `Original-Source-Headers.txt`).

The fonts' original copyright and family-name metadata is reproduced in their `*-Notice.txt` files. Latin Modern Math, Cyrillic Modern and AMS Capital Blackboard Bold are embedded in `Editor/Plugins/MathMesher.Fonts.dll`.

`MathMesher.Fonts.dll` is built from the Typography sources above: their namespaces are prefixed with `MathMesher.Internal` and nothing else is changed. It contains no other third-party code, no native code and no NuGet runtime dependency. The TeX parser, typesetter and mesh generation in `Editor/Engine` are MathMesher's own code.

LibTessDotNet source namespaces are prefixed with `MathMesher.Internal`. Original copyright and license headers are retained.

To reproduce the DLL, the tessellation sources and `Original-Source-Headers.txt`, run `Tools~/BuildMathEngine.ps1` with a .NET SDK capable of targeting .NET Standard 2.1. The script downloads the pinned original sources. Installed package users do not need to run it.
