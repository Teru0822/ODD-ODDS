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
        if (cards == null) return;

        foreach (var card in cards)
        {
            if (card == null) continue;

            // 上限チェック：超えた場合は先頭（最も古い）カードを捨てる
            while (_heldCards.Count >= maxHandSize)
            {
                Debug.Log($"[PlayerHand] 上限({maxHandSize}枚)を超えたため {_heldCards[0].CardName} を破棄します。");
                _heldCards.RemoveAt(0);
            }

            _heldCards.Add(card);
            Debug.Log($"[PlayerHand] {card.CardName} をデッキに追加しました。現在カード数: {_heldCards.Count}");
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
