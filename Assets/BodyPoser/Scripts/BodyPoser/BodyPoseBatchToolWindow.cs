#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RevolvingGear.BodyPoser
{
    public class BodyPoseBatchToolWindow : EditorWindow
    {
        private BodyPoseBatchTool tool;
        private bool includeChildren = true;

        private string statusMessage = "";
        private double statusUntilTime = 0;

        [MenuItem("Tools/RevolvingGearStudios/Body Poser/Batch Pose Tool")]
        public static void Open()
        {
            var window = GetWindow<BodyPoseBatchToolWindow>("Batch Pose Tool");
            window.minSize = new Vector2(420, 420);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.Space(6);

            DrawToolField();

            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(tool == null))
            {
                DrawSelectionSection();
                EditorGUILayout.Space(10);
                DrawTargetsSection();
                EditorGUILayout.Space(10);
                DrawBatchActionsSection();
            }

            EditorGUILayout.Space(8);
            DrawStatusBar();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Revolving Gear Studios", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Body Poser • Batch Pose Tool", EditorStyles.largeLabel);
        }

        private void DrawToolField()
        {
            EditorGUILayout.LabelField("Batch Tool Reference", EditorStyles.boldLabel);

            tool = (BodyPoseBatchTool)EditorGUILayout.ObjectField(
                new GUIContent("Tool", "Scene reference to a BodyPoseBatchTool component."),
                tool,
                typeof(BodyPoseBatchTool),
                true
            );

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find In Scene"))
                {
                    tool = FindFirstObjectByType<BodyPoseBatchTool>();
                    ShowStatus(tool != null ? "Found Batch Tool in scene." : "No Batch Tool found in scene.");
                }

                if (GUILayout.Button("Create In Scene"))
                {
                    tool = CreateToolInScene();
                    ShowStatus("Created Batch Tool in scene.");
                }

                using (new EditorGUI.DisabledScope(tool == null))
                {
                    if (GUILayout.Button("Select Tool"))
                    {
                        Selection.activeObject = tool.gameObject;
                        EditorGUIUtility.PingObject(tool.gameObject);
                    }
                }
            }

            if (tool != null && tool.targets != null)
                EditorGUILayout.LabelField($"Targets: {tool.targets.Length}");
        }

        private void DrawSelectionSection()
        {
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

            includeChildren = EditorGUILayout.ToggleLeft(
                new GUIContent("Include Children", "If enabled, finds BodyPoser components on selected objects and all their children."),
                includeChildren
            );

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Replace From Selection"))
                {
                    var found = FindBodyPosersInSelection(includeChildren);
                    ReplaceTargets(found);
                    ShowStatus($"Replaced targets: {tool.targets.Length}");
                }

                if (GUILayout.Button("Add From Selection"))
                {
                    var found = FindBodyPosersInSelection(includeChildren);
                    int added = AddTargets(found);
                    ShowStatus($"Added {added}. Total: {tool.targets.Length}");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cleanup (Nulls + De-dupe)"))
                {
                    CleanupTargets();
                    ShowStatus($"Cleaned. Total: {tool.targets.Length}");
                }

                if (GUILayout.Button("Select Targets"))
                {
                    SelectCurrentTargets();
                }
            }
        }

        private void DrawTargetsSection()
        {
            EditorGUILayout.LabelField("Targets List", EditorStyles.boldLabel);

            // Show & edit the underlying serialized array so user can still drag/drop manually.
            SerializedObject so = new SerializedObject(tool);
            SerializedProperty targetsProp = so.FindProperty("targets");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(targetsProp, includeChildren: true);
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tool);
            }
        }

        private void DrawBatchActionsSection()
        {
            EditorGUILayout.LabelField("Batch Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture All Poses"))
                {
                    tool.CaptureAllPoses();
                    ShowStatus("All poses captured.");
                }

                if (GUILayout.Button("Apply All Stored Poses"))
                {
                    bool ok = tool.ApplyAllStoredPoses();
                    ShowStatus(ok ? "All poses applied successfully." : "Some models failed to apply poses.");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Remove All Physics Components"))
                {
                    tool.RemoveAllPhysics();
                    ShowStatus("Physics components removed on listed models.");
                }

                if (GUILayout.Button("Make All Static"))
                {
                    int count = 0;
                    if (tool.targets != null)
                    {
                        foreach (var poser in tool.targets)
                        {
                            if (poser == null) continue;
                            poser.gameObject.isStatic = true;
                            count++;
                        }
                    }
                    ShowStatus($"{count} object(s) marked as static.");
                }
            }
        }

        private void DrawStatusBar()
        {
            if (EditorApplication.timeSinceStartup > statusUntilTime)
                return;

            if (!string.IsNullOrEmpty(statusMessage))
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        private void ShowStatus(string msg, double seconds = 2.0)
        {
            statusMessage = msg;
            statusUntilTime = EditorApplication.timeSinceStartup + seconds;
            Repaint();
        }

        private BodyPoseBatchTool CreateToolInScene()
        {
            var go = new GameObject("BodyPoseBatchTool");
            Undo.RegisterCreatedObjectUndo(go, "Create BodyPoseBatchTool");
            var created = go.AddComponent<BodyPoseBatchTool>();
            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
            EditorUtility.SetDirty(created);
            return created;
        }

        private List<BodyPoser> FindBodyPosersInSelection(bool includeChildrenSearch)
        {
            var results = new List<BodyPoser>();
            var selected = Selection.gameObjects;

            if (selected == null || selected.Length == 0)
                return results;

            foreach (var go in selected)
            {
                if (go == null) continue;

                if (includeChildrenSearch)
                {
                    var posers = go.GetComponentsInChildren<BodyPoser>(true);
                    if (posers != null && posers.Length > 0)
                        results.AddRange(posers);
                }
                else
                {
                    var poser = go.GetComponent<BodyPoser>();
                    if (poser != null)
                        results.Add(poser);
                }
            }

            // De-dupe while preserving order
            var set = new HashSet<BodyPoser>();
            var deduped = new List<BodyPoser>();
            foreach (var p in results)
            {
                if (p != null && set.Add(p))
                    deduped.Add(p);
            }

            return deduped;
        }

        private void ReplaceTargets(List<BodyPoser> newTargets)
        {
            Undo.RecordObject(tool, "Replace BodyPoser Targets");
            tool.targets = newTargets != null ? newTargets.ToArray() : new BodyPoser[0];
            EditorUtility.SetDirty(tool);
        }

        private int AddTargets(List<BodyPoser> toAdd)
        {
            var set = new HashSet<BodyPoser>();

            if (tool.targets != null)
            {
                foreach (var t in tool.targets)
                    if (t != null) set.Add(t);
            }

            int before = set.Count;

            if (toAdd != null)
            {
                foreach (var t in toAdd)
                    if (t != null) set.Add(t);
            }

            Undo.RecordObject(tool, "Add BodyPoser Targets");
            tool.targets = set.ToArray();
            EditorUtility.SetDirty(tool);

            return set.Count - before;
        }

        private void CleanupTargets()
        {
            var set = new HashSet<BodyPoser>();

            if (tool.targets != null)
            {
                foreach (var t in tool.targets)
                    if (t != null) set.Add(t);
            }

            Undo.RecordObject(tool, "Cleanup BodyPoser Targets");
            tool.targets = set.ToArray();
            EditorUtility.SetDirty(tool);
        }

        private void SelectCurrentTargets()
        {
            if (tool.targets == null || tool.targets.Length == 0)
            {
                ShowStatus("No targets to select.");
                return;
            }

            var objs = tool.targets
                .Where(t => t != null)
                .Select(t => (Object)t.gameObject)
                .ToArray();

            Selection.objects = objs;
            if (objs.Length > 0)
                EditorGUIUtility.PingObject(objs[0]);

            ShowStatus($"Selected {objs.Length} object(s).");
        }
    }
}
#endif
