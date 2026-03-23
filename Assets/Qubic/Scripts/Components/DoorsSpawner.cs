using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.vyglmbxow3yn")]
    public class DoorsSpawner : AutoSpawner
    {
        public EnteranceStrategy DoorsStrategy = EnteranceStrategy.Tree;

        public override int Order => 120;

        public override IEnumerator OnCellsCaptured()
        {
            // ====== find candidates for doors
            var candidates = new List<Candidate>();
            var processed = new Vector3IntSet();
            var possibleLevels = new HashSet<int>();

            foreach (var room in Builder.Spawners.OfType<Room>())
            {
                foreach(var edgeIndex in room.MyWalls)
                {
                    if (!processed.Add(edgeIndex))
                        continue;

                    var pair = QubicHelper.EdgeToCells(edgeIndex);
                    var fromCell = Map[pair.from * 2];
                    var toCell = Map[pair.to * 2];

                    if (fromCell.Room == null || toCell.Room == null || fromCell.Room == toCell.Room)
                        continue;// no rooms

                    if (fromCell.Room == toCell.Room)
                        continue;// same room

                    if ((Map[fromCell.Index + Vector3Int.down].Tags & FloorTags.Floor) == 0 ||
                        (Map[toCell.Index + Vector3Int.down].Tags & FloorTags.Floor) == 0)
                        continue;// no floor

                    if ((fromCell.Tags & CellTags.Impassible) != 0 || (toCell.Tags & CellTags.Impassible) != 0)
                        continue;// one of rooms is impassable

                    if (fromCell.Room.Features.DoorsStrategy == DoorStrategy.NoDoors || toCell.Room.Features.DoorsStrategy == DoorStrategy.NoDoors)
                        continue;// one of rooms has no doors

                    if (fromCell.Room.Features.DoorsStrategy == DoorStrategy.Custom && !fromCell.Room.Doors.Allowed.Contains(toCell.Room))
                        continue;// one of rooms has custom doors and does not allow this room

                    if (toCell.Room.Features.DoorsStrategy == DoorStrategy.Custom && !toCell.Room.Doors.Allowed.Contains(fromCell.Room))
                        continue;// one of rooms has custom doors and does not allow this room

                    candidates.Add(new Candidate(Map[edgeIndex], fromCell.Room, toCell.Room));
                    possibleLevels.Add(edgeIndex.y);
                }
            }

            candidates = candidates.OrderBy(c => c.FromRoom.InstanceIndex).ThenBy(c => c.ToRoom.InstanceIndex).ToList();

            yield return null;

            // build tree
            var isTree = DoorsStrategy == EnteranceStrategy.Labyrinth || DoorsStrategy == EnteranceStrategy.Tree;
            HashSet<(int, Room, Room)> allowedPassages = new HashSet<(int, Room, Room)>();
            if (isTree)
            {
                foreach (var levelY in possibleLevels)
                {
                    var graph = BuildGraph(candidates.Where(c => c.Edge.Index.y == levelY).Select(c => (c.FromRoom, c.ToRoom)));
                    allowedPassages.AddRange(BuildForest(graph, DoorsStrategy).Select(p=>(levelY, p.Item1, p.Item2)));
                }
            }
            //

            var sign = DoorsStrategy == EnteranceStrategy.FullyConnected ? 0 : DoorsStrategy == EnteranceStrategy.Tree ? 1 : -1;

            // ====== spawn Door tags
            // sort candidates by spatial hash, do not take to attention Y
            var spawnedDoor = new HashSet<(int y, Room from, Room to)>();
            var doorIsSpawnedForRoom = new HashSet<(int y, Room room)>();
            foreach (var candidate in candidates.OrderBy(c => Rnd.SpatialInt(c.Edge.Index.x, c.Edge.Index.z, c.Edge.Seed)))
            {
                var levelY = candidate.Edge.Index.y;
                if (!spawnedDoor.Add((levelY, candidate.FromRoom, candidate.ToRoom)))
                    continue;// door is spawned already between these rooms on this level
                var noDoorsInRoom = doorIsSpawnedForRoom.Add((levelY, candidate.ToRoom));
                noDoorsInRoom = doorIsSpawnedForRoom.Add((levelY, candidate.FromRoom)) || noDoorsInRoom;

                if (isTree)
                if (!allowedPassages.Contains((levelY, candidate.FromRoom, candidate.ToRoom)) && !allowedPassages.Contains((levelY, candidate.ToRoom, candidate.FromRoom)))
                {
                    var shouldBeMultipleDoors = IsMultiple(candidate.FromRoom) || IsMultiple(candidate.ToRoom);
                    if (!shouldBeMultipleDoors)
                        continue;
                }

                candidate.Edge.Tags = WallTags.Door;
            }

            bool IsMultiple(Room room)
            {
                if (room.Features.DoorsStrategy == DoorStrategy.Inherited)
                    return DoorsStrategy == EnteranceStrategy.FullyConnected;
                return room.Features.DoorsStrategy == DoorStrategy.FullyConnected;
            }
        }

        struct Candidate
        {
            public readonly QubicEdge Edge;
            public readonly Room FromRoom;
            public readonly Room ToRoom;

            public Candidate(QubicEdge edge, Room fromRoom, Room toRoom)
            {
                Edge = edge;
                var swap = fromRoom.InstanceIndex < toRoom.InstanceIndex;
                FromRoom = swap ? toRoom : fromRoom;
                ToRoom = swap ? fromRoom : toRoom;
            }
        }

        #region Graph and Tree utils

        static Dictionary<Room, List<Room>> BuildGraph(IEnumerable<(Room, Room)> edges)
        {
            var graph = new Dictionary<Room, List<Room>>();
            foreach (var (u, v) in edges)
            {
                if (!graph.ContainsKey(u)) graph[u] = new List<Room>();
                if (!graph.ContainsKey(v)) graph[v] = new List<Room>();
                graph[u].Add(v);
                graph[v].Add(u);
            }
            return graph;
        }

        static HashSet<(Room, Room)> BuildForest(Dictionary<Room, List<Room>> graph, EnteranceStrategy type)
        {
            var treeEdges = new HashSet<(Room, Room)>();
            var visited = new HashSet<Room>();

            var sign = type == EnteranceStrategy.Labyrinth ? -1 : 1;
            var allNodes = graph.Keys.OrderBy(r => r.InstanceIndex).ToList();

            foreach (var node in allNodes)
            {
                if (visited.Contains(node))
                    continue;

                if (type == EnteranceStrategy.Tree)
                {
                    var queue = new Queue<Room>();
                    visited.Add(node);
                    queue.Enqueue(node);

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        foreach (var neighbor in graph[current])
                        {
                            if (!visited.Contains(neighbor))
                            {
                                visited.Add(neighbor);
                                treeEdges.Add((current, neighbor));
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
                else if (type == EnteranceStrategy.Labyrinth)
                {
                    void DFS(Room current, Room parent)
                    {
                        visited.Add(current);
                        if (parent != null)
                            treeEdges.Add((parent, current));

                        foreach (var neighbor in graph[current])
                        {
                            if (!visited.Contains(neighbor))
                                DFS(neighbor, current);
                        }
                    }

                    DFS(node, null);
                }
            }

            return treeEdges;
        }

        #endregion

    }

    [Serializable]
    public class DoorList
    {
        public Room[] Allowed;
    }

    [Serializable]
    public enum EnteranceStrategy
    {
        Tree = 0,
        Labyrinth = 1,
        FullyConnected = 2,
    }
}