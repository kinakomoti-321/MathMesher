// Copy into an isolated verification project's Assets/Editor directory.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using MathMesher;
using MathMesher.Editor;

public static class MathMesherVerification
{
    public static void Run()
    {
        string output = Directory.GetParent(Application.dataPath).Parent.FullName;
        if (!AssetDatabase.IsValidFolder("Assets/MathMesherExamples")) AssetDatabase.CreateFolder("Assets", "MathMesherExamples");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMath("Quadratic", @"x = \frac{-b \pm \sqrt{b^2-4ac}}{2a}", false, new Vector3(0, 1.25f, 0), Color.white);
        var integral = CreateMath("Integral", @"\int_0^\infty e^{-x^2}\,dx=\frac{\sqrt{\pi}}{2}", true, new Vector3(0, -1.2f, 0), new Color(0.25f, 0.9f, 0.78f));
        integral.transform.rotation = Quaternion.Euler(12, -18, 0);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -12);
        camera.orthographic = true;
        camera.orthographicSize = 3.1f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.05f, 0.075f);
        camera.tag = "MainCamera";
        EditorSceneManager.SaveScene(scene, "Assets/MathMesherExamples/Examples.unity");

        var render = new RenderTexture(1600, 900, 24) { antiAliasing = 4 };
        var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = render;
            camera.Render();
            RenderTexture.active = render;
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            texture.Apply();
            int visible = texture.GetPixels32().Count(c => c.r > 100 || c.g > 100);
            File.WriteAllBytes(Path.Combine(output, "Preview.png"), texture.EncodeToPNG());
            if (visible < 1000) throw new Exception("Rendered formulas were not visible: " + visible);
            File.WriteAllText(Path.Combine(output, "render-result.txt"), "Visible formula pixels: " + visible);
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(render);
        }

        // Add a live authoring object to check the scene build callback as well as baked assets.
        var live = new GameObject("BuildCallbackFormula").AddComponent<MathMesh>();
        live.LaTeX = "x^2";
        live.AutoUpdate = false;
        live.transform.position = new Vector3(0, 0, 4);
        new GameObject("Player Probe").AddComponent<MathMesherPlayerProbe>();
        EditorSceneManager.SaveScene(scene, "Assets/BuildSmoke.unity");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/BuildSmoke.unity" },
            locationPathName = Path.Combine(output, "Player/MathMesherSmoke.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Standalone build failed: " + report.summary.result);
        if (Directory.GetFiles(Path.Combine(output, "Player"), "MathMesher.Fonts.dll", SearchOption.AllDirectories).Length != 0)
            throw new Exception("Math engine was incorrectly included in the Player.");
        File.WriteAllText(Path.Combine(output, "build-result.txt"), "StandaloneWindows64: " + report.summary.result + "; math engine DLL excluded.");
        Debug.Log("MathMesher rendering and standalone build verification passed.");
    }

    private static GameObject CreateMath(string name, string latex, bool extruded, Vector3 position, Color color)
    {
        var go = new GameObject(name);
        var component = go.AddComponent<MathMesh>();
        component.LaTeX = latex;
        component.Settings.mode = extruded ? MathMeshMode.Extruded : MathMeshMode.Flat;
        component.Settings.emSize = 0.75f;
        component.Settings.depth = 0.22f;
        component.Settings.color = color;
        MathMeshAuthoring.BakeToAsset(component, "Assets/MathMesherExamples/" + name + " Mesh.asset");
        UnityEngine.Object.DestroyImmediate(component);
        go.transform.position = position;
        return go;
    }
}
