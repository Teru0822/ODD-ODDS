using UnityEngine;

/// <summary>
/// お金カードのデータ構造
/// 引いた瞬間（即時）に所持金を増やす
/// </summary>
[CreateAssetMenu(fileName = "NewMoneyCard", menuName = "Card Data/Money Card")]
public class MoneyCardData : CardData
{
    [Header("Money Settings")]
    [Tooltip("増加する金額")]
    public int Amount = 100;

    private void OnEnable()
    {
        Type = CardType.Money;
    }
}
