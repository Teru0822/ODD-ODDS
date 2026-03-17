using UnityEngine;

/// <summary>
/// メインカメラが特定の対象（プレイヤー等）に滑らかに追従するためのクラス
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("カメラが追いかける対象（_doll など）")]
    public Transform Target;

    [Header("Camera Positioning")]
    [Tooltip("対象からどれだけ離れた位置から移すかの相対オフセット値")]
    public Vector3 Offset = new Vector3(0f, 5f, -5f);

    [Header("Follow Settings")]
    [Tooltip("カメラが対象に追いつくまでの滑らかさ（値が小さいほど速い）")]
    [Range(0.01f, 1f)]
    public float SmoothTime = 0.125f;

    // SmoothDamp で使用する現在の速度参照用変数
    private Vector3 _velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        // 対象の位置にオフセットを加えた「カメラが最終的に到達すべき理想の座標」
        Vector3 desiredPosition = Target.position + Offset;

        // 現在の位置から理想の座標に向けて、SmoothDamp で滑らかに移動させる
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, SmoothTime);

        // カメラの座標を更新
        transform.position = smoothedPosition;

        // 必要に応じて、カメラが常に対象の方向を向くように設定する（見下ろしの場合は固定でも良い可能性があるため今回は常にTargetを見る設定にする）
        // ※固定角(X:45度など)のまま動かす場合はLookAtをコメントアウトする設計も可
        transform.LookAt(Target);
    }
}
