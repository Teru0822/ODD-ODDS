using UnityEngine;
using UnityEditor;

/// <summary>
/// MazeMarkerManager のカスタムインスペクター＆Sceneビュー クリック編集機能。
/// Sceneビュー上で迷路のマスをクリックすることでマーカーを配置・削除できます。
/// </summary>
[CustomEditor(typeof(MazeMarkerManager))]
public class MazeMarkerEditor : Editor
{
    private bool _editMode = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MazeMarkerManager manager = (MazeMarkerManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── マーカー編集ツール ──", EditorStyles.boldLabel);

        // 編集モードの切り替えボタン
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = _editMode ? Color.green : Color.gray;
        if (GUILayout.Button(_editMode ? "■ 編集モード ON（Sceneビューをクリック）" : "□ 編集モードを開始する", GUILayout.Height(30)))
        {
            _editMode = !_editMode;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = prevColor;

        if (_editMode)
        {
            EditorGUILayout.HelpBox(
                "Sceneビュー上で迷路のマスをクリックするとマーカーを配置します。\n" +
                "既にマーカーがある場所をクリックすると削除します。\n" +
                "配置するマーカーの種類は上の「Current Brush Type」で変更できます。",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(5);

        // マーカーの一括削除ボタン
        if (GUILayout.Button("全マーカーを削除"))
        {
            if (EditorUtility.DisplayDialog("確認", "すべてのマーカーを削除しますか？", "削除", "キャンセル"))
            {
                Undo.RecordObject(manager, "全マーカー削除");
                manager.markers.Clear();
                EditorUtility.SetDirty(manager);
            }
        }

        // マーカー数の表示
        EditorGUILayout.LabelField($"配置済みマーカー数: {manager.markers.Count}");
    }

    private void OnSceneGUI()
    {
        if (!_editMode) return;

        MazeMarkerManager manager = (MazeMarkerManager)target;
        if (manager.mazeGizmoDisplay == null) return;

        // Sceneビューの操作をブロックし、クリックをキャプチャするためにControlIDを取得
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        Event evt = Event.current;

        if (evt.type == EventType.MouseDown && evt.button == 0)
        {
            // マウス位置からレイキャストし、マス座標を計算
            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            float cellSize = manager.mazeGizmoDisplay.cellSize;
            Vector3 origin = manager.mazeGizmoDisplay.transform.position;

            // Y=0の平面（drawHeight付近）との交点を求める
            float drawHeight = manager.mazeGizmoDisplay.drawHeight;
            Plane plane = new Plane(Vector3.up, new Vector3(0, drawHeight, 0));

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 localPoint = hitPoint - origin;

                // グリッド座標に変換
                int gridX = Mathf.RoundToInt(localPoint.x / cellSize);
                int gridY = Mathf.RoundToInt(localPoint.z / cellSize);
                Vector2Int gridPos = new Vector2Int(gridX, gridY);

                // Undoに記録
                Undo.RecordObject(manager, "マーカー配置/削除");

                // 既存マーカーがあれば削除、なければ追加
                MazeMarkerData existing = manager.GetMarker(gridPos);
                if (existing != null)
                {
                    manager.RemoveMarker(gridPos);
                    Debug.Log($"[MazeMarkerEditor] マーカーを削除: ({gridX}, {gridY})");
                }
                else
                {
                    manager.SetMarker(gridPos, manager.currentBrushType);
                    Debug.Log($"[MazeMarkerEditor] {manager.currentBrushType} マーカーを配置: ({gridX}, {gridY})");
                }

                EditorUtility.SetDirty(manager);
                evt.Use();
            }
        }

        // 編集モード中はSceneビューの左上にラベルを表示
        Handles.BeginGUI();
        GUIStyle style = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f, 0.8f);
        GUI.Box(new Rect(10, 10, 280, 30), $"マーカー編集中: {manager.currentBrushType}", style);
        GUI.backgroundColor = Color.white;
        Handles.EndGUI();
    }
}
