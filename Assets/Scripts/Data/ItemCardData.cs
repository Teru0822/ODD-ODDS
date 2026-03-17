using UnityEngine;

/// <summary>
/// アイテムカードのデータ構造
/// 任意で使用し、特殊効果をもたらす
/// </summary>
[CreateAssetMenu(fileName = "NewItemCard", menuName = "Card Data/Item Card")]
public class ItemCardData : CardData
{
    [Header("Item Effect Settings")]
    [Tooltip("アイテムの効果の種類")]
    public ItemEffectType EffectType = ItemEffectType.None;

    [Tooltip("効果の量（例: 移動量追加なら加算される値）")]
    public int EffectValue;

    private void OnEnable()
    {
        Type = CardType.Item;
    }
}
