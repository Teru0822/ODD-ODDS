using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.73d10udi68u")]
    public partial class PrefabSpawner : AutoSpawner
    {
        [SerializeField, Range(0, 15)] int MaxContentPerEdge = 4;
        public bool RandomizeStartDir = true;
        [Header("Check Collisions")]
        public LayerMask ContentBlockingLayers = 1;
        public ContentBlockingMode ContentBlockingMode = ContentBlockingMode.Intersection;

        List<QubicEdge> cells = new List<QubicEdge>();
        List<QubicEdge> edges = new List<QubicEdge>();
        static RandomBuckets wallBuckets = new RandomBuckets();
        static RandomBuckets columnBuckets = new RandomBuckets(10);
        static RandomBuckets contentBuckets = new RandomBuckets();
        static List<IRuleChecker> ruleCheckers;

        HashSet<Prefab> spawnedInCell = new HashSet<Prefab> { };
        List<Rule> rulesForCell = new List<Rule>();
        Collider[] colliders = new Collider[64];

        public void Start()
        {
            // this method is required just to show Enable flag in inspector
        }

        public override IEnumerator OnPrepareSpawnPrefabs()
        {
            // prepare prefabs
            Builder.PrefabDatabase.Prefabs.ForEach(p => p.Prepare(Builder));

            // prepare edges
            PrepareEdges();

            yield return null;

            // prepare rules
            var wallRules = ListPool<Rule>.Get();
            var columnRules = ListPool<Rule>.Get();
            var contentRules = ListPool<Rule>.Get();

            foreach (var prefab in Builder.PrefabDatabase.Prefabs.Where(a => a.PrefabInfo.Prefab != null && a.Rules != null))
                foreach (var rule in prefab.Rules)
                    switch (rule.Prefab.Type)
                    {
                        case PrefabType.Content: contentRules.Add(rule); break;
                        case PrefabType.Corner:
                        case PrefabType.Wall: wallRules.Add(rule); break;
                        case PrefabType.Column: columnRules.Add(rule); break;
                    }

            // prepare random prefab buckets
            wallBuckets.PrepareBuckets(wallRules);
            contentBuckets.PrepareBuckets(contentRules);
            columnBuckets.PrepareBuckets(columnRules);

            // prepare rule checkers
            if (ruleCheckers == null)
                ruleCheckers = TypesHelper.GetDerivedTypeInstances<IRuleChecker>().ToList();
            foreach (var checker in ruleCheckers)
                checker.Prepare(Builder, wallRules.Concat(contentRules).Concat(columnRules));

            ListPool<Rule>.Release(wallRules);
            ListPool<Rule>.Release(columnRules);
            ListPool<Rule>.Release(contentRules);
        }

        #region Current state (to use in RuleCheckers)
        public static bool IsNarrow;
        public static QubicEdge[] AroundEdges = new QubicEdge[4];
        public static bool IsRightWall;
        public static bool IsLeftWall;
        public static bool IsConvexCorner;
        public static bool IsConcaveCornerLeft;
        public static bool IsConcaveCornerRight;
        public static bool NoFloor;
        public static bool NoCeiling;
        public static QubicEdge CurrentEdge;
        public static QubicEdge CurrentFromCell;
        public static float OffsetY;
        #endregion

        public override IEnumerator OnSpawnContent()
        {
            var rulesCount = contentBuckets.Count / contentBuckets.BucketsCount;
            var boundsChecker = new BoundsIntersectionChecker(32);
            var totalSpawned = 0;
            var frameCounter = 0;
            var cellHalfHeight = Builder.PrefabDatabase.CellHeight / 2;

            foreach (var cell in cells)
            {
                var cellHex = cell.Index / 2;
                CurrentFromCell = cell;

                if (cell.Flags.HasFlag(QubicEdgeFlags.NoContent))
                    continue; // skip cell, because it is not allowed to spawn content

                // get allowed rules for the cell
                rulesForCell.Clear();
                var bucketIndex = cell.Seed.Mod(contentBuckets.BucketsCount);
                var prefabIndex = bucketIndex * rulesCount;
                for (var i = 0; i < rulesCount; i++)
                {
                    var rule = contentBuckets[prefabIndex++];
                    if ((rule.FromMask & cell.Tags) == 0)
                        continue;// is not my cell tags
                    if (!rule.Prefab.Levels.InRange(cellHex.y))
                        continue;// is not my level
                    rulesForCell.Add(rule);
                }

                // check walls around cell
                for (var iDir = 0; iDir < 4; iDir++)
                    //AroundEdgesTags[iDir] = Map[cell.Index + QubicHelper.Dirs4[iDir]].Tags & ~WallTags.Content;
                    AroundEdges[iDir] = Map[cell.Index + QubicHelper.Dirs4[iDir]];

                // is floor/ceil ?
                var floorEdge = Map[cell.Index + Vector3Int.down];
                NoFloor = floorEdge.Tags == 0;
                NoCeiling = Map[cell.Index + Vector3Int.up].Tags == 0;

                // offset by Y
                var floorOffsetY = (floorEdge.SpawnedPrefab?.WallFeatures?.ContentPadding ?? PrefabDatabaseFeatures.PADDING) - PrefabDatabaseFeatures.PADDING;

                // content count per edge
                var contentCountPerWall = CalcContentCountPerWall(cell);

                // reset
                spawnedInCell.Clear();
                boundsChecker.Clear();

                // calc start direction
                var startDir = RandomizeStartDir ? cell.Seed % 4 : 0;

                // add user defined colliders
                if (this.ContentBlockingLayers != 0 && ContentBlockingMode != ContentBlockingMode.None)
                {
                    AddUsersCollidersToBoundsChecker(cell, boundsChecker, startDir);
                    if (boundsChecker.Count > 0 && ContentBlockingMode == ContentBlockingMode.WholeCell)
                        continue;
                }

                // build 4 edges
                for (var iDirection = 0; iDirection < 4; iDirection++)
                {
                    var iDir = (iDirection + startDir) % 4;
                    var dir = QubicHelper.Dirs4[iDir];
                    var right = QubicHelper.Dirs4[(iDir + 1) % 4];

                    // get tags from edge
                    var edge = CurrentEdge = Map[cell.Index + dir];
                    var edgeTags = edge.Tags;
                    var opCellIndex = cell.Index + dir * 2;
                    var opCellTags = Map[opCellIndex].Tags;
                    var oppositeEdge = Map[cell.Index - dir];
                    var opCellHex = opCellIndex / 2;

                    IsNarrow = AroundEdges[(iDir + 2) % 4].IsSpawned;
                    IsRightWall = AroundEdges[(iDir + 1) % 4].IsSpawned;
                    IsLeftWall = AroundEdges[(iDir + 3) % 4].IsSpawned;
                    IsConvexCorner = IsLeftWall || IsRightWall;
                    IsConcaveCornerRight = IsConcaveCorner(edge.Index, dir, right);
                    IsConcaveCornerLeft = IsConcaveCorner(edge.Index, dir, -right);

                    var isContentWall = (edgeTags & WallTags.Content) != 0;
                    var toSpawn = isContentWall ? MaxContentPerEdge : contentCountPerWall;// max items per edge
                    if (toSpawn <= 0)
                        continue;

                    var leftColumnEdge = Map[cell.Index + dir - right];
                    var rightColumnEdge = Map[cell.Index + dir + right];

                    // enumerate rules in bucket
                    for (int i = 0; i < rulesForCell.Count; i++)
                    {
                        // check edge tags and opposite cell tags
                        var rule = rulesForCell[i];
                        if ((rule.EdgeMask & edgeTags) == 0 || (rule.ToMask & opCellTags) == 0)
                            continue;// is not suitable

                        // ===== check other conditions
                        var prefab = rule.Prefab;
                        var features = prefab.ContentFeatures;
                        if (NoFloor && features.NeedFloor)
                            continue;// no needed floor
                        if (NoCeiling && features.NeedCeiling)
                            continue;// no needed ceiling
                        if (IsNarrow && features.AvoidNarrow switch { TriBoolValue.Off => false, TriBoolValue.On => true, _ => prefab.IsFullDepth })
                            continue;// do not spawn in narrow corridors
                        if (IsLeftWall && rule.Prefab.IsLeftColumn)
                            continue;// can not spawn column because here is wall
                        if (IsRightWall && rule.Prefab.IsRightColumn)
                            continue;// can not spawn column because here is wall
                        if (features.AvoidConvexCorners && IsConvexCorner)
                            continue;// avoid convex corner
                        if (!CheckCheckers(rule, cellHex, opCellHex))
                            continue;// is not suitable for checkers
                        var prefabBounds = prefab.BoundsByDirs[iDirection];
                        OffsetY = prefabBounds.minY > cellHalfHeight || features.IgnoreFloorPadding ? 0 : floorOffsetY;
                        if (!features.IgnoreCollisions && boundsChecker.IntersectsAny(prefabBounds, OffsetY))
                            continue;// intersects with other prefabs in the cell
                        if (features.AvoidConcaveCorners && IsConcaveCornerFor(prefab))
                            continue;// avoid concave corner
                        if (features.CheckWallsAround switch { TriBoolValue.Off => false, TriBoolValue.On => !CheckWallsAround(rule.EdgeMask), _ => prefab.IsFullDepth && !CheckWallsAround(rule.EdgeMask) })
                            continue;// walls around me are not suitable for full cell spawn
                        if (features.SingleInCell && spawnedInCell.Contains(prefab))
                            continue;// we are already spawned in the cell

                        // ==== spawn
                        Spawn(rule, edge.Seed, cellHex, opCellHex, OffsetY);

                        // ==== on successfully spawned
                        boundsChecker.AddBounds(rule.Prefab.BoundsByDirs[iDirection], OffsetY);
                        spawnedInCell.Add(rule.Prefab);
                        totalSpawned++;

                        toSpawn--;

                        //if (rule.Priority <= 0)// stop spawning if priority is 0 or less
                        if (toSpawn <= 0)// stop spawning if we reached max count
                            break;
                    }

                    bool IsConcaveCornerFor(Prefab prefab)
                    {
                        if (prefab.IsRightColumn) return IsConcaveCornerRight;
                        if (prefab.IsLeftColumn) return IsConcaveCornerLeft;
                        return IsConcaveCornerLeft || IsConcaveCornerRight;
                    }
                }

                if (frameCounter++ % 3 == 0)
                    yield return null;
            }
        }

        private void AddUsersCollidersToBoundsChecker(QubicEdge cell, BoundsIntersectionChecker boundsChecker, int startDir)
        {
            var layer = Builder.CollisionLayerTagMapper.GetOrCreate(Prefab.DefaultCollisionLayer);
            var pos = Map.CellToPos(cell.Index / 2);
            var cellBounds = new Bounds(pos + Vector3.up * Map.CellHeight / 2, new Vector3(Map.CellSize, Map.CellHeight, Map.CellSize));
            var count = Physics.OverlapBoxNonAlloc(cellBounds.center, cellBounds.extents, colliders, Quaternion.identity, ContentBlockingLayers);
            for (int i = 0; i < count; i++)
                if (colliders[i].GetComponentInParent<QubicHolder>() == null)
                {
                    var bounds = colliders[i].bounds;
                    bounds.center -= pos;
                    bounds.center = bounds.center.rotateAroundAxis(Vector3.up, -90 * startDir);
                    bounds.center += new Vector3(Map.CellSize / 2, 0, Map.CellSize / 2);
                    if (startDir % 2 == 1)
                        bounds.size = new Vector3(bounds.size.z, bounds.size.y, bounds.size.x);
                    boundsChecker.AddBounds(new FastBounds(bounds, layer), 0);
                }
        }

        private int CalcContentCountPerWall(QubicEdge cell)
        {
            var contentCountPerWall = MaxContentPerEdge;
            if (cell.Room != null && cell.Room.Features.WallContentCount != ContentCount.Max)
            {
                var rnd = cell.Room.RootRnd.GetBranch(cell.Seed, 137);
                switch (cell.Room.Features.WallContentCount)
                {
                    case ContentCount.Zero: contentCountPerWall = 0; break;
                    case ContentCount.One: contentCountPerWall = 1; break;
                    case ContentCount.Two: contentCountPerWall = 2; break;
                    case ContentCount.From0toMax: contentCountPerWall = rnd.Int(0, MaxContentPerEdge + 1); break;
                    case ContentCount.From1toMax: contentCountPerWall = rnd.Int(1, MaxContentPerEdge + 1); break;
                    case ContentCount.ZeroOrMax: contentCountPerWall = rnd.Bool(0.5f) ? 0 : MaxContentPerEdge; break;
                    case ContentCount.OneOrMax: contentCountPerWall = rnd.Bool(0.5f) ? 1 : MaxContentPerEdge; break;
                }
            }

            return contentCountPerWall;
        }

        public override IEnumerator OnSpawnCompleted()
        {
            foreach (var checker in ruleCheckers)
            {
                checker.OnSpawnCompleted();
                yield return null;
            }
        }

        bool IsConcaveCorner(Vector3Int edgeIndex, Vector3Int fwd, Vector3Int right)
        {
            ulong mask = ~(WallTags.Content | WallTags.Steps | WallTags.Passage);
            var cornerIndex = edgeIndex + right;

            if ((Map[cornerIndex - fwd].Tags & mask) != 0) return false;
            if ((Map[cornerIndex + right].Tags & mask) != 0) return false;
            if ((Map[cornerIndex + fwd].Tags & mask) != 0) return true;
            return false;
        }

        private bool CheckWallsAround(UInt64 wallTags)
        {
            foreach (var wall in AroundEdges)
            {
                var nn = wall.Tags & ~WallTags.Content;
                if (nn != 0ul && (nn & wallTags) == 0ul)
                    return false;
            }

            return true;
        }

        public override IEnumerator OnSpawnWalls()
        {
            var hasOneSidePrefabs = Builder.PrefabDatabase.Prefabs.Any(p => p.Type == PrefabType.Wall && p.WallFeatures.IsOneSideWall);

            var frameCounter = 0;
            var rulesCount = wallBuckets.Count / wallBuckets.BucketsCount;
            var toSpawn = new List<(Rule, Vector3Int from, Vector3Int to)>(edges.Count);
            foreach (var edge in edges)
            {
                if (edge.IsSpawned)
                    continue;
                CurrentEdge = edge;

                // get bucket index based on edge seed
                var bucketIndex = edge.Seed.Mod(wallBuckets.BucketsCount);
                var prefabIndex = bucketIndex * rulesCount;
                // get tags from edge and cells
                var cells = QubicHelper.EdgeToCells(edge.Index);
                var (fromTag, toTag) = (Map[cells.from * 2].Tags, Map[cells.to * 2].Tags);
                var edgeTags = edge.Tags;
                var setTagsMask = 0ul;
                var isFloor = cells.from.y != cells.to.y;// is floor edge

                // enumerate rules in bucket
                for (int i = 0; i < rulesCount; i++)
                {
                    var rule = wallBuckets[prefabIndex++];
                    if ((rule.EdgeMask & edgeTags) == 0)
                        continue;// wall type is not suitable

                    var prefab = rule.Prefab;

                    if (!prefab.Levels.InRange(edge.Index.y / 2))
                        continue;// is not my level

                    if (edge.SpawnedMask != 0 && prefab.SetTagsMask != setTagsMask)
                        continue;// the edge already has spawned prefab (one side of wall) and it has incompatable tags

                    if (prefab.WallFeatures.Side == SideWall.IsDoubleSide && edge.SpawnedMask != 0)
                        continue;// the edge already has spawned prefab

                    var offsetY = isFloor ? Builder.PrefabDatabase.Features.FloorOffsetY : 0f;

                    // check one direction
                    if (prefab.WallFeatures.IsOneSideWall ? edge.SpawnedPrefab0 == null : edge.SpawnedPrefab == null)
                    if ((rule.FromMask & fromTag) != 0 && (rule.ToMask & toTag) != 0)// check From and To tags
                    if (CheckCheckers(rule, cells.from, cells.to))
                    {
                        Spawn(rule, edge.Seed, cells.from, cells.to, offsetY);
                        if (!hasOneSidePrefabs || edge.SpawnedMask == 0b111)
                            break;// successfully spawned
                        setTagsMask = rule.Prefab.SetTagsMask;
                    }

                    // check opposite direction
                    if (prefab.WallFeatures.IsOneSideWall ? edge.SpawnedPrefab1 == null : edge.SpawnedPrefab == null)
                    if ((rule.FromMask & toTag) != 0 && (rule.ToMask & fromTag) != 0)// check From and To tags
                    if (CheckCheckers(rule, cells.to, cells.from))
                    {
                        Spawn(rule, edge.Seed, cells.to, cells.from, offsetY);
                        if (!hasOneSidePrefabs || edge.SpawnedMask == 0b111)
                            break;// successfully spawned
                        setTagsMask = rule.Prefab.SetTagsMask;
                    }
                }

                if (frameCounter++ % 3 == 0)
                    yield return null;
            }
        }

        private bool CheckCheckers(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            // check checkers
            var checkers = rule.Checkers;
            for (int i = 0; i < checkers.Count; i++)
            if (!checkers[i].Check(rule, fromCell, toCell))
                return false;

            return true;
        }

        private void Spawn(Rule rule, int edgeSeed, Vector3Int fromCell, Vector3Int toCell, float offsetY, bool isLeft = false)
        {
            var prefab = rule.Prefab;
            var selectedPrefab = prefab.PrefabInfo.Prefab;
            if (prefab.PrefabInfo.Alternates != null && prefab.PrefabInfo.Alternates.Length > 0)
                selectedPrefab = new Rnd(edgeSeed, 17).GetRndItem(prefab.Prefabs);
            if (selectedPrefab == null)
                return;
            var obj = Builder.Pool.GetOrCreate(selectedPrefab);
            //
            var dir = toCell - fromCell;
            if (isLeft)
                dir *= -1;
            var posIndex = fromCell + toCell;

            // get wall padding
            var padding = 0f;
            if (dir.y == 0 && rule.Prefab.Type == PrefabType.Content && !rule.Prefab.ContentFeatures.IgnoreWallPadding)
            {
                var spawnedWall = CurrentEdge.GetSpawnedForPadding(fromCell, toCell);
                padding = spawnedWall?.WallFeatures.ContentPadding ?? Builder.PrefabDatabase.Features.DefaultContentPadding;
            }

            //
            if (rule.Prefab.Type == PrefabType.Column)
                posIndex += dir.RotateY(isLeft ? -1 : 1);

            // rotate and position
            var dirRotation = dir.y == 0 ? QubicHelper.RotationFromDirection(dir) : Quaternion.identity;
            var pos = Map.EdgeToPos(posIndex) - (Vector3)dir * padding + Vector3.up * offsetY;
            obj.transform.rotation = dirRotation * prefab.PrefabInfo.Rotation;
            obj.transform.position = pos + dirRotation * prefab.PrefabInfo.Anchor;
            obj.transform.localScale = selectedPrefab.transform.localScale;
            obj.isStatic = Builder.gameObject.isStatic;

            // build variants
            if (prefab.HasVariants)
                Variant.Build(obj, new Rnd(selectedPrefab.name, edgeSeed), true);

            // save to spawned objects
            Builder.SpawnedObjects[obj] = new SpawnedObjectInfo { Object = obj, Prefab = prefab, FromRoom = Map[fromCell * 2].Room, ToRoom = Map[toCell * 2].Room };

            // callbacks
            rule.OnSpawned(prefab.PrefabInfo.Prefab, fromCell, toCell);

            // callbacks
            foreach (var checker in rule.Checkers)
                checker.OnSpawned(rule, obj, fromCell, toCell);
        }

        public override IEnumerator OnSpawnColumns()
        {
            // ===== build column candidate list
            var candidates = new List<ColumnCandidate>();//TODO: pool
            foreach (var request in Builder.ColumnRequestQueue)
            {
                var fwd = request.Fwd;
                var right = fwd.RotateY(1);
                var columnEdgeIndex = request.WallEdgeIndex + right;
                var columnEdge = Map[columnEdgeIndex];
                if (columnEdge.IsSpawned)
                    continue;// column is already spawned

                var wallEdge = CurrentEdge = Map[request.WallEdgeIndex];
                var rulesCount = columnBuckets.Count / columnBuckets.BucketsCount;

                // get bucket
                var bucketIndex = wallEdge.Seed.Mod(columnBuckets.BucketsCount);
                var prefabIndex = bucketIndex * rulesCount;
                // get tags from edge and cells
                var edgeTags = wallEdge.Tags;
                var from = request.WallEdgeIndex - fwd;
                var to = request.WallEdgeIndex + fwd;
                CurrentFromCell = Map[from];
                var fromTag = CurrentFromCell.Tags;

                var toCell = Map[to];
                var toTag = toCell.Tags;

                var fromCellHex = from / 2;
                var toCellHex = to / 2;
                // enumerate rules in bucket
                for (int i = 0; i < rulesCount; i++)
                {
                    var rule = columnBuckets[prefabIndex++];

                    if (!rule.Prefab.Levels.InRange(fromCellHex.y))
                        continue;// is not my level

                    if ((rule.EdgeMask & edgeTags) == 0)
                        continue;// wall type is not suitable

                    if ((rule.FromMask & fromTag) == 0 || (rule.ToMask & toTag) == 0)// check From and To tags
                        continue;// cells tags are wrong 

                    if (!CheckCheckers(rule, fromCellHex, toCellHex))
                        continue;// is not suitable for checkers

                    // add to candidate list
                    candidates.Add(new ColumnCandidate(rule, wallEdge.Seed, fromCellHex, toCellHex, columnEdge));
                    break;// select only one candidate per request
                }
            }

            yield return null;

            // ===== sort candidates by priority
            candidates.CountingSort(c => c.rule.Priority, Rule.MinPriority, Rule.MaxPriority, true);

            yield return null;

            // ===== spawn candidates
            var frameCounter = 0;
            foreach (var candidate in candidates)
            {
                if (candidate.ColumnEdge.IsSpawned)
                    continue;

                // spawn
                Spawn(candidate.rule, candidate.EdgeSeed, candidate.FromCell, candidate.ToCell, 0);

                if (frameCounter++ % 10 == 0)
                    yield return null;
            }
        }

        struct ColumnCandidate
        {
            public Rule rule;
            public int EdgeSeed;
            public Vector3Int FromCell;
            public Vector3Int ToCell;
            public QubicEdge ColumnEdge;

            public ColumnCandidate(Rule rule, int edgeSeed, Vector3Int fromCell, Vector3Int toCell, QubicEdge columnEdge)
            {
                this.rule = rule;
                EdgeSeed = edgeSeed;
                FromCell = fromCell;
                ToCell = toCell;
                ColumnEdge = columnEdge;
            }
        }

        private void PrepareEdges()
        {
            cells.Clear();
            edges.Clear();

            // should we check distance?
            var checkDistance = false;
            Vector3Int centralCell = Vector3Int.zero;
            int centralLevel = 0;
            int distToRebuildIndexSqr = Preferences.Instance.RadiusToRebuild * Preferences.Instance.RadiusToRebuild * 4;
            int dLevelToRebuildSqr = Preferences.Instance.LevelDeltaToRebuild * Preferences.Instance.LevelDeltaToRebuild;

#if UNITY_EDITOR
            if (Builder.LastSelectedRoom != null)
            {
                centralCell = Builder.LastSelectedRoom.StartCell.XZ() * 2;
                centralLevel = Builder.LastSelectedRoom.StartCell.y;
                checkDistance = Map.Count >= Preferences.Instance.MinMapCellsCountToEnableFastMode && Builder.BuildType == 0 && Preferences.Instance.FastMode;
            }
#endif
            // sort edges by Y
            var inside = ~CellTags.Outside;
            var allEdges = Map.Values.Where(p => (p.Tags & inside) != 0).ToList();
            allEdges.CountingSort(p => GetSortIndex(p), 0, 200);

            foreach (var edge in allEdges)
            {
#if UNITY_EDITOR
                // super fast mode? => skip spawning far prefabs
                if (checkDistance && edge.Room != Builder.LastSelectedRoom)
                if ((edge.Index.XZ() - centralCell).sqrMagnitude > distToRebuildIndexSqr || Mathf.Abs(Mathf.FloorToInt((edge.Index.y + 1) / 2f) - centralLevel) > dLevelToRebuildSqr)
                    continue;
#endif

                if (edge.Index.IsCell())
                {
                    cells.Add(edge);
                    // assign spatial seed
                    edge.Seed = Mathf.Abs(edge.Seed ^ Rnd.SpatialInt(edge.Index.x, edge.Index.z, edge.Index.y));
                }
                else
                {
                    this.edges.Add(edge);
                    // assign spatial seed
                    edge.Seed = Mathf.Abs(edge.Seed ^ Rnd.SpatialInt(edge.Index.x, edge.Index.z));
                }
            }
        }

        private int GetSortIndex(QubicEdge p)
        {
            var res = p.Index.y * 2 + 60;

            var fromCell = p.Index / 2;
            var toCell = p.Index - fromCell;
            if (fromCell == toCell)
                p.Type = EdgeType.Cell;
            else
            if (p.Index.y % 2 != 0)
                p.Type = EdgeType.Floor;
            else
                p.Type = EdgeType.Wall;

            if (p.Type != EdgeType.Wall)
                return res;

            // corner?
            var fwd = toCell - fromCell;
            var right = fwd.RotateY(1);
            if ((Map.TryGetValue(p.Index + right - fwd, out var n) && n.Tags != 0) ||
                (Map.TryGetValue(p.Index - right + fwd, out n) && n.Tags != 0))
            {
                p.Flags |= QubicEdgeFlags.IsPartOfCorner;
                res -= 1;
            }
            return res;
        }
    }

    public enum ContentBlockingMode
    {
        None = 0,
        Intersection = 1,
        WholeCell = 10
    }

    public struct ColumnRequest
    {
        public readonly Vector3Int WallEdgeIndex;
        public readonly Vector3Int Fwd;
        public Rule Candidate;

        public ColumnRequest(Vector3Int wallEdgeIndex, Vector3Int fwd)
        {
            WallEdgeIndex = wallEdgeIndex;
            Fwd = fwd;
            Candidate = null;
        }
    }
}