using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;

namespace QubicNS
{
    public class PrefabManager : EditorWindow
    {
        private PrefabDatabase database;
        private GameObject selectedObject;
        private GameObject selectedPrefabObject;
        private GameObject candidatePrefabObject;
        bool isVisible => false;//IsWindowOpenAndVisible<PrefabManager>();

        public static Texture2D RotateToolIcon => EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "RotateTool On" : "RotateTool").image as Texture2D;
        public static Texture2D RectToolIcon => EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "RectTool On" : "RectTool").image as Texture2D;

        bool IsWindowOpenAndVisible<T>() where T : EditorWindow
        {
            var windows = Resources.FindObjectsOfTypeAll<T>();
            return windows.Length > 0 && windows[0];
        }

        static GUIStyle greenButtonStyle;
        static GUIStyle buttonStyle;
        static GUIStyle buttonStyle2;

        public static void ShowWindow()
        {
            var window = GetWindow<PrefabManager>(false, "Prefab Manager", true);
            window.Focus();
        }

        static void InitStyles()
        {
            if (buttonStyle != null)
                return;

            greenButtonStyle = new GUIStyle(EditorStyles.miniButton) 
            {
                normal = { textColor = Color.green },
                fontStyle = FontStyle.Bold,
                fontSize = 22
            };

            buttonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                richText = true,
                fixedHeight = 25,
                alignment = TextAnchor.MiddleCenter,
            };

            buttonStyle2 = new GUIStyle(EditorStyles.toolbarButton)
            {
                richText = true,
                fixedHeight = 25,
                alignment = TextAnchor.MiddleRight,
            };
        }

        private void OnEnable()
        {
            OnSelectionChanged();

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            SceneView.duringSceneGui += DrawObjInsideRoomPanel;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            if (selectedPrefabObject == null)
                return;
            NeedRebuild();
            RebuildScene();
            Repaint();
        }

        private void OnDisable()
        {
            BoundsEditor.StopEdit();
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnBecameInvisible()
        {
            BoundsEditor.StopEdit();
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnBecameVisible()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnSceneGUI(SceneView view)
        {
            GetDatabase();

            if (saveAndRebuildTime < DateTime.Now)
            {
                RebuildScene();
                saveAndRebuildTime = DateTime.MaxValue;
            }

            // Only run while the scene view is focused
            if (view != SceneView.lastActiveSceneView)
                return;

            if (selectedObject == Selection.activeGameObject)
                DrawSize(selectedObject);

            if (IsScenePrefabInstanceNoMicroBuild)
            {
                if (selectedObject.transform.hasChanged)
                {
                    selectedObject.transform.hasChanged = false;
                    Repaint();
                }
            }

            if (IsScenePrefabInstance)
            if (currentPrefab != null)
                DrawPivot(currentPrefab, selectedObject);
        }

        private void DrawSize(GameObject selectedObject)
        {
            if (selectedObject == null)
                return;
            var bounds = Helper.GetTotalBounds(selectedObject);
            if (bounds.size != Vector3.zero)
            {
                Handles.BeginGUI();
                var prevColor = GUI.color;
                GUI.color = new Color(1, 0, 0, 1);
                Handles.Label(bounds.max, $"W={bounds.size.x:0.00} H={bounds.size.y:0.00} D={bounds.size.z:0.00}");
                GUI.color = prevColor;
                Handles.EndGUI();
            }
        }

        DateTime saveAndRebuildTime = DateTime.MaxValue;

        static readonly Quaternion ZeroQ = new Quaternion(0, 0, 0, 0);

        private void DrawPivot(Prefab currentPrefab, GameObject selectedObject)
        {
            if (Tools.current == Tool.Move)
                Tools.current = Tool.None;

            var rot = Helper.GetDeltaRotation(currentPrefab.PrefabInfo.Rotation, selectedObject.transform.rotation);
            if (rot.norm() < 0.01f) rot = Quaternion.identity;

            var inverseRot = Helper.GetDeltaRotation(selectedObject.transform.rotation, currentPrefab.PrefabInfo.Rotation);
            if (inverseRot.norm() < 0.01f) inverseRot = Quaternion.identity;

            var center = rot * -currentPrefab.PrefabInfo.Anchor + selectedObject.transform.position;
            Handles.color = Color.green;
            Handles.DrawSolidDisc(center, Vector3.up, 0.07f);
            Handles.color = Color.white;
            var cellCenter = center - rot * Vector3.forward;
            DrawArrowXZ(cellCenter + (rot * Vector3.forward) / 4, -(rot * Vector3.forward), Color.white, 0.5f);
            Handles.Label(cellCenter - rot * Vector3.forward / 3, "Face");
            var right = (rot * Vector3.right) * currentPrefab.Bounds.size.x;
            var fwd = (rot * Vector3.forward) * currentPrefab.Bounds.size.z;
            var type = CornerType.X;

            // left line
            Handles.color = Color.green;
            Handles.DrawLine(center - right, center);
            // right line
            Handles.color = CalcColor(CornerType.X | CornerType.Straight | CornerType.T);
            Handles.DrawLine(center + right, center);
            // back line
            Handles.color = CalcColor(CornerType.X | CornerType.T | CornerType.ConvexL | CornerType.ConcaveL | (CornerType)CornerMasks.GenericL);
            Handles.DrawLine(center - fwd, center);
            // fwd line
            Handles.color = CalcColor(CornerType.X);
            Handles.DrawLine(center + fwd, center);

            Color CalcColor(CornerType expected)
            {
                if ((type & expected) == 0)
                    return Color.white * 0.6f;
                return Color.green;
            }

            // draw bounds box
            if (!BoundsEditor.IsEditing)
            {
                var boundsRotation = selectedObject.transform.localRotation * Quaternion.Inverse(currentPrefab.PrefabInfo.PrefabSceneRotation);
                var rotatedBounds = QubicHelper.RotateBounds90(currentPrefab.PrefabInfo.Bounds, selectedObject.transform.position, boundsRotation);
                Handles.color = Color.yellow;
                Handles.DrawWireCube(rotatedBounds.center, rotatedBounds.size);
            }

            if (!BoundsEditor.IsEditing)
            {
                // draw Anchor tool
                EditorGUI.BeginChangeCheck();

                var dY = Vector3.up * 0.01f;
                var startPos = dY + (IsScenePrefabInstanceNoMicroBuild ? center : selectedObject.transform.position);
                Vector3 newPos = Handles.PositionHandle(startPos, rot);
                var offset = newPos - startPos;

                // after build selected object wes changed?
                if (offset.magnitude > currentPrefab.Bounds.size.magnitude + 1)
                {
                    Selection.activeObject = null;
                    selectedPrefabObject = selectedObject = null;
                    candidatePrefabObject = null;
                    BoundsEditor.StopEdit();
                    EditorGUI.EndChangeCheck();
                    return;
                }

                if (!IsScenePrefabInstanceNoMicroBuild)
                {
                    offset = startPos - newPos;
                    selectedObject.transform.position -= offset;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(database, "Move Anchor");
                    currentPrefab.PrefabInfo.Anchor = currentPrefab.PrefabInfo.Anchor - inverseRot * offset;
                    NeedRebuild();
                }
            }

            OnPlaceButtons(selectedObject);
        }

        private void DrawArrowXZ(Vector3 position, Vector3 direction, Color color, float length)
        {
            Handles.color = color;

            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            Handles.ArrowHandleCap(
                controlID,
                position,
                Quaternion.LookRotation(direction),
                length,
                EventType.Repaint
            );
        }

        private void NeedRebuild()
        {
            EditorUtility.SetDirty(database);
            saveAndRebuildTime = DateTime.Now.AddMilliseconds(120);
        }

        private void OnPlaceButtons(GameObject selectedObject)
        {
            Handles.BeginGUI();
            var initPos = HandleUtility.WorldToGUIPoint(selectedObject.transform.position);
            initPos.y -= 100;
            initPos.x -= 100;
            var pos = initPos;

            if (GUI.Button(new Rect(pos.x, pos.y, 25, 25), PrefabManager.RotateToolIcon))
            {
                currentPrefab.PrefabInfo.RotationY = (ushort)((currentPrefab.PrefabInfo.RotationY + 90) % 360);
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                RebuildScene();
            }
            pos.x += 25;

            var boundsRotation = selectedObject.transform.localRotation * Quaternion.Inverse(currentPrefab.PrefabInfo.PrefabSceneRotation);
            var rotatedBounds = QubicHelper.RotateBounds90(currentPrefab.PrefabInfo.Bounds, selectedObject.transform.position, boundsRotation);

            if (!BoundsEditor.IsEditing && currentPrefab.Type == QubicNS.PrefabType.Content)
            if (GUI.Button(new Rect(pos.x, pos.y, 25, 25), RectToolIcon))
            {
                BoundsEditor.StartEdit(rotatedBounds);
            }

            if (BoundsEditor.IsEditing && currentPrefab.Type == QubicNS.PrefabType.Content)
            if (GUI.Button(new Rect(pos.x, pos.y, 100, 25), "Apply Bounds"))
            {
                var rot = Quaternion.Euler(0, currentPrefab.PrefabInfo.RotationY, 0);
                var newBounds = BoundsEditor.currentBounds;
                newBounds.center -= selectedObject.transform.position;
                newBounds = QubicHelper.RotateBounds90(newBounds, Vector3.zero, Quaternion.Inverse(boundsRotation));
                currentPrefab.PrefabInfo.Bounds = newBounds;
                BoundsEditor.StopEdit();
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                RebuildScene();
            }

            Handles.EndGUI();
        }

        private void OnSelectionChanged()
        {
            var wasPrefab = IsScenePrefabInstance && currentPrefab != null;

            BoundsEditor.StopEdit();
            GetDatabase();

            if (Selection.activeGameObject != null)
            {
                selectedPrefabObject = null;
                currentPrefab = null;
                candidatePrefabObject = null;
            }

            if (Selection.transforms.Length > 1)
            {
                selectedObject = null;
                selectedPrefabObject = null;
                currentPrefab = null;
                candidatePrefabObject = null;
                goto exit;
            }

            if (Selection.activeGameObject != null)
            {
                selectedObject = Selection.activeGameObject;

                // Get original prefab of the object
                //{
                //    GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(selectedObject);

                //    if (prefab != null)
                //    {
                //        if (IsPrefabRoot(selectedObject))
                //            selectedPrefabObject = prefab;
                //        else
                //            TryFindParentPrefabCandidate();
                //    }
                //    else
                //    {
                //        // If the object is a prefab (from Project view)
                //        if (IsPrefabFromProjectView())
                //            selectedPrefabObject = selectedObject;
                //    }
                //}

                {
                    // If the object is a prefab (from Project view) ?
                    if (IsPrefabFromProjectView())
                        selectedPrefabObject = selectedObject;
                    else
                    {
                        GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(selectedObject);

                        if (prefab != null)
                        {
                            if (IsPrefabRoot(selectedObject))
                                selectedPrefabObject = prefab;
                            else
                                TryFindParentPrefabCandidate();
                        }
                    }
                }
            }
exit:
            var nowPrefab = IsScenePrefabInstance && currentPrefab != null;
            if (wasPrefab && !nowPrefab && Tools.current == Tool.None) 
                Tools.current = Tool.Move;

            Repaint();
        }

        private bool IsPrefabFromProjectView()
        {
            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(path))
                return false;

            if (!Preferences.Instance.AllowToUse3DModelsAsPrefab)
            if (!path.EndsWith(".prefab"))
                return false;

            if (!PrefabUtility.IsPartOfPrefabAsset(selectedObject))
                return false;
            
            return true;
        }

        private void TryFindParentPrefabCandidate()
        {
            if (database == null)
                return;

            var parentPrefabOnScene = PrefabUtility.GetNearestPrefabInstanceRoot(selectedObject);
            if (parentPrefabOnScene == null || parentPrefabOnScene.transform.parent == null)
                return;

            var parentPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(parentPrefabOnScene);

            if (parentPrefab == null)
                return;

            if (database.FindPrefab(parentPrefab, out _, out _))
                candidatePrefabObject = parentPrefabOnScene;
        }

        private void GetDatabase()
        {
            if (database == null)
            if (QubicBuilder.LastSelectedBuilder != null && QubicBuilder.LastSelectedBuilder.PrefabDatabase != null)
                database = QubicBuilder.LastSelectedBuilder.PrefabDatabase;
        }

        private static bool IsNestedPrefab(GameObject go)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(go))
                return false;

            return PrefabUtility.GetNearestPrefabInstanceRoot(go) == go;
        }

        private static bool IsPrefabRoot(GameObject gameObject) 
        { 
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject)) 
                return false; 
            if (gameObject.transform.parent != null && PrefabUtility.IsPartOfPrefabInstance(gameObject.transform.parent.gameObject))
                return false;
            return true; 
        }

        private Vector2 scrollPos;

        private void OnGUI()
        {
            InitStyles();

            currentPrefab = null;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height));

            EditorGUILayout.BeginHorizontal();
            //GUI.enabled = false;
            database = (PrefabDatabase)EditorGUILayout.ObjectField("Prefab Database", database, typeof(PrefabDatabase), false);
            //GUI.enabled = true;
            GUILayout.Space(10);

            if (GUILayout.Button("New Database", buttonStyle, GUILayout.Width(100)))
            {
                string path = EditorUtility.SaveFilePanel(
                    "Save Prefab Database",
                    "Assets",
                    "PrefabDatabase",
                    "asset");

                if (!string.IsNullOrEmpty(path))
                {
                    database = ScriptableObject.CreateInstance<PrefabDatabase>();

                    string relativePath = FileUtil.GetProjectRelativePath(path);

                    AssetDatabase.CreateAsset(database, relativePath);
                    AssetDatabase.SaveAssets();
                    Debug.Log("PrefabDatabase created: " + relativePath);
                }
                GUILayout.Space(10);
            }

            EditorGUILayout.EndHorizontal();

            if (database == null)
            {
                GUILayout.Label("Select or create a database.", EditorStyles.helpBox);
            }
            else
            if (selectedPrefabObject != null)
            {
                DrawSelectedPrefab();

                // Add assembler buttons for all Assembler derived classes
                DrawAddAssemblerButtons();
            }
            else
            if (candidatePrefabObject != null)
            {
                GUILayout.Label(candidatePrefabObject.name, EditorStyles.largeLabel);
                if (GUILayout.Button("Select parent prefab"))
                    Selection.activeTransform = candidatePrefabObject.transform;
            }
            else
            if (Selection.transforms.Length > 1)
            {
                if (GUILayout.Button("Combine into one GameObject"))
                    Selection.activeTransform = CombineIntoOneObject(Selection.transforms);
            }
            else
            if (Selection.activeGameObject != null && IsNestedPrefab(Selection.activeGameObject))
            {
                GUILayout.Label("You have selected a prefab inside another prefab. Unpack the outer prefab to add the current prefab to the database.", EditorStyles.helpBox);
            }
            else
            {
                GUILayout.Label("Select a prefab from the scene or project.", EditorStyles.helpBox);
            }

            EditorGUILayout.EndScrollView();
        }

        private Transform CombineIntoOneObject(Transform[] transforms)
        {
            var parent = new GameObject(transforms[0].name + " Combined");
            parent.transform.SetParent(transforms[0].parent);
            parent.transform.position = transforms[0].position;
            foreach (var tr in transforms)
                tr.SetParent(parent.transform, true);

            return parent.transform;
        }

        private void DrawObjInsideRoomPanel(SceneView view)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null) 
                return;

            var builder = selected.GetComponentInParent<QubicBuilder>();
            if (builder == null)
                return;

            if (selected.transform.GetComponentInParent<QubicHolder>() == null)
                return;

            // find my room
            var room = builder.ObjToRoomGlobal(selected);

            Handles.BeginGUI();

            var panelPos = new Vector2(50, 10);
            var height = 90;
            if (!isVisible)
                height += 22;

            GUILayout.BeginArea(new Rect(panelPos.x, panelPos.y, 160, height), selected.name, GUI.skin.window);

            if (room != null)
            {
                GUILayout.Label("Room: " + room.name);

                if (GUILayout.Button("Select Parent Room"))
                {
                    CollapseHolder();
                    Selection.activeGameObject = room.gameObject;
                }
            }
            else
            {
                GUILayout.Label(builder.name);

                if (GUILayout.Button("Select Parent Builder"))
                {
                    CollapseHolder();
                    Selection.activeGameObject = builder.gameObject;
                }
            }

            if (!isVisible)
            if (GUILayout.Button("Show Prefab Manager"))
            {
                var window = GetWindow<PrefabManager>();
                window.Focus();
                window.Show();
            }

            if (GUILayout.Button("Rebuild (F5)"))
            {
                builder.RebuildNeeded();
            }

            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private static void CollapseHolder()
        {
            var holder = Selection.activeGameObject?.transform?.GetComponentInParent<QubicHolder>();
            if (holder != null)
                QubicHelper.CollapseHierarchy(holder.gameObject);
        }

        private const float ButtonHeight = 18f, ButtonSpacing = 3f, RightPadding = 10, ButtonWidth = 100;

        private void DrawAddAssemblerButtons()
        {
            if (currentPrefab != null)
                return;

            if (database == null)
                return;

            GUILayout.Label($"Add Prefab to {database.name} as:", EditorStyles.boldLabel);

            var templates = TypesHelper.GetStaticMethods(typeof(Templates), typeof(Prefab)).ToArray();

            float inspectorWidth = EditorGUIUtility.currentViewWidth - RightPadding;
            int buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt(inspectorWidth / (ButtonWidth + ButtonSpacing)));
            Rect buttonRect = new(position.x, position.y, ButtonWidth, ButtonHeight);
            var w = inspectorWidth / buttonsPerRow;

            var i = buttonsPerRow;
            GUILayout.BeginHorizontal();
            var prevOrder = -100;
            foreach (var method in templates.OrderBy(t => t.GetCustomAttribute<OrderAttribute>()?.Order ?? 0).ThenBy(t => t.GetInspectorTitleAttribute()))
            {
                var order = method.GetCustomAttribute<OrderAttribute>()?.Order ?? 0;
                if (prevOrder != order && prevOrder != -100)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    i = buttonsPerRow;
                }
                prevOrder = order;

                if (GUILayout.Button($"{method.GetInspectorTitleAttribute()}", EditorStyles.toolbarButton, GUILayout.Width(w)))
                {
                    AddPrefabFromTemplate(method);
                }

                i--;
                if (i == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    i = buttonsPerRow;
                }
            }

            GUILayout.EndHorizontal();
        }

        private void AddPrefabFromTemplate(MethodInfo template)
        {
            var prefab = (Prefab) template.Invoke(this, new object[] { selectedPrefabObject });
            prefab.CaptureBounds(true);

            if (prefab.Type != QubicNS.PrefabType.Content)
            if (prefab.PrefabInfo.Bounds.size.z > prefab.PrefabInfo.Bounds.size.x * 2)
                prefab.PrefabInfo.RotationY = 90;

            database.Prefabs.Add(prefab);
            currentPrefab = prefab;

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            RebuildScene();
        }

        private void RebuildScene()
        {
            if (QubicBuilder.LastSelectedBuilder)
                QubicBuilder.LastSelectedBuilder.RebuildNeeded();
            else
                foreach (var builder in QubicBuilder.AllEnabledBuilders.Where(b => b.PrefabDatabase == database).TakeLast(5))
                    builder.RebuildNeeded();
        }

        Prefab currentPrefab;
        bool IsScenePrefabInstance => selectedObject != null && selectedPrefabObject != null && selectedObject != selectedPrefabObject;
        bool IsScenePrefabInstanceNoMicroBuild => IsScenePrefabInstance && selectedObject?.GetComponentInParent<QubicBuilder>() == null;

        private void DrawSelectedPrefab()
        {
            if (database == null)
                return;
            Prefab prefab;
            int prefabIndex;

            database.FindPrefab(selectedPrefabObject, out prefab, out prefabIndex);

            if (prefab == null)
            {
                GUILayout.Label($"Selected Prefab: {selectedPrefabObject.name}", EditorStyles.boldLabel);
                var size = Helper.GetTotalBounds(selectedPrefabObject).size;
                if (size != Vector3.zero)
                    GUILayout.Label($"Size: {size}", EditorStyles.label);
                GUILayout.Label("Unknown prefab in the current database.", EditorStyles.helpBox);
                return;
            }

            prefab.OnValidate();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(/*"box"*/);

            // Создание SerializedObject для AssemblerDatabase
            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty assemblersProp = serializedDatabase.FindProperty("Prefabs");
            SerializedProperty assemblerElement = assemblersProp.GetArrayElementAtIndex(prefabIndex);

            // Show list element
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(assemblerElement, true);

            //if ( assemblerElement.isExpanded )
            if (currentPrefab == null)
            {
                currentPrefab = prefab;
                assemblerElement.isExpanded = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                prefab.OnValidate();
                serializedDatabase.ApplyModifiedProperties();
                RebuildScene();
                Repaint();
            }

            //if (assemblerElement.isExpanded)
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Capture Bounds", "Capture Bounds and Auto Align Anchor"), buttonStyle))
                {
                    prefab.CaptureBounds(false);
                    RebuildScene();
                }

                if (GUILayout.Button(new GUIContent($"ℹ️ Help", "Open Help"), buttonStyle))
                {
                    var url = prefab.GetType().GetCustomAttribute<HelpURLAttribute>()?.URL;
                    if (url != null)
                        Application.OpenURL(url);
                }
                if (GUILayout.Button(new GUIContent($"<color=red>✖</color> Remove Prefab", "Remove " + prefab.GetType().GetInspectorTitleAttribute() + " from database " + database.name), buttonStyle))
                {
                    database.Prefabs.Remove(prefab);
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                    RebuildScene();
                }

                if (GUILayout.Button(new GUIContent($"<color=#00FF00FF>✚</color> Add Rule", "Add Rule to " + prefab.GetType().GetInspectorTitleAttribute()), buttonStyle))
                {
                    var rules = prefab.Rules;
                    Array.Resize(ref rules, prefab.Rules.Length + 1);
                    if (rules.Length >= 2)
                        rules[rules.Length - 1] = rules[rules.Length - 2].CloneDeep();
                    prefab.Rules = rules;
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            DrawAdditionalInfo(prefab, database);

            EditorGUILayout.EndVertical();
        }

        private static void DrawAdditionalInfo(Prefab prefab, PrefabDatabase database)
        {
            if (prefab.Type == PrefabType.Content && database != null)
            {
                prefab.CalcAdditionalProperties(database);

                if (prefab.IsLeftColumn || prefab.IsRightColumn)
                {
                    var text = prefab.IsLeftColumn ? "ℹ️ This is Left Column Content" : "ℹ️ This is Right Column Content";
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (EditorGUILayout.LinkButton(text, GUILayout.ExpandWidth(false)))
                        Application.OpenURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.mnnwb7q1xnmy");
                    EditorGUILayout.EndHorizontal();
                }

                if (prefab.IsFullDepth)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (EditorGUILayout.LinkButton("ℹ️ This is Full Depth Content", GUILayout.ExpandWidth(false)))
                        Application.OpenURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.mnnwb7q1xnmy");
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}