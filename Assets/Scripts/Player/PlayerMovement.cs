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

    [Header("Movement Settings")]
    [Tooltip("1マスあたりのワールド移動距離")]
    [SerializeField]
    private float _moveStepWorldSize = 0.04f;

    private void Start()
    {
        // 例えば初期位置のバリデーションなどに使用
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
    /// DirectionType に対応するワールド空間での進行方向ベクトルを取得する
    /// </summary>
    private Vector3 GetWorldDirection(DirectionType direction)
    {
        switch (direction)
        {
            case DirectionType.Up:    return new Vector3(1f, 0f, 0f);   // 前進: +X
            case DirectionType.Left:  return new Vector3(0f, 0f, 1f);   // 左: +Z
            case DirectionType.Down:  return new Vector3(-1f, 0f, 0f);  // 後退: -X
            case DirectionType.Right: return new Vector3(0f, 0f, -1f);  // 右: -Z
            default: return Vector3.zero;
        }
    }

    /// <summary>
    /// 引き当てた移動カード等によって、指定方向へ一定数進む
    /// </summary>
    /// <param name="direction">移動方向</param>
    /// <param name="steps">移動するマス数</param>
    public void Move(DirectionType direction, int steps)
    {
        if (steps <= 0) return;

        Vector2Int moveVector = direction.ToVector2Int();
        Vector2Int targetPosition = _currentGridPosition;

        // 1マスずつ進めるか判定する（壁抜け防止のため）
        int actualMovedSteps = 0;
        for (int i = 0; i < steps; i++)
        {
            Vector2Int nextPos = targetPosition + moveVector;

            // TODO: 後日のIssueで壁(障害物)判定が追加されたら、ここに条件を追加
            if (GridManager.Instance != null && !GridManager.Instance.IsValidGridPosition(nextPos))
            {
                Debug.Log($"[PlayerMovement] 盤面の端に到達したため、進行を停止します。 {targetPosition} -> {nextPos}");
                break;
            }

            targetPosition = nextPos;
            actualMovedSteps++;
        }

        if (actualMovedSteps > 0)
        {
            _currentGridPosition = targetPosition;
            Debug.Log($"[PlayerMovement] Moved {actualMovedSteps} step(s) to {direction}. New Position: {_currentGridPosition}");
            
            // 指定されたワールド方向に合わせて移動させる
            Vector3 worldDirection = GetWorldDirection(direction);
            Vector3 moveDelta = worldDirection * (_moveStepWorldSize * actualMovedSteps);
            transform.position += moveDelta;

            // 体の向きを進行方向に向ける
            if (worldDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            }
        }
        else
        {
            Debug.Log("[PlayerMovement] 移動できませんでした。");
        }
    }
}
