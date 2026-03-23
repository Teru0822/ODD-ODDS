using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.8ia0p9d96o8g")]
    public class IsometricSpawner : AutoSpawner
    {
        [FormerlySerializedAs("Mode")]
        public IsometricMode Mode = IsometricMode.None;
        [ShowIf(nameof(Mode), IsometricMode.None, Op = DrawIfOp.AllFalse)]
        [TagSet(nameof(GetWallTags))]
        public string RemoveWallTags = "Wall,Window";

        public override int Order => 110;

        public Vector3IntSet EdgesToHide { get; private set; }
        public ulong RemoveWallMask { get; private set; }
        private IEnumerable<string> GetWallTags() => UITagsProvider.GetEdgeTags();

        public override IEnumerator OnTagsPostprocess()
        {
            var outsideMask = (ulong)CellTags.Outside;
            RemoveWallMask = TagsMapper.GetOrCreate(RemoveWallTags.SplitAndTrim());
            EdgesToHide = new Vector3IntSet();

            var mode = Mode;
            if (mode == IsometricMode.None)
            {
                if (Builder.Debug.ForcedIsometricView)
                    mode = IsometricMode.HideOutsideWalls;
                else
                    yield break;
            }

            foreach (var room in Builder.Spawners.OfType<Room>())
            {
                foreach (var edgeIndex in room.MyWalls)
                {
                    var edge = Map[edgeIndex];
                    if (edge.Tags == 0)
                        continue;

                    var cells = QubicHelper.EdgeToCells(edgeIndex);
                    if (cells.from.x > cells.to.x || cells.from.z > cells.to.z)
                        cells = (cells.to, cells.from);

                    switch (mode)
                    {
                        case IsometricMode.HideOutsideWalls:
                            if ((Map[cells.from * 2].Tags & outsideMask) != 0)
                                EdgesToHide.Add(edge.Index);
                            break;

                        case IsometricMode.HideOutsideAndInsideWalls:
                            if ((Map[cells.to * 2].Tags & ~outsideMask) != 0)
                                EdgesToHide.Add(edge.Index);
                            break;
                    }
                }
            }

            yield break;
        }

        public override IEnumerator OnSpawnWalls()
        {
            var transparentMask = (ulong)WallTags.Transparent;

            foreach (var edgeIndex in EdgesToHide)
            {
                var edge = Map[edgeIndex];
                if (edge.IsSpawned)
                    continue;// because the wall was spawned, we do not need to set Transparent flag there
                edge.Tags |= transparentMask; 
                edge.Tags &= ~RemoveWallMask;
            }

            yield break;
        }

        public bool IsTransparentEdge(Vector3Int edgeIndex, ulong edgeTags)
        {
            if (EdgesToHide.Contains(edgeIndex))
            if ((edgeTags & RemoveWallMask) != 0)
                return true;

            return false;
        }
    }

    class IsoRule : IRuleChecker
    {
        IsometricSpawner isoSpawner;
        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            isoSpawner = builder.Spawners.OfType<IsometricSpawner>().FirstOrDefault();
            if (isoSpawner == null)
                return;

            if (isoSpawner.Mode == IsometricMode.None && !builder.Debug.ForcedIsometricView)
                return;

            ulong transparentMask = WallTags.Transparent;
            ulong replaceWallMask = builder.TagsMapper.GetMask(isoSpawner.RemoveWallTags.SplitAndTrim());

            foreach (var rule in rules)
            {
                switch (rule.Prefab.Type)
                {
                    case PrefabType.Wall:
                        if (isoSpawner.Mode == IsometricMode.HideContent)
                            continue;
                        break;
                    case PrefabType.Content:
                        if (isoSpawner.Mode != IsometricMode.HideContent) 
                            continue;
                        break;
                    default:
                        continue;
                }
                rule.Checkers.Add(this);
            }
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var edge = isoSpawner.Builder.Map[fromCell + toCell];
            var edgeToHide = isoSpawner.EdgesToHide.Contains(edge.Index);

            if (rule.PrefabType == PrefabType.Content && isoSpawner.Mode == IsometricMode.HideContent)
                edgeToHide = fromCell.x > toCell.x || fromCell.z > toCell.z;

            if (!edgeToHide)
                return true;// is not iso wall

            // get result tag that will be in edge
            var resultWallTags = rule.Prefab.SetTagsMask == 0 || rule.PrefabType != PrefabType.Wall ? edge.Tags : rule.Prefab.SetTagsMask;

            // check if this tag should be removed
            if ((resultWallTags & isoSpawner.RemoveWallMask) != 0)
                return false;// should be removed

            return true;
        }
    }

    [Serializable]
    public enum IsometricMode
    {
        None,
        HideOutsideWalls = 2,
        HideOutsideAndInsideWalls = 4,
        HideContent = 8,
    }
}