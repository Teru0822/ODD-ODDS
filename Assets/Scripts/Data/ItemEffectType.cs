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
    
    // 必要に応じて任意に追加していく
}
