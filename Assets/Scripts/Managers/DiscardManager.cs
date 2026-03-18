using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// カード保持上限（10枚）を超えた際の、破棄カード選択モードを管理するクラス
/// </summary>
public class DiscardManager : MonoBehaviour
{
    public static DiscardManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("破棄選択時に表示するUIパネル（テキストと決定ボタンを含む親）")]
    public GameObject discardUIPanel;
    [Tooltip("破棄を確定させるボタン")]
    public Button confirmButton;
    [Tooltip("「あと○枚選んでください」等の状況を表示するテキスト（任意）")]
    public Text statusText;

    [Header("Settings")]
    [Tooltip("選択時のハイライト色")]
    public Color highlightColor = Color.red;

    // 現在のモード状態
    public bool IsDiscarding { get; private set; } = false;

    // 破棄対象として選択されているカードのリスト
    private HashSet<CardObject> _selectedCards = new HashSet<CardObject>();
    
    // 現在展開中の全カードデータ（既存+新規）
    private List<CardData> _pendingCards = new List<CardData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (discardUIPanel != null) discardUIPanel.SetActive(false);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        
        if (confirmButton == null)
        {
            Debug.LogError("[DiscardManager] ConfirmButton が設定されていません！Inspectorを確認してください。");
        }
    }

    private void Update()
    {
        if (IsDiscarding)
        {
            // 他のスクリプト（Camera等）がカーソルを上書きしないよう、毎フレーム強制する
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // デバッグログ: クリックが検知されているか確認（新InputSystemを使用）
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Debug.Log($"[DiscardManager] Mouse Clicked at: {mousePos}");

                // 【診断用】マウスの下にあるUIオブジェクトをすべてリストアップする
                if (EventSystem.current != null)
                {
                    PointerEventData eventData = new PointerEventData(EventSystem.current);
                    eventData.position = mousePos;
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, results);

                    if (results.Count > 0)
                    {
                        foreach (var res in results)
                        {
                            Debug.Log($"[DiscardManager] UIレイキャスト検知: {res.gameObject.name} (Layer: {res.gameObject.layer})");
                            
                            // 【強制的解決策】EventSystemのOnClickが動かない場合への保険として、直接ボタン判定を行う
                            if (confirmButton != null && (res.gameObject == confirmButton.gameObject || res.gameObject.transform.parent?.gameObject == confirmButton.gameObject))
                            {
                                Debug.Log("[DiscardManager] 手動クリック判定による実行を行います。");
                                OnConfirmButtonClicked();
                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[DiscardManager] UIレイキャスト検知なし。UIシステムにクリックが届いていません。");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 破棄選択モードを開始します
    /// </summary>
    public void BeginDiscardFlow(List<CardData> allCards)
    {
        if (IsDiscarding) return;
        StartCoroutine(BeginDiscardFlowRoutine(allCards));
    }

    private IEnumerator BeginDiscardFlowRoutine(List<CardData> allCards)
    {
        IsDiscarding = true;
        _pendingCards = allCards;
        _selectedCards.Clear();

        Debug.Log($"[DiscardManager] 破棄選択フロー開始: 合計 {allCards.Count} 枚。10枚以下になるまでカードを選んでください。");

        // UI表示
        if (discardUIPanel != null) discardUIPanel.SetActive(true);
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            Debug.Log("[DiscardManager] 決定ボタンを初期状態(無効)に設定しました。");
        }

        // HandManagerに全カードを展開させる
        var handManager = FindObjectOfType<HandManager>();
        if (handManager != null)
        {
            // まず既存の表示をクリア
            handManager.ClearHand();
            // 【重要】消去アニメーションが終わるまで待つ（描画競合を防ぐ最重要ポイント）
            yield return new WaitForSeconds(0.6f);

            // カメラを手元（カード展開位置）に移動
            if (CameraFollow.Instance != null && handManager.handCenterPoint != null)
            {
                CameraFollow.Instance.MoveToView(handManager.handCenterPoint);
                yield return new WaitForSeconds(0.5f); // カメラ移動を少し待つ
            }

            // 全カードを描画
            handManager.DrawCards(_pendingCards);
        }

        UpdateStatusText();

        // マウスカーソルを強制表示
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// カードの選択状態を切り替えます（CardObjectから呼ばれる）
    /// </summary>
    public void ToggleSelection(CardObject card)
    {
        if (!IsDiscarding) return;

        if (_selectedCards.Contains(card))
        {
            _selectedCards.Remove(card);
            card.SetHighlight(false, highlightColor);
        }
        else
        {
            _selectedCards.Add(card);
            card.SetHighlight(true, highlightColor);
        }

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        int max = PlayerHand.Instance != null ? PlayerHand.Instance.maxHandSize : 10;
        int remaining = _pendingCards.Count - _selectedCards.Count;
        int needToDiscard = _pendingCards.Count - max;

        bool canConfirm = (remaining <= max);
        if (confirmButton != null) confirmButton.interactable = canConfirm;

        string msg = "";
        if (canConfirm)
        {
            msg = "決定ボタンを押して完了してください";
        }
        else
        {
            int more = remaining - max;
            msg = $"あと {more} 枚、捨てるカードを選んでください";
        }

        if (statusText != null) statusText.text = msg;
        Debug.Log($"[DiscardManager] {msg} (現在の手札予定: {remaining}/{max})");
    }

    /// <summary>
    /// 決定ボタン押下時の処理
    /// </summary>
    public void OnConfirmButtonClicked()
    {
        Debug.Log("[DiscardManager] OnConfirmButtonClicked 呼び出し検知！");
        if (!IsDiscarding)
        {
            Debug.Log("[DiscardManager] IsDiscarding が false のため中断します。");
            return;
        }

        // シーン内の全カードを確認
        var handManager = FindObjectOfType<HandManager>();
        if (handManager == null)
        {
            Debug.LogError("[DiscardManager] handManager が見つかりません。");
            return;
        }

        // 残すカード（選択されていないカード）を抽出
        List<CardData> keptCards = new List<CardData>();
        
        // 【修正】インスタンスベースで判定し、重複データも正しく扱えるようにする
        foreach (var cardInHand in handManager.GetDrawnCardObjects())
        {
            if (cardInHand == null) continue;

            if (!_selectedCards.Contains(cardInHand))
            {
                // 選択されていない（＝捨てるリストに入っていない）カードを残す
                keptCards.Add(cardInHand.CardData);
            }
        }

        // 上限チェック（10枚以下になっているか）
        if (keptCards.Count > 10)
        {
            Debug.LogWarning($"[DiscardManager] まだ {keptCards.Count} 枚残っています。10枚以下になるように選んでください。");
            return;
        }

        // 破棄確定処理
        FinalizeDiscard(keptCards);
    }

    private void FinalizeDiscard(List<CardData> keptCards)
    {
        // 1. PlayerHandを更新
        if (PlayerHand.Instance != null)
        {
            PlayerHand.Instance.ClearAll();
            PlayerHand.Instance.AddCards(keptCards);
        }

        // 2. モード終了
        IsDiscarding = false;
        
        // 3. UI非表示
        if (discardUIPanel != null) discardUIPanel.SetActive(false);

        // 4. HandManagerに現在の（整理された）手札を表示させる演出
        var handManager = FindObjectOfType<HandManager>();
        if (handManager != null)
        {
            // いったん全消去して、 keptCards だけを戻す演出
            handManager.ClearHand();
            // 少し待ってから展開（DeckViewと同じ挙動）
            StartCoroutine(ReturnToDeckFlow(handManager, keptCards));
        }
        
        Debug.Log("[DiscardManager] Discard finalized. Remaining cards saved to PlayerHand.");
    }

    private IEnumerator ReturnToDeckFlow(HandManager hm, List<CardData> keptCards)
    {
        yield return new WaitForSeconds(0.6f);
        hm.DrawCards(keptCards);
    }
}
