using System;
using UnityEditor;
using UnityEngine;

namespace MathMesher.Editor
{
    [CustomEditor(typeof(MathMesh))]
    public sealed class MathMeshInspector : UnityEditor.Editor
    {
        private SerializedProperty latex, autoUpdate;
        private SerializedProperty mode, mathStyle, alignment, emSize, depth, curveTolerance, doubleSided, color, fallbackFonts;
        private string actionError;

        private void OnEnable()
        {
            latex = serializedObject.FindProperty("latex");
            autoUpdate = serializedObject.FindProperty("autoUpdate");
            var settings = serializedObject.FindProperty("settings");
            mode = settings.FindPropertyRelative("mode");
            mathStyle = settings.FindPropertyRelative("mathStyle");
            alignment = settings.FindPropertyRelative("alignment");
            emSize = settings.FindPropertyRelative("emSize");
            depth = settings.FindPropertyRelative("depth");
            curveTolerance = settings.FindPropertyRelative("curveTolerance");
            doubleSided = settings.FindPropertyRelative("doubleSided");
            color = settings.FindPropertyRelative("color");
            fallbackFonts = settings.FindPropertyRelative("fallbackFonts");
        }

        private static GUIContent Label(SerializedProperty p, string text) => new GUIContent(text, p.tooltip);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var component = (MathMesh)target;

            EditorGUILayout.PropertyField(latex, new GUIContent("Formula"));
            EditorGUILayout.PropertyField(autoUpdate, new GUIContent("Auto update"));
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Update"))
                {
                    actionError = null;
                    MathMeshAuthoring.Regenerate(component, true);
                }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(mode, Label(mode, "Mode"));
            // settings that only apply to one mode are indented under it
            bool mixed = mode.hasMultipleDifferentValues;
            bool extruded = !mixed && mode.enumValueIndex == (int)MathMeshMode.Extruded;
            using (new EditorGUI.IndentLevelScope())
            {
                if (extruded || mixed) EditorGUILayout.PropertyField(depth, Label(depth, "Depth"));
                if (!extruded || mixed) EditorGUILayout.PropertyField(doubleSided, Label(doubleSided, "Double sided"));
            }
            EditorGUILayout.PropertyField(mathStyle, Label(mathStyle, "Math style"));
            EditorGUILayout.PropertyField(alignment, Label(alignment, "Alignment"));
            EditorGUILayout.PropertyField(emSize, Label(emSize, "Font size"));
            EditorGUILayout.PropertyField(curveTolerance, Label(curveTolerance, "Curve tolerance"));
            EditorGUILayout.PropertyField(color, Label(color, "Color"));
            EditorGUILayout.PropertyField(fallbackFonts, Label(fallbackFonts, "Fallback fonts"), true);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Save mesh...")) Save(component);
            if (component.SavedMesh)
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Saved mesh", component.SavedMesh, typeof(Mesh), false);

            var error = actionError ?? MathMeshAuthoring.GetError(component);
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            var mesh = component.GetComponent<MeshFilter>().sharedMesh;
            if (mesh)
            {
                var (ms, cached) = MathMeshAuthoring.GetTiming(component);
                var timing = cached ? "cached" : $"{ms:F1} ms";
                EditorGUILayout.LabelField($"Vertices: {mesh.vertexCount:N0}    Triangles: {(mesh.subMeshCount > 0 ? mesh.GetIndexCount(0) / 3 : 0):N0}    Generated: {timing}", EditorStyles.miniLabel);
            }
        }

        private void Save(MathMesh component)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save mesh", component.gameObject.name, "asset", "Choose where to save the mesh.");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var asset = MathMeshAuthoring.BakeToAsset(component, path);
                actionError = null;
                EditorGUIUtility.PingObject(asset);
            }
            catch (Exception ex) { actionError = ex.Message; }
        }

        [MenuItem("GameObject/MathMesher/Math mesh", false, 10)]
        private static void Create(MenuCommand command)
        {
            var go = new GameObject("Math mesh");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create math mesh");
            var component = go.AddComponent<MathMesh>();
            MathMeshAuthoring.Regenerate(component);
            Selection.activeGameObject = go;
        }
    }
}
