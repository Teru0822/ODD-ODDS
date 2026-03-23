using UnityEditor;

namespace QubicNS
{
    [InitializeOnLoad]
    public static class SelectionTracker
    {
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            var builder = Selection.activeGameObject?.GetComponentInParent<QubicBuilder>();
            if (builder != null)
                QubicBuilder.LastSelectedBuilder = builder;
        }
    }
}
