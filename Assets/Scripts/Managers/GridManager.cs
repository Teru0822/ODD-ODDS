using UnityEngine;

/// <summary>
/// 盤面（グリッド）のサイズや座標に関する情報を一元管理するマネージャークラス
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("盤面のサイズ（幅, 高さ）")]
    public Vector2Int MapSize = new Vector2Int(10, 10);

    private void Awake()
    {
        // シングルトン設定
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 引数で渡された座標 (x, y) が盤面の範囲内か判定する
    /// 座標は 0 <= x < width, 0 <= y < height の範囲とする
    /// </summary>
    /// <param name="position">判定したいグリッド座標</param>
    /// <returns>範囲内であれば true</returns>
    public bool IsValidGridPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < MapSize.x &&
               position.y >= 0 && position.y < MapSize.y;
    }
}
