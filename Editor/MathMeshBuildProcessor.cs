using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathMesher.Editor
{
    /// <summary>Build scene copies use freshly generated meshes and no authoring components.</summary>
    public sealed class MathMeshBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MathMesh>(true))
                {
                    try
                    {
                        var result = MathMeshGenerator.Generate(component.LaTeX, component.Settings);
                        result.Mesh.hideFlags = HideFlags.None;
                        component.GetComponent<MeshFilter>().sharedMesh = result.Mesh;
                        if (!component.GetComponent<MeshRenderer>().sharedMaterial)
                            component.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.kinankomoti.mathmesher/Runtime/Materials/MathMesher.mat");
                        UnityEngine.Object.DestroyImmediate(component);
                    }
                    catch (Exception ex) { throw new BuildFailedException("MathMesher: could not generate the formula of " + component.gameObject.name + ".\n" + ex.Message); }
                }
        }
    }
}
