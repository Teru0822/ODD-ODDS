using UnityEngine;

/// <summary>
/// 人形（プレイヤー）の盤面上での座標移動や計算を管理するクラス
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Position Settings")]
    [Tooltip("現在の盤面（グリッド）上の座標")]
    [SerializeField]
    private Vector2Int _currentGridPosition = Vector2Int.zero;
    private int _bonusSteps = 0; // アイテムカード等で付与される一時的な移動量ボーナス

    private void Start()
    {
        // 初期グリッド座標をワールド座標に反映（GridManager の位置に同期）
        SnapToGrid();
        Debug.Log($"[PlayerMovement] Initialized at Grid Position: {_currentGridPosition}");
    }

    /// <summary>
    /// 現在の座標を取得する
    /// </summary>
    public Vector2Int GetCurrentPosition()
    {
        return _currentGridPosition;
    }

    /// <summary>
    /// DirectionType に対応する GridManager の X/Z 方向を返す
    /// </summary>
    private Vector3 GetWorldDirection(DirectionType direction)
    {
        // GridToWorld は X軸(gridPos.x)→ワールドX、Y軸(gridPos.y)→ワールドZ に対応している
        switch (direction)
        {
            case DirectionType.Up:    return Vector3.right;    // +X
            case DirectionType.Down:  return Vector3.left;     // -X
            case DirectionType.Left:  return Vector3.forward;  // +Z
            case DirectionType.Right: return Vector3.back;     // -Z
            default: return Vector3.zero;
        }
    }

    /// <summary>
    /// 次回移動時に追加されるボーナスステップを加算します
    /// </summary>
    public void AddMoveBonus(int amount)
    {
        _bonusSteps += amount;
        Debug.Log($"[PlayerMovement] Move Bonus Added: +{amount} (Total Bonus: {_bonusSteps})");
    }

    /// <summary>
    /// 指定方向へ一定数進む
    /// </summary>
    public void Move(DirectionType direction, int steps)
    {
        int totalSteps = steps + _bonusSteps;
        Debug.Log($"[PlayerMovement] Moving in direction {direction}: Base={steps}, Bonus={_bonusSteps}, Total={totalSteps}");
        
        _bonusSteps = 0; // ボーナスは一度の移動で消費

        if (totalSteps <= 0) return;

        Vector2Int moveVector = direction.ToVector2Int();
        Vector2Int targetPosition = _currentGridPosition;

        int actualMovedSteps = 0;
        for (int i = 0; i < totalSteps; i++)
        {
            Vector2Int nextPos = targetPosition + moveVector;

            if (GridManager.Instance != null && !GridManager.Instance.IsValidGridPosition(nextPos))
            {
                Debug.Log($"[PlayerMovement] 盤面の端に到達したため停止: {targetPosition} -> {nextPos}");
                break;
            }

            targetPosition = nextPos;
            actualMovedSteps++;
        }

        if (actualMovedSteps > 0)
        {
            _currentGridPosition = targetPosition;

            // GridManager.GridToWorld() でワールド座標を正確に取得して反映
            if (GridManager.Instance != null)
            {
                Vector3 worldPos = GridManager.Instance.GridToWorld(_currentGridPosition);
                // Y座標はオブジェクト自身の高さを維持する
                transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);
            }

            // 進行方向に体を向ける
            Vector3 dir = GetWorldDirection(direction);
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            Debug.Log($"[PlayerMovement] {actualMovedSteps}マス移動完了 → Grid:{_currentGridPosition}");
        }
        else
        {
            Debug.Log("[PlayerMovement] 移動できませんでした。");
        }
    }

    /// <summary>
    /// グリッド座標に基づいて Transform を即座にスナップする
    /// </summary>
    private void SnapToGrid()
    {
        if (GridManager.Instance == null) return;
        Vector3 worldPos = GridManager.Instance.GridToWorld(_currentGridPosition);
        transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);
    }

    /// <summary>
    /// 複数の移動コマンドを順番に実行するコルーチン
    /// </summary>
    /// <param name="moveOrder">移動カードのリスト</param>
    /// <param name="delayBetweenMoves">移動ごとの待機時間</param>
    public System.Collections.IEnumerator MoveSequence(System.Collections.Generic.List<MoveCardData> moveOrder, float delayBetweenMoves)
    {
        if (moveOrder == null || moveOrder.Count == 0) yield break;

        Debug.Log($"[PlayerMovement] MoveSequence開始（全 {moveOrder.Count} 回）");

        foreach (var moveCmd in moveOrder)
        {
            if (moveCmd == null) continue;

            Move(moveCmd.Direction, moveCmd.Steps);
            
            // 各移動の後に指定時間待機する
            yield return new WaitForSeconds(delayBetweenMoves);
        }

        Debug.Log("[PlayerMovement] MoveSequence終了");
    }
}
