using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// カードの束をクリックした際に未開封パックを1つ消費し、カメラを手元視点へ移動させてカードを展開します
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
    [Tooltip("クリック時に開封するパックの種類ID（デフォルト0）")]
    public int targetPackId = 0;

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
            if (PackManager.Instance == null)
            {
                Debug.LogError("[InteractCardDeck] PackManagerが存在しません。");
                return;
            }

            // パックを開封し、カードデータのリストを取得
            List<CardData> drawnCards = PackManager.Instance.OpenPack(targetPackId);
            if (drawnCards == null || drawnCards.Count == 0)
            {
                // 所持していない、またはデータベースが空の場合は終了
                Debug.Log("[InteractCardDeck] パックがないため、またはエラーのためドローできませんでした。");
                return;
            }

            _mainCamera.MoveToView(deckViewTarget);
            Debug.Log("[InteractCardDeck] カメラをカード展開視点へ移動します。");
            
            if (handManager != null)
            {
                // カメラが移動したあとにドロー開始した方が自然なため、少し遅らせて実行
                StartCoroutine(DrawAfterCameraMove(drawnCards));
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

    private IEnumerator DrawAfterCameraMove(List<CardData> drawnCards)
    {
        // カメラが指定視点へ向かうのとおおよそ同じ時間（smoothTime目安）待機
        yield return new WaitForSeconds(0.4f);
        
        // 排出されたカードデータのリストを手元に展開
        handManager.DrawCards(drawnCards);
    }
}
