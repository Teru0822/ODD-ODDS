using UnityEngine;

/// <summary>
/// 移動カードのデータ構造
/// 引いた瞬間（即時）に人形を移動させる
/// </summary>
[CreateAssetMenu(fileName = "NewMoveCard", menuName = "Card Data/Move Card")]
public class MoveCardData : CardData
{
    [Header("Move Settings")]
    [Tooltip("移動させる方向（下=0, 左=1, 上=2, 右=3）")]
    public DirectionType Direction;

    [Tooltip("移動するマス数")]
    [Min(1)]
    public int Steps = 1;

    private void OnEnable()
    {
        Type = CardType.Move;
    }
}
