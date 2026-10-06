// Verification only: copy into an isolated project's Assets directory.
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using MathMesher;

public sealed class MathMesherPlayerProbe : MonoBehaviour
{
    private void Start()
    {
        string output = null;
        var arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < arguments.Length; i++) if (arguments[i] == "-texmexshReport") output = arguments[i + 1];
        try
        {
            var filters = FindObjectsOfType<MeshFilter>();
            if (filters.Length < 3 || filters.Any(f => !f.sharedMesh || f.sharedMesh.vertexCount == 0)) throw new Exception("Missing generated Mesh in Player.");
            if (FindObjectsOfType<MathMesh>().Length != 0) throw new Exception("Authoring components were not removed during build.");
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "MathMesher.Fonts")) throw new Exception("Math engine loaded in Player.");
            if (output != null) File.WriteAllText(output, "PASS: 3 meshes present; no authoring component or math engine loaded.");
            Debug.Log("MathMesher Player verification passed.");
            Application.Quit(0);
        }
        catch (Exception ex)
        {
            if (output != null) File.WriteAllText(output, "FAIL: " + ex.Message);
            Debug.LogException(ex);
            Application.Quit(1);
        }
    }
}
