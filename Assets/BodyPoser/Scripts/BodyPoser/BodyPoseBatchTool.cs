using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RevolvingGear.BodyPoser
{
    public class BodyPoseBatchTool : MonoBehaviour
    {
        [Tooltip("Optional toggle to disable physics on all models at start. Sets rigidbodies to kinematic and disables colliders.")]
        [SerializeField] private bool turnOffPhysicsOnStart = false;

        [Tooltip("List of BodyPoser components to control in bulk. Drag them in manually.")]
        [SerializeField] public BodyPoser[] targets;

        private void Start()
        {
            if (turnOffPhysicsOnStart)
            {
                foreach (var poser in targets)
                {
                    if (poser != null)
                    {
                        var rbList = poser.GetComponentsInChildren<Rigidbody>();
                        foreach (var rb in rbList) rb.isKinematic = true;

                        var colList = poser.GetComponentsInChildren<Collider>();
                        foreach (var col in colList) col.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Saves the current pose of all listed BodyPoser targets to individual JSON files.
        /// </summary>
        public void CaptureAllPoses()
        {
            foreach (var poser in targets)
            {
                if (poser != null)
                    poser.CapturePoseToJson();
            }
        }

        /// <summary>
        /// Tries to apply stored poses to all listed BodyPoser targets.
        /// </summary>
        /// <returns>True if all poses were applied successfully; false if any failed.</returns>
        public bool ApplyAllStoredPoses()
        {
            bool allSuccess = true;
            foreach (var poser in targets)
            {
                if (poser != null)
                {
                    bool result = poser.TryApplyStoredPose();
                    if (!result)
                        allSuccess = false;
                }
            }
            return allSuccess;
        }

        /// <summary>
        /// Removes Rigidbody, Collider, and CharacterJoint components from all targets.
        /// </summary>
        public void RemoveAllPhysics()
        {
            foreach (var poser in targets)
            {
                if (poser != null)
                    poser.RemovePhysicsComponents();
            }
        }
    }
}

#if UNITY_EDITOR
namespace RevolvingGear.BodyPoser
{
    [CustomEditor(typeof(BodyPoseBatchTool))]
    public class BodyPoseBatchToolEditor : Editor
    {
        private static string statusMessage = "";
        private static float messageDisplayTime = 2f;
        private static float lastMessageTime = -10f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BodyPoseBatchTool tool = (BodyPoseBatchTool)target;

            EditorGUILayout.HelpBox(
                "This is a batch poser tool used to control multiple bodies/models at once.\n" +
                "Make sure each object has a Rigidbody and a BodyPoser script attached.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("📸 Capture All Poses"))
            {
                tool.CaptureAllPoses();
                ShowMessage("All poses captured.");
            }

            if (GUILayout.Button("📐 Apply All Stored Poses"))
            {
                bool allApplied = tool.ApplyAllStoredPoses();
                ShowMessage(allApplied ? "All poses applied successfully." : "Some models failed to apply poses.");
            }

            if (GUILayout.Button("🗑️ Remove All Physics Components"))
            {
                tool.RemoveAllPhysics();
                ShowMessage("Physics components removed on listed models.");
            }

            if (GUILayout.Button("🪨 Make All Static"))
            {
                int count = 0;
                foreach (var poser in tool.targets)
                {
                    if (poser != null)
                    {
                        poser.gameObject.isStatic = true;
                        count++;
                    }
                }
                ShowMessage($"{count} object(s) marked as static.");
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                if ((float)EditorApplication.timeSinceStartup - lastMessageTime < messageDisplayTime)
                {
                    EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
                    Repaint();
                }
                else statusMessage = "";
            }
        }

        private void ShowMessage(string msg)
        {
            statusMessage = msg;
            lastMessageTime = (float)EditorApplication.timeSinceStartup;
        }
    }
}
#endif
