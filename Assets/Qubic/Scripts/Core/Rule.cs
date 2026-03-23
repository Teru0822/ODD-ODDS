using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.nl9zl4o4lz97")]
    [Serializable]
    public sealed partial class Rule
    {
        [TagSet(nameof(GetCellTags))]
        public string From = "Any";
        [TagSet(nameof(GetWallTags))]
        public string Wall = "Wall";
        [TagSet(nameof(GetCellTags))]
        public string To = "Any";
        public SteepCondition Steep = SteepCondition.Any;
        [ShowIf(nameof(IsColumn))]
        public bool BetweenSamePrefabs = false;

        [FieldButtons(@"<color=white>{0:<color=\#00FF00FF>High +0</color>;<color=\#FF55AAFF>Low -0</color>;Default}</color>", "#404040A0", Min = MinPriority, Max = MaxPriority)]
        public int Priority = 0;
        [Range(0, 10)]
        public int Chance = 5;

        [HideInInspector] public PrefabType PrefabType;
        [NonSerialized] public Prefab Prefab;
        [NonSerialized] public QubicBuilder Builder;
        [NonSerialized] public UInt64 EdgeMask;
        [NonSerialized] public UInt64 FromMask;
        [NonSerialized] public UInt64 ToMask;
        [NonSerialized] public List<IRuleChecker> Checkers;
        [NonSerialized] public float TempSeed;
        [NonSerialized] public Rnd TempRnd;
        //TODO: do not check each edge, make table of possible edges

        public const int MinPriority = -3;
        public const int MaxPriority = 8;

        bool IsColumn => PrefabType == PrefabType.Column;

        public Rule(Prefab prefab, string wall, string from, string to, int priority = 0, SteepCondition steep = SteepCondition.Any)
        {
            Prefab = prefab;
            Wall = wall;
            From = from;
            To = to;
            Priority = priority;
            Steep = steep;
            PrefabType = prefab.Type;
        }

        public void Prepare(QubicBuilder builder, Prefab prefab)
        {
            Builder = builder;
            Prefab = prefab;
            PrefabType = prefab.Type;
            (EdgeMask, _) = ParseTags(builder, Wall);
            (FromMask, _) = ParseTags(builder, From);
            (ToMask, _) = ParseTags(builder, To);

            if (PrefabType == PrefabType.Content && Prefab.ContentFeatures.SpawnInsideRoom)
                EdgeMask |= WallTags.Content;

            Checkers ??= new List<IRuleChecker>(4);
            Checkers.Clear();
        }

        public (UInt64 need, UInt64 avoid) ParseTags(QubicBuilder builder, string tags)
        {
            UInt64 need = 0;
            UInt64 avoid = 0;
            foreach (var tag in tags.SplitAndTrim())
            {
                if (tag.Length > 2 && tag[0] == 'n' && tag[1] == 'o' && char.IsUpper(tag[2])) // noXXX ?
                    avoid |= builder.TagsMapper.GetOrCreate(tag);
                else
                if (tag == "Any")// Any?
                    need |= ~0ul;
                else
                    need |= builder.TagsMapper.GetOrCreate(tag);
            }

            return (need, avoid);
        }

        public void OnSpawned(GameObject go, Vector3Int fromCell, Vector3Int toCell)
        {
            var edgeIndex = fromCell + toCell;
            var fwd = toCell - fromCell;

            switch (Prefab.Type)
            {
                case PrefabType.Wall:
                    if (Prefab.WidthInEdges > 1)
                        SetWide(fromCell, toCell);// wall
                    else
                        Set(fromCell, toCell);// wall
                    break;
                case PrefabType.Corner:
                {
                    var right = fwd.RotateY(1);
                    // left part
                    Set(fromCell, toCell);
                    // column
                    edgeIndex += right;
                    var toCell2 = fromCell + right;
                    var columnEdge = Builder.Map[edgeIndex];
                    columnEdge.SetSpawned(Prefab, fromCell, toCell + right);
                    // right part
                    edgeIndex -= fwd;
                    fwd = right;
                    Set(fromCell, toCell2);
                    break;
                }
                case PrefabType.Column:
                {
                    var right = fwd.RotateY(1);
                    // column
                    edgeIndex += right;
                    var columnEdge = Builder.Map[edgeIndex];
                    columnEdge.SetSpawned(Prefab, fromCell, toCell);
                    break;
                }
            }

            void Set(Vector3Int from, Vector3Int to)
            {
                var edge = Builder.Map[edgeIndex];
                edge.SetSpawned(Prefab, from, to);
                if (!edgeIndex.IsFloor())
                {
                    // request column
                    Builder.ColumnRequestQueue.Add(new ColumnRequest(edgeIndex, fwd));
                    Builder.ColumnRequestQueue.Add(new ColumnRequest(edgeIndex, -fwd));
                }
                if (Prefab.SetTagsMask != 0)
                    edge.Tags = Prefab.SetTagsMask;
            }

            void SetWide(Vector3Int fromCell, Vector3Int toCell)
            {
                var fwd = toCell - fromCell;
                var right = fwd.RotateY(1);
                for (int i = 0; i < Prefab.WidthInEdges; i++)
                {
                    var from = fromCell + right * i;
                    var to = from + fwd;
                    var e = Builder.Map[from + to];
                    e.SetSpawned(Prefab, from, to);
                    if (Prefab.SetTagsMask != 0)
                        e.Tags = Prefab.SetTagsMask;
                }

                if (!edgeIndex.IsFloor())
                {
                    // request column
                    Builder.ColumnRequestQueue.Add(new ColumnRequest(edgeIndex + right * ((Prefab.WidthInEdges - 1) * 2) , fwd));
                    Builder.ColumnRequestQueue.Add(new ColumnRequest(edgeIndex, -fwd));
                }
            }
        }

        public override string ToString()
        {
            return Prefab?.PrefabInfo?.Prefab?.name ?? Wall;
        }

        public string GetTitle()
        {
            return $"{From} -> {Wall} -> {To}";
        }

        private IEnumerable<string> GetWallTags() => UITagsProvider.GetEdgeTags();
        private IEnumerable<string> GetCellTags() => UITagsProvider.GetCellTags();
    }
}