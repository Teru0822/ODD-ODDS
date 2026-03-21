using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// カードの束をクリックした際にPlayerHandに保持されているカードを展開します
/// （パック開封はガチャボタンが担当します）
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

    private CameraFollow _mainCamera;

    private void Start()
    {
        if (Camera.main != null)
        {
            _mainCamera = Camera.main.GetComponent<CameraFollow>();
        }

        // インスペクターで未設定の場合、自動検索する
        if (handManager == null)
        {
            handManager = FindObjectOfType<HandManager>();
        }
    }

    private void Update()
    {
        // 破棄選択中は他のインタラクトを禁止
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
        if (_mainCamera == null || deckViewTarget == null)
        {
            Debug.LogWarning("[InteractCardDeck] カメラまたは視点ターゲットが設定されていません。");
            return;
        }

        if (PlayerHand.Instance == null)
        {
            // インスタンスがなければシーン内を再検索してみる
            var foundHand = FindObjectOfType<PlayerHand>();
            if (foundHand == null)
            {
                Debug.LogError("[InteractCardDeck] PlayerHandがシーンに存在しません。PlayerHandスクリプトを _Managers 等のオブジェクトにアタッチしてください。");
                return;
            }
        }

        // PlayerHandの保持カードを取得
        List<CardData> heldCards = PlayerHand.Instance.GetHeldCards();
        if (heldCards == null || heldCards.Count == 0)
        {
            Debug.Log("[InteractCardDeck] 保持しているカードがありません。");
            return;
        }

        // カメラをDeckViewへ移動し、保持カードを展開
        _mainCamera.MoveToView(deckViewTarget);
        Debug.Log($"[InteractCardDeck] 保持カード {heldCards.Count} 枚を展開します。");

        if (handManager != null)
        {
            StartCoroutine(DrawAfterCameraMove(heldCards));
        }
        else
        {
            Debug.LogWarning("[InteractCardDeck] HandManagerがアタッチされていないためカード展開をスキップしました。");
        }
    }

    private IEnumerator DrawAfterCameraMove(List<CardData> heldCards)
    {
        yield return new WaitForSeconds(0.4f);
        handManager.DrawCards(heldCards);
    }
}
