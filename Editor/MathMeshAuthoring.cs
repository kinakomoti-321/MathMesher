using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MathMesher.Editor
{
    [InitializeOnLoad]
    public static class MathMeshAuthoring
    {
        private sealed class State
        {
            internal MathMesh Component;
            internal Mesh Preview;
            internal string Error;
            internal double Due;
            internal bool Pending;
            internal string LastKey;
            internal double LastMilliseconds;
            internal bool LastFromCache;
        }
        private static readonly ConcurrentQueue<MathMesh> changed = new ConcurrentQueue<MathMesh>();
        private static readonly Dictionary<int, State> states = new Dictionary<int, State>();
        private const string MaterialPath = "Packages/com.kinankomoti.mathmesher/Runtime/Materials/MathMesher.mat";

        static MathMeshAuthoring()
        {
            MathMesh.EditorChanged += component => changed.Enqueue(component);
            MathMesh.EditorDestroyed += component => changed.Enqueue(component);
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
            EditorApplication.quitting += Cleanup;
            Undo.undoRedoPerformed += EnqueueAll;
            EditorApplication.delayCall += EnqueueAll;
        }

        private static void EnqueueAll()
        {
            foreach (var component in Resources.FindObjectsOfTypeAll<MathMesh>())
                if (component.gameObject.scene.IsValid()) changed.Enqueue(component);
        }

        private static State GetState(MathMesh component)
        {
            int id = component.GetInstanceID();
            if (!states.TryGetValue(id, out var state)) states[id] = state = new State { Component = component };
            return state;
        }

        private static void Update()
        {
            while (changed.TryDequeue(out var component))
            {
                if (!component) continue;
                var state = GetState(component);
                // generation is incremental and cached, so a very short debounce keeps typing responsive
                state.Due = EditorApplication.timeSinceStartup + 0.02;
                state.Pending = true;
            }
            var removed = new List<int>();
            foreach (var pair in states)
            {
                var state = pair.Value;
                if (!state.Component)
                {
                    DestroyPreview(state);
                    removed.Add(pair.Key);
                    continue;
                }
                if (!state.Pending || EditorApplication.timeSinceStartup < state.Due || EditorApplication.isCompiling || Application.isPlaying) continue;
                state.Pending = false;
                if (state.Component.AutoUpdate && !EditorUtility.IsPersistent(state.Component)) Regenerate(state.Component);
            }
            foreach (var id in removed) states.Remove(id);
        }

        private static void DestroyPreview(State state)
        {
            if (state.Preview) UnityEngine.Object.DestroyImmediate(state.Preview);
            state.Preview = null;
        }

        private static void Cleanup()
        {
            foreach (var state in states.Values)
            {
                if (state.Component)
                {
                    var filter = state.Component.GetComponent<MeshFilter>();
                    if (filter && filter.sharedMesh == state.Preview) filter.sharedMesh = state.Component.SavedMesh;
                }
                DestroyPreview(state);
            }
            states.Clear();
        }

        public static string GetError(MathMesh component) => GetState(component).Error;

        /// <summary>Last generation time in milliseconds and whether it was served from the cache.</summary>
        public static (double milliseconds, bool fromCache) GetTiming(MathMesh component)
        {
            var state = GetState(component);
            return (state.LastMilliseconds, state.LastFromCache);
        }

        private static string Key(MathMesh component) => component.LaTeX + "\n" + JsonUtility.ToJson(component.Settings);

        public static bool Regenerate(MathMesh component) => Regenerate(component, false);

        public static bool Regenerate(MathMesh component, bool force)
        {
            if (!component) return false;
            var state = GetState(component);
            state.Pending = false;
            var filter = component.GetComponent<MeshFilter>();
            var renderer = component.GetComponent<MeshRenderer>();
            string key = Key(component);
            if (!force && state.Preview && state.Error == null && key == state.LastKey && filter.sharedMesh == state.Preview) return true;
            try
            {
                bool created = !state.Preview;
                var mesh = created ? new Mesh { name = "MathMesher Preview", hideFlags = HideFlags.DontSaveInEditor } : state.Preview;
                MathMeshResult result;
                try { result = MathMeshGenerator.GenerateInto(mesh, component.LaTeX, component.Settings); }
                catch { if (created) UnityEngine.Object.DestroyImmediate(mesh); throw; }
                state.Preview = mesh;
                state.LastKey = key;
                state.LastMilliseconds = result.Milliseconds;
                state.LastFromCache = result.FromCache;
                if (filter.sharedMesh != mesh) filter.sharedMesh = mesh;
                state.Error = null;
                if (!renderer.sharedMaterial) renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                SceneView.RepaintAll();
                return true;
            }
            catch (Exception ex)
            {
                state.Error = ex.Message;
                state.LastKey = null;
                return false;
            }
        }

        /// <summary>Save or update a Mesh asset. An existing Mesh at this path keeps its GUID.</summary>
        public static Mesh BakeToAsset(MathMesh component, string assetPath)
        {
            ValidateAssetPath(assetPath, ".asset");
            var existing = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (existing && !(existing is Mesh)) throw new ArgumentException("An asset that is not a mesh already exists at that path.");
            var result = MathMeshGenerator.Generate(component.LaTeX, component.Settings);
            var mesh = existing as Mesh;
            if (mesh)
            {
                Undo.RegisterCompleteObjectUndo(mesh, "Save math mesh");
                EditorUtility.CopySerialized(result.Mesh, mesh);
                UnityEngine.Object.DestroyImmediate(result.Mesh);
                EditorUtility.SetDirty(mesh);
            }
            else
            {
                mesh = result.Mesh;
                AssetDatabase.CreateAsset(mesh, assetPath);
            }
            mesh.name = Path.GetFileNameWithoutExtension(assetPath);
            Undo.RecordObjects(new UnityEngine.Object[] { component, component.GetComponent<MeshFilter>(), component.GetComponent<MeshRenderer>() }, "Save math mesh");
            component.SavedMesh = mesh;
            component.GetComponent<MeshFilter>().sharedMesh = mesh;
            if (!component.GetComponent<MeshRenderer>().sharedMaterial)
                component.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var state = GetState(component);
            DestroyPreview(state);
            state.Pending = false;
            state.Error = null;
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component.GetComponent<MeshFilter>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(component.GetComponent<MeshRenderer>());
            if (component.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static void ValidateAssetPath(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") || !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a " + extension + " file inside Assets.");
            if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(path).Replace('\\', '/')))
                throw new ArgumentException("The destination folder does not exist.");
        }
    }
}
