# Changelog

## 0.3.2 — 2026-10-11

- Licensed MathMesher itself under the MIT License and added the SPDX manifest field.
- Added Unity Single Pass Instanced stereo macros and instancing shader variants to the built-in MathMesher shader for VR use.
- Added a GitHub Actions release workflow for the repository layout where the package is stored at the repository root.

## 0.3.1 — 2026-10-06

- Removed CSharpMath. The bundled DLL is rebuilt as `Editor/Plugins/MathMesher.Fonts.dll` and contains only Typography's OpenType reader (OpenFont, GlyphOutlineBuilderBase) and the embedded math fonts; namespaces are `MathMesher.Internal.*`. The DLL's `.meta` (GUID) is kept. Size 1.81 MB → 1.44 MB.
- Third-party notices: dropped CSharpMath and Typography TextBreak, regenerated the per-file license headers of the compiled Typography sources, and added the FreeType credit.

## 0.3.0 — 2026-10-06

- Renamed the package from TeXmexsh to MathMesher: package id `com.kinankomoti.mathmesher`, namespaces `MathMesher` / `MathMesher.Editor`, classes `MathMesh`, `MathMeshSettings`, `MathMeshGenerator`, `MathMeshAuthoring`, menu **GameObject → MathMesher → Math mesh**, shader `MathMesher/Vertex Color`. Script GUIDs are unchanged. The prebuilt DLL keeps its internal name `TeXmexsh.MathEngine`. Entries below use the new names.
- Inspector: English labels in sentence case; "Formula" field; **Update** button under **Auto update**; `Depth` shown only for Extruded and `Double sided` only for Flat; `Em size` is now labelled **Font size**; removed the title and the hint box.
- Removed prefab export (`ExportPrefab` and its button). **Save mesh...** remains.

## 0.2.2 — 2026-10-06

- Fixed font commands used as a script base (`\mathbb{R}^n`, `\mathbf{v}_i`) being drawn as poor man's bold without their alphabet.
- Fixed an internal error for colors, styles, sizes and `\mathchoice` used as a script base (`\textcolor{red}{x}^2`).

## 0.2.1 — 2026-10-05

- Added all 2447 symbol commands of the unicode-math package (for example `\wedgedot`, `\veedot`, `\lbag`, `\llangle`, `\Bbbsum`), generated from `unicode-math-table.tex` by `Tools~/GenerateUnicodeMathSymbols.py`. Every generated command has a glyph in the bundled fonts. Existing LaTeX / AMS definitions keep priority.

## 0.2.0 — 2026-10-05

- Replaced the CSharpMath typesetter with a package-owned TeX math parser and typesetter (TeX Appendix G rules with OpenType MATH metrics).
- Added broad notation coverage: braces, over/under sets, extensible arrows and wide accents, `\middle` and `\big` delimiters, all amsmath environments, array column specs and rules, `\multicolumn`, CD diagrams, tags, text mode with nested math, xcolor models, boxes, cancel and `\enclose`, spacing, phantoms, laps and many more symbols.
- Added STIX Two Math as a bundled fallback math font and user-selectable fallback fonts for other scripts.
- Added glyph triangulation, per-piece vertex and whole-result caches; the live preview now updates in place within milliseconds after the first generation.
- `\colorbox` and other backgrounds are placed slightly behind the text to avoid z-fighting.

## 0.1.0 — 2026-10-05

- Added a self-contained UPM package with a managed Editor-only math engine and embedded fonts.
- Added TeX math authoring, debounced scene preview, flat and extruded meshes, curve quality, alignment and vertex color.
- Added Mesh saving with stable asset references and export of prefabs with built-in rendering components.
- Added build-time regeneration and removal of authoring components from build scenes.
- Added geometry, compatibility and asset lifecycle tests, examples and reproduction tools.
