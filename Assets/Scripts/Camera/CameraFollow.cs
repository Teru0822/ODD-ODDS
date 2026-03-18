using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// メインカメラの追従機能および、特定オブジェクトクリック時の視点移動を管理するクラス
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public enum CameraMode
    {
        Follow,
        FixedView
    }

    [Header("Current Mode")]
    [Tooltip("現在のカメラ動作モード")]
    public CameraMode currentMode = CameraMode.FixedView; // 初期は固定位置

    [Header("Follow Settings")]
    [Tooltip("カメラが追いかける対象（_doll など）")]
    public Transform target;

    [Tooltip("対象からどれだけ離れた位置から移すかの相対オフセット値")]
    public Vector3 offset = new Vector3(0f, 5f, -5f);

    [Header("Movement Settings")]
    [Tooltip("カメラが移動する際の滑らかさ（値が小さいほど速い）")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.125f;

    [Tooltip("視点移動時の回転の滑らかさ")]
    public float rotationLerpSpeed = 5f;

    // FixedView用のターゲット座標と回転
    private Vector3 _fixedViewPosition;
    private Quaternion _fixedViewRotation;

    // 初期状態の保存用
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // SmoothDamp で使用する現在の速度参照用変数
    private Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        // 最初の位置と回転を保存して、初期カメラとして設定
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        
        MoveToPosition(_initialPosition, _initialRotation);
    }

    private void Update()
    {
        // ESCキーで初期状態（元の画面）へ戻る
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            MoveToPosition(_initialPosition, _initialRotation);
        }
    }

    private void LateUpdate()
    {
        if (currentMode == CameraMode.Follow)
        {
            if (target == null) return;

            // 対象の位置にオフセットを加えた「カメラが最終的に到達すべき理想の座標」
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, smoothTime);

            Vector3 direction = target.position - transform.position;
            if (direction.sqrMagnitude > 0.001f) // LookRotationのErrorを防止
            {
                // 追従時は対象を常に向く
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
            }
        }
        else if (currentMode == CameraMode.FixedView)
        {
            // スクリプトで指定された固定視点へ滑らかに移動・回転
            transform.position = Vector3.SmoothDamp(transform.position, _fixedViewPosition, ref _velocity, smoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _fixedViewRotation, Time.deltaTime * rotationLerpSpeed);
        }
    }

    /// <summary>
    /// 指定したTransform（ダミーの視点オブジェクト）の座標・回転へカメラを移動させます
    /// </summary>
    public void MoveToView(Transform viewTransform)
    {
        if (viewTransform == null) return;
        MoveToPosition(viewTransform.position, viewTransform.rotation);
    }

    /// <summary>
    /// 指定した座標と回転にカメラを移動させ実行します
    /// </summary>
    public void MoveToPosition(Vector3 pos, Quaternion rot)
    {
        _fixedViewPosition = pos;
        _fixedViewRotation = rot;
        currentMode = CameraMode.FixedView;

        // どこかの視点へ移動する際やESCキー戻り時はマネーUIを隠す
        HideAllUIs();
    }

    /// <summary>
    /// 通常のプレイヤー追従モードに戻します
    /// </summary>
    public void ResetToFollow()
    {
        currentMode = CameraMode.Follow;
        HideAllUIs();
    }

    private void HideAllUIs()
    {
        var interactMoney = FindObjectOfType<InteractMoney>();
        if (interactMoney != null)
        {
            interactMoney.HideUI();
        }
    }
}
