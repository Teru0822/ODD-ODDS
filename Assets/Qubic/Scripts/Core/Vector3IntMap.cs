using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace QubicNS
{
    /// <summary> Fast dictionary for Vector3Int key  </summary>
    public class Vector3IntMap<T> : IVector3IntMap<T> where T : new()
    {
        private const long OFFSET = 1L << 20; // offset, to work with negative coordinates
        private readonly Dictionary<long, T> data = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Encode(Vector3Int v)
        {
            return ((v.x + OFFSET) << 42) | ((long)(v.y + OFFSET) << 21) | (v.z + OFFSET);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3Int Decode(long key)
        {
            int z = (int)(key & ((1 << 21) - 1)) - (int)OFFSET;
            int y = (int)((key >> 21) & ((1 << 21) - 1)) - (int)OFFSET;
            int x = (int)((key >> 42) & ((1 << 21) - 1)) - (int)OFFSET;
            return new Vector3Int(x, y, z);
        }

        public virtual T CreateNew(Vector3Int pos) => new T();

        public T this[Vector3Int pos]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var key = Encode(pos);
                return data.TryGetValue(key, out var value) ? value : data[key] = CreateNew(pos);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => data[Encode(pos)] = value;
        }

        public bool TryGetValue(Vector3Int pos, out T val) => data.TryGetValue(Encode(pos), out val);
        public bool Contains(Vector3Int pos) => data.ContainsKey(Encode(pos));
        public bool Remove(Vector3Int pos) => data.Remove(Encode(pos));
        public void Clear() => data.Clear();
        public IEnumerable<KeyValuePair<Vector3Int, T>> KeyValues => data.Select(pair => new KeyValuePair<Vector3Int, T>(Decode(pair.Key), pair.Value));
        public IEnumerable<Vector3Int> Keys => data.Keys.Select(k => Decode(k));
        public IEnumerable<T> Values => data.Values;
        public int Count => data.Count;
        public void Add(Vector3Int key, T value) => data.Add(Encode(key), value);

        #region Sync Serialized 
        bool isSynced = false;

        [Serializable]
        public class Pair
        {
            public Vector3Int Key;
            public T Value;
        }

        [SerializeField, HideInInspector] List<Pair> cellsList = new List<Pair>();

        public void SyncSerialized()
        {
            cellsList.Clear();
            cellsList.AddRange(this.KeyValues.Select(kvp => new Pair { Key = kvp.Key, Value = kvp.Value }));
            isSynced = true;
        }

        public void LoadSerialized()
        {
            if (isSynced) return;
            isSynced = true;
            Clear();
            foreach (var pair in cellsList)
                this.Add(pair.Key, pair.Value);
        }
        #endregion
    }

    /// <summary> Fast hashset Vector3Int </summary>
    public class Vector3IntSet : IEnumerable<Vector3Int>
    {
        private const long Offset = 1L << 20; // offset, to work with negative coordinates
        private readonly HashSet<long> set = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Encode(Vector3Int v)
        {
            return ((v.x + Offset) << 42) | ((long)(v.y + Offset) << 21) | (v.z + Offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3Int Decode(long key)
        {
            int z = (int)(key & ((1 << 21) - 1)) - (int)Offset;
            int y = (int)((key >> 21) & ((1 << 21) - 1)) - (int)Offset;
            int x = (int)((key >> 42) & ((1 << 21) - 1)) - (int)Offset;
            return new Vector3Int(x, y, z);
        }

        public bool Add(Vector3Int v) => set.Add(Encode(v));
        public bool Remove(Vector3Int v) => set.Remove(Encode(v));
        public bool Contains(Vector3Int v) => set.Contains(Encode(v));
        public void Clear() => set.Clear();
        public int Count => set.Count;

        public void AddRange(IEnumerable<Vector3Int> collection) 
        {
            foreach (var v in collection)
                set.Add(Encode(v));
        }

        public IEnumerable<Vector3Int> Values() => set.Select(k => Decode(k));

        public IEnumerator<Vector3Int> GetEnumerator() => Values().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class Array3DMap<T> : IVector3IntMap<T>
    {
        private readonly T[,,] array;
        private readonly Vector3Int origin;

        public Array3DMap(Vector3Int size, Vector3Int origin = default)
        {
            this.array = new T[size.x, size.y, size.z];
            this.origin = origin;
        }

        public T this[Vector3Int pos]
        {
            get => array[
                pos.x - origin.x,
                pos.y - origin.y,
                pos.z - origin.z
            ];
            set => array[
                pos.x - origin.x,
                pos.y - origin.y,
                pos.z - origin.z
            ] = value;
        }
    }

    public interface IVector3IntMap<T>
    {
        T this[Vector3Int pos] { get; set; }
    }
}