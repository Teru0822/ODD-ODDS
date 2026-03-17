using UnityEngine;

/// <summary>
/// グリッド座標計算に関する各種ヘルパー拡張メソッド
/// </summary>
public static class GridExtensions
{
    /// <summary>
    /// DirectionType（方向）をVector2Intの移動ベクトルへ変換する
    /// 左下を (0,0) とした2Dグリッドを想定
    /// </summary>
    /// <param name="direction">対象の方向</param>
    /// <returns>その方向への1ステップ移動量</returns>
    public static Vector2Int ToVector2Int(this DirectionType direction)
    {
        switch (direction)
        {
            case DirectionType.Down:
                return new Vector2Int(0, -1);
            case DirectionType.Left:
                return new Vector2Int(-1, 0);
            case DirectionType.Up:
                return new Vector2Int(0, 1);
            case DirectionType.Right:
                return new Vector2Int(1, 0);
            default:
                Debug.LogWarning($"[GridExtensions] Unknown DirectionType: {direction}");
                return Vector2Int.zero;
        }
    }
}
