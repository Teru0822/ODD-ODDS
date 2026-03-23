using System;
using UnityEngine;

/// <summary>
/// プレイヤーのHP・MP、およびレベルを管理するマネージャークラス
/// </summary>
public class PlayerStatusManager : MonoBehaviour
{
    public static PlayerStatusManager Instance { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private int _currentLevel = 1;
    [Tooltip("初期のレベルアップ費用")]
    [SerializeField] private int _baseLevelUpCost = 500;

    [Header("HP Settings")]
    [SerializeField] private int _maxHP = 100;
    private int _currentHP;

    [Header("MP Settings")]
    [SerializeField] private int _maxMP = 100;
    private int _currentMP;

    /// <summary> Level変動イベント (現在レベル) </summary>
    public event Action<int> OnLevelChanged;

    /// <summary> HP変動イベント (現在HP, 最大HP) </summary>
    public event Action<int, int> OnHPChanged;
    
    /// <summary> MP変動イベント (現在MP, 最大MP) </summary>
    public event Action<int, int> OnMPChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // シーン遷移しても値を保持
    }

    private void Start()
    {
        _currentHP = _maxHP;
        _currentMP = _maxMP;

        // UIなどに初期状態を通知する
        NotifyLevelChanged();
        NotifyHPChanged();
        NotifyMPChanged();
        
        Debug.Log($"[PlayerStatusManager] Initialized Lv:{_currentLevel}, HP:{_currentHP}/{_maxHP}, MP:{_currentMP}/{_maxMP}");
    }

    #region Level Methods

    public int GetCurrentLevel() => _currentLevel;

    /// <summary>
    /// 次のレベルアップに必要なコストを計算する
    /// 初期500から始まり、レベルが上がるごとに2倍（500, 1000, 2000, 4000...）になる
    /// </summary>
    public int GetLevelUpCost()
    {
        return _baseLevelUpCost * (int)Mathf.Pow(2, _currentLevel - 1);
    }

    /// <summary>
    /// お金を消費してレベルアップを試みる処理
    /// </summary>
    public bool TryLevelUp()
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("[PlayerStatusManager] MoneyManager is missing! Cannot level up without money.");
            return false;
        }

        int cost = GetLevelUpCost();
        
        // MoneyManagerからお金を消費できるか判定し、可能なら引かれる
        if (MoneyManager.Instance.TryConsumeMoney(cost))
        {
            _currentLevel++;
            Debug.Log($"[PlayerStatusManager] Leveled up to {_currentLevel}! (Consumed {cost} money)");
            NotifyLevelChanged();
            return true;
        }
        else
        {
            Debug.Log($"[PlayerStatusManager] Not enough money to level up. Target cost: {cost}");
            return false;
        }
    }

    private void NotifyLevelChanged() => OnLevelChanged?.Invoke(_currentLevel);

    #endregion

    #region HP Methods

    public int GetCurrentHP() => _currentHP;
    public int GetMaxHP() => _maxHP;

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        _currentHP = Mathf.Max(0, _currentHP - amount);
        Debug.Log($"[PlayerStatusManager] Took {amount} damage. Current HP: {_currentHP}");
        NotifyHPChanged();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
        Debug.Log($"[PlayerStatusManager] Healed {amount} HP. Current HP: {_currentHP}");
        NotifyHPChanged();
    }

    private void NotifyHPChanged() => OnHPChanged?.Invoke(_currentHP, _maxHP);

    #endregion

    #region MP Methods

    public int GetCurrentMP() => _currentMP;
    public int GetMaxMP() => _maxMP;

    public bool TryConsumeMP(int amount)
    {
        if (amount <= 0) return false;
        if (_currentMP >= amount)
        {
            _currentMP -= amount;
            Debug.Log($"[PlayerStatusManager] Consumed {amount} MP. Current MP: {_currentMP}");
            NotifyMPChanged();
            return true;
        }
        
        Debug.LogWarning($"[PlayerStatusManager] Not enough MP to consume {amount}. Current MP: {_currentMP}");
        return false;
    }

    public void RestoreMP(int amount)
    {
        if (amount <= 0) return;
        _currentMP = Mathf.Min(_maxMP, _currentMP + amount);
        Debug.Log($"[PlayerStatusManager] Restored {amount} MP. Current MP: {_currentMP}");
        NotifyMPChanged();
    }

    private void NotifyMPChanged() => OnMPChanged?.Invoke(_currentMP, _maxMP);

    #endregion
}
