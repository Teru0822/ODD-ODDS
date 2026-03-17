using UnityEngine;

/// <summary>
/// すべてのカード（お金・移動・アイテム）の共通データを持つ抽象クラス
/// </summary>
public abstract class CardData : ScriptableObject
{
    [Header("Basic Information")]
    [Tooltip("カードの名前")]
    public string CardName;

    [Tooltip("カードの種類")]
    public CardType Type;

    [Tooltip("カードの画像 (UI用)")]
    public Sprite Icon;

    [TextArea]
    [Tooltip("カードの説明文")]
    public string Description;
}
