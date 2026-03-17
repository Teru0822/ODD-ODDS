using UnityEngine;

/// <summary>
/// ゲームの進行状態を定義する列挙型
/// </summary>
public enum GameState
{
    None,

    /// <summary>
    /// 各ターン開始時の準備フェーズ（机の上の確認、アイテムの仕様、パックの購入などを行う状態）
    /// </summary>
    Preparation,

    /// <summary>
    /// パックを開封し、カードの効果が解決される状態
    /// </summary>
    PackOpening,

    /// <summary>
    /// 人形の移動結果や所持金に基づく勝敗判定などを行う状態
    /// </summary>
    ResultCheck,

    /// <summary>
    /// ゴール到達（クリア）時の状態
    /// </summary>
    GameClear,

    /// <summary>
    /// お金が尽き、パックが買えなくなったとき等の敗北状態
    /// </summary>
    GameOver
}
