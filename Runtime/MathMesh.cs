using UnityEngine;

namespace MathMesher
{
    /// <summary>Authoring data. All parsing and mesh generation happen in the Editor.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("MathMesher/Math mesh")]
    public sealed class MathMesh : MonoBehaviour
    {
        [SerializeField, TextArea(3, 12)] private string latex = @"x = \frac{-b \pm \sqrt{b^2-4ac}}{2a}";
        [SerializeField] private MathMeshSettings settings = new MathMeshSettings();
        [SerializeField] private bool autoUpdate = true;
        [SerializeField, HideInInspector] private Mesh savedMesh;

        public string LaTeX { get => latex; set { latex = value; NotifyChanged(); } }
        public MathMeshSettings Settings => settings;
        public bool AutoUpdate { get => autoUpdate; set { autoUpdate = value; NotifyChanged(); } }
        public Mesh SavedMesh { get => savedMesh; set => savedMesh = value; }

#if UNITY_EDITOR
        public static event System.Action<MathMesh> EditorChanged;
        public static event System.Action<MathMesh> EditorDestroyed;
        private void OnEnable() => NotifyChanged();
        private void OnValidate() => NotifyChanged();
        private void OnDestroy() => EditorDestroyed?.Invoke(this);
#endif

        /// <summary>Call after editing Settings through a script to request an Editor preview.</summary>
        public void NotifyChanged()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) EditorChanged?.Invoke(this);
#endif
        }
    }
}
