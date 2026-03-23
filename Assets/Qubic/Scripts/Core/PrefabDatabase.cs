using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.1fnuh1pufqgr")]
    [CreateAssetMenu(fileName = "PrefabDatabase", menuName = "Qubic/PrefabDatabase")]
    public class PrefabDatabase : ScriptableObject
    {
        [Tooltip("Size of cell (meters). Usually this value is typical width of wall prefabs.")]
        public float CellSize = 2;
        [Tooltip("Height of cell (meters). Usually this value is typical height of wall prefabs.")]
        public float CellHeight = 3;
        public PrefabDatabaseFeatures Features = new PrefabDatabaseFeatures();

        [Space]
        public InspectorButton _CollapseAllFoldouts;

        public List<Prefab> Prefabs = new List<Prefab>();

        private void OnValidate()
        {
            if (Features == null)
                Features = new PrefabDatabaseFeatures();

            foreach (var p in Prefabs)
                p.OnValidate();
        }

        public bool FindPrefab(GameObject sourcePrefab, out Prefab prefab, out int prefabIndex)
        {
            prefab = default(Prefab);
            prefabIndex = -1;
            for (int i = 0; i < Prefabs.Count; i++)
            {
                if (Prefabs[i].PrefabInfo?.Prefab == sourcePrefab)
                {
                    prefab = Prefabs[i];
                    prefabIndex = i;
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        void CollapseAllFoldouts()
        {
            Prefab.collapseRequestTime = DateTime.Now;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    [Serializable]
    public class PrefabDatabaseFeatures
    {
        public const float PADDING = 0.1f;//do not change this

        [Tooltip("Default content padding for Content Walls, in meters.")]
        public float DefaultContentPadding = PADDING;
        [Tooltip("The factor that is used to determine whether an object will spawn on a column, along X, in fractions of CellSize, from 0 to 1.")]
        public float ColumnCoeff = 0.1f;
        [Tooltip("A coefficient that is used to automatically determine \"large\" furniture, in fractions, 0-1.")]
        public float FullDepthCoeff = 0.25f;
        public float FloorOffsetY = 0f;
    }
}