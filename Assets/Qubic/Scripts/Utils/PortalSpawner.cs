using System.Collections;

namespace QubicNS
{
    public class PortalSpawner : BaseSpawner
    {
        public string PortalTag = "Door";

        public override int Order => 50;

        public void GrabFrom(PortalSpawner other)
        {
            PortalTag = other.PortalTag;
        }

        public override IEnumerator OnTagsPostprocess()
        {
            yield break;

            //var buildersToRebuild = new HashSet<MicroBuild>();
            //var prevPortals = new List<Vector3>(Builder.PortalPositions);
            //Builder.PortalPositions.Clear();

            //// find outer cells connected with other buildings
            //foreach (var room in Builder.Spawners.OfType<Room>())
            //foreach (var edge in room.OutsideEdges)
            //{
            //    var cells = edge.GetCells(room);
            //    var adjRoom = Map[cells.to].Room;
            //    if (adjRoom != null) continue;

            //    // try get cell from other builder
            //    if (MicroBuild.PosToCellGlobal(Map.HexToPos(cells.to) + Vector3.up * 0.1f, out var otherBuilder, out var otherCell))
            //    if (otherBuilder.Map != null)
            //    {
            //        var isMain = MicroBuildHelper.GetHigherInHierarchy(room.gameObject, otherCell.Room.gameObject) == room.gameObject;
            //        var isMyRoof = room.CheckLevel(cells.from.y, LevelFilter.Roof, true);
            //        var isAdjRoof = otherCell.Room.CheckLevel(otherCell.Pos.y, LevelFilter.Roof, true);
            //        var hasFloorBothSides = Map[cells.from * 2 + Vector3Int.down].Tags != 0 && otherBuilder.Map[otherCell.Pos * 2 + Vector3Int.down].Tags != 0;
            //        if (isMyRoof || isAdjRoof)
            //        {
            //            if (isMyRoof)
            //                edge.AddVote(SpawnPriority.SpawnForced, Builder.TagsMapper.GetOrCreate(TagSpawner.NoneTag), edge.ResultOwner);
            //            continue;// this is roof => do not build portal
            //        }

            //        if (!hasFloorBothSides)
            //        {
            //            if (!isMain)
            //                edge.AddVote(SpawnPriority.SpawnForced, Builder.TagsMapper.GetOrCreate(TagSpawner.NoneTag), edge.ResultOwner);
            //            continue;// there no floor => do not build portal
            //        }

            //        var tag = isMain ? PortalTag : TagSpawner.NoneTag;
            //        edge.AddVote(SpawnPriority.SpawnForced, Builder.TagsMapper.GetOrCreate(tag), edge.ResultOwner);
            //        var edgePos = Map.HexToPos(edge.Index);
            //        Builder.PortalPositions.Add(edgePos);

            //        // need rebuild adj building?
            //        if (!prevPortals.Any(p => p.Approximately(edgePos)))
            //        {
            //            if (otherBuilder != null && otherBuilder != Builder)
            //            {
            //                // rebuild adj building
            //                buildersToRebuild.Add(otherBuilder);
            //            }
            //        }
            //    }
            //}

            //// rebuild disconnected builders
            //foreach (var prev in prevPortals)
            //if (!Builder.PortalPositions.Any(p=>p.Approximately(prev)))
            //    buildersToRebuild.AddRange(MicroBuild.AllEnabledBuilders.Where(b=>b.PortalPositions.Any(p => p.Approximately(prev))));

            //// delalyed rebuilding of adj buildings
            //foreach (var b in buildersToRebuild) 
            //    b.RebuildNeeded();
        }
    }
}