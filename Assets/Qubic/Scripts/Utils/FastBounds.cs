using System;
using System.Collections.Generic;
using UnityEngine;

namespace QubicNS
{
    public readonly struct FastBounds
    {
        public readonly float minX, maxX;
        public readonly float minY, maxY;
        public readonly float minZ, maxZ;
        public readonly ulong layerMask;

        public FastBounds(Bounds bounds, ulong layerMask)
        {
            minX = bounds.min.x;
            maxX = bounds.max.x;
            minY = bounds.min.y;
            maxY = bounds.max.y;
            minZ = bounds.min.z;
            maxZ = bounds.max.z;
            this.layerMask = layerMask;
        }

        public FastBounds(FastBounds bounds, float offsetY)
        {
            minX = bounds.minX;
            maxX = bounds.maxX;
            minY = bounds.minY + offsetY;
            maxY = bounds.maxY + offsetY;
            minZ = bounds.minZ;
            maxZ = bounds.maxZ;
            this.layerMask = bounds.layerMask;
        }
    }

    public class BoundsIntersectionChecker
    {
        private readonly List<FastBounds> boundsCollection;

        public BoundsIntersectionChecker(int initialCapacity = 16)
        {
            boundsCollection = new List<FastBounds>(initialCapacity);
        }

        public void AddBounds(FastBounds bounds) => boundsCollection.Add(bounds);
        public void AddBounds(FastBounds bounds, float offsetY) => boundsCollection.Add(new FastBounds(bounds, offsetY));

        public bool IntersectsAny(FastBounds target, float offsetY = 0)
        {
            var minY = target.minY + offsetY;
            var maxY = target.maxY + offsetY;

            for (int i = 0; i < boundsCollection.Count; i++)
            {
                FastBounds b = boundsCollection[i];

                if ((b.layerMask & target.layerMask) == 0) continue;
                if (target.minX > b.maxX) continue;
                if (target.maxX < b.minX) continue;
                if (minY > b.maxY) continue;
                if (maxY < b.minY) continue;
                if (target.minZ > b.maxZ) continue;
                if (target.maxZ < b.minZ) continue;

                return true;
            }

            return false;
        }

        public void Clear() => boundsCollection.Clear();

        public int Count => boundsCollection.Count;

        internal void CopyTo(BoundsIntersectionChecker debugBoundsChecker)
        {
            debugBoundsChecker.Clear();
            debugBoundsChecker.boundsCollection.AddRange(boundsCollection);
        }
    }
}