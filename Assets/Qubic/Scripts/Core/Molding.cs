using System;
using System.Collections.Generic;
using UnityEngine;

namespace QubicNS
{
    partial class Templates
    {
        [InspectorTitle("Molding"), Order(1)]
        public static Prefab Molding(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Content);
            res.ContentFeatures.IsMolding = true;
            res.ContentFeatures.Decimation = Decimation.None;
            res.ContentFeatures.IgnoreWallPadding = true;
            res.ContentFeatures.IgnoreFloorPadding = true;
			res.ContentFeatures.IgnoreCollisions = true;

            res.Rules = new[]
            {
                new Rule(res, "Wall,Window", "Room", "Any", 2)
            };
            return res;
        }
    }

    partial class ContentPrefabFeatures
    {
        [HideInInspector] public bool IsMolding = false;
    }

    public class MoldingRule : IRuleChecker
    {
        QubicBuilder builder;
        Dictionary<Prefab, List<(Vector3Int from, Vector3Int to, GameObject obj)>> prefabToMap = new Dictionary<Prefab, List<(Vector3Int from, Vector3Int to, GameObject obj)>>();

        public void Prepare(QubicBuilder builder, IEnumerable<Rule> rules)
        {
            prefabToMap.Clear();
            this.builder = builder;

            // register this rule checker for all moldings
            foreach (var rule in rules)
            if (rule.Prefab.ContentFeatures.IsMolding)
                rule.Checkers.Add(this);
        }

        public bool Check(Rule rule, Vector3Int fromCell, Vector3Int toCell)
        {
            return true;
        }

        void IRuleChecker.OnSpawned(Rule rule, GameObject obj, Vector3Int fromCell, Vector3Int toCell)
        {
            if (!prefabToMap.TryGetValue(rule.Prefab, out var map))
                map = prefabToMap[rule.Prefab] = new List<(Vector3Int from, Vector3Int to, GameObject obj)>();
            map.Add((fromCell, toCell, obj));
        }

        enum Type
        {
            Straight,
            Convex,
            Concave,
            RightCap,
            LeftCap,
        }

        void IRuleChecker.OnSpawnCompleted()
        {
            var cellMap = builder.Map;

            foreach (var pair in prefabToMap)
            {
                var prefab = pair.Key;

                // make hashmap
                var map = new Vector3IntSet();
                var hasConnectorLeft = new Vector3IntSet();
                foreach (var data in pair.Value)
                {
                    var dir = data.to - data.from;
                    var index = data.from * 3 + dir;
                    map.Add(index);
                }

                // enumerate spawned moldings and spawn right parts
                foreach (var data in pair.Value)
                {
                    var index = data.from * 3;
                    var fwd = data.to - data.from;
                    var right = fwd.RotateY(1);
                    var cellIndex = data.from * 2;

                    // calc corner
                    Type corner;
                    if (map.Contains(index += right)) corner = Type.Convex; else
                    if (cellMap[cellIndex += right].IsSpawned) corner = Type.RightCap; else
                    if (map.Contains(index += right)) corner = Type.RightCap; else
                    if (map.Contains(index += right + fwd)) corner = Type.Straight; else
                    if (cellMap[cellIndex += right + fwd].IsSpawned) corner = Type.RightCap; else
                    if (map.Contains(index += fwd)) corner = Type.RightCap; else
                    if (map.Contains(index += fwd - right)) corner = Type.Concave; else
                        corner = Type.RightCap;

                    if (corner != Type.RightCap)
                        hasConnectorLeft.Add(index);

                    // hide parts
                    var cornerName = corner.ToString();

                    for (var i = 0; i < data.obj.transform.childCount; i++)
                    {
                        var child = data.obj.transform.GetChild(i);
                        if (child.name == "Main")
                            continue;
                        child.gameObject.SetActive(child.name == cornerName);
                    }
                }

                // enumerate spawned moldings and spawn left endcap
                var leftCapName = Type.LeftCap.ToString();
                foreach (var data in pair.Value)
                {
                    var fwd = data.to - data.from;
                    var index = data.from * 3 + fwd;
                    for (var i = 0; i < data.obj.transform.childCount; i++)
                    {
                        var child = data.obj.transform.GetChild(i);
                        if (child.name != leftCapName)
                            continue;
                        child.gameObject.SetActive(!hasConnectorLeft.Contains(index));
                    }
                }
            }
        }
    }
}