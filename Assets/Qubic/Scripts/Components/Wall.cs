using System.Collections;
using UnityEngine;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.ghespjjbfvev")]
    public class Wall : BaseRoom
    {
        public override bool CanBeRotated => true;

        public override IEnumerator OnCaptureCells()
        {
            yield return null;

            foreach( var cell in CellsToCapture(IntersectionMode.Overlap))
            {
                Map[cell * 2 + QubicHelper.Dirs4[Rotation]].Tags = SetTagsMask;
            }
        }
    }
}