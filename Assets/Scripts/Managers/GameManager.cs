using System;
using UnityEngine;

/// <summary>
/// ゲーム全体の進行状態（GameState）を管理するマネージャークラス
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// 現在のゲームステート
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.None;

    /// <summary>
    /// ステートが変更された際に呼ばれるイベント。
    /// 引数には 新しいGameState が渡される。
    /// </summary>
    public event Action<GameState> OnStateChanged;

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
        // 初期ステートの設定
        ChangeState(GameState.Preparation);
    }

    /// <summary>
    /// ゲームのステートを変更する
    /// </summary>
    /// <param name="newState">移行先の新しいステート</param>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        Debug.Log($"[GameManager] State changed to: {CurrentState}");

        // イベントを発行して他のシステムに通知する
        OnStateChanged?.Invoke(CurrentState);
    }
}
