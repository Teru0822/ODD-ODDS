using System;
using System.Collections.Generic;
using UnityEngine;

namespace QubicNS
{
    public class Pool
    {
        Dictionary<GameObject, Queue<GameObject>> prefabToSpawned = new Dictionary<GameObject, Queue<GameObject>>();
        Dictionary<GameObject, GameObject> spawnedToPrefab = new Dictionary<GameObject, GameObject>();
        Transform holder;

        public void Reset(Transform holder)
        {
            this.holder = holder;

            prefabToSpawned.Clear();

            // remove all unknown objects from holder
            for (int i = holder.childCount - 1; i >= 0 ; i--)
            {
                var obj = holder.GetChild(i).gameObject;
                if (!spawnedToPrefab.TryGetValue(obj, out var prefab))
                {
                    // remove unknown object
                    Helper.DestroySafe(obj);
                    continue;
                }

                // save to reuse
                var list = prefabToSpawned.GetOrCreate(prefab);
                list.Enqueue(obj);
            }

            spawnedToPrefab.Clear();
        }

        public void RemoveUnused()
        {
            foreach(var pair in prefabToSpawned)
            {
                while (pair.Value.Count > 0)
                    Helper.DestroySafe(pair.Value.Dequeue());
            }
        }

        public GameObject GetOrCreate(GameObject prefab)
        {
            GameObject obj;

            // try reuse spawned objects
            if (prefabToSpawned.TryGetValue(prefab, out var spawnedList) && spawnedList.Count > 0)
            {
                obj = spawnedList.Dequeue();
            }
            else
            {
                // Instantiate
                obj = null;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                if (UnityEditor.PrefabUtility.GetPrefabAssetType(prefab) != UnityEditor.PrefabAssetType.NotAPrefab)
                    obj = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, holder) as GameObject;
#endif
                if (obj == null)
                    obj = GameObject.Instantiate(prefab, holder);
            }

            spawnedToPrefab[obj] = prefab;

            return obj;
        }
    }
}