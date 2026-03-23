using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace QubicNS
{
    [InitializeOnLoad]
    public class RoomToolbar
    {
        static bool changed;
        static Manipulator maxManipulator = new Manipulator() { restrictXZplane = true };
        static Manipulator minManipulator = new Manipulator() { restrictXZplane = true };
        static Manipulator moveManipulator = new Manipulator() { restrictXZplane = true, CapFunction = Handles.RectangleHandleCap, Size = 0.25f, OnDragStart = OnDragStart };
        static Manipulator moveYManipulator = new Manipulator() { restrictY = true, CapFunction = Handles.ArrowHandleCap, Size = 0.5f, OnDragStart = OnDragStart };
        static Manipulator levelManipulator = new Manipulator() { restrictY = true, Color = new Color(0.1f, 0.7f, 0.7f)};
        static Vector3 prevMouse;
        static BaseRoom room;
        static QubicBuilder builder;
        static RectOffset Size;

        static string[] roomTemplatesStr;
        static MethodInfo[] roomTemplates;


        static RoomToolbar()
        {
            // Subscribe to the update event to draw custom toolbar items
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnEnable()
        {
            roomTemplates = TypesHelper.GetStaticMethods(typeof(Templates), typeof(BaseRoom)).ToArray();
            roomTemplatesStr = roomTemplates.Select(s => s.GetInspectorTitleAttribute()).ToArray();
        }

        static Rect panelRect;

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (SceneLoadTracker.SceneIsOpening)
                return;

            // Only run while the scene view is focused
            if (sceneView != SceneView.lastActiveSceneView)
                return;
            var r = Selection.activeGameObject?.GetComponent<BaseRoom>();
            if (r == null)
            {
                if (room != null && Tools.current == Tool.None)
                    Tools.current = Tool.Move; 

                // room was removed?
                if (room == null && !object.ReferenceEquals(room, null) && builder != null)
                    builder.RebuildNeeded();
                room = null;
                return;
            }

            if (builder != null)
                builder.LastSelectedRoom = r;

            if (room != r)
                OnEnable();
            room = r;
            Size = room.SizeRotated;

            builder = room.GetComponentInParent<QubicBuilder>();
            if (builder == null || builder.Map == null || room.Size == null || builder.Lock || builder.PrefabDatabase == null)
                return;

            builder.InitMap();

            if (room != null)
                Tools.current = Tool.None;

            changed = false;

            Undo.RecordObject(room, "Room editing");

            // Create a new toolbar at the top left of the scene view
            panelRect = GUILayout.Window(
                1,
                new Rect(new Vector2(50, 10), new Vector2(160, 0)),
                DrawPanel,
                room?.name ?? "",
                GUILayout.Width(160)
            );

            Handles.BeginGUI();
            OnPlaceButtons();
            Handles.EndGUI();

            if (room.CanBeRotated)
            {
                var dir = QubicHelper.Dirs4[room.Rotation];
                Handles.color = Color.white * 0.7f;
                Handles.DrawLine(room.transform.position, room.transform.position + dir * 3, 2);
            }

            if ((Event.current.shift || Event.current.control) && Event.current.button == 0)
                DrawCells();
            else
                ResizeHandles(sceneView);
            
            if (changed)
            {
                room.SizeRotated = Size;
                builder.RebuildNeeded();
                EditorUtility.SetDirty(room);
                SceneView.RepaintAll();
            }
        }

        static void DrawPanel(int id)
        {
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            {
                if (GUILayout.Button("Rebuild (F5)"))
                {
                    //changed = true;
                    builder.BuildInEditor(3);
                    UnityEditor.EditorUtility.SetDirty(builder.gameObject);
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Levels ↑"))
                    {
                        room.LevelCount++;
                        changed = true;
                    }

                    if (GUILayout.Button("Levels ↓"))
                    {
                        room.LevelCount = Mathf.Max(1, room.LevelCount - 1);
                        changed = true;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Seed ↑"))
                    {
                        room.Seed++;
                        changed = true;
                    }

                    if (GUILayout.Button("Seed ↓"))
                    {
                        room.Seed--;
                        changed = true;
                    }
                }
                GUILayout.EndHorizontal();

                // Add custom toolbar buttons
                if (room.forbiddenCells.Count > 0 || room.customCells.Count > 0)
                    if (GUILayout.Button("Clear Custom Cells"))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Confirmation",
                            "Do you want to сlear all custom cells of the room?",
                            "OK",
                            "Cancel"
                        ))
                        {
                            room.ClearCustomCells();
                            changed = true;
                        }
                    }

                if (room.CanBeRotated)
                {
                    if (GUILayout.Button(new GUIContent("Rotate", PrefabManager.RotateToolIcon), GUI.skin.button))
                    {
                        room.Rotation = (room.Rotation + 1) % 4;
                        Size = room.SizeRotated;
                        changed = true;
                    }
                }

                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUI.skin.button);
                GUILayout.Label("Create Room ", GUILayout.Width(120));

                var selectedPreset = EditorGUILayout.Popup(-1, roomTemplatesStr, GUILayout.Width(20));
                if (selectedPreset >= 0 && !builder.Lock)
                {
                    var newRoom = (BaseRoom)roomTemplates[selectedPreset].Invoke(null, null);
                    var go = newRoom.gameObject;
                    go.transform.SetParent(room.transform.parent, false);
                    go.isStatic = builder.gameObject.isStatic;
                    newRoom.LevelCount = Mathf.Max(newRoom.LevelCount, room.LevelCount);
                    newRoom.transform.position = room.transform.position + new Vector3(builder.Map.CellSize * (Size.right + 1), 0, 0);
                    Selection.activeObject = newRoom.gameObject;
                    changed = true;
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            if (changed)
            {
                room.SizeRotated = Size;
                builder.RebuildNeeded();
                EditorUtility.SetDirty(room);
                SceneView.RepaintAll();
                changed = false;
            }
        }

        public static bool IsRightMouseButton(Event e)
        {
            if (Application.platform == RuntimePlatform.OSXEditor && e.control && e.button == 0)
            {
                return true;
            }

            return e.button == 1;
        }

        private static void OnPlaceButtons()
        {
            var initPos = HandleUtility.WorldToGUIPoint(room.transform.position);

            initPos.y -= 100;
            initPos.x -= 100;
            var pos = initPos;

            if (room.CanBeRotated)
            {
                if (GUI.Button(new Rect(pos.x, pos.y, 25, 25), PrefabManager.RotateToolIcon))
                {
                    room.Rotation = (room.Rotation + 1) % 4;
                    Size = room.SizeRotated;
                    changed = true;
                }
                pos.x += 25;
            }
        }

        static void DrawCells()
        {
            Event e = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            Plane plane = new Plane(Vector3.up, room.transform.position);

            float distance;
            if (plane.Raycast(ray, out distance))
            {              
                Vector3 intersectionPoint = ray.GetPoint(distance);

                if ((prevMouse - intersectionPoint).sqrMagnitude > 1)
                {
                    SceneView.RepaintAll();
                    prevMouse = intersectionPoint;
                }

                var aligned = builder.Map.AlignToCellCenter(intersectionPoint);
                //Handles.color = Color.black;

                if (Event.current.shift || Event.current.control)
                {
                    var color = Event.current.control ? Color.red : Color.green;
                    color.a = GUIUtility.hotControl == 0 ? 0.4f : 0.2f;
                    Handles.color = color;

                    for (int i = 0; i < room.LevelCount + 1; i++)
                    {
                        if (i > 0 && i < room.LevelCount)
                            continue;
                        var s = builder.Map.CellSize;
                        var pp = aligned + new Vector3(-s /2, i * builder.Map.CellHeight, -s / 2) ;
                        Handles.DrawSolidRectangleWithOutline(new Vector3[] { pp, pp + Vector3.forward * s, pp + (Vector3.forward + Vector3.right) * s, pp + Vector3.right * s}, Handles.color, Handles.color);
                    }

                    var isDrag = Event.current.type == EventType.MouseDrag;

                    if (Handles.Button(aligned, Quaternion.LookRotation(Vector3.up), builder.Map.CellSize / 2, builder.Map.CellSize / 2, Handles.RectangleHandleCap))
                        isDrag = true;

                    if (isDrag)
                    //if (GUIUtility.hotControl != 0)
                    {
                        var relative = room.GetRelativeCellHex(builder.Map.PosToCell(aligned));

                        if (Event.current.shift)//add forced cell
                        {
                            if (room.AddCustomCell(relative))
                                changed = true;
                        }

                        if (Event.current.control)//add forbidden cell
                        {
                            if (room.RemoveCustomCell(relative))
                                changed = true;
                        }
                    }
                        
                }
            }

            //drawCellsManipulator.Update();
        }

        static void OnDragStart()
        {
            if (room != null)
                Undo.RecordObject(room.gameObject, "Room editing");
        }

        private static void ResizeHandles(SceneView sceneView)
        {
            if (Event.current.type == EventType.MouseMove)
                sceneView.Repaint();

            // resize
            var transformPos = builder.Map.CellToPos(builder.Map.PosToCell(room.transform.position));
            var scale = (float)builder.Map.CellSize;
            var scaleY = builder.Map.CellHeight;
            var maxPos = transformPos + new Vector3(Size.right + 0.5f, 0, Size.top + 0.5f) * scale;
            var minPos = transformPos - new Vector3(Size.left + 0.5f, 0, Size.bottom + 0.5f) * scale;

            maxManipulator.Update();
            minManipulator.Update();
            levelManipulator.Update();
            moveYManipulator.Update();
            moveManipulator.Update();

            // Draw wireframe box between min and max points
            Handles.color = Color.yellow;
            Handles.DrawWireCube((minManipulator.Position / 2 + maxManipulator.Position / 2).withSetY(transformPos.y), (maxManipulator.Position - minManipulator.Position).withSetY(0.001f));

            var prevSize = Size.ToString();
            var prevLevels = room.LevelCount;
            const float maxDistance = 500;

            if (!maxManipulator.IsDragging)
                maxManipulator.SetPosition(maxPos);
            else
            if (maxManipulator.Position.dist(maxManipulator.PressStartPosition) < maxDistance)
            {
                var delta = maxManipulator.Position - transformPos;
                Size.right = Mathf.Max(0, Mathf.FloorToInt((delta.x - 0.5f) / scale));
                Size.top = Mathf.Max(0, Mathf.FloorToInt((delta.z - 0.5f) / scale));
            }

            if (!minManipulator.IsDragging)
                minManipulator.SetPosition(minPos);
            else
            if (minManipulator.Position.dist(minManipulator.PressStartPosition) < maxDistance)
            {
                var delta = minManipulator.Position - transformPos;
                Size.left = Mathf.Max(0, Mathf.FloorToInt(-(delta.x - 0.5f) / scale));
                Size.bottom = Mathf.Max(0, Mathf.FloorToInt(-(delta.z - 0.5f) / scale));
            }

            if (!levelManipulator.IsDragging)
                levelManipulator.SetPosition(transformPos + Vector3.up * room.LevelCount * scaleY);
            else
            if (levelManipulator.Position.dist(levelManipulator.PressStartPosition) < maxDistance)
            {
                var delta = levelManipulator.Position - transformPos;
                room.LevelCount = Mathf.Max(1, Mathf.RoundToInt(delta.y / scaleY));
            }

            if (Size.ToString() != prevSize)
                changed = true;
            if (room.LevelCount != prevLevels)
                changed = true;

            var prevPos = builder.Map.CellToPos(builder.Map.PosToCell(room.transform.position)) + Vector3.up * 0.2f;

            if (!moveManipulator.IsDragging)
                moveManipulator.SetPosition(room.transform.position);
            else
                room.transform.position = moveManipulator.Position;

            if (!moveYManipulator.IsDragging)
                moveYManipulator.SetPosition(room.transform.position);
            else
                room.transform.position = moveYManipulator.Position;

            var newPos = builder.Map.CellToPos(builder.Map.PosToCell(room.transform.position)) + Vector3.up * 0.2f;
            room.transform.position = newPos;

            if (prevPos != newPos)
            {
                changed = true;
            }
        }
    }
}