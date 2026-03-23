using System;
using UnityEngine;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.cm051u9982vx")]
    public partial class Stairs : Room
    {
        public override bool CanBeRotated => true;

        [Space]
        public StairsType Type = StairsType.Straight;

        protected override void SetFloorTags(Vector3Int cell)
        {
            var relativeLevelY = cell.y - StartCell.y;
            var edgeIndex = cell * 2 + Vector3Int.down;

            if (relativeLevelY == 0)
            {
                if (Features.FloorOnFirstLevel)
                    Map[edgeIndex].Tags |= FloorTags.Floor;
                return;
            }

            // remove floor on mid levels
            Map[edgeIndex].Tags &= ~(FloorTags.Floor | FloorTags.Ceiling);
        }

        protected override void SetWallTags(Vector3Int from, Vector3Int to)
        {
            base.SetWallTags(from, to);

            var dir = (to - from).DirToInt();
            var isFront = dir == Rotation;
            var isBack = (dir + 2) % 4 == Rotation;
            var relativeLevelY = from.y - StartCell.y;

            switch (Type)
            {
                case StairsType.Straight:
                    if (isFront && relativeLevelY < LevelCount - 1)
                        Map[from + to].Tags = WallTags.Steps;

                    if (isBack && relativeLevelY > 0 && relativeLevelY < LevelCount)
                        Map[from + to].Tags = WallTags.Passage;
                    break;

                case StairsType.UShaped:
                    if (isFront && relativeLevelY < LevelCount - 1)
                        Map[from + to].Tags = WallTags.Steps;

                    if (isFront && relativeLevelY > 0 && relativeLevelY == LevelCount - 1)
                        Map[from + to].Tags = WallTags.Passage;
                    break;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Initialized == 0)
            {
                Initialized = 1;
                Features.DoorsStrategy = DoorStrategy.Impassable;
            }
        }
    }

    [Serializable]
    public enum StairsType
    {
        Straight = 0, UShaped = 1
    }
}