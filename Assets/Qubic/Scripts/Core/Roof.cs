using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    partial class Templates
    {
        [InspectorTitle("Roof"), Order(1)]
        public static Prefab Roof(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Content);
            res.ContentFeatures.IsRoof = true;
            res.ContentFeatures.Decimation = Decimation.None;
            res.ContentFeatures.IgnoreWallPadding = true;
            res.ContentFeatures.IgnoreFloorPadding = true;
			res.ContentFeatures.IgnoreCollisions = true;
            res.ContentFeatures.NeedFloor = false;
            res.ContentFeatures.SpawnInsideRoom = true;

            res.Rules = new[]
            {
                new Rule(res, "Parapet", "Roof", "Outside", 2)
            };
            return res;
        }
    }

    partial class ContentPrefabFeatures
    {
        [HideInInspector] public bool IsRoof = false;
        [ShowIf(nameof(IsRoof))] public RoofPart RoofType = RoofPart.Straight;

        [ShowIf(nameof(IsRoof)), Label("Room Style")] public string RoofRoomStyle = "Room";
    }

    public class RoofRule : IRuleChecker
    {
        QubicBuilder builder;
        ulong roofMask;
        HashSet<Vector3Int> taken = new HashSet<Vector3Int>();
        Dictionary<Room, Rule> roomToRule = new Dictionary<Room, Rule>();

        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            this.builder = builder;
            roofMask = CellTags.Roof.Mask;
            taken.Clear();
            roomToRule.Clear();

            foreach (var rule in rules)
            if (rule.Prefab.ContentFeatures.IsRoof)
                rule.Checkers.Add(this);
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            var roomMask = builder.TagsMapper.GetMask(rule.Prefab.ContentFeatures.RoofRoomStyle);
            var map = rule.Builder.Map;
            var features = rule.Prefab.ContentFeatures;

            var fwd = toCell - fromCell;
            var right = fwd.RotateY(1);
            var rightEmpty = (map[fromCell * 2 + right * 2].Tags & roofMask) == 0ul;
            var rightFwdEmpty = (map[fromCell * 2 + right * 2 + fwd * 2].Tags & roofMask) == 0ul;
            var leftEmpty = (map[fromCell * 2 - right * 2].Tags & roofMask) == 0ul;

            var room = map[fromCell * 2].Room;
            if (room == null)
                room = map[fromCell * 2 - Vector3Int.up * 2].Room;

            // check room style
            if (room != null && (room.SetTagsMask & roomMask) == 0)
                return false;

            // check corner type
            switch (features.RoofType)
            {
                case RoofPart.Straight:
                    return !rightEmpty && !leftEmpty;
                case RoofPart.Convex:
                    return !rightEmpty && leftEmpty;
                case RoofPart.Concave:
                    return !rightEmpty && !rightFwdEmpty;
                case RoofPart.Flat:                    
                    if (room != null)
                        roomToRule[room] = rule;
                    return false;// will spawn later
            }

            return false;
        }

        void IRuleChecker.OnSpawnCompleted()
        {
            // spawn flat part of roofs
            var outsideMask = (ulong)CellTags.Outside;

            foreach ((var room, var rule) in roomToRule)
            {
                var prefab = rule.Prefab;
                var hasRoof = room.Features.Roof;
                if (!hasRoof)
                    continue;
                if (prefab.PrefabInfo?.Prefab == null)
                    continue;

                foreach (var cell in room.MyCells)
                {
                    // roof ?
                    var cellIndex = (cell + Vector3Int.up) * 2;
                    var e = builder.Map[cellIndex];
                    if ((e.Tags & roofMask) == 0ul)
                        continue;

                    if (cell.Neighbors8().Any(n => (builder.Map[n * 2].Tags & outsideMask) != 0ul))
                        continue;

                    // == spawn flat roof ==
                    var obj = builder.Pool.GetOrCreate(prefab.PrefabInfo?.Prefab);

                    // rotate and position
                    var pos = builder.Map.EdgeToPos(cell * 2 + Vector3Int.up);
                    obj.transform.rotation = prefab.PrefabInfo.Rotation;
                    obj.transform.position = pos + prefab.PrefabInfo.Anchor;
                    obj.transform.localScale = prefab.PrefabInfo.Prefab.transform.localScale;
                    obj.isStatic = builder.gameObject.isStatic;

                    // save to spawned objects
                    builder.SpawnedObjects[obj] = new SpawnedObjectInfo { Object = obj, Prefab = prefab, FromRoom = room, ToRoom = room };
                }
            }
        }
    }

    [Serializable]
    public enum RoofPart
    {
        Straight,
        Convex,
        Concave,
        Flat,
    }
}