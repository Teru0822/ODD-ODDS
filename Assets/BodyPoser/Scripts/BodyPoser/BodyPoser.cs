using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace RevolvingGear.BodyPoser
{

    public class BodyPoser : MonoBehaviour
    {
        [Header("Physics")]
        [Tooltip("Disables physics (Rigidbodies and Colliders) at startup to keep the body static. Only if you did not remove physics elements")]
        [SerializeField] private bool turnOffPhysics = false;
        [Header("Editor mode")]
        [Tooltip("Use rotators and transformers to put a model in a specific position")]
        [SerializeField] public bool poseEditMode = false; // Toggle for Editor Gizmo Mode

        [System.Serializable]
        public class BonePose
        {
            public string boneName;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        [System.Serializable]
        public class PoseDataWrapper
        {
            public List<BonePose> poses;
            public string timestamp;
        }

        private static readonly string PoseDataPath = "Assets/BodyPoser/TempPoseData/";

        private void Start()
        {
            //turns of physics for if you do not want to remove things
            if (turnOffPhysics)
            {
                DisablePhysicsIfPresent();
            }
        }

        public void CapturePoseToJson()
        {
            var bones = GetComponentsInChildren<Transform>();
            List<BonePose> capturedPose = new List<BonePose>();

            foreach (var bone in bones)
            {
                capturedPose.Add(new BonePose
                {
                    boneName = bone.name,
                    localPosition = bone.localPosition,
                    localRotation = bone.localRotation
                });
            }

            PoseDataWrapper wrapper = new PoseDataWrapper
            {
                poses = capturedPose,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            if (!Directory.Exists(PoseDataPath))
                Directory.CreateDirectory(PoseDataPath);

            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(GetJsonFilePath(), json);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
            BodyPoserEditor.ShowMessage("Pose Captured and Stored!");
#endif
        }

        public bool TryApplyStoredPose()
        {
            if (!HasStoredPose())
                return false;

            string json = File.ReadAllText(GetJsonFilePath());
            PoseDataWrapper wrapper = JsonUtility.FromJson<PoseDataWrapper>(json);

            bool success = true;
            foreach (var pose in wrapper.poses)
            {
                var bone = FindBoneByName(pose.boneName);
                if (bone != null)
                {
                    bone.localPosition = pose.localPosition;
                    bone.localRotation = pose.localRotation;
                }
                else
                {
                    success = false;
                }
            }

            if (success)
            {
                File.Delete(GetJsonFilePath());
#if UNITY_EDITOR
                AssetDatabase.Refresh();
                BodyPoserEditor.ShowMessage("Pose Applied and JSON Cleared!");
#endif
            }
            else
            {
#if UNITY_EDITOR
                BodyPoserEditor.ShowMessage("Pose Applied Partially! JSON NOT cleared.");
#endif
            }

            return success;
        }

        public void ClearStoredPose()
        {
            if (HasStoredPose())
            {
                File.Delete(GetJsonFilePath());
#if UNITY_EDITOR
                AssetDatabase.Refresh();
                BodyPoserEditor.ShowMessage("JSON Pose Data Cleared Manually.");
#endif
            }
        }

        public bool HasStoredPose() => File.Exists(GetJsonFilePath());

        public bool HasPhysicsComponents() => GetComponentInChildren<Rigidbody>() != null || GetComponentInChildren<Collider>() != null;

        public void RemovePhysicsComponents()
        {
            var joints = GetComponentsInChildren<CharacterJoint>();
            foreach (var joint in joints)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(joint);
                else
#endif
                    Destroy(joint);
            }

            var rigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(rb);
                else
#endif
                    Destroy(rb);
            }

            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(col);
                else
#endif
                    Destroy(col);
            }

#if UNITY_EDITOR
            BodyPoserEditor.ShowMessage("Physics Components Removed.");
            AssetDatabase.Refresh();
#endif
        }

        private void DisablePhysicsIfPresent()
        {
            var rigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies) rb.isKinematic = true;

            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = false;
        }

        private string GetJsonFilePath() => $"{PoseDataPath}{gameObject.name}.json";

        private Transform FindBoneByName(string name)
        {
            var bones = GetComponentsInChildren<Transform>();
            foreach (var bone in bones)
                if (bone.name == name) return bone;

            return null;
        }
    }
}

namespace RevolvingGear.BodyPoser
{
#if UNITY_EDITOR
    [CustomEditor(typeof(BodyPoser))]
    public class BodyPoserEditor : Editor
    {
        private static string statusMessage = "";
        private static float messageDisplayTime = 2f;
        private static float lastMessageTime = -10f;

        public static void ShowMessage(string message)
        {
            statusMessage = message;
            lastMessageTime = (float)EditorApplication.timeSinceStartup;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BodyPoser poser = (BodyPoser)target;

            EditorGUILayout.HelpBox(
                "This is the Body Poser tool. Use it to manually pose a model in the editor or capture poses after using Rigidbody physics in play mode.",
                MessageType.Info
            );
            EditorGUILayout.Space();

            //poser.poseEditMode = EditorGUILayout.Toggle("Enable Pose Editing", poser.poseEditMode);

            EditorGUILayout.Space();

            if (GUILayout.Button("📸 Capture Current Pose to JSON"))
                poser.CapturePoseToJson();

            if (GUILayout.Button("📐 Apply Stored Pose from JSON"))
                poser.TryApplyStoredPose();

            if (GUILayout.Button("🗑️ Clear Stored JSON Pose"))
                poser.ClearStoredPose();

            EditorGUILayout.Space();

            if (GUILayout.Button("⚙️ Remove Physics Components"))
                poser.RemovePhysicsComponents();

            EditorGUILayout.Space();

            if (poser.HasStoredPose())
                EditorGUILayout.HelpBox($"Pose Data: FOUND 🟠\nFile: {poser.gameObject.name}.json", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("Pose Data: CLEARED ✅", MessageType.Info);

            if (poser.HasPhysicsComponents())
                EditorGUILayout.HelpBox("Physics Components: PRESENT 🟢", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Physics Components: REMOVED ✅", MessageType.Info);

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

        private void OnSceneGUI()
        {
            BodyPoser poser = (BodyPoser)target;
            if (!poser.poseEditMode) return;

            var bones = poser.GetComponentsInChildren<Transform>();

            foreach (var bone in bones)
            {
                if (bone == poser.transform) continue; // Skip root

                EditorGUI.BeginChangeCheck();

                // Interactive Handles in World Space
                Vector3 newWorldPos = Handles.PositionHandle(bone.position, bone.rotation);
                Quaternion newWorldRot = Handles.RotationHandle(bone.rotation, bone.position);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(bone, "Modify Bone Pose");
                    bone.position = newWorldPos;
                    bone.rotation = newWorldRot;
                }

                // Draw Hierarchy Lines
                if (bone.parent != null)
                {
                    Handles.color = Color.cyan;
                    Handles.DrawLine(bone.position, bone.parent.position);
                }
            }
        }
    }
#endif
}
