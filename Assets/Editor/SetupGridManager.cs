using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GridManager をシーンに自動でセットアップするエディタツール
/// </summary>
public class SetupGridManager
{
    [MenuItem("Tools/Setup GridManager")]
    public static void Setup()
    {
        // すでに存在する場合はスキップ
        GridManager existing = Object.FindObjectOfType<GridManager>();
        if (existing != null)
        {
            Debug.Log($"[SetupGridManager] GridManager はすでに '{existing.gameObject.name}' にアタッチされています。");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // 空のGameObjectを作成してアタッチ
        GameObject go = new GameObject("[System]_GridManager");
        GridManager gm = go.AddComponent<GridManager>();

        // デフォルトのグリッドサイズ（必要に応じてInspectorで変更してください）
        gm.MapSize = new Vector2Int(10, 10);

        // Hierarchyで選択状態にして確認しやすくする
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupGridManager] [System]_GridManager を作成し、GridManager をアタッチしました！MapSize を迷路のマス数に合わせてください。");
    }
}
