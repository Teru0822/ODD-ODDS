using System.Collections.Generic;
using UnityEngine;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.85jjp0ymguy6")]
    public class Atrium : Room
    {
        protected override void SetFloorTags(Vector3Int cell)
        {
            var relativeLevelY = cell.y - StartCell.y;
            var edgeIndex = cell * 2 + Vector3Int.down;

            if (relativeLevelY == 0)
                return;

            // remove floor tag
            Map[edgeIndex].Tags &= ~(FloorTags.Floor | FloorTags.Ceiling);
        }

        protected override IEnumerable<Vector3Int> CellsToCapture(IntersectionMode IntersectionMode)
        {
            foreach (var cell in base.CellsToCapture(IntersectionMode))
                if (cell.y > StartCell.y || Features.FloorOnFirstLevel)
                    yield return cell;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Initialized == 0)
            {
                Initialized = 1;
                Features.DoorsStrategy = DoorStrategy.NoDoors;
            }
        }
    }
}