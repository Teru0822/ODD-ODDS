using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// カードの束をクリックした際にカメラを手元視点へ移動させます
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractCardDeck : MonoBehaviour, IClickInteractable
{
    [Header("Camera Target")]
    [Tooltip("カード展開時用の手元視点Transform（空オブジェクト等）を指定します")]
    public Transform deckViewTarget;

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
        if (_mainCamera != null && deckViewTarget != null)
        {
            _mainCamera.MoveToView(deckViewTarget);
            Debug.Log("[InteractCardDeck] カメラをカード展開視点へ移動します。");
            
            // TODO: ここで「目の前にカードが並ぶ」処理のトリガーを呼ぶか、追加のUI表示処理を行います。
        }
        else
        {
            Debug.LogWarning("[InteractCardDeck] カメラまたは視点ターゲットが設定されていません。");
        }
    }
}
