using System.Collections.Generic;
using UnityEngine;

namespace QubicNS
{
    public interface IRuleChecker
    {
        void Prepare(QubicBuilder builder, IEnumerable<Rule> rules);
        bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell);
        void OnSpawned(Rule rule, GameObject obj, Vector3Int fromCell, Vector3Int toCell) { }
        void OnSpawnCompleted() { }
    }

    public sealed class SteepChecker : IRuleChecker
    {
        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
                if (rule.Steep != SteepCondition.Any)
                    rule.Checkers.Add(this);
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var map = rule.Builder.Map;
            var fromFloor = map[fromCell * 2 + Vector3Int.down].Tags != 0;
            var toFloor = map[toCell * 2 + Vector3Int.down].Tags != 0;
            switch (rule.Steep)
            {
                case SteepCondition.Steep: return fromFloor != toFloor;
                case SteepCondition.NoSteep: return fromFloor == toFloor;
            }

            return true;
        }
    }

    public enum SteepCondition : byte
    {
        Any = 0,
        Steep = 1,
        NoSteep = 2
    }

    public sealed class CornerTypeChecker : IRuleChecker
    {
        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
                if (rule.Prefab.Type == PrefabType.Corner)
                    rule.Checkers.Add(this);
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var map = rule.Builder.Map;
            var edgeIndex = toCell + fromCell;
            if (!map[edgeIndex].Flags.HasFlag(QubicEdgeFlags.IsPartOfCorner)) return false;// is not corner

            var fwd = toCell - fromCell;
            var right = fwd.RotateY(1);
            var rightEdgeIndex = fromCell * 2 + right;
            var rightEdge = map[rightEdgeIndex];
            //if ((rightEdge.Tags & rule.EdgeMask) == 0ul) return false;// is not my wall type
            if (rightEdge.Tags != map[edgeIndex].Tags) return false;// both walls should be with same tags
            if (rightEdge.IsSpawned) return false; // right part is spawned already
            var columnEdgeIndex = rightEdgeIndex + fwd;
            if (map[columnEdgeIndex].IsSpawned) return false;// here is spawned column already
            if (map[columnEdgeIndex + fwd].Tags != 0) return false;// is not L corner
            if (map[columnEdgeIndex + right].Tags != 0) return false;// is not L corner

            return true;
        }
    }

    public sealed class MultiEdgeWall : IRuleChecker
    {
        Map Map;

        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            this.Map = builder.Map;

            foreach (var rule in rules)
                if (rule.Prefab != null && rule.Prefab.Type == PrefabType.Wall && rule.Prefab.WidthInEdges > 1)
                    rule.Checkers.Add(this);
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var fwd = toCell - fromCell;
            var right = fwd.RotateY(1);
            var expectedFromTags = Map[fromCell * 2].Tags;
            var expectedToTags = Map[toCell * 2].Tags;
            var expectedEdgeTags = Map[fromCell + toCell].Tags;
            for (int i = 1; i < rule.Prefab.WidthInEdges; i++)
            {
                var from = fromCell + right * i;
                var to = from + fwd;
                if (Map[from + to].IsSpawned) return false;
                if (Map[from + to].Tags != expectedEdgeTags) return false;
                if (Map[from * 2].Tags != expectedFromTags) return false;
                if (Map[to * 2].Tags != expectedToTags) return false;
            }

            return true;
        }
    }

    public sealed class ColumnChecker : IRuleChecker
    {
        ulong contentInversedMask;

        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
                if (rule.Prefab.Type == PrefabType.Column)
                    rule.Checkers.Add(this);

            contentInversedMask = ~WallTags.Content;
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var map = rule.Builder.Map;
            var features = rule.Prefab.ColumnFeatures;
            var edgeIndex = toCell + fromCell;

            var fwd = toCell - fromCell;
            var right = fwd.RotateY(1);
            var edge = map[edgeIndex];
            var rightEdge = map[edgeIndex + right * 2];

            var fwdEdge = map[edgeIndex + right + fwd];
            var backEdge = map[edgeIndex + right - fwd];

            // check corner type
            if (features.CornerType != ColumnCornerType.Any)
            {
                // calc corner code
                var cornerCode = 0;
                if (backEdge.IsSpawned) cornerCode |= 0x001;
                if (rightEdge.IsSpawned) cornerCode |= 0b010;
                if (fwdEdge.IsSpawned) cornerCode |= 0b100;

                switch (cornerCode)
                {
                    // end
                    case 0b000: if (!features.CornerType.HasFlag(ColumnCornerType.End)) return false; break;
                    // straight
                    case 0b010:
                        if (!features.CornerType.HasFlag(ColumnCornerType.Straight)) return false; break;
                    case 0b110:// straight + T
                        if (!features.CornerType.HasFlag(ColumnCornerType.AllowT)) return false;
                        goto case 0b010;
                    // LCornerConcave
                    case 0b001:
                        if (!features.CornerType.HasFlag(ColumnCornerType.LCornerConcave)) return false;
                        break;
                    case 0b011:// LCornerConcave + T
                        if (!features.CornerType.HasFlag(ColumnCornerType.AllowT)) return false;
                        goto case 0b001;
                    case 0b111:// LCornerConcave + X
                        if (!features.CornerType.HasFlag(ColumnCornerType.AllowX)) return false;
                        goto case 0b001;
                    // LCornerConvex
                    case 0b100:
                        if (!features.CornerType.HasFlag(ColumnCornerType.LCornerConvex)) return false;
                        break;
                    case 0b101:// LCornerConvex + T
                        if (!features.CornerType.HasFlag(ColumnCornerType.AllowT)) return false;
                        goto case 0b100;
                }
            }

            if (edge.SpawnedPrefab?.WallFeatures.RequiresColumn == true)
                return true;// column is required by prefab

            if (rightEdge.SpawnedPrefab?.WallFeatures.RequiresColumn == true)
                return true;// column is required by next prefab right

            if (rule.BetweenSamePrefabs || IsDifferentSpawned(edge, rightEdge))
                return true;// spawning between different prefabs is neccessary

            if (fwdEdge.IsSpawned || backEdge.IsSpawned)
                return true;// L, T or X shaped corner => must be column

            return false;
        }

        private bool IsDifferentSpawned(QubicEdge e0, QubicEdge e1)
        {
            return (e0.SpawnedPrefab != e1.SpawnedPrefab)
                || (e0.SpawnedPrefab0 != e1.SpawnedPrefab0)
                || (e0.SpawnedPrefab1 != e1.SpawnedPrefab1);
        }
    }

    public sealed class ContentNeedColumn : IRuleChecker
    {
        QubicBuilder builder;

        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            this.builder = builder;
            foreach (var rule in rules)
                if (rule.Prefab != null && rule.Prefab.Type == PrefabType.Content && rule.Prefab.ContentFeatures.NeedColumn)
                {
                    if (rule.Prefab.IsLeftColumn || rule.Prefab.IsRightColumn)
                        rule.Checkers.Add(this);
                }
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var dir = rule.Prefab.IsLeftColumn ? fromCell - toCell : toCell - fromCell;
            var right = dir.RotateY(1);
            var index = PrefabSpawner.CurrentEdge.Index;
            var isColumn = builder.Map[index + right].IsSpawned;
            return isColumn;
        }
    }

    public sealed class ContentDecimation : IRuleChecker
    {
        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
                if (rule.Prefab != null && rule.Prefab.Type == PrefabType.Content && rule.Prefab.ContentFeatures.Decimation > Decimation.OnePerCell)
                    rule.Checkers.Add(this);
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var decimation = rule.Prefab.ContentFeatures.Decimation;
            switch (decimation)
            {
                case Decimation.OnePerCell:
                case Decimation.None: return true;
                default:
                    var hash = QubicHelper.GetDecimationSpatialHash(fromCell, Mathf.Abs(rule.Prefab.Seed), (int)decimation);
                    return hash == 0;
            }
        }
    }

    public sealed class ContentSetAndAvoidTags : IRuleChecker
    {
        List<(Rule rule, GameObject obj, Vector3Int fromCell, Vector3Int toCell)> spawned = new ();
        Map map;

        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            spawned.Clear();
            map = builder.Map;
            foreach (var rule in rules)
                if (rule.Prefab.Type == PrefabType.Content && (rule.Prefab.SetTagsMask != 0ul || rule.Prefab.AvoidTagsMask != 0ul))
                    rule.Checkers.Add(this);
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var mask = rule.Prefab.AvoidTagsMask;
            if ((map[fromCell + toCell].Tags & mask) != 0ul || // check edge
                (map[fromCell + fromCell].Tags & mask) != 0ul) // check from cell
                return false;

            return true;
        }

        public void OnSpawned(Rule rule, GameObject obj, Vector3Int fromCell, Vector3Int toCell)
        {
            var setMask = rule.Prefab.SetTagsMask;

            if (rule.Prefab.SetTagsRadius <= 0 || setMask == 0ul)
            {
                spawned.Add((rule, obj, fromCell, toCell));
                return;
            }

            foreach (var n in fromCell.Neighbors(rule.Prefab.SetTagsRadius - 1))
                if (map.TryGetValue(n * 2, out var cell))
                    cell.Tags |= setMask;
        }

        public void OnSpawnCompleted()
        {
            // postprocess
            // because some content could be spawned before forbidden tags created by other content
            foreach (var (rule, obj, fromCell, toCell) in spawned)
            {
                if (Check(rule, fromCell, toCell))
                    continue;
                Helper.DestroySafe(obj);
            }

            spawned.Clear();
        }
    }
}