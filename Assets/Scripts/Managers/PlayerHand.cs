using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーが保持するカードデッキ（手持ちカード）を管理するシングルトンマネージャー
/// 最大10枚のカードを保持し、上限を超えた場合は古いカードから破棄します
/// </summary>
public class PlayerHand : MonoBehaviour
{
    public static PlayerHand Instance { get; private set; }

    [Tooltip("保持できるカードの最大枚数")]
    public int maxHandSize = 10;

    // 現在保持しているカードデータのリスト
    private List<CardData> _heldCards = new List<CardData>();

    private void Awake()
    {
        // シングルトン設定
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 現在保持しているカードリストを取得します
    /// </summary>
    public List<CardData> GetHeldCards()
    {
        return new List<CardData>(_heldCards);
    }

    /// <summary>
    /// カードリストをデッキに追加します。上限を超えた場合は古いものから破棄します
    /// </summary>
    public void AddCards(List<CardData> cards)
    {
        if (cards == null || cards.Count == 0) return;

        Debug.Log($"[PlayerHand] AddCards呼び出し: 現在={_heldCards.Count}枚, 追加={cards.Count}枚");
        foreach(var c in _heldCards) Debug.Log($"  - 保持中: {c?.CardName}");
        foreach(var c in cards) Debug.Log($"  - 追加分: {c?.CardName}");

        // 全カードを一旦追加したと仮定して上限チェック
        int totalProjectedCount = _heldCards.Count + cards.Count;
        if (totalProjectedCount > maxHandSize)
        {
            Debug.Log($"[PlayerHand] 合計 {totalProjectedCount} 枚となり上限 {maxHandSize} を超えるため、破棄選択モードへ移行します。");
            
            // 既存の保持カードと新規カードを合体させたリストを作成
            List<CardData> combinedList = new List<CardData>(_heldCards);
            combinedList.AddRange(cards);

            if (DiscardManager.Instance != null)
            {
                DiscardManager.Instance.BeginDiscardFlow(combinedList);
            }
            else
            {
                Debug.LogError("[PlayerHand] DiscardManagerが見つかりません。自動削除にフォールバックします。");
                AutoDiscardAndAdd(cards);
            }
            return;
        }

        // 10枚以下の場合は追加（同じ種類のカードも複数持てるように重複チェックはしない）
        foreach (var card in cards)
        {
            if (card == null) continue;
            _heldCards.Add(card);
            Debug.Log($"[PlayerHand] {card.CardName} をデッキに追加しました。現在カード数: {_heldCards.Count}");
        }
    }

    private void AutoDiscardAndAdd(List<CardData> cards)
    {
        foreach (var card in cards)
        {
            if (card == null) continue;
            while (_heldCards.Count >= maxHandSize)
            {
                _heldCards.RemoveAt(0);
            }
            _heldCards.Add(card);
        }
    }

    /// <summary>
    /// 指定したカードをデッキから取り除きます（使用・破棄時）
    /// </summary>
    public void RemoveCard(CardData card)
    {
        if (_heldCards.Remove(card))
        {
            Debug.Log($"[PlayerHand] {card?.CardName} をデッキから取り除きました。現在カード数: {_heldCards.Count}");
        }
    }

    /// <summary>
    /// デッキを完全にクリアします
    /// </summary>
    public void ClearAll()
    {
        _heldCards.Clear();
        Debug.Log("[PlayerHand] デッキをすべてクリアしました。");
    }

    /// <summary>
    /// 現在保持しているカード数を返します
    /// </summary>
    public int GetCardCount() => _heldCards.Count;
}
