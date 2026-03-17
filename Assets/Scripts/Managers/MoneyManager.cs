using System;
using UnityEngine;

/// <summary>
/// プレイヤーの所持金を管理するマネージャークラス
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [SerializeField]
    [Tooltip("ゲーム開始時の初期所持金")]
    private int _initialMoney = 1000;

    /// <summary>
    /// 現在の所持金
    /// </summary>
    private int _currentMoney;

    /// <summary>
    /// 所持金が変動した際に呼ばれるイベント
    /// 引数には (変動後の金額, 変動した差額) が渡される
    /// </summary>
    public event Action<int, int> OnMoneyChanged;

    private void Awake()
    {
        // シングルトンの設定（簡易版）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 初期化
        _currentMoney = _initialMoney;
        Debug.Log($"[MoneyManager] Initialized with {_currentMoney} money.");
    }

    /// <summary>
    /// 現在の所持金を取得する
    /// </summary>
    public int GetCurrentMoney()
    {
        return _currentMoney;
    }

    /// <summary>
    /// 所持金を増やす（または強制的に減らす）
    /// </summary>
    /// <param name="amount">変動させる金額</param>
    public void AddMoney(int amount)
    {
        if (amount == 0) return;

        _currentMoney += amount;

        Debug.Log($"[MoneyManager] Money changed by {amount}. Current Money: {_currentMoney}");
        OnMoneyChanged?.Invoke(_currentMoney, amount);
    }

    /// <summary>
    /// 指定した金額を消費できるか判定し、可能であれば消費する
    /// パック購入や強化などに使用。
    /// </summary>
    /// <param name="amount">消費したい金額（正の値）</param>
    /// <returns>消費に成功した場合は true、所持金が足りない場合は false</returns>
    public bool TryConsumeMoney(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[MoneyManager] TryConsumeMoney should be called with a positive value.");
            return false;
        }

        if (_currentMoney >= amount)
        {
            _currentMoney -= amount;
            Debug.Log($"[MoneyManager] Consumed {amount} money. Current Money: {_currentMoney}");
            
            // 消費ということで マイナス値 として通知する
            OnMoneyChanged?.Invoke(_currentMoney, -amount);
            return true;
        }
        else
        {
            Debug.Log($"[MoneyManager] Not enough money. Required: {amount}, Current: {_currentMoney}");
            return false;
        }
    }
}
