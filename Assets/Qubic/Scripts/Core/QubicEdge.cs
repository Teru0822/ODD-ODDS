using System;
using UnityEngine;

namespace QubicNS
{
    [Serializable]
    public partial class QubicEdge
    {
        public Vector3Int Index;
        public ulong Tags;
        public int Seed;
        public Room Room;
        public Prefab SpawnedPrefab { get; private set; }
        public Prefab SpawnedPrefab0 { get; private set; }// one side prefab (from)
        public Prefab SpawnedPrefab1 { get; private set; }// one side prefab (to)
        public EdgeType Type;
        public bool IsSpawned => SpawnedMask != 0;
        public byte SpawnedMask;
        public QubicEdgeFlags Flags = 0;

        public void SetSpawned(Prefab prefab, Vector3Int from, Vector3Int to)
        {
            switch (prefab.WallFeatures.Side)
            {
                case SideWall.IsOneSide:
                    var near = Index / 2 == from;
                    if (near)
                    {
                        SpawnedPrefab0 = prefab;
                        SpawnedMask |= 0b001;
                    }
                    else
                    {
                        SpawnedPrefab1 = prefab;
                        SpawnedMask |= 0b010;
                    }
                    break;

                case SideWall.Default:
                    SpawnedPrefab = prefab;
                    SpawnedMask |= 0b100;
                    break;

                case SideWall.IsDoubleSide:
                    SpawnedPrefab = SpawnedPrefab0 = SpawnedPrefab1 = prefab;
                    SpawnedMask |= 0b111;
                    break;
            }
        }

        public Prefab GetSpawnedForPadding(Vector3Int from, Vector3Int to)
        {
            var near = Index / 2 == from;
            if (near)
                return SpawnedPrefab0 ?? SpawnedPrefab;
            else
                return SpawnedPrefab1 ?? SpawnedPrefab;
        }
    }

    public enum EdgeType : byte
    {
        Cell, Wall, Floor
    }

    [Flags]
    public enum QubicEdgeFlags : byte
    {
        NoContent = 1 << 0, // wall without content
        IsPartOfCorner = 1 << 1, // edge is part of corner
    }
}