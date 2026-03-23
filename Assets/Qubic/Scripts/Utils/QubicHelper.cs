using QubicNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;

namespace QubicNS
{
    public static class QubicHelper
    {
#if UNITY_EDITOR
        public static IEnumerable<Type> GetAllDerivedTypesInEditor<T>()
        {
            return UnityEditor.TypeCache.GetTypesDerivedFrom<T>();
        }
#endif

        public static void CountingSort<T>(this List<T> items, Func<T, int> keySelector, int minKey, int maxKey, bool byDescending = false)
        {
            var backetsCount = maxKey - minKey + 1;
            var buckets = new List<T>[backetsCount];

            // Distribute into baskets
            for ( int i = 0; i < items.Count; i++)
            {
                // get sorting key
                var item = items[i];
                int key = keySelector(item) - minKey;

                // Get the list from the pool only if it is needed
                var bucket = buckets[key];
                if (bucket == null)
                    bucket = buckets[key] = ListPool<T>.Get();

                bucket.Add(item);
            }

            // Collect the result and return the lists back to the pool
            items.Clear();
            for (int i = 0; i < backetsCount; i++)
            {
                var bucket = byDescending ? buckets[backetsCount - i - 1] : buckets[i];
                if (bucket != null)
                {
                    items.AddRange(bucket);
                    ListPool<T>.Release(bucket);
                }
            }
        }

        public static string TrimEnd(this string source, string value)
        {
            if (source != null && value != null && source.EndsWith(value))
                return source.Substring(0, source.Length - value.Length);
            return source;
        }

        public static int[] Primes = new[] { 1009, 1013, 1019, 1021, 1031, 1033, 1039, 1049, 1051, 1061, 1063, 1069, 1087, 1091, 1093, 1097, 1103, 1109, 1117, 1123, 1129, 1151, 1153, 1163, 1171, 1181, 1187, 1193, 1201, 1213, 1217, 1223, 1229, 1231, 1237, 1249, 1259, 1277, 1279, 1283, 1289, 1291, 1297, 1301, 1303, 1307, 1319, 1321, 1327, 1361, 1367, 1373, 1381, 1399, 1409, 1423, 1427, 1429, 1433, 1439, 1447, 1451, 1453, 1459, 1471, 1481, 1483, 1487, 1489, 1493, 1499, 1511, 1523, 1531, 1543, 1549, 1553, 1559, 1567, 1571, 1579, 1583, 1597, 1601, 1607, 1609, 1613, 1619, 1621, 1627, 1637, 1657, 1663, 1667, 1669, 1693, 1697, 1699, 1709, 1721, 1723, 1733, 1741, 1747, 1753, 1759, 1777, 1783, 1787, 1789, 1801, 1811, 1823, 1831, 1847, 1861, 1867, 1871, 1873, 1877, 1879, 1889, 1901, 1907, 1913, 1931, 1933, 1949, 1951, 1973, 1979, 1987, 1993, 1997, 1999, 2003, 2011, 2017, 2027, 2029, 2039, 2053, 2063, 2069, 2081, 2083, 2087, 2089, 2099, 2111, 2113, 2129, 2131, 2137, 2141, 2143, 2153, 2161, 2179, 2203, 2207, 2213, 2221, 2237, 2239, 2243, 2251, 2267, 2269, 2273, 2281, 2287, 2293, 2297, 2309, 2311, 2333, 2339, 2341, 2347, 2351, 2357, 2371, 2377, 2381, 2383, 2389, 2393, 2399, 2411, 2417, 2423, 2437, 2441, 2447, 2459, 2467, 2473, 2477, 2503, 2521, 2531, 2539, 2543, 2549, 2551, 2557, 2579, 2591, 2593, 2609, 2617, 2621, 2633, 2647, 2657, 2659, 2663, 2671, 2677, 2683, 2687, 2689, 2693, 2699, 2707, 2711, 2713, 2719, 2729, 2731, 2741, 2749, 2753, 2767, 2777, 2789, 2791, 2797, 2801, 2803, 2819, 2833, 2837, 2843, 2851, 2857, 2861, 2879, 2887, 2897, 2903, 2909, 2917, 2927, 2939, 2953, 2957, 2963, 2969, 2971, 2999, 3001, 3011, 3019, 3023, 3037, 3041, 3049, 3061, 3067, 3079, 3083, 3089, 3109, 3119, 3121, 3137, 3163, 3167, 3169, 3181, 3187, 3191, 3203, 3209, 3217, 3221, 3229, 3251, 3253, 3257, 3259, 3271, 3299, 3301, 3307, 3313, 3319, 3323, 3329, 3331, 3343, 3347, 3359, 3361, 3371, 3373, 3389, 3391, 3407, 3413, 3433, 3449, 3457, 3461, 3463, 3467, 3469, 3491, 3499, 3511, 3517, 3527, 3529, 3533, 3539, 3541, 3547, 3557, 3559, 3571, 3581, 3583, 3593, 3607, 3613, 3617, 3623, 3631, 3637, 3643, 3659, 3671, 3673, 3677, 3691, 3697, 3701, 3709, 3719, 3727, 3733, 3739, 3761, 3767, 3769, 3779, 3793, 3797, 3803, 3821, 3823, 3833, 3847, 3851, 3853, 3863, 3877, 3881, 3889, 3907, 3911, 3917, 3919, 3923, 3929, 3931, 3943, 3947, 3967, 3989, 4001, 4003, 4007, 4013, 4019, 4021, 4027, 4049, 4051, 4057, 4073, 4079, 4091, 4093, 4099, 4111, 4127, 4129, 4133, 4139, 4153, 4157, 4159, 4177, 4201, 4211, 4217, 4219, 4229, 4231, 4241, 4243, 4253, 4259, 4261, 4271, 4273, 4283, 4289, 4297, 4327, 4337, 4339, 4349, 4357, 4363, 4373, 4391, 4397, 4409, 4421, 4423, 4441, 4447, 4451, 4457, 4463, 4481, 4483, 4493, 4507, 4513, 4517, 4519, 4523, 4547, 4549, 4561, 4567, 4583, 4591, 4597, 4603, 4621, 4637, 4639, 4643, 4649, 4651, 4657, 4663, 4673, 4679, 4691, 4703, 4721, 4723, 4729, 4733, 4751, 4759, 4783, 4787, 4789, 4793, 4799, 4801, 4813, 4817, 4831, 4861, 4871, 4877, 4889, 4903, 4909, 4919, 4931, 4933, 4937, 4943, 4951, 4957, 4967, 4969, 4973, 4987, 4993, 4999, 5003, 5009, 5011, 5021, 5023, 5039, 5051, 5059, 5077, 5081, 5087, 5099, 5101, 5107, 5113, 5119, 5147, 5153, 5167, 5171, 5179, 5189, 5197, 5209, 5227, 5231, 5233, 5237, 5261, 5273, 5279, 5281, 5297, 5303, 5309, 5323, 5333, 5347, 5351, 5381, 5387, 5393, 5399, 5407, 5413, 5417, 5419, 5431, 5437, 5441, 5443, 5449, 5471, 5477, 5479, 5483, 5501, 5503, 5507, 5519, 5521, 5527, 5531, 5557, 5563, 5569, 5573, 5581, 5591, 5623, 5639, 5641, 5647, 5651, 5653, 5657, 5659, 5669, 5683, 5689, 5693, 5701, 5711, 5717, 5737, 5741, 5743, 5749, 5779, 5783, 5791, 5801, 5807, 5813, 5821, 5827, 5839, 5843, 5849, 5851, 5857, 5861, 5867, 5869, 5879, 5881, 5897, 5903, 5923, 5927, 5939, 5953, 5981, 5987, 6007, 6011, 6029, 6037, 6043, 6047, 6053, 6067, 6073, 6079, 6089, 6091, 6101, 6113, 6121, 6131, 6133, 6143, 6151, 6163, 6173, 6197, 6199, 6203, 6211, 6217, 6221, 6229, 6247, 6257, 6263, 6269, 6271, 6277, 6287, 6299, 6301, 6311, 6317, 6323, 6329, 6337, 6343, 6353, 6359, 6361, 6367, 6373, 6379, 6389, 6397, 6421, 6427, 6449, 6451, 6469, 6473, 6481, 6491, 6521, 6529, 6547, 6551, 6553, 6563, 6569, 6571, 6577, 6581, 6599, 6607, 6619, 6637, 6653, 6659, 6661, 6673, 6679, 6689, 6691, 6701, 6703, 6709, 6719, 6733, 6737, 6761, 6763, 6779, 6781, 6791, 6793, 6803, 6823, 6827, 6829, 6833, 6841, 6857, 6863, 6869, 6871, 6883, 6899, 6907, 6911, 6917, 6947, 6949, 6959, 6961, 6967, 6971, 6977, 6983, 6991, 6997, 7001, 7013, 7019, 7027, 7039, 7043, 7057, 7069, 7079, 7103, 7109, 7121, 7127, 7129, 7151, 7159, 7177, 7187, 7193, 7207, 7211, 7213, 7219, 7229, 7237, 7243, 7247, 7253, 7283, 7297, 7307, 7309, 7321, 7331, 7333, 7349, 7351, 7369, 7393, 7411, 7417, 7433, 7451, 7457, 7459, 7477, 7481, 7487, 7489, 7499, 7507, 7517, 7523, 7529, 7537, 7541, 7547, 7549, 7559, 7561, 7573, 7577, 7583, 7589, 7591, 7603, 7607, 7621, 7639, 7643, 7649, 7669, 7673, 7681, 7687, 7691, 7699, 7703, 7717, 7723, 7727, 7741, 7753, 7757, 7759, 7789, 7793, 7817, 7823, 7829, 7841, 7853, 7867, 7873, 7877, 7879, 7883, 7901, 7907, 7919 };
        public static Vector3Int[] MagicNumbers =
        {
        new Vector3Int(1,  1,  1),//0 - do not use
        new Vector3Int(1,  1,  1),//1 - do not use
        new Vector3Int(1,  1,  1),//for 2
        new Vector3Int(1,  1,  1),//for 3
        new Vector3Int(2,  1,  3),//for 4
        new Vector3Int(2,  1,  3),//for 5
        new Vector3Int(3,  1,  5),//for 6
        new Vector3Int(4,  3,  5),//for 7
        new Vector3Int(4,  3,  5),//for 8
        new Vector3Int(5,  2,  7),//for 9
        new Vector3Int(5,  3,  7)};//for 10

        public static int GetMagicSpatialHash(Vector3Int pos, int seed, int distance)
        {
            var magic = QubicHelper.MagicNumbers[distance % 11];
            var hash = (pos.x * magic.y + pos.z * magic.z + seed) % (distance * magic.x);
            return hash;
        }

        public static int GetDecimationSpatialHash(Vector3Int pos, int seed, int distance)
        {
            var hash = (pos.x + pos.z + seed) % distance;
            return hash;
        }

        public static string GetInspectorTitleAttribute(this Type type) => type.GetCustomAttribute<InspectorTitleAttribute>()?.Name ?? type.Name.ToReadableFormat();
        public static string GetInspectorTitleAttribute(this MethodInfo method) => method.GetCustomAttribute<InspectorTitleAttribute>()?.Name ?? method.Name.ToReadableFormat();

        static List<int> rndValues = new List<int>(23 * 23);

        static QubicHelper()
        {
            var rnd = new Rnd(23);
            for (int i = 0; i < rndValues.Capacity; i++)
                rndValues.Add(rnd.Int());
        }

        public static int GetStableRandom(int x, int y)
        {
            unchecked
            {
                var a = x.Mod(23);
                var b = y.Mod(23);
                return rndValues[a * 23 + b] + a + b;
            }
        }

        public static string[] SplitAndTrim(this string strWithCommas)
        {
            if (strWithCommas.IsNullOrEmpty())
                return Array.Empty<string>();

            var res = strWithCommas.Split(',');
            for (int i = 0; i < res.Length; i++)
                res[i] = res[i].Trim();
            return res;
        }

        public static Bounds RotateY(this Bounds bounds, int rotationY90)
        {
            if (rotationY90 % 2 == 0)
                return bounds;
            var x = bounds.size.z;
            var y = bounds.size.y;
            var z = bounds.size.x;
            
            return new Bounds(bounds.center, new Vector3(x, y, z));
        }

        public static Bounds RotateY(this Bounds bounds, Vector3 pivot, int rotationY90)
        {
            rotationY90 = rotationY90.Mod(4);

            Vector3 centerOffset = bounds.center - pivot;
            Vector3 rotatedOffset = centerOffset;

            switch (rotationY90)
            {
                case 1:
                    rotatedOffset = new Vector3(-centerOffset.z, centerOffset.y, centerOffset.x);
                    break;
                case 2:
                    rotatedOffset = new Vector3(-centerOffset.x, centerOffset.y, -centerOffset.z);
                    break;
                case 3:
                    rotatedOffset = new Vector3(centerOffset.z, centerOffset.y, -centerOffset.x);
                    break;
            }

            Vector3 size = bounds.size;
            if (rotationY90 % 2 == 1)
            {
                size = new Vector3(size.z, size.y, size.x);
            }

            Vector3 newCenter = pivot + rotatedOffset;
            return new Bounds(newCenter, size);
        }

        public static Vector2Int ToVector2Int(this Vector3Int vecInt3) => new Vector2Int(vecInt3.x, vecInt3.z);

        public static float InverseDistance(this Vector3 v0, Vector3 v1)
        {
            return 1 / (1 + Mathf.Abs(v0.x - v1.x)) + 1 / (1 + Mathf.Abs(v0.y - v1.y)) + 1 / (1 + Mathf.Abs(v0.z - v1.z));
        }

        public static IEnumerable<Vector3Int> Neighbors8(this Vector3Int pos)
        {
            yield return new Vector3Int(pos.x - 1, pos.y, pos.z + 1);
            yield return new Vector3Int(pos.x, pos.y, pos.z + 1);
            yield return new Vector3Int(pos.x + 1, pos.y, pos.z + 1);
            yield return new Vector3Int(pos.x + 1, pos.y, pos.z);
            yield return new Vector3Int(pos.x + 1, pos.y, pos.z - 1);
            yield return new Vector3Int(pos.x, pos.y, pos.z - 1);
            yield return new Vector3Int(pos.x - 1, pos.y, pos.z - 1);
            yield return new Vector3Int(pos.x - 1, pos.y, pos.z);
        }

        public static IEnumerable<Vector3Int> Neighbors4(this Vector3Int pos)
        {
            yield return new Vector3Int(pos.x, pos.y, pos.z + 1);
            yield return new Vector3Int(pos.x + 1, pos.y, pos.z);
            yield return new Vector3Int(pos.x, pos.y, pos.z - 1);
            yield return new Vector3Int(pos.x - 1, pos.y, pos.z);
        }

        public static IEnumerable<Vector3Int> Neighbors4Diag(this Vector3Int pos)
        {
            yield return new Vector3Int(pos.x + 1, pos.y, pos.z + 1);
            yield return new Vector3Int(pos.x + 1, pos.y, pos.z - 1);
            yield return new Vector3Int(pos.x - 1, pos.y, pos.z - 1);
            yield return new Vector3Int(pos.x - 1, pos.y, pos.z + 1);
        }

        public static IEnumerable<Vector3Int> Neighbors6(this Vector3Int pos)
        {
            yield return new Vector3Int(pos.x, pos.y, pos.z + 1);
            yield return new Vector3Int(pos.x + 1, pos.y, pos.z);
            yield return new Vector3Int(pos.x, pos.y, pos.z - 1);
            yield return new Vector3Int(pos.x - 1, pos.y, pos.z);
            yield return new Vector3Int(pos.x, pos.y + 1, pos.z);
            yield return new Vector3Int(pos.x, pos.y - 1, pos.z);
        }

        public static readonly EdgeCornerShape[] EdgeCornerLookup = new EdgeCornerShape[16]
        {
            EdgeCornerShape.o, // 0000 (no neighbors)
            EdgeCornerShape.i,    // 0001 (top only)
            EdgeCornerShape.i,    // 0010 (right only)
            EdgeCornerShape.L,    // 0011 (top and right)
            EdgeCornerShape.i,    // 0100 (bottom only)
            EdgeCornerShape.I,    // 0101 (top and bottom)
            EdgeCornerShape.L,    // 0110 (right and bottom)
            EdgeCornerShape.T,    // 0111 (top, right, bottom)
            EdgeCornerShape.i,    // 1000 (left only)
            EdgeCornerShape.L,    // 1001 (top and left)
            EdgeCornerShape.I,    // 1010 (right and left)
            EdgeCornerShape.T,    // 1011 (top, right, left)
            EdgeCornerShape.L,    // 1100 (bottom and left)
            EdgeCornerShape.T,    // 1101 (top, bottom, left)
            EdgeCornerShape.T,    // 1110 (right, bottom, left)
            EdgeCornerShape.X     // 1111 (all sides)
        };

        public static readonly int[] EdgeCornerRotation = new int[16]
        {
            0,    // 0000 (no neighbors)
            0,    // 0001 (top only)
            3,    // 0010 (right only)
            0,    // 0011 (top and right)
            2,    // 0100 (bottom only)
            0,    // 0101 (top and bottom)
            3,    // 0110 (right and bottom)
            0,    // 0111 (top, right, bottom)
            1,    // 1000 (left only)
            1,    // 1001 (top and left)
            1,    // 1010 (right and left)
            1,    // 1011 (top, right, left)
            2,    // 1100 (bottom and left)
            2,    // 1101 (top, bottom, left)
            3,    // 1110 (right, bottom, left)
            0     // 1111 (all sides)
        };

        public static bool Intersects(this CornerType type, ushort mask)
            => ((ushort)type & mask) != 0;

        public static CornerType Strongest(this CornerType type)
        {
            if ((type & CornerType.X) != 0) return CornerType.X;
            if ((type & CornerType.T) != 0) return CornerType.T;
            return type;
        }

        public static IEnumerable<Vector3Int> Neighbors(this Vector3Int pos, int width, int height, bool includeMe = false)
        {
            width--;
            height--;
            var fromX = -width / 2;
            var toX = Mathf.CeilToInt(width / 2f);
            var fromY = -height / 2;
            var toY = Mathf.CeilToInt(height / 2f);
            for (int i = fromX; i <= toX; i++)
            for (int j = fromY; j <= toY; j++)
            if (i != 0 || j != 0 || includeMe)
                yield return new Vector3Int(pos.x + i, pos.y, pos.z + j);
        }

        public static IEnumerable<Vector3Int> Neighbors(this Vector3Int pos, RectOffset offests)
        {
            var fromX = pos.x - offests.left;
            var toX = pos.x + offests.right;
            var fromZ = pos.z - offests.bottom;
            var toZ = pos.z + offests.top;
            for (int i = fromX; i <= toX; i++)
            for (int j = fromZ; j <= toZ; j++)
                yield return new Vector3Int(i, pos.y, j);
        }

        public static IEnumerable<Vector3Int> Neighbors(this Vector3Int pos, int radius)
        {
            var fromX = pos.x - radius;
            var toX = pos.x + radius;
            var fromZ = pos.z - radius;
            var toZ = pos.z + radius;
            for (int i = fromX; i <= toX; i++)
            for (int j = fromZ; j <= toZ; j++)
                yield return new Vector3Int(i, pos.y, j);
        }

        public static Vector3Int XZ(this Vector3Int pos) => new Vector3Int(pos.x, 0, pos.z);

        public static int IndexOf<T>(this IList<T> list, Func<T, bool> condition)
        {
            for (int i = 0; i < list.Count; i++)
                if (condition(list[i]))
                    return i;
            return -1;
        }

        public static bool CheckCellType(this IList<string> CellTypes, string type)
        {
            if (CellTypes == null || CellTypes.Count == 0)
                return true;

            var hasPositive = false;

            foreach (var s in CellTypes)
            {
                if (s.Length > 0 && s.StartsWith('-'))
                {
                    if (type == s.Substring(1))
                        return false;
                }
                else
                    hasPositive = true;
            }

            if (hasPositive)
                return CellTypes.Contains(type);

            return true;
        }

        public static IEnumerable<(Vector3Int cell, Vector3Int dir)> GetOutsideEdges(this IEnumerable<Vector3Int> cells)
        {
            foreach (var cell in cells)
                foreach (var n in cell.Neighbors4())
                    if (!cells.Contains(n))
                        yield return (cell, n - cell);
        }
        public static IEnumerable<(Vector3Int cell, Vector3Int dir)> GetInsideEdges(this IEnumerable<Vector3Int> cells)
        {
            foreach (var cell in cells)
                foreach (var n in cell.Neighbors4().Take(2))
                    if (cells.Contains(n))
                        yield return (cell, n - cell);
        }

        public static (Vector3Int cell, Vector3Int dir) GetNearestEdge(this Map map, Vector3 pos)
        {
            var cell = map.PosToCell(pos);
            var center = map.CellToPos(cell);
            var d = pos - center;
            var dir = Math.Abs(d.x) > Math.Abs(d.z) ? new Vector3Int(Math.Sign(d.x), 0, 0) : new Vector3Int(0, 0, Math.Sign(d.z));
            return (cell, dir);
        }

        public static bool IsEqual(this (Vector3Int pos, Vector3Int dir) edge0, (Vector3Int pos, Vector3Int dir) edge1)
        {
            return edge0.pos * 2 + edge0.dir == edge1.pos * 2 + edge1.dir;
        }

        public static bool IsWall(this Vector3Int dir) => dir.sqrMagnitude == 1;
        public static bool IsFloor(this Vector3Int dir) => dir.y % 2 != 0;
        public static bool IsCell(this Vector3Int index) => (index / 2) * 2 == index;

        public static (Vector3Int from, Vector3Int to) EdgeToCells(Vector3Int edge)
        {
            var cell0 = edge / 2;
            var cell1 = edge - cell0;
            return (cell0, cell1);
        }

        public static Vector3Int EdgeToCell(Vector3Int edge) => new Vector3Int(Mathf.FloorToInt(edge.x / 2f), Mathf.FloorToInt(edge.y / 2f), Mathf.FloorToInt(edge.z / 2f));
        public static Vector3Int EdgeToAdjCell(Vector3Int edge) => new Vector3Int(Mathf.CeilToInt(edge.x / 2f), Mathf.CeilToInt(edge.y / 2f), Mathf.CeilToInt(edge.z / 2f));

        public static Vector3Int GetNormal(Vector3Int fromCell, Vector3Int toCell)
        {
            var dir = toCell - fromCell;
            return new Vector3Int(dir.z, 0, -dir.x);
        }

        static Vector3Int[] rotations = new[] { Vector3Int.zero, Vector3Int.forward, Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.up, Vector3Int.down };
        static Side[] inverts = new[] { Side.Any, Side.Back, Side.Left, Side.Forward, Side.Right, Side.Down, Side.Up };

        public static Vector3Int ToDirection(this Side orientation)
        {
            return rotations[(int)orientation];
        }

        public static Side ToSide(this Vector3Int dir)
        {
            if (dir.z > 0) return Side.Forward;
            if (dir.z < 0) return Side.Back;
            if (dir.x > 0) return Side.Right;
            if (dir.x < 0) return Side.Left;
            if (dir.y > 0) return Side.Up;
            if (dir.y < 0) return Side.Down;

            return Side.Any;
        }

        public static Side Rotate(this Side side, int rotation)
        {
            if (side < Side.Forward || side > Side.Left)
                return (Side)255;

            return (Side)(1 + ((int)side + 3 - rotation) % 4);
        }

        public static Side Invert(this Side side) => inverts[(int)side];

        public static BoundsInt GetBoundsInt(this IEnumerable<Vector3Int> points)
        {
            if (points == null) return new BoundsInt(Vector3Int.zero, Vector3Int.zero);

            using (var enumerator = points.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return new BoundsInt(Vector3Int.zero, Vector3Int.zero);

                // Инициализируем минимальные и максимальные координаты первой точкой
                Vector3Int first = enumerator.Current;
                int minX = first.x, minY = first.y, minZ = first.z;
                int maxX = first.x, maxY = first.y, maxZ = first.z;

                // Обходим остальные точки
                while (enumerator.MoveNext())
                {
                    Vector3Int p = enumerator.Current;
                    minX = Mathf.Min(minX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    minZ = Mathf.Min(minZ, p.z);

                    maxX = Mathf.Max(maxX, p.x);
                    maxY = Mathf.Max(maxY, p.y);
                    maxZ = Mathf.Max(maxZ, p.z);
                }

                // Рассчитываем позицию и размер BoundsInt
                Vector3Int position = new Vector3Int(minX, minY, minZ);
                Vector3Int size = new Vector3Int(maxX - minX + 1, maxY - minY + 1, maxZ - minZ + 1);

                return new BoundsInt(position, size);
            }
        }

        public static Vector3Int RotateY(this Vector3Int v, int angleIn90)
        {
            int normalizedAngle = ((angleIn90 % 4) + 4) % 4;

            return normalizedAngle switch
            {
                1 => new Vector3Int(v.z, v.y, -v.x),
                2 => new Vector3Int(-v.x, v.y, -v.z),
                3 => new Vector3Int(-v.z, v.y, v.x),
                _ => v
            };
        }

        public static RectOffset Rotate(this RectOffset rect, int angleIn90)
        {
            int normalizedAngle = ((angleIn90 % 4) + 4) % 4;

            return normalizedAngle switch
            {
                1 => new RectOffset(rect.bottom, rect.top, rect.left, rect.right),
                2 => new RectOffset(rect.right, rect.left, rect.bottom, rect.top),
                3 => new RectOffset(rect.top, rect.bottom, rect.right, rect.left),
                _ => rect
            };
        }

        public static readonly Quaternion[] Rotations = new Quaternion[]
        {
                Quaternion.identity, // No rotation
                Quaternion.Euler(0, 90, 0),
                Quaternion.Euler(0, 180, 0),
                Quaternion.Euler(0, 270, 0),

                Quaternion.Euler(90, 0, 0),
                Quaternion.Euler(90, 90, 0),
                Quaternion.Euler(90, 180, 0),
                Quaternion.Euler(90, 270, 0),

                Quaternion.Euler(180, 0, 0),
                Quaternion.Euler(180, 90, 0),
                Quaternion.Euler(180, 180, 0),
                Quaternion.Euler(180, 270, 0),

                Quaternion.Euler(270, 0, 0),
                Quaternion.Euler(270, 90, 0),
                Quaternion.Euler(270, 180, 0),
                Quaternion.Euler(270, 270, 0),

                Quaternion.Euler(0, 0, 90),
                Quaternion.Euler(0, 0, 270),
                Quaternion.Euler(90, 0, 90),
                Quaternion.Euler(90, 0, 270),
                Quaternion.Euler(270, 0, 90),
                Quaternion.Euler(270, 0, 270),
                Quaternion.Euler(180, 0, 90),
                Quaternion.Euler(180, 0, 270)
        };

        public static Vector3Int[] Dirs4 = new[] { Vector3Int.forward, Vector3Int.right, Vector3Int.back, Vector3Int.left };
        public static Quaternion[] Quaternions4 = new[] { Quaternion.Euler(0, 0, 0), Quaternion.Euler(0, 90, 0), Quaternion.Euler(0, 180, 0), Quaternion.Euler(0, 270, 0) };

        public static int DirToInt(this Vector3Int dir)
        {
            for (int i = 0; i < Dirs4.Length; i++)
            if (dir == Dirs4[i])
                return i;
            return -1;
        }

        public static Quaternion RotationFromDirection(Vector3Int dir)
        {
            if (dir == Vector3Int.forward) return Quaternion.identity;
            if (dir == Vector3Int.back) return Quaternion.Euler(0, 180, 0);
            if (dir == Vector3Int.left) return Quaternion.Euler(0, -90, 0);
            if (dir == Vector3Int.right) return Quaternion.Euler(0, 90, 0);
            return Quaternion.identity;
        }

        public static GameObject GetHigherInHierarchy(GameObject objA, GameObject objB)
        {
            GameObject rootA = objA.transform.root.gameObject;
            GameObject rootB = objB.transform.root.gameObject;

            if (rootA == rootB)
            {
                if (IsParentOf(objA.transform, objB.transform)) return objA;
                if (IsParentOf(objB.transform, objA.transform)) return objB;
            }

            // Собираем все загруженные сцены и их корневые объекты
            List<GameObject> allRootObjects = new List<GameObject>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                allRootObjects.AddRange(scene.GetRootGameObjects());
            }

            int indexA = allRootObjects.IndexOf(rootA);
            int indexB = allRootObjects.IndexOf(rootB);

            if (indexA == -1 || indexB == -1)
            {
                return null;
            }

            return indexA < indexB ? objA : objB;
        }

        private static bool IsParentOf(Transform potentialParent, Transform child)
        {
            Transform current = child.parent;
            while (current != null)
            {
                if (current == potentialParent)
                    return true;
                current = current.parent;
            }
            return false;
        }

        public static Bounds GetBoundsFromPrefab(GameObject prefab, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab is null");
                return new Bounds();
            }

            GameObject instance = GameObject.Instantiate(prefab);
            if (instance == null)
            {
                return new Bounds();
            }

            instance.transform.position = Vector3.zero;
            instance.transform.rotation = rotation * instance.transform.rotation;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("No Renderers in object");
                GameObject.DestroyImmediate(instance);
                return new Bounds();
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            //GameObject.DestroyImmediate(instance);

            return bounds;
        }

        public static UInt64 AnyTags => 0xffffffffffffffff;

        //public static Bounds RotateBounds(Bounds bounds, Vector3 position, Quaternion rotation)
        //{
        //    var center = position + rotation * bounds.center;
        //    var size = rotation * bounds.size;
        //    if (size.x < 0) size.x = -size.x;
        //    if (size.y < 0) size.y = -size.y;
        //    if (size.z < 0) size.z = -size.z;
        //    return new Bounds(center, size);
        //}

        public static Bounds RotateBounds(Bounds bounds, Vector3 pos, Quaternion rotation)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Vector3[] vertices = new Vector3[8];

            vertices[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            vertices[1] = center + new Vector3(-extents.x, -extents.y, extents.z);
            vertices[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
            vertices[3] = center + new Vector3(-extents.x, extents.y, extents.z);
            vertices[4] = center + new Vector3(extents.x, -extents.y, -extents.z);
            vertices[5] = center + new Vector3(extents.x, -extents.y, extents.z);
            vertices[6] = center + new Vector3(extents.x, extents.y, -extents.z);
            vertices[7] = center + new Vector3(extents.x, extents.y, extents.z);

            for (int i = 0; i < 8; i++)
            {
                vertices[i] = rotation * (vertices[i] - center) + center;
            }

            Vector3 min = vertices[0];
            Vector3 max = vertices[0];

            for (int i = 1; i < 8; i++)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }

            return new Bounds(pos + (min + max) * 0.5f, max - min);
        }

        public static Bounds RotateBounds90(Bounds bounds, Vector3 pos, Quaternion rotation)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            Vector3 newSize = size;
            Vector3 newCenter = center;

            Vector3 euler = rotation.eulerAngles;
            int xRot = Mathf.RoundToInt(euler.x / 90f) * 90;
            int yRot = Mathf.RoundToInt(euler.y / 90f) * 90;
            int zRot = Mathf.RoundToInt(euler.z / 90f) * 90;

            if (xRot % 180 != 0)
                newSize = new Vector3(newSize.x, newSize.z, newSize.y);

            if (yRot % 180 != 0)
                newSize = new Vector3(newSize.z, newSize.y, newSize.x);

            if (zRot % 180 != 0)
                newSize = new Vector3(newSize.y, newSize.x, newSize.z);

            newCenter = pos + rotation * center;

            return new Bounds(newCenter, newSize);
        }

        public static Vector3Int GetClosestToCenter(IEnumerable<Vector3Int> cells)
        {
            if (cells == null || !cells.Any())
                throw new System.ArgumentException("Пустой набор ячеек");

            // 1. Считаем "центр масс"
            Vector3 average = Vector3.zero;
            int count = 0;

            foreach (var cell in cells)
            {
                average += cell;
                count++;
            }

            average /= count;

            // 2. Находим ближайшую к центру ячейку
            Vector3Int closest = default;
            float minDistanceSqr = float.MaxValue;

            foreach (var cell in cells)
            {
                float distSqr = (cell - average).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closest = cell;
                }
            }

            return closest;
        }

#if UNITY_EDITOR
        public static void CollapseHierarchy(GameObject go)
        {
            try
            {
                // Get internal UnityEditor.SceneHierarchyWindow
                var hierarchyWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                var window = UnityEditor.EditorWindow.GetWindow(hierarchyWindowType);

                var method = hierarchyWindowType.GetMethod("SetExpanded", BindingFlags.Instance | BindingFlags.NonPublic);

                if (method != null)
                {
                    method.Invoke(window, new object[] { go.GetInstanceID(), false }); // false to collapse
                }
                else
                {
                    Debug.LogWarning("Could not find SetExpandedRecursive method.");
                }
            }
            catch (Exception)
            {
            }
        }
#endif
    }

    [Serializable]
    public enum EdgeCornerShape : UInt16
    {
        None = CornerMasks.None,
        i = CornerMasks.End,
        I = CornerMasks.Straight,
        L = CornerMasks.GenericL,
        T = CornerMasks.T,
        X = CornerMasks.X,
        o = CornerMasks.o
    }
}