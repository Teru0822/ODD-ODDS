using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ガチャパックの所持数を管理し、購入処理を行うマネージャークラス
/// </summary>
public class PackManager : MonoBehaviour
{
    public static PackManager Instance { get; private set; }

    [Header("Gacha Inventory")]
    [SerializeField] private int _packTypeCount = 6;//パックの種類数

    [Header("Card Database")]
    [Tooltip("ゲーム内に登場する全種類のカードデータを登録します")]
    [SerializeField] private List<CardData> _allAvailableCards = new List<CardData>();

    // パックIDをキー、所持数を値とするDictionary
    private Dictionary<int, int> _ownedPacks = new Dictionary<int, int>();

    private void Awake()
    {
        // シングルトンの初期化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初期化
        _ownedPacks.Clear();
        for(int i = 0; i < _packTypeCount; i++)
        {
            _ownedPacks.Add(i, 0);
        }
    }

    /// <summary>
    /// 指定されたパックを購入する
    /// </summary>
    public bool BuyPack(int packId, int price)
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[PackManager] MoneyManagerがシーンに存在しません。");
            return false;
        }

        // お金が足りるか確認し、足りれば消費
        if (MoneyManager.Instance.TryConsumeMoney(price))
        {
            if (!_ownedPacks.ContainsKey(packId))
            {
                Debug.LogWarning($"[PackManager] 指定されたパック(ID: {packId}) は存在しません。");
                return false;
            }
            
            _ownedPacks[packId]++;
            Debug.Log($"[PackManager] パック(ID: {packId}) を購入しました。現在の所持数: {_ownedPacks[packId]}");
            return true;
        }
        else
        {
            Debug.LogWarning($"[PackManager] お金が足りないため、パック(ID: {packId}) の購入に失敗しました。");
            return false;
        }
    }

    /// <summary>
    /// パックを1つ開封し、ランダムな5枚のカードデータ（必ず1枚は移動カード）を排出する
    /// </summary>
    public List<CardData> OpenPack(int packId)
    {
        if (!_ownedPacks.ContainsKey(packId) || _ownedPacks[packId] <= 0)
        {
            Debug.LogWarning($"[PackManager] パック(ID: {packId}) を所持していないため開封できません。");
            return null;
        }

        // パック所持数を減らす
        _ownedPacks[packId]--;

        // カードを抽選して返す
        List<CardData> drawnCards = GetRandomCards(5);
        Debug.Log($"[PackManager] パック(ID: {packId}) を開封しました。残り所持数: {_ownedPacks[packId]}");
        return drawnCards;
    }

    /// <summary>
    /// 指定された枚数のカードをデータベースからランダムに取得します（パック消費なし、アイテム効果用）
    /// </summary>
    /// <param name="count">取得する枚数</param>
    public List<CardData> GetRandomCards(int count)
    {
        if (_allAvailableCards == null || _allAvailableCards.Count == 0)
        {
            Debug.LogError("[PackManager] データベースに CardData が1枚も登録されていません。");
            return null;
        }

        List<CardData> drawnCards = new List<CardData>();

        // 必須の「移動カード」を1枚探して入れる
        List<CardData> moveCards = _allAvailableCards.FindAll(c => c.Type == CardType.Move);
        if (moveCards.Count > 0)
        {
            drawnCards.Add(moveCards[Random.Range(0, moveCards.Count)]);
        }
        else
        {
            Debug.LogWarning("[PackManager] データベースに移動カード(MoveCardData)が登録されていません！");
            drawnCards.Add(_allAvailableCards[Random.Range(0, _allAvailableCards.Count)]);
        }

        // 残りの（count-1）枚をランダムに抽選
        int remainingCount = count - 1;
        for (int i = 0; i < remainingCount; i++)
        {
            drawnCards.Add(_allAvailableCards[Random.Range(0, _allAvailableCards.Count)]);
        }

        // 配列の中身をシャッフル（移動カードが必ず1枚目にならないように）
        for (int i = 0; i < drawnCards.Count; i++)
        {
            CardData temp = drawnCards[i];
            int randomIndex = Random.Range(i, drawnCards.Count);
            drawnCards[i] = drawnCards[randomIndex];
            drawnCards[randomIndex] = temp;
        }

        return drawnCards;
    }

    /// <summary>
    /// 指定したパックの現在の所持数を取得する
    /// </summary>
    public int GetPackCount(int packId)
    {
        if (_ownedPacks.TryGetValue(packId, out int count))
        {
            return count;
        }
        return 0;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            Debug.Log("[PackManager] --- 現在のパック所持状況 ---");
            if (_ownedPacks.Count == 0)
            {
                Debug.Log("[PackManager] 所持しているパックはありません。");
            }
            else
            {
                foreach (var kvp in _ownedPacks)
                {
                    Debug.Log($"[PackManager] パック(ID: {kvp.Key}) : {kvp.Value} 個");
                }
            }
            Debug.Log("[PackManager] ----------------------------");
        }
    }
#endif
}
