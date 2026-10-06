# Validation — 0.2.0

Date: 2026-10-05

Environment: Windows, Unity 2022.3.22f1, Built-in Render Pipeline, Editor only.

- 64 EditMode tests passed in a separate minimal project that references the package by `file:` path. Coverage includes a notation corpus (environments, braces, extensible arrows, enclosures, colors, text mode, CD diagrams, tags), hollow glyphs, extrusion and normal orientation, curve quality, invalid input, missing glyphs, result caching, in-place preview updates, background depth ordering, saving without changing GUIDs, prefab export, build-scene regeneration and Editor-only plugin configuration.
- A standalone .NET harness compiled the Unity-independent engine (`Editor/Engine`) and rendered about 130 formulas to images for visual inspection against TeX output. All parsed and produced geometry.
- Timing on the development machine for a Schrödinger-equation-sized formula: about 195 ms for the first generation in a session (including font loading), 0.3–7 ms for subsequent edits (only new glyphs are triangulated), and about 0.2 ms when the same formula is requested again.

Not re-validated for 0.2.0: Windows Standalone Player build and run (validated for 0.1.0; the build processor and runtime components are unchanged), the `.tgz` import, URP/HDRP, other Editor operating systems, IL2CPP, mobile and WebGL Player builds, and VRChat upload workflows.

## 0.1.0

37 EditMode tests, a Windows Standalone Player build and run, and a `.tgz` import into a clean project passed. See the package history for details.
