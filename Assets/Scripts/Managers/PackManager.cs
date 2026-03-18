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

    // パックIDをキー、所持数を値とするDictionary
    private Dictionary<int, int> _ownedPacks = new Dictionary<int, int>();

    [SerializeField] private int _packTypeCount = 6;//パックの種類数

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
    /// <param name="packId">購入するパックのID</param>
    /// <param name="price">パックの売価</param>
    /// <returns>購入に成功したかどうか</returns>
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
    /// 指定したパックの現在の所持数を取得する
    /// </summary>
    /// <param name="packId">取得したいパックのID</param>
    /// <returns>所持数</returns>
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
