using UnityEngine;
using UnityEditor;

/// <summary>
/// GridManager のカスタムインスペクター
/// マップサイズを +1/-1 ずつボタンで変更でき、追加セルも管理できます
/// </summary>
[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    private Vector2Int _newExtraCell = Vector2Int.zero;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridManager gm = (GridManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("─── マップサイズ調整 ───────────────", EditorStyles.boldLabel);

        // Width
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Width  (現在: {gm.MapSize.x})", GUILayout.Width(160));
        if (GUILayout.Button("－", GUILayout.Width(40))) { Undo.RecordObject(gm, "Grid Width"); gm.MapSize = new Vector2Int(Mathf.Max(1, gm.MapSize.x - 1), gm.MapSize.y); EditorUtility.SetDirty(gm); }
        if (GUILayout.Button("＋", GUILayout.Width(40))) { Undo.RecordObject(gm, "Grid Width"); gm.MapSize = new Vector2Int(gm.MapSize.x + 1, gm.MapSize.y);               EditorUtility.SetDirty(gm); }
        EditorGUILayout.EndHorizontal();

        // Height
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Height (現在: {gm.MapSize.y})", GUILayout.Width(160));
        if (GUILayout.Button("－", GUILayout.Width(40))) { Undo.RecordObject(gm, "Grid Height"); gm.MapSize = new Vector2Int(gm.MapSize.x, Mathf.Max(1, gm.MapSize.y - 1)); EditorUtility.SetDirty(gm); }
        if (GUILayout.Button("＋", GUILayout.Width(40))) { Undo.RecordObject(gm, "Grid Height"); gm.MapSize = new Vector2Int(gm.MapSize.x, gm.MapSize.y + 1);               EditorUtility.SetDirty(gm); }
        EditorGUILayout.EndHorizontal();

        // CellSize
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"CellSize (現在: {gm.CellSize:F3})", GUILayout.Width(160));
        if (GUILayout.Button("－", GUILayout.Width(40))) { Undo.RecordObject(gm, "Cell Size"); gm.CellSize = Mathf.Max(0.001f, gm.CellSize - 0.01f); EditorUtility.SetDirty(gm); }
        if (GUILayout.Button("＋", GUILayout.Width(40))) { Undo.RecordObject(gm, "Cell Size"); gm.CellSize = gm.CellSize + 0.01f;                    EditorUtility.SetDirty(gm); }
        EditorGUILayout.EndHorizontal();

        // ─── 追加セル管理 ───────────────────────────
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("─── 追加セル（変則マス） ───────────", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("ベース矩形の外に1マスずつ追加できます。\nオレンジ色でSceneViewに表示されます。", MessageType.Info);

        // 追加するセルの座標入力
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("追加する座標 X,Y", GUILayout.Width(120));
        _newExtraCell.x = EditorGUILayout.IntField(_newExtraCell.x, GUILayout.Width(50));
        _newExtraCell.y = EditorGUILayout.IntField(_newExtraCell.y, GUILayout.Width(50));
        if (GUILayout.Button("追加", GUILayout.Width(60)))
        {
            Undo.RecordObject(gm, "Add Extra Cell");
            gm.AddExtraCell(_newExtraCell);
            EditorUtility.SetDirty(gm);
        }
        EditorGUILayout.EndHorizontal();

        // 追加済みセルの一覧と削除ボタン
        if (gm.ExtraCells.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("追加済みセル一覧：");
            for (int i = gm.ExtraCells.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  ({gm.ExtraCells[i].x}, {gm.ExtraCells[i].y})", GUILayout.Width(100));
                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    Undo.RecordObject(gm, "Remove Extra Cell");
                    gm.ExtraCells.RemoveAt(i);
                    EditorUtility.SetDirty(gm);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("  （追加セルなし）", EditorStyles.miniLabel);
        }
    }
}
