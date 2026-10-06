using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MathMesher.Editor;

namespace MathMesher.Tests
{
    public sealed class MathMeshTests
    {
        [TestCase(@"x^2+y_1=0")]
        [TestCase(@"\frac{1}{\frac{a}{b}+1}")]
        [TestCase(@"\sqrt[3]{x^2+1}")]
        [TestCase(@"\sum_{n=1}^{\infty}\frac{1}{n^2}=\frac{\pi^2}{6}")]
        [TestCase(@"\int_0^\infty e^{-x^2}\,dx=\frac{\sqrt{\pi}}{2}")]
        [TestCase(@"\left(\frac{a}{b}\right)")]
        [TestCase(@"\begin{pmatrix}a&b\\c&d\end{pmatrix}")]
        [TestCase(@"\begin{cases}x^2&x\ge0\\-x&x<0\end{cases}")]
        [TestCase(@"\begin{aligned}a&=b+c\\d&=e\end{aligned}")]
        [TestCase(@"\hat{x}+\vec{v}+\overline{AB}")]
        [TestCase(@"\mathbb{R}\subset\mathcal{P}(X)")]
        [TestCase(@"\nabla\cdot\mathbf{E}=\frac{\rho}{\varepsilon_0}")]
        [TestCase(@"\hbar\omega+\varphi+\aleph_0")]
        [TestCase(@"\lim_{x\to0}\frac{\sin x}{x}=1")]
        [TestCase(@"\binom{n}{k}")]
        [TestCase(@"\underline{a+\cdots+a}")]
        [TestCase(@"\dfrac{a}{b}+\tfrac{c}{d}")]
        [TestCase(@"f^\prime(x)=x'")]
        [TestCase(@"\operatorname{rank}(A)")]
        [TestCase(@"\color{red}{x}+y")]
        [TestCase(@"$$\frac{a}{b}$$")]
        [TestCase(@"\[x^2\]")]
        [TestCase(@"\overbrace{a+b}^{n}+\underbrace{x+y}_{2}")]
        [TestCase(@"\overset{!}{=}\underset{x}{\max}\stackrel{\text{def}}{=}")]
        [TestCase(@"A\xrightarrow[\text{below}]{\text{above}}B\xlongequal{a}C")]
        [TestCase(@"\left\{x\middle|x>0\right\}\Bigl(\bigr)")]
        [TestCase(@"\boxed{E=mc^2}\colorbox{yellow}{x}\fcolorbox{red}{white}{y}")]
        [TestCase(@"\cancel{x}\bcancel{y}\xcancel{z}\cancelto{0}{x}\sout{a}")]
        [TestCase(@"\begin{array}{|c|c|}\hline\multicolumn{2}{|c|}{AB}\\\hline a&b\\\hline\end{array}")]
        [TestCase(@"\begin{align*}a&=b\tag{1}\\c&=d\end{align*}")]
        [TestCase(@"\begin{gather}a\\b\end{gather}\begin{multline}a\\b\\c\end{multline}")]
        [TestCase(@"\begin{CD}A@>a>>B\\@VbVV@AAcA\\C@=D\end{CD}")]
        [TestCase(@"\sum_{\substack{0<i<m\\0<j<n}}P(i,j)\sideset{_1^2}{_3^4}\prod")]
        [TestCase(@"\text{if $x>0$ and \'e \textbf{bold}}\mathscr{L}\mathfrak{g}\boldsymbol{\alpha}")]
        [TestCase(@"\widehat{xyz}\widetilde{abc}\overrightarrow{AB}\underleftarrow{x}\overgroup{AB}")]
        [TestCase(@"\not\in\nleq\coloneqq\iff\pmod{m}\bmod\iiint\oiint")]
        [TestCase(@"\genfrac{(}{)}{0pt}{}{n}{k}{a\over b}\sfrac{1}{2}\cfrac{1}{1+\cfrac{1}{x}}")]
        [TestCase(@"\enclose{circle}{x}\angl{n}\phase{\phi}\bbox[yellow]{x}")]
        [TestCase(@"\color[HTML]{FF8800}A\color{ForestGreen}B\textcolor{red!50!blue}{C}")]
        [TestCase(@"a&=b\\c&=d")]
        [TestCase(@"\begin{pmatrix*}[r]-1&2\\3&-4\end{pmatrix*}\begin{smallmatrix}a&b\end{smallmatrix}")]
        [TestCase(@"\char""263A\unicode{x2603}\verb|a_b|\TeX\LaTeX")]
        [TestCase(@"\text{Привет}")]
        [TestCase(@"a\wedgedot b\veedot c\wedgeodot d\veeodot e")]
        [TestCase(@"\mathbb{R}^n\textcolor{red}{x}^2\mathbf{v}_i\boldsymbol{\alpha}^2\mathchoice{a}{b}{c}{d}^2")]
        [TestCase(@"\lbag x\rbag\llangle y\rrangle\Bbbsum_{k}\fullouterjoin\sumint_0^1\QED\mitBbbd\candra{a}")]
        public void MathCorpusHasFiniteGeometry(string latex)
        {
            var result = MathMeshGenerator.Generate(latex);
            try
            {
                Assert.Greater(result.Mesh.vertexCount, 0);
                Assert.Greater(result.Mesh.triangles.Length, 0);
                Assert.IsTrue(result.Mesh.vertices.All(p => !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y)));
                Assert.Greater(result.AdvanceWidth, 0);
            }
            finally { UnityEngine.Object.DestroyImmediate(result.Mesh); }
        }

        [TestCase("0")]
        [TestCase("8")]
        public void GlyphCountersAreNotFilled(string latex)
        {
            var mesh = MathMeshGenerator.Generate(latex, new MathMeshSettings { doubleSided = false }).Mesh;
            try
            {
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                var center = mesh.bounds.center;
                if (latex == "8") center.y += mesh.bounds.size.y * 0.23f;
                bool covered = false;
                for (int i = 0; i < indices.Length; i += 3)
                    covered |= InsideTriangle(center, vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]);
                Assert.IsFalse(covered, "A point inside the glyph counter must remain uncovered.");
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        private static bool InsideTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float Cross(Vector3 v, Vector3 w) => v.x * w.y - v.y * w.x;
            var x = Cross(b - a, p - a); var y = Cross(c - b, p - b); var z = Cross(a - c, p - c);
            return (x >= 0 && y >= 0 && z >= 0) || (x <= 0 && y <= 0 && z <= 0);
        }

        [Test]
        public void ExtrusionHasThicknessAndOutwardNormalsIncludingHoleWalls()
        {
            var mesh = MathMeshGenerator.Generate("0", new MathMeshSettings { mode = MathMeshMode.Extruded, depth = 0.2f }).Mesh;
            try
            {
                Assert.AreEqual(0.2f, mesh.bounds.size.z, 0.00001f);
                Assert.IsTrue(mesh.normals.Any(n => n.z < -0.99f));
                Assert.IsTrue(mesh.normals.Any(n => n.z > 0.99f));
                var vertices = mesh.vertices; var normals = mesh.normals;
                var walls = Enumerable.Range(0, vertices.Length).Where(i => Mathf.Abs(normals[i].z) < 0.01f).ToArray();
                Assert.IsTrue(walls.Any(i => Vector3.Dot(vertices[i] - mesh.bounds.center, normals[i]) < 0), "Counter walls should face into the hole.");
                Assert.IsTrue(walls.Any(i => Vector3.Dot(vertices[i] - mesh.bounds.center, normals[i]) > 0), "Outer walls should face away from the glyph.");
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    Assert.Greater(Vector3.Dot(Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]), normals[a]), 0);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void HigherCurveQualityAddsVertices()
        {
            var coarse = MathMeshGenerator.Generate("8", new MathMeshSettings { curveTolerance = 0.02f }).Mesh;
            var fine = MathMeshGenerator.Generate("8", new MathMeshSettings { curveTolerance = 0.0002f }).Mesh;
            try { Assert.Greater(fine.vertexCount, coarse.vertexCount); }
            finally { UnityEngine.Object.DestroyImmediate(coarse); UnityEngine.Object.DestroyImmediate(fine); }
        }

        [TestCase(@"\newcommand{\foo}{x}\foo")]
        [TestCase(@"\def\foo{x}")]
        [TestCase(@"\documentclass{article}")]
        [TestCase(@"\notARealMathCommand{x}")]
        [TestCase(@"\left(x")]
        [TestCase(@"\begin{unknownenv}x\end{unknownenv}")]
        [TestCase(@"x^2^3")]
        [TestCase(@"\text{日本語}")]
        [TestCase(@"\frac{a}{")]
        public void InvalidInputsReportErrors(string latex) => Assert.Throws<ArgumentException>(() => MathMeshGenerator.Generate(latex));

        [Test]
        public void RepeatedFormulaIsServedFromCacheAndEditsReuseGlyphs()
        {
            var settings = new MathMeshSettings();
            var first = MathMeshGenerator.Generate(@"\frac{a+b}{c}", settings);
            var second = MathMeshGenerator.Generate(@"\frac{a+b}{c}", settings);
            var edited = MathMeshGenerator.Generate(@"\frac{a+b}{c}+b", settings);
            try
            {
                Assert.IsTrue(second.FromCache);
                Assert.AreEqual(first.Mesh.vertexCount, second.Mesh.vertexCount);
                Assert.IsFalse(edited.FromCache);
                Assert.Greater(edited.Mesh.vertexCount, first.Mesh.vertexCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first.Mesh);
                UnityEngine.Object.DestroyImmediate(second.Mesh);
                UnityEngine.Object.DestroyImmediate(edited.Mesh);
            }
        }

        [Test]
        public void PreviewReusesTheSameMeshObject()
        {
            var go = new GameObject("Math");
            try
            {
                var component = go.AddComponent<MathMesh>();
                component.LaTeX = "x";
                Assert.IsTrue(MathMeshAuthoring.Regenerate(component));
                var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                int before = mesh.vertexCount;
                component.LaTeX = "x+y";
                Assert.IsTrue(MathMeshAuthoring.Regenerate(component));
                Assert.AreSame(mesh, go.GetComponent<MeshFilter>().sharedMesh);
                Assert.Greater(mesh.vertexCount, before);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void ColorboxBackgroundSitsBehindText()
        {
            var settings = new MathMeshSettings { doubleSided = false, alignment = MathMeshAlignment.BaselineLeft };
            var mesh = MathMeshGenerator.Generate(@"\colorbox{yellow}{\color{blue}x}", settings).Mesh;
            try
            {
                var colors = mesh.colors;
                var vertices = mesh.vertices;
                float textZ = float.MaxValue, backZ = float.MinValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (colors[i].r < 0.5f) textZ = Mathf.Min(textZ, vertices[i].z);
                    else backZ = Mathf.Max(backZ, vertices[i].z);
                }
                Assert.Less(textZ, backZ, "The text must be in front of (smaller z than) the background.");
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void EmptyInputClearsGeometry()
        {
            var mesh = MathMeshGenerator.Generate("").Mesh;
            try { Assert.AreEqual(0, mesh.vertexCount); }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void SavingKeepsGuidAndUsesTheVertexColorMaterial()
        {
            string folder = "Assets/MathMesherTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            var go = new GameObject("Math");
            try
            {
                var component = go.AddComponent<MathMesh>();
                component.LaTeX = "x";
                var first = MathMeshAuthoring.BakeToAsset(component, folder + "/Math.asset");
                string guid = AssetDatabase.AssetPathToGUID(folder + "/Math.asset");
                float width = first.bounds.size.x;
                component.LaTeX = "x+x+x";
                var second = MathMeshAuthoring.BakeToAsset(component, folder + "/Math.asset");
                Assert.AreSame(first, second);
                Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(folder + "/Math.asset"));
                Assert.Greater(second.bounds.size.x, width);
                Assert.AreSame(second, go.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreEqual("MathMesher/Vertex Color", go.GetComponent<MeshRenderer>().sharedMaterial.shader.name);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); AssetDatabase.DeleteAsset(folder); }
        }

        [Test]
        public void InvalidPreviewKeepsPreviousMesh()
        {
            var go = new GameObject("Math");
            try
            {
                var component = go.AddComponent<MathMesh>();
                component.LaTeX = "x";
                Assert.IsTrue(MathMeshAuthoring.Regenerate(component));
                var original = go.GetComponent<MeshFilter>().sharedMesh;
                component.LaTeX = @"\badcommand";
                Assert.IsFalse(MathMeshAuthoring.Regenerate(component));
                Assert.AreSame(original, go.GetComponent<MeshFilter>().sharedMesh);
                Assert.IsNotEmpty(MathMeshAuthoring.GetError(component));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void BuildSceneBakesCurrentInputAndRemovesAuthoringComponent()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            Mesh builtMesh = null;
            try
            {
                var go = new GameObject("Math");
                SceneManager.MoveGameObjectToScene(go, scene);
                var component = go.AddComponent<MathMesh>();
                component.LaTeX = "x";
                Assert.IsTrue(MathMeshAuthoring.Regenerate(component));
                float originalWidth = go.GetComponent<MeshFilter>().sharedMesh.bounds.size.x;
                component.AutoUpdate = false;
                component.LaTeX = "x+x+x";
                new MathMeshBuildProcessor().OnProcessScene(scene, null);
                Assert.IsNull(go.GetComponent<MathMesh>());
                builtMesh = go.GetComponent<MeshFilter>().sharedMesh;
                Assert.Greater(builtMesh.bounds.size.x, originalWidth);
                Assert.AreEqual(HideFlags.None, builtMesh.hideFlags);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                if (builtMesh) UnityEngine.Object.DestroyImmediate(builtMesh);
            }
        }

        [Test]
        public void MathEngineIsEditorOnlyAndHasNoNuGetRuntimeDependencies()
        {
            var importer = (PluginImporter)AssetImporter.GetAtPath("Packages/com.kinankomoti.mathmesher/Editor/Plugins/TeXmexsh.MathEngine.dll");
            Assert.IsTrue(importer.GetCompatibleWithEditor());
            Assert.IsFalse(importer.GetCompatibleWithAnyPlatform());
            Assert.IsFalse(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64));
            var references = System.Reflection.Assembly.Load("TeXmexsh.MathEngine").GetReferencedAssemblies();
            Assert.IsFalse(references.Any(a => a.Name == "System.Memory" || a.Name == "System.Runtime.CompilerServices.Unsafe"));
        }
    }
}
