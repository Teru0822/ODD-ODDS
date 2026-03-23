using QubicNS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    [Serializable]
    public class Map : Vector3IntMap<QubicEdge>
    {
        public float CellSize = 4;
        public float CellHeight = 4;
        public Vector3 WorldPos;

        public override QubicEdge CreateNew(Vector3Int pos) => new QubicEdge() { Index = pos };

        Vector3 MulCellSize(Vector3 v) => new Vector3(v.x * CellSize, v.y * CellHeight, v.z * CellSize);
        Vector3 DivCellSize(Vector3 v) => new Vector3(v.x / CellSize, v.y / CellHeight, v.z / CellSize);

        public Vector3 AlignToCellCenter(Vector3 pos) => CellToPos(PosToCell(pos));

        public Vector3Int PosToCell(Vector3 pos)
        {
            return Vector3Int.FloorToInt(DivCellSize(pos - WorldPos));
        }

        public Vector3Int PosRoundToCell(Vector3 pos)
        {
            return Vector3Int.RoundToInt(DivCellSize(pos - WorldPos));
        }

        static Vector3 half = new Vector3(0.5f, 0, 0.5f);
        static Vector3 half2 = new Vector3(0.5f, 0.5f, 0.5f);

        public Vector3 CellToPos(Vector3Int cellIndex)
        {
            return new Vector3(CellSize * (cellIndex.x + 0.5f), CellHeight * cellIndex.y, CellSize * (cellIndex.z + 0.5f)) + WorldPos;
        }

        public Vector3 EdgeToPos(Vector3Int edgeIndex)
        {
            return new Vector3(CellSize * (edgeIndex.x + 1) / 2f, CellHeight * Mathf.FloorToInt((edgeIndex.y + 1) / 2f), CellSize * (edgeIndex.z + 1) / 2f) + WorldPos;
        }

        public Vector3 EdgeToPosLocal(Vector3Int edgeIndex)
        {
            return new Vector3(CellSize * (edgeIndex.x + 1) / 2f, CellHeight * Mathf.FloorToInt((edgeIndex.y + 1) / 2f), CellSize * (edgeIndex.z + 1) / 2f);
        }
    }

    [Serializable]
    public enum LevelFilter
    {
        [InspectorName("Any")]
        Any = 0,
        [InspectorName("Any (include Roof)")]
        AnyInclRoof = 6,
        BasementAndBelow = 8,
        First = 1,
        [InspectorName("First and Above")]
        FirstAndAbove = 9,
        [InspectorName("Above First")]
        AboveFirst = 2,
        [InspectorName("Above First (include Roof)")]
        AboveFirstInclRoof = 11,
        AboveFirstUnderLast = 7,
        UnderLast = 4,
        Last = 3,
        Roof = 5,
        None = 10,
    }

    [Serializable]
    public enum Side : byte
    {
        Any = 0, Forward = 1, Right = 2, Back = 3, Left = 4, Up = 5, Down = 6
    }

    [Serializable]
    public enum SideInGUI : byte
    {
        Any, Front = 1, Right = 2, Back = 3, Left = 4
    }
}