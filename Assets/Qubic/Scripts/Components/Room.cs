using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.um3w9ilawc9v")]
    public partial class Room : BaseRoom
    {
        public IntersectionMode IntersectionMode = IntersectionMode.Complement;

        public RoomFeatures Features = new RoomFeatures();

        [ShowIf(nameof(IsCustomDoors))]
        public DoorList Doors;
        bool IsCustomDoors => Features.DoorsStrategy == DoorStrategy.Custom;

        public InsideContentFeatures InsideContent = new InsideContentFeatures();
        public Vector3IntSet MyWalls { get; } = new Vector3IntSet();
        public Vector3IntSet MyInsideEdges { get; } = new Vector3IntSet();

        public override int Order => base.Order + (IntersectionMode == IntersectionMode.Aggressive ? -1 : IntersectionMode == IntersectionMode.Weak ? 1 : 0);

        public override IEnumerator OnPrepare(QubicBuilder builder)
        {
            yield return base.OnPrepare(builder);

            if (Features.DoorsStrategy == DoorStrategy.Impassable)
                SetTagsMask |= CellTags.Impassible;
        }

        public override IEnumerator OnCaptureCells()
        {
            MyCells.Clear();
            MyCells.AddRange(CellsToCapture(IntersectionMode));

            if (SetTagsMask != 0ul)
            foreach (var cell in MyCells)
            {
                switch (Features.SetTagsStrategy)
                {
                    case SetTagsStrategy.Replace: Map[cell * 2].Tags = SetTagsMask; break;
                    case SetTagsStrategy.Add: Map[cell * 2].Tags |= SetTagsMask; break;
                }

                Map[cell * 2].Seed ^= TagsSeed;
            }

            yield return null;

            // assign wall tags
            MyWalls.Clear();
            MyInsideEdges.Clear();

            foreach (var cell in MyCells) 
            {
                foreach (var n in cell.Neighbors4())
                {
                    if (MyCells.Contains(n))
                        MyInsideEdges.Add(cell + n);
                    else
                        SetWallTags(cell, n);
                }

                // set floor tags
                SetFloorTags(cell);
                SetCeilingTags(cell);

                // capture cell
                if (Features.DoorsStrategy != DoorStrategy.NoDoors)
                    Map[cell * 2].Room = this;

                if (Features.WallContentCount == ContentCount.Zero)
                    Map[cell * 2].Flags |= QubicEdgeFlags.NoContent;
            }
        }

        protected virtual void GenerateInsideContent()
        {
            // prepare border cells
            var borderCells = MyCells.Where(c => c.Neighbors8().Any(n => !MyCells.Contains(n))).ToHashSet();
            var narrowCells = MyCells.Where(c => c.Neighbors4().Count(n => !MyCells.Contains(n)) > 1 && Features.Walls).ToHashSet();
            var contentWallTag = WallTags.Content;
            var spawned = new Vector3IntSet();
            var layout = InsideContent.Layout;

            if (layout == ContentSpawnerLayout.OneInCenterAlongX || layout == ContentSpawnerLayout.OneInCenterAlongZ)
            {
                // for each level
                foreach (var y in MyInsideEdges.Select(e => e.y).Distinct())
                {
                    // get central edge
                    var edges = MyInsideEdges.Where(e => e.y == y).ToHashSet();
                    var eIndex = QubicHelper.GetClosestToCenter(edges);
                    var e = Map[eIndex];
                    var alongX = e.Index.z.IsOdd();
                    if (!alongX && layout == ContentSpawnerLayout.OneInCenterAlongZ || alongX && layout == ContentSpawnerLayout.OneInCenterAlongX)
                    {
                        if (e.Tags == 0)
                        {
                            e.Tags |= contentWallTag;
                            spawned.Add(eIndex);
                        }
                    }
                    else
                    foreach (var n in e.Index.Neighbors4Diag())
                    {
                        if (edges.Contains(n))
                        {
                            var ee = Map[n];
                            if (ee.Tags == 0)
                            {
                                ee.Tags |= contentWallTag;
                                spawned.Add(n);
                                break;
                            }
                        }
                    }
                }

                goto exit;
            }

            var full = layout == ContentSpawnerLayout.Full || layout == ContentSpawnerLayout.AlongZFull || layout == ContentSpawnerLayout.AlongXFull;

            foreach (var e in MyInsideEdges)
            {
                var cells = QubicHelper.EdgeToCells(e);
                if (narrowCells.Contains(cells.from) || narrowCells.Contains(cells.to))
                    continue;
                if (InsideContent.DoNotAffectWalls)
                if (borderCells.Contains(cells.from) && borderCells.Contains(cells.to))
                    continue;


                if (e.z.IsOdd())
                {
                    if (layout == ContentSpawnerLayout.AlongZ || layout == ContentSpawnerLayout.AlongZFull)
                        continue;
                    if (!full && spawned.Contains(e - Vector3Int.right * 2) || spawned.Contains(e + Vector3Int.right * 2))
                        continue;
                }
                if (e.x.IsOdd())
                {
                    if (layout == ContentSpawnerLayout.AlongX || layout == ContentSpawnerLayout.AlongXFull)
                        continue;
                    if (!full && spawned.Contains(e - Vector3Int.forward * 2) || spawned.Contains(e + Vector3Int.forward * 2))
                        continue;
                }

                if (Rnd.SpatialInt(e.x, e.z) % InsideContent.Decimation != 0)
                    continue;

                var edge = Map[e];
                if (edge.Tags == 0)
                {
                    edge.Tags |= contentWallTag;
                    spawned.Add(e);
                }
            }

            exit:;
        }

        public override IEnumerator OnCellsCaptured()
        {
            yield return base.OnCellsCaptured();

            if (InsideContent.Layout != ContentSpawnerLayout.None)
                GenerateInsideContent();
        }

        protected virtual void SetWallTags(Vector3Int from, Vector3Int to)
        {
            if (!Features.Walls)
                return;
            var edgeIndex = from + to;
            MyWalls.Add(edgeIndex);
            var edge = Map[edgeIndex];
            if (edge.Tags == 0ul)
            {
                edge.Tags |= WallTags.Wall;
            }

            edge.Seed ^= TagsSeed;
        }

        protected virtual void SetCeilingTags(Vector3Int cell)
        {
            if (!Features.Roof)
                return;// we do not need to build roof and ceiling

            if (MyCells.Contains(cell + Vector3Int.up))
                return;// it is not ceiling

            if (Builder.Debug.DoNotSpawnRoof || Builder.Debug.ForcedIsometricView)
                return;

            var edgeIndex = cell * 2 + Vector3Int.up;
            var edge = Map[edgeIndex];
            if (edge.Tags == 0)
            {
                edge.Tags |= FloorTags.Ceiling;
                edge.Seed ^= TagsSeed;
            }
        }

        protected virtual void SetFloorTags(Vector3Int cell)
        {
            var relativeLevelY = cell.y - StartCell.y;
            var floorEdge = cell * 2 + Vector3Int.down;

            if (relativeLevelY == 0)
            {
                if (!Features.FloorOnFirstLevel)// first floor
                    return;
            }
            else
            if (relativeLevelY < LevelCount)// intermediate floors
            {
                if (!Features.FloorOnIntermediateLevels)
                    return;
            }

            var edge = Map[floorEdge];
            edge.Tags = FloorTags.Floor;
            edge.Seed ^= TagsSeed;
        }
    }

    [Serializable]
    public partial class RoomFeatures
    {
        [Header("Build")]
        [WideCheckbox] public bool Walls = true;
        [WideCheckbox] public bool FloorOnFirstLevel = true;
        [WideCheckbox] public bool FloorOnIntermediateLevels = true;
        [Label("Roof / Ceiling")]
        [WideCheckbox] public bool Roof = true;

        [Header("Other")]
        public DoorStrategy DoorsStrategy = DoorStrategy.Inherited;
        public ContentCount WallContentCount = ContentCount.Max;
        public SetTagsStrategy SetTagsStrategy = SetTagsStrategy.Replace;
    }

    public enum ContentCount
    {
        Zero = 0,
        One  = 1,
        Two = 2,
        Max = 5,
        From0toMax = 10,
        From1toMax = 20,
        ZeroOrMax = 30,
        OneOrMax = 40,
    }

    [Serializable]
    public partial class InsideContentFeatures
    {
        public ContentSpawnerLayout Layout = ContentSpawnerLayout.Islands;

        [Range(1, 20)]
        public int Decimation = 2;

        [WideCheckbox]
        public bool DoNotAffectWalls = true;
    }

    [Serializable]
    public enum DoorStrategy
    {
        FullyConnected = 2,
        [InspectorName("Inherited (from DoorsSpawner)")]
        Inherited = 10,
        NoDoors = 15,
        Impassable = 20,
        [InspectorName("Custom (from Doors)")]
        Custom = 25,
    }

    [Serializable]
    public enum SetTagsStrategy
    {
        Replace = 0, Add = 1
    }

    [Serializable]
    public enum ContentSpawnerLayout : byte
    {
        None,
        Full = 1,
        Islands = 10,
        AlongX = 210,
        AlongXFull = 211,
        AlongZ = 220,
        AlongZFull = 221,
        OneInCenterAlongX = 231,
        OneInCenterAlongZ = 232
    }
}