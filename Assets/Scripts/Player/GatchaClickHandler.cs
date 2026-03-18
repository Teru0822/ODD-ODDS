using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// _gatya オブジェクトに付けるスクリプト。
/// メインカメラからのRaycastを使い、クリック時に移動力をチャージする。
/// カメラが固定されているため、奥行き(X軸方向)は考慮しない。
/// _gatya オブジェクトには Collider が必要です。
/// </summary>
[RequireComponent(typeof(Collider))]
public class GatchaClickHandler : MonoBehaviour
{
    [Header("Charge Settings")]
    [Tooltip("1回クリックで獲得できる移動力の最小値")]
    public int minCharge = 1;
    [Tooltip("1回クリックで獲得できる移動力の最大値")]
    public int maxCharge = 6;

    [Header("References")]
    [Tooltip("移動力チャージ先のPlayerMovement（未設定ならシーンから自動検索）")]
    public MovementChargeReceiver chargeReceiver;

    private void Start()
    {
        if (chargeReceiver == null)
        {
            chargeReceiver = FindObjectOfType<MovementChargeReceiver>();
            if (chargeReceiver == null)
            {
                Debug.LogWarning("[GatchaClickHandler] MovementChargeReceiver がシーン内に見つかりません。");
            }
        }
    }

    private void Update()
    {
        // 左クリック判定（新Input System）
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // Raycastで _gatya オブジェクトに当たったかチェック
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                int charged = Random.Range(minCharge, maxCharge + 1);
                Debug.Log($"[GatchaClickHandler] クリック成功！ {charged} 移動力を獲得");
                chargeReceiver?.AddMoves(charged);
            }
        }
    }
}
