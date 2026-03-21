using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 人形の箱をクリックした際にカメラを見下ろし視点へ移動させます
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractDollBox : MonoBehaviour, IClickInteractable
{
    [Header("Camera Target")]
    [Tooltip("箱見下ろし用の視点Transform（空オブジェクト等）を指定します")]
    public Transform boxViewTarget;

    private CameraFollow _mainCamera;

    private void Start()
    {
        if (Camera.main != null)
        {
            _mainCamera = Camera.main.GetComponent<CameraFollow>();
        }
    }

    private void Update()
    {
        // 破棄選択中やフロー中は他のインタラクトを禁止
        if (DiscardManager.Instance != null && DiscardManager.Instance.IsDiscarding) return;
        if (CardFlowManager.Instance != null && CardFlowManager.Instance.IsInFlow) return;

        // UI（Canvas）上のクリックだった場合は3D空間側のクリック処理を行わない
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform == transform)
                    {
                        OnInteract();
                    }
                }
            }
        }
    }

    public void OnInteract()
    {
        if (_mainCamera != null && boxViewTarget != null)
        {
            _mainCamera.MoveToView(boxViewTarget);
            Debug.Log("[InteractDollBox] カメラを箱の見下ろし視点へ移動します。");
        }
        else
        {
            Debug.LogWarning("[InteractDollBox] カメラまたは視点ターゲットが設定されていません。");
        }
    }
}
