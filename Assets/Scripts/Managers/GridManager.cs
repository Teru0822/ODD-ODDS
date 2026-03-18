using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 盤面（グリッド）のサイズや座標に関する情報を一元管理するマネージャークラス。
/// 矩形ベースに加えて、任意の1マス単位で追加セルを登録できる。
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("ベース盤面のサイズ（幅, 高さ）")]
    public Vector2Int MapSize = new Vector2Int(10, 10);

    [Tooltip("1マスあたりのワールド単位でのサイズ")]
    public float CellSize = 0.04f;

    [Header("Extra Cells")]
    [Tooltip("ベース矩形の外に追加する個別セルのリスト")]
    public List<Vector2Int> ExtraCells = new List<Vector2Int>();

    [Header("Gizmos")]
    [Tooltip("シーンビューでグリッド線を表示する")]
    public bool ShowGizmos = true;
    [Tooltip("ベースグリッドの色")]
    public Color GizmoColor = new Color(0f, 1f, 0f, 0.5f);
    [Tooltip("追加セルの色")]
    public Color ExtraCellColor = new Color(1f, 0.6f, 0f, 0.8f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 座標がベース矩形または追加セルとして有効か判定する
    /// </summary>
    public bool IsValidGridPosition(Vector2Int position)
    {
        // ベース矩形の範囲内
        bool inBase = position.x >= 0 && position.x < MapSize.x &&
                      position.y >= 0 && position.y < MapSize.y;
        if (inBase) return true;

        // 追加セルのリストに含まれているか
        return ExtraCells.Contains(position);
    }

    /// <summary>
    /// グリッド座標をワールド座標に変換する
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return transform.position + new Vector3(gridPos.x * CellSize, 0f, gridPos.y * CellSize);
    }

    /// <summary>
    /// 追加セルを1つ追加する（重複なし）
    /// </summary>
    public void AddExtraCell(Vector2Int pos)
    {
        if (!ExtraCells.Contains(pos))
            ExtraCells.Add(pos);
    }

    /// <summary>
    /// 追加セルを1つ削除する
    /// </summary>
    public void RemoveExtraCell(Vector2Int pos)
    {
        ExtraCells.Remove(pos);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!ShowGizmos) return;
        Vector3 origin = transform.position;

        // ベースグリッドの線
        Gizmos.color = GizmoColor;
        float w = MapSize.x * CellSize;
        float h = MapSize.y * CellSize;
        for (int x = 0; x <= MapSize.x; x++)
            Gizmos.DrawLine(origin + new Vector3(x * CellSize, 0f, 0f),
                            origin + new Vector3(x * CellSize, 0f, h));
        for (int y = 0; y <= MapSize.y; y++)
            Gizmos.DrawLine(origin + new Vector3(0f, 0f, y * CellSize),
                            origin + new Vector3(w,   0f, y * CellSize));

        // 追加セルをオレンジ色の塗りつぶし四角で表示
        Gizmos.color = ExtraCellColor;
        foreach (var cell in ExtraCells)
        {
            Vector3 cellOrigin = origin + new Vector3(cell.x * CellSize, 0.001f, cell.y * CellSize);
            // 4辺を描く
            Vector3 a = cellOrigin;
            Vector3 b = cellOrigin + new Vector3(CellSize, 0f, 0f);
            Vector3 c = cellOrigin + new Vector3(CellSize, 0f, CellSize);
            Vector3 d = cellOrigin + new Vector3(0f, 0f, CellSize);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
    }
#endif
}
