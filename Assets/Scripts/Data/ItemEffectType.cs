/// <summary>
/// アイテムカードがもたらす特殊効果の種類を定義する列挙型
/// </summary>
public enum ItemEffectType
{
    None,

    /// <summary>
    /// 次の移動量を加算するなどの効果の例
    /// </summary>
    AddMoveStep,

    /// <summary>
    /// 手札の引き直しを行う効果の例
    /// </summary>
    Redraw,
    
    /// <summary>
    /// HPをEffectValue分だけ固定回復する
    /// </summary>
    HealHP_Flat,

    /// <summary>
    /// HPを最大HPのEffectValue%回復する
    /// </summary>
    HealHP_Percent,

    /// <summary>
    /// MPをEffectValue分だけ固定回復する
    /// </summary>
    RestoreMP_Flat,

    /// <summary>
    /// MPを最大MPのEffectValue%回復する
    /// </summary>
    RestoreMP_Percent,

    /// <summary>
    /// 次のターン終了まで受けるダメージを無効化する
    /// </summary>
    InvincibleOneTurn,

    // 必要に応じて任意に追加していく
}
