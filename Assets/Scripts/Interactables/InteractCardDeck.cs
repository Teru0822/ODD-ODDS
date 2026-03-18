using System.Collections;
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

    [Header("Card Drawing")]
    [Tooltip("カードの生成と配置をつかさどる HandManager の参照")]
    public HandManager handManager;
    [Tooltip("1回のクリックで引くカードの枚数（テスト・デフォルト用）")]
    public int cardsToDraw = 3;

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
            
            if (handManager != null)
            {
                // カメラが移動したあとにドロー開始した方が自然なため、少し遅らせて実行
                StartCoroutine(DrawAfterCameraMove());
            }
            else
            {
                Debug.LogWarning("[InteractCardDeck] HandManagerがアタッチされていないため、カード展開処理がスキップされました。");
            }
        }
        else
        {
            Debug.LogWarning("[InteractCardDeck] カメラまたは視点ターゲットが設定されていません。");
        }
    }

    private IEnumerator DrawAfterCameraMove()
    {
        // カメラが指定視点へ向かうのとおおよそ同じ時間（smoothTime目安）待機
        yield return new WaitForSeconds(0.4f);
        
        // カードのドロー指示
        handManager.DrawCards(cardsToDraw);
    }
}
