using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// カード使用フローの各状態を定義する列挙型
/// </summary>
public enum CardFlowPhase
{
    Idle,                // 待機中（通常のゲームプレイ）
    ShowingCards,        // パックからカード展開中
    SelectingMoveOrder,  // 移動カード順序選択中
    OptionalItemUse,     // アイテムカード即時使用選択中
    ProcessingMoney,     // マネーカード自動処理中
    SavingItems,         // アイテムカード保存中
    DiscardingItems,     // アイテム破棄選択中（DiscardManagerに委譲）
    MovingDoll           // 俯瞰視点で人形移動中
}

/// <summary>
/// カードパック開封後の使用フローを一元管理するマネージャークラス
/// </summary>
public class CardFlowManager : MonoBehaviour
{
    public static CardFlowManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("移動カード順序選択時に表示するUIパネル")]
    public GameObject moveOrderUIPanel;
    [Tooltip("移動順序決定ボタン")]
    public Button confirmMoveOrderButton;
    [Tooltip("移動順序リセットボタン")]
    public Button resetMoveOrderButton;

    [Space(10)]
    [Tooltip("アイテム即時使用選択時に表示するUIパネル")]
    public GameObject optionalItemUseUIPanel;
    [Tooltip("アイテム使用を終えて次へ進むボタン")]
    public Button proceedFromItemsButton;

    [Header("Settings")]
    public float delayBetweenMoves = 1.0f; // 俯瞰移動時のコマンド間の待機時間

    public CardFlowPhase CurrentPhase { get; private set; } = CardFlowPhase.Idle;

    public bool IsInFlow => CurrentPhase != CardFlowPhase.Idle;

    // パックから出たカードの種別ごとのリスト
    private List<MoveCardData> _moveCards = new List<MoveCardData>();
    private List<MoneyCardData> _moneyCards = new List<MoneyCardData>();
    private List<ItemCardData> _itemCards = new List<ItemCardData>();

    // 順序選択用の管理リスト
    private List<CardObject> _selectedMoveCardObjects = new List<CardObject>();
    private List<MoveCardData> _moveOrder = new List<MoveCardData>();

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
        // UIイベントの登録と初期非表示設定
        if (moveOrderUIPanel != null) moveOrderUIPanel.SetActive(false);
        if (optionalItemUseUIPanel != null) optionalItemUseUIPanel.SetActive(false);

        if (confirmMoveOrderButton != null) confirmMoveOrderButton.onClick.AddListener(OnConfirmMoveOrder);
        if (resetMoveOrderButton != null) resetMoveOrderButton.onClick.AddListener(OnResetMoveOrder);
        if (proceedFromItemsButton != null) proceedFromItemsButton.onClick.AddListener(OnProceedFromItems);

        // DiscardManagerのコールバックを登録
        if (DiscardManager.Instance != null)
        {
            DiscardManager.Instance.OnDiscardCompleted += OnDiscardCompleted;
        }
    }

    private void OnDestroy()
    {
        if (DiscardManager.Instance != null)
        {
            DiscardManager.Instance.OnDiscardCompleted -= OnDiscardCompleted;
        }
    }

    /// <summary>
    /// パックから出たカードのリストを受け取り、フローを開始します。
    /// </summary>
    public void StartFlow(List<CardData> drawnCards)
    {
        if (IsInFlow) return;

        CurrentPhase = CardFlowPhase.ShowingCards;
        Debug.Log("[CardFlowManager] フローを開始します。");

        // GameStateを対応させる（PackOpening）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.PackOpening);
        }

        // リストの初期化
        _moveCards.Clear();
        _moneyCards.Clear();
        _itemCards.Clear();
        _selectedMoveCardObjects.Clear();
        _moveOrder.Clear();

        // 種別ごとに振り分け
        foreach (var c in drawnCards)
        {
            if (c is MoveCardData moveCard) _moveCards.Add(moveCard);
            else if (c is MoneyCardData moneyCard) _moneyCards.Add(moneyCard);
            else if (c is ItemCardData itemCard) _itemCards.Add(itemCard);
        }

        StartCoroutine(ShowingCardsRoutine(drawnCards));
    }

    private IEnumerator ShowingCardsRoutine(List<CardData> drawnCards)
    {
        var handManager = Object.FindFirstObjectByType<HandManager>();
        if (handManager != null)
        {
            // カメラ移動と描画
            if (CameraFollow.Instance != null && handManager.deckOrigin != null) // deckViewTargetが無いためhandCenterPointなどを使う想定
            {
                CameraFollow.Instance.MoveToView(handManager.handCenterPoint);
                yield return new WaitForSeconds(0.5f);
            }
            handManager.DrawCards(drawnCards, true); // パックからのカードとして描画
            yield return new WaitForSeconds(0.5f + (drawnCards.Count * handManager.delayBetweenCards));
        }

        // 描画後、次のフェーズへ（アイテム使用フェーズを先に持ってくる）
        TransitionToOptionalItemUse();
    }

    private void TransitionToSelectingMoveOrder()
    {
        CurrentPhase = CardFlowPhase.SelectingMoveOrder;

        if (_moveCards.Count == 0)
        {
            Debug.Log("[CardFlowManager] 移動カードがないため、スキップします。");
            TransitionToProcessingMoney();
            return;
        }

        Debug.Log("[CardFlowManager] 移動カード順序選択フェーズ開始。");
        if (moveOrderUIPanel != null) moveOrderUIPanel.SetActive(true);
        if (confirmMoveOrderButton != null) confirmMoveOrderButton.interactable = false;
        
        // 1枚しか無い場合は自動確定しても良いが、一旦はプレイヤーにクリックさせるか、仕様による
        // 今回の仕様ではクリックして選ばせる（確認のため）
    }

    /// <summary>
    /// CardObjectのOnInteractから呼ばれる（移動カードの場合）
    /// </summary>
    public void OnMoveCardClicked(CardObject card)
    {
        if (CurrentPhase != CardFlowPhase.SelectingMoveOrder) return;
        if (!(card.CardData is MoveCardData)) return;

        // すでに選択済みの場合は解除
        if (_selectedMoveCardObjects.Contains(card))
        {
            _selectedMoveCardObjects.Remove(card);
            _moveOrder.Remove(card.CardData as MoveCardData);
            card.ClearOrderNumber();
            
            // 残りのカードの番号を振り直す
            for (int i = 0; i < _selectedMoveCardObjects.Count; i++)
            {
                _selectedMoveCardObjects[i].SetOrderNumber(i + 1);
            }
        }
        else
        {
            // 選ばれていない場合、追加
            _selectedMoveCardObjects.Add(card);
            _moveOrder.Add(card.CardData as MoveCardData);
            card.SetOrderNumber(_selectedMoveCardObjects.Count);
        }

        // 選ばれた枚数が手持ちの移動カードと同じなら決定可能
        if (confirmMoveOrderButton != null)
        {
            confirmMoveOrderButton.interactable = (_selectedMoveCardObjects.Count == _moveCards.Count);
        }
    }

    private void OnResetMoveOrder()
    {
        foreach (var cardObj in _selectedMoveCardObjects)
        {
            cardObj.ClearOrderNumber();
        }
        _selectedMoveCardObjects.Clear();
        _moveOrder.Clear();

        if (confirmMoveOrderButton != null) confirmMoveOrderButton.interactable = false;
    }

    private void OnConfirmMoveOrder()
    {
        if (_selectedMoveCardObjects.Count != _moveCards.Count) return;

        if (moveOrderUIPanel != null) moveOrderUIPanel.SetActive(false);
        TransitionToProcessingMoney();
    }

    private void TransitionToOptionalItemUse()
    {
        CurrentPhase = CardFlowPhase.OptionalItemUse;

        if (_itemCards.Count == 0)
        {
            Debug.Log("[CardFlowManager] アイテムカードがないため、スキップします。");
            TransitionToSelectingMoveOrder();
            return;
        }

        Debug.Log("[CardFlowManager] アイテム即時使用フェーズ開始。");
        if (optionalItemUseUIPanel != null) optionalItemUseUIPanel.SetActive(true);
    }

    /// <summary>
    /// CardObjectのOnInteractから呼ばれる（アイテムカードの場合）
    /// </summary>
    public void OnItemCardClicked(CardObject card)
    {
        if (CurrentPhase != CardFlowPhase.OptionalItemUse) return;
        if (!(card.CardData is ItemCardData itemData)) return;

        // 効果を発動
        Debug.Log($"[CardFlowManager] {itemData.CardName} を即時使用しました。");
        
        switch (itemData.EffectType)
        {
            case ItemEffectType.AddMoveStep:
                var playerMov = Object.FindFirstObjectByType<PlayerMovement>();
                if (playerMov != null) playerMov.AddMoveBonus(itemData.EffectValue);
                break;
            case ItemEffectType.Redraw:
                Debug.Log($"[CardFlowManager] Redraw効果発動！フローをリセットして新しく引き直します。");
                StartCoroutine(RedrawFlowRoutine());
                return; // ここで返す（以降の処理は不要）
            default:
                Debug.LogWarning($"[CardFlowManager] {itemData.EffectType} の処理はスキップされました。");
                break;
        }

        // 使用したカードをリストから消し、見た目も消す
        _itemCards.Remove(itemData);
        var handManager = Object.FindFirstObjectByType<HandManager>();
        if (handManager != null) handManager.RemoveCardFromHand(card.gameObject);
        Destroy(card.gameObject);

        // すべてのアイテムを使い切ったら自動で次へ
        if (_itemCards.Count == 0)
        {
            OnProceedFromItems();
        }
    }

    private void OnProceedFromItems()
    {
        if (optionalItemUseUIPanel != null) optionalItemUseUIPanel.SetActive(false);
        TransitionToSelectingMoveOrder();
    }

    private IEnumerator RedrawFlowRoutine()
    {
        // UIを隠す
        if (optionalItemUseUIPanel != null) optionalItemUseUIPanel.SetActive(false);
        
        // 手元のカードを片付けるアニメーション
        var handManager = Object.FindFirstObjectByType<HandManager>();
        if (handManager != null) handManager.ClearHand();
        
        // 少し待つ
        yield return new WaitForSeconds(0.6f);

        // 新しいカードを引く
        if (PackManager.Instance != null)
        {
            List<CardData> newCards = PackManager.Instance.GetRandomCards(5);
            
            // 現在のフローの変数をクリア
            _moveCards.Clear();
            _moneyCards.Clear();
            _itemCards.Clear();
            _selectedMoveCardObjects.Clear();
            _moveOrder.Clear();
            
            // 新しいフローとしてやり直す
            CurrentPhase = CardFlowPhase.ShowingCards;
            
            // 種別ごとに振り分け
            foreach (var c in newCards)
            {
                if (c is MoveCardData moveCard) _moveCards.Add(moveCard);
                else if (c is MoneyCardData moneyCard) _moneyCards.Add(moneyCard);
                else if (c is ItemCardData itemCard) _itemCards.Add(itemCard);
            }
            
            StartCoroutine(ShowingCardsRoutine(newCards));
        }
        else
        {
            // フォールバック
            EndFlow();
        }
    }

    private void TransitionToProcessingMoney()
    {
        CurrentPhase = CardFlowPhase.ProcessingMoney;
        StartCoroutine(ProcessingMoneyRoutine());
    }

    private IEnumerator ProcessingMoneyRoutine()
    {
        Debug.Log("[CardFlowManager] マネーカード自動処理フェーズ開始。");
        var handManager = Object.FindFirstObjectByType<HandManager>();
        List<GameObject> moneyCardObjs = new List<GameObject>();

        if (handManager != null)
        {
            foreach (var cardObj in handManager.GetDrawnCardObjects())
            {
                if (cardObj.CardData is MoneyCardData)
                {
                    moneyCardObjs.Add(cardObj.gameObject);
                }
            }
        }

        foreach (var moneyCard in _moneyCards)
        {
            // お金を追加
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AddMoney(moneyCard.Amount);
            }
            
            // 対象のGameObjectを消去
            GameObject targetObj = moneyCardObjs.Find(go => {
                var co = go.GetComponent<CardObject>();
                return co != null && co.CardData == moneyCard;
            });

            if (targetObj != null)
            {
                // 簡易演出：少し待って消す
                moneyCardObjs.Remove(targetObj);
                if (handManager != null) handManager.RemoveCardFromHand(targetObj);
                Destroy(targetObj);
                yield return new WaitForSeconds(0.3f);
            }
        }

        TransitionToSavingItems();
    }

    private void TransitionToSavingItems()
    {
        CurrentPhase = CardFlowPhase.SavingItems;
        Debug.Log("[CardFlowManager] アイテム保存フェーズ開始。");

        // 手持ちの表示用CardObjectを消去
        var handManager = FindObjectOfType<HandManager>();
        if (handManager != null)
        {
            List<GameObject> itemCardObjs = new List<GameObject>();
            foreach (var cardObj in handManager.GetDrawnCardObjects())
            {
                if (cardObj.CardData is ItemCardData)
                {
                    itemCardObjs.Add(cardObj.gameObject);
                }
            }
            foreach (var go in itemCardObjs)
            {
                handManager.RemoveCardFromHand(go);
                Destroy(go);
            }
        }

        // 残ったアイテムを PlayerHand へ渡す
        if (_itemCards.Count > 0 && PlayerHand.Instance != null)
        {
            // たとえば AddCards() に渡す際はキャストが必要なら行う
            List<CardData> cardsToSave = new List<CardData>();
            foreach (var c in _itemCards) cardsToSave.Add(c);
            
            // AddCards 内で上限判定が発生する想定
            PlayerHand.Instance.AddCards(cardsToSave);

            // AddCards の中で DiscardManager.BeginDiscardFlow が呼ばれる場合がある
            // その場合は DiscardingItems フェーズへ移行する
            if (DiscardManager.Instance != null && DiscardManager.Instance.IsDiscarding)
            {
                CurrentPhase = CardFlowPhase.DiscardingItems;
                return; // DiscardManager側の完了コールバック街
            }
        }

        // 破棄が呼ばれなかった場合は直接次へ
        TransitionToMovingDoll();
    }

    /// <summary>
    /// DiscardManagerの破棄が完了した時に呼ばれるコールバック
    /// </summary>
    private void OnDiscardCompleted()
    {
        if (CurrentPhase == CardFlowPhase.DiscardingItems)
        {
            Debug.Log("[CardFlowManager] 破棄完了を確認。次へ進みます。");
            TransitionToMovingDoll();
        }
    }

    private void TransitionToMovingDoll()
    {
        CurrentPhase = CardFlowPhase.MovingDoll;

        if (_moveOrder.Count == 0)
        {
            EndFlow();
            return;
        }

        Debug.Log("[CardFlowManager] 俯瞰移動フェーズ開始。");
        // 残っている移動カードのオブジェクトも画面から消す
        var handManager = FindObjectOfType<HandManager>();
        if (handManager != null)
        {
            handManager.ClearHand();
        }

        StartCoroutine(MovingDollRoutine());
    }

    private IEnumerator MovingDollRoutine()
    {
        // カメラを盤面ビューへ
        var handManager = FindObjectOfType<HandManager>();
        if (CameraFollow.Instance != null && handManager != null && handManager.boardViewTarget != null)
        {
            CameraFollow.Instance.MoveToView(handManager.boardViewTarget);
            yield return new WaitForSeconds(handManager.boardViewWaitTime);
        }

        // 人形の移動
        var playerMov = FindObjectOfType<PlayerMovement>();
        if (playerMov != null)
        {
            // MoveSequenceコルーチンが終了するのを待つ
            yield return playerMov.StartCoroutine(playerMov.MoveSequence(_moveOrder, delayBetweenMoves));
        }
        else
        {
            Debug.LogWarning("[CardFlowManager] PlayerMovement が見つかりませんでした。");
        }

        EndFlow();
    }

    private void EndFlow()
    {
        CurrentPhase = CardFlowPhase.Idle;
        _moveCards.Clear();
        _moneyCards.Clear();
        _itemCards.Clear();
        _moveOrder.Clear();
        _selectedMoveCardObjects.Clear();

        Debug.Log("[CardFlowManager] フローを終了します。");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Preparation);
        }
    }
}
