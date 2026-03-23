using System.Collections;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.3t7gtqy7v342")]
    public class TagsPostProcessing : AutoSpawner
    {
        public override IEnumerator OnTagsPostprocess()
        {
            var insideMask = (ulong)CellTags.Inside;
            var outsideMask = (ulong)CellTags.Outside;
            var roofMask = (ulong)CellTags.Roof;
            var parapetMask = (ulong)WallTags.Parapet;
            var roofCells = new Vector3IntSet();

            // set Outside tag
            foreach (var room in Builder.Spawners.OfType<Room>())
            {
                var isInsideRoom = (room.SetTagsMask & outsideMask) == 0;
                var hasRoof = room.Features.Roof;
                foreach (var cell in room.MyCells)
                {
                    if (isInsideRoom)
                        Map[cell * 2].Tags |= insideMask;

                    var roofY = (cell.y + 1) * 2;

                    foreach (var n in cell.Neighbors6())
                    {
                        var e = Map[n * 2];
                        if (e.Tags == 0ul)
                            e.Tags = outsideMask;
                    }

                    // roof
                    if (hasRoof)
                    {
                        var e = Map[(cell + Vector3Int.up) * 2];
                        if ((e.Tags & ~outsideMask) == 0ul)
                        {
                            e.Tags |= roofMask;
                            roofCells.Add(e.Index / 2);
                        }
                    }
                }
            }

            // set Parapet tag
            foreach (var cell in roofCells)
            {
                foreach (var n in cell.Neighbors4())
                if (!roofCells.Contains(n))
                {
                    var edge = Map[cell + n];
                    if (edge.Tags == 0ul)
                        edge.Tags = parapetMask;
                    var dir = n - cell;
                    var c = Map[edge.Index + dir];
                    if (c.Tags == 0)
                        c.Tags = outsideMask;
                }
            }

            yield break;
        }
    }
}