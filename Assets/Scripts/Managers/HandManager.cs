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

    [Header("Layout Settings")]
    [Tooltip("展開するカード間の隙間（横幅）")]
    public float cardSpacing = 0.5f;
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

    /// <summary>
    /// 指定されたカードデータのリストを受け取り、順番に手元へアニメーション配置します
    /// </summary>
    public void DrawCards(List<CardData> cardsToDraw)
    {
        if (cardPrefab == null || deckOrigin == null || handCenterPoint == null)
        {
            Debug.LogError("[HandManager] CardPrefab, DeckOrigin または HandCenterPoint が設定されていません。");
            return;
        }

        if (cardsToDraw == null || cardsToDraw.Count == 0) return;

        // 前回のカードがあればクリア
        ClearHand();

        StartCoroutine(DrawCardsRoutine(cardsToDraw));
    }

    private IEnumerator DrawCardsRoutine(List<CardData> cardsToDraw)
    {
        int count = cardsToDraw.Count;
        for (int i = 0; i < count; i++)
        {
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
            cardObj.Initialize(cardsToDraw[i], this);

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
    /// i番目のカードが並ぶべき手元の座標を計算します
    /// </summary>
    private Vector3 CalculateCardPosition(int index, int totalCount)
    {
        // 中心を0として、総枚数に応じた現在のカードの相対的なXオフセット幅を計算
        float offsetIndex = index - (totalCount - 1) / 2f;
        
        // 中心座標を基準として、X軸(左右), Y軸(上下), Z軸(前後) 方向へずらす
        // ※カメラ(handCenterPoint)のローカル座標でZが前方になります
        Vector3 localOffset = new Vector3(offsetIndex * cardSpacing, yOffset, zOffset);
        
        // handCenterPointのローカル座標系に基づいてワールド座標に変換して返す
        return handCenterPoint.TransformPoint(localOffset);
    }

    /// <summary>
    /// 手札のカードをデッキへ戻すアニメーションを行い、破棄します（リセット用）
    /// </summary>
    public void ClearHand()
    {
        if (_drawnCards.Count == 0) return;

        // すでにドロー中・戻し中なら一旦停止
        StopAllCoroutines();
        StartCoroutine(ReturnCardsRoutine());
    }

    private IEnumerator ReturnCardsRoutine()
    {
        // リストの最後（一番右）のカードから順に戻していく
        for (int i = _drawnCards.Count - 1; i >= 0; i--)
        {
            GameObject card = _drawnCards[i];
            if (card != null)
            {
                CardAnimator animator = card.GetComponent<CardAnimator>();
                if (animator != null && deckOrigin != null)
                {
                    // デッキから引き抜く時より少し早めの時間（例：半分の時間）で戻す
                    animator.AnimateTo(deckOrigin.position, deckOrigin.rotation, animationDuration * 0.5f);
                }

                // 戻るアニメーションが終わるまで少しだけ待つ
                yield return new WaitForSeconds(delayBetweenCards * 0.5f);
            }
        }

        // アニメーション完了を待ってからまとめて削除
        yield return new WaitForSeconds(animationDuration * 0.5f);

        foreach (var card in _drawnCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        _drawnCards.Clear();
    }
}
