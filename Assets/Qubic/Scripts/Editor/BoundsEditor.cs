using UnityEditor;
using UnityEngine;
using System;

namespace QubicNS
{
    [InitializeOnLoad]
    public static class BoundsEditor
    {
        public static Bounds currentBounds;
        private static bool isEditing = false;
        public static bool IsEditing => isEditing;
        private static Action<Bounds> onBoundsChanged;

        private static Vector3[] directions = {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back
        };

        static BoundsEditor()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static void StartEdit(Bounds bounds, Action<Bounds> onChangedCallback = null)
        {
            currentBounds = bounds;
            onBoundsChanged = onChangedCallback;
            isEditing = true;
        }

        public static void StopEdit()
        {
            isEditing = false;
            onBoundsChanged = null;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!isEditing) return;

            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                StopEdit();
                e.Use();
                SceneView.RepaintAll();
                return;
            }

            Handles.color = Color.yellow;
            Handles.DrawWireCube(currentBounds.center, currentBounds.size);

            EditorGUI.BeginChangeCheck();

            foreach (Vector3 dir in directions)
            {
                Vector3 handlePos = currentBounds.center + Vector3.Scale(dir, currentBounds.size * 0.5f);
                float handleSize = HandleUtility.GetHandleSize(handlePos) * 0.15f;
                Handles.color = dir.y != 0 ? Color.green : dir.x != 0 ? Color.red : Color.blue;
                Vector3 newHandlePos = Handles.Slider(handlePos, dir, handleSize, Handles.CubeHandleCap, 0.01f);

                if (handlePos != newHandlePos)
                {
                    float delta = Vector3.Dot(newHandlePos - handlePos, dir);
                    Vector3 deltaVec = dir * delta;

                    Vector3 newSize = currentBounds.size + Vector3.Scale(deltaVec, dir);
                    Vector3 newCenter = currentBounds.center + deltaVec * 0.5f;

                    // Clamp to prevent negative sizes
                    newSize.x = Mathf.Max(0.01f, newSize.x);
                    newSize.y = Mathf.Max(0.01f, newSize.y);
                    newSize.z = Mathf.Max(0.01f, newSize.z);

                    currentBounds = new Bounds(newCenter, newSize);
                    onBoundsChanged?.Invoke(currentBounds);
                    break;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }
    }
}