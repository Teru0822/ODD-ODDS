using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 自販機をクリックした際にカメラを自販機のアップ視点へ移動させます
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractVendingMachine : MonoBehaviour, IClickInteractable
{
    [Header("Camera Target")]
    [Tooltip("自販機アップ用の視点Transform（空オブジェクト等）を指定します")]
    public Transform vendingMachineViewTarget;

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
        // 破棄選択中は他のインタラクトを禁止
        if (DiscardManager.Instance != null && DiscardManager.Instance.IsDiscarding) return;

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
        if (_mainCamera != null && vendingMachineViewTarget != null)
        {
            _mainCamera.MoveToView(vendingMachineViewTarget);
            Debug.Log("[InteractVendingMachine] カメラを自販機視点へ移動します。");
        }
        else
        {
            Debug.LogWarning("[InteractVendingMachine] カメラまたは視点ターゲットが設定されていません。");
        }
    }
}
