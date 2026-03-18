using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// デッキからカードを引き、カメラ（手元）の前に等間隔で並べるアニメーションを管理します
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("Card Settings")]
    [Tooltip("描画するカードの3Dプレハブ")]
    public GameObject cardPrefab;

    [Header("Position Settings")]
    [Tooltip("カードが飛び出してくる発生源（デッキの位置等）")]
    public Transform deckOrigin;
    [Tooltip("カードが並ぶ際の基準点（カメラの手元・中心座標用オブジェクト）")]
    public Transform handCenterPoint;
    [Tooltip("移動カード使用後にカメラを向ける盤面ビュー（BoxView用の空オブジェクトをアサイン）")]
    public Transform boardViewTarget;
    [Tooltip("移動カード使用時、カメラが向き終わるまでの待機時間（秒）。カメラの smoothTime に合わせて調整してください")]
    public float boardViewWaitTime = 0.8f;

    [Header("Layout Settings")]
    [Tooltip("展開するカード間の隙間（横幅）")]
    public float cardSpacing = 0.5f;
    [Tooltip("2段表示の際、上下の行間の距離")]
    public float rowSpacing = 0.8f;
    [Tooltip("手元中心（カメラ）から前方へどれくらい離すかのZ距離")]
    public float zOffset = 1.0f;
    [Tooltip("手元中心から上下へどれくらいずらすかのY距離")]
    public float yOffset = -0.3f;
    
    [Tooltip("カードの基本的な回転角度（オイラー角）。もしカードが裏返っていたり横を向いている場合は数値を調整してください")]
    public Vector3 cardRotationOffset = new Vector3(0, 0, 0);

    [Header("Animation Settings")]
    [Tooltip("1枚のカードが移動完了するまでの秒数")]
    public float animationDuration = 0.3f;
    [Tooltip("次のカードが飛び出してくるまでの待機秒数（ドロー間隔）")]
    public float delayBetweenCards = 0.1f;

    // 現在生成されているカードのリスト
    private List<GameObject> _drawnCards = new List<GameObject>();
    private bool _isRedrawing = false; // 引き直し処理中の重複防止フラグ

    /// <summary>
    /// 指定されたカードデータのリストを受け取り、順番に手元へアニメーション配置します
    /// </summary>
    public void DrawCards(List<CardData> cardsToDraw, bool fromPack = false)
    {
        if (cardPrefab == null || deckOrigin == null || handCenterPoint == null)
        {
            Debug.LogError("[HandManager] CardPrefab, DeckOrigin または HandCenterPoint が設定されていません。");
            return;
        }

        if (cardsToDraw == null || cardsToDraw.Count == 0) return;

        // 【復活】新しいカードを引く前に、現在の表示をクリアする
        ClearHand();

        Debug.Log($"[HandManager] Drawing {cardsToDraw.Count} cards (FromPack: {fromPack})...");
        StartCoroutine(DrawCardsRoutine(cardsToDraw, fromPack));
    }

    private IEnumerator DrawCardsRoutine(List<CardData> cardsToDraw, bool fromPack)
    {
        int count = cardsToDraw.Count;
        for (int i = 0; i < count; i++)
        {
            if (cardsToDraw[i] == null) continue;

            // デッキの位置にカードを生成
            GameObject newCard = Instantiate(cardPrefab, deckOrigin.position, deckOrigin.rotation);
            _drawnCards.Add(newCard);

            // CardAnimatorコンポーネントがなければ追加
            CardAnimator animator = newCard.GetComponent<CardAnimator>();
            if (animator == null)
            {
                animator = newCard.AddComponent<CardAnimator>();
            }

            // === 追加：CardObjectとしてのデータ初期化 ===
            CardObject cardObj = newCard.GetComponent<CardObject>();
            if (cardObj == null)
            {
                cardObj = newCard.AddComponent<CardObject>();
            }
            cardObj.Initialize(cardsToDraw[i], this, boardViewTarget);
            cardObj.IsFromPack = fromPack; // 【新機能】パックからの新規分か、デッキの既存分かをセット

            // このカードの最終的な目標座標を計算
            Vector3 targetPosition = CalculateCardPosition(i, count);
            // 回転はHandCenterPointの向きを基準に、角度補正（Offset）を加える
            Quaternion targetRotation = handCenterPoint.rotation * Quaternion.Euler(cardRotationOffset);

            // アニメーション実行
            animator.AnimateTo(targetPosition, targetRotation, animationDuration);

            // 次のカードのドローまで少し待つ
            yield return new WaitForSeconds(delayBetweenCards);
        }
    }

    /// <summary>
    /// 手元にある特定のカードリストから除外します（使用された場合など）
    /// </summary>
    public void RemoveCardFromHand(GameObject cardObj)
    {
        if (_drawnCards.Contains(cardObj))
        {
            _drawnCards.Remove(cardObj);
        }
    }

    /// <summary>
    /// 現在展開中のカードオブジェクトのリストを取得します
    /// </summary>
    public List<CardObject> GetDrawnCardObjects()
    {
        List<CardObject> objects = new List<CardObject>();
        foreach (var cardGO in _drawnCards)
        {
            if (cardGO != null)
            {
                CardObject cardObj = cardGO.GetComponent<CardObject>();
                if (cardObj != null) objects.Add(cardObj);
            }
        }
        return objects;
    }

    /// <summary>
    /// 現在展開されている全カード（移動カード以外）を
    /// プレイヤーの保持デッキ（PlayerHand）へ保存（上書き同期）し、展開リストからクリアします。
    /// </summary>
    public void SaveRemainingCardsToPlayerHand()
    {
        if (PlayerHand.Instance == null) return;

        List<CardData> newCardsToSave = new List<CardData>();
        foreach (var cardGO in _drawnCards)
        {
            if (cardGO == null) continue;
            CardObject cardObj = cardGO.GetComponent<CardObject>();
            if (cardObj != null && cardObj.CardData != null)
            {
                // 移動カードは保持しない
                if (cardObj.CardData.Type == CardType.Move) continue;

                // 【修正】パックから出た新規カードのみを保存対象にする（二重保存防止）
                if (cardObj.IsFromPack)
                {
                    newCardsToSave.Add(cardObj.CardData);
                    cardObj.IsFromPack = false; // 保存済みにマーク
                }
            }
        }

        // 新規分があれば追加（ここでの上限チェックが破棄フローをキックする）
        if (newCardsToSave.Count > 0)
        {
            PlayerHand.Instance.AddCards(newCardsToSave);
        }

        // 可視的な演出（デッキに戻る）を行う。
        ClearHand();
    }

    /// <summary>
    /// 現在の手札をすべて破棄し、新たにパック(ID:0)から5枚引き直します。
    /// </summary>
    public void RedrawCards()
    {
        if (_isRedrawing) return;
        _isRedrawing = true;

        // 1. 現在のカードを消去
        ClearHand();

        if (PackManager.Instance == null)
        {
            _isRedrawing = false;
            return;
        }

        // 2. 新しいカードを取得（アイテム効果などによる引き直しなので所持パックを消費しない GetRandomCards を使用）
        List<CardData> newList = PackManager.Instance.GetRandomCards(5);
        if (newList == null || newList.Count == 0)
        {
            _isRedrawing = false;
            return;
        }

        // 3. 少し待ってから（消去演出の後）再展開
        StartCoroutine(RedrawRoutine(newList));
    }

    private IEnumerator RedrawRoutine(List<CardData> newList)
    {
        // 消去アニメーション(ReturnCardsRoutine)の終了を待つ
        yield return new WaitForSeconds(0.6f);
        DrawCards(newList);
        _isRedrawing = false;
    }

    /// <summary>
    /// i番目のカードが並ぶべき手元の座標を計算します
    /// </summary>
    private Vector3 CalculateCardPosition(int index, int totalCount)
    {
        // 表示枚数に応じた全体の高さ調整
        float yAdjustment = 0f;
        if (totalCount > 10) yAdjustment = -0.6f;      // 3段表示時 (机に埋もれないよう少し上げる)
        else if (totalCount > 5) yAdjustment = -0.4f;   // 2段表示時

        // 最大15枚、3段表示（1段5枚ずつ）のロジック
        // 下段（手前）：0〜4, 中段：5〜9, 上段（奥）：10〜14
        int rowIndex = index / 5; // 0=下段, 1=中段, 2=上段
        int colIndex = index % 5; // 各段の左から何枚目か

        // 各段ごとの総枚数（その段に何枚あるか）を判定
        int countInRow;
        if (rowIndex == 0) countInRow = Mathf.Min(totalCount, 5);
        else if (rowIndex == 1) countInRow = Mathf.Min(totalCount - 5, 5);
        else countInRow = totalCount - 10;
        
        // その段内での中心からのオフセット
        float offsetIndex = colIndex - (countInRow - 1) / 2f;
        
        // Z(前後)とY(上下)の奥行き・高さ調整
        // 段が上がるごとに少しずつ奥(Z+)、少しずつ上(Y+)に配置する
        float currentZOffset = zOffset + rowIndex * 0.25f; 
        float currentYOffset = yOffset + yAdjustment + (rowIndex * 0.45f); // 行間の調整

        Vector3 localOffset = new Vector3(offsetIndex * cardSpacing, currentYOffset, currentZOffset);
        
        return handCenterPoint.TransformPoint(localOffset);
    }

    /// <summary>
    /// 手札のカードをデッキへ戻すアニメーションを行い、破棄します（リセット用）
    /// </summary>
    public void ClearHand()
    {
        if (_drawnCards.Count == 0) return;

        // 【修正】リストを即座にコピーしてクリアすることで、直後の DrawCards との競合を防ぐ
        List<GameObject> cardsToReturn = new List<GameObject>(_drawnCards);
        _drawnCards.Clear();

        StartCoroutine(ReturnCardsRoutine(cardsToReturn));
    }

    private IEnumerator ReturnCardsRoutine(List<GameObject> cardsToReturn)
    {
        // リストの最後（一番右）のカードから順に戻していく
        for (int i = cardsToReturn.Count - 1; i >= 0; i--)
        {
            GameObject card = cardsToReturn[i];
            if (card != null)
            {
                CardAnimator animator = card.GetComponent<CardAnimator>();
                if (animator != null && deckOrigin != null)
                {
                    animator.AnimateTo(deckOrigin.position, deckOrigin.rotation, animationDuration * 0.5f);
                }
                yield return new WaitForSeconds(delayBetweenCards * 0.5f);
            }
        }

        yield return new WaitForSeconds(animationDuration * 0.5f);

        foreach (var card in cardsToReturn)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
    }
}
