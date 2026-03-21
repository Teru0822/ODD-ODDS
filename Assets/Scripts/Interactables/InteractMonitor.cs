using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// モニターをクリックした際にカメラをモニターのアップ視点へ移動させます
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractMonitor : MonoBehaviour, IClickInteractable
{
    [Header("Camera Target")]
    [Tooltip("モニターアップ用の視点Transform（空オブジェクト等）を指定します")]
    public Transform monitorViewTarget;

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

        // UIへのクリック貫通防止
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

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
        if (_mainCamera != null && monitorViewTarget != null)
        {
            _mainCamera.MoveToView(monitorViewTarget);
            Debug.Log("[InteractMonitor] カメラをモニター視点へ移動します。");
        }
        else
        {
            Debug.LogWarning("[InteractMonitor] カメラまたは視点ターゲットが設定されていません。");
        }
    }
}
