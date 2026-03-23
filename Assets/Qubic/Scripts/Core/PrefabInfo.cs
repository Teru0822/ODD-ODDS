using QubicNS;
using System;
using UnityEngine;

namespace QubicNS
{
    /// <summary>
    /// PrefabInfo is a class that holds information about a prefab, including its rotation, anchor point, and bounds.
    /// It also provides a method to copy its properties to another PrefabInfo instance.
    /// </summary>
    [Serializable]
    public class PrefabInfo
    {
        [SerializeField]
        public GameObject Prefab;
        public Vector3 Anchor = Vector3.zero;
        public Quaternion PrefabSceneRotation => Prefab == null ? Quaternion.identity : Prefab.transform.rotation;//TODO: remove
        public Quaternion Rotation => Quaternion.Euler(0, RotationY, 0) * PrefabSceneRotation;
        [HideInInspector]
        public Bounds Bounds;// bounds relative to pivot pos
        [FieldButtons("{0}\u00B0", Min = 0, Max = 270, Step = 90)]
        public ushort RotationY;
        public GameObject[] Alternates;

        public virtual void CopyTo(PrefabInfo target)
        {
            target.Anchor = this.Anchor;
            //target.PrefabSceneRotation = this.PrefabSceneRotation;
            target.Bounds = this.Bounds;
            target.Prefab = this.Prefab;
            target.RotationY = this.RotationY;
            target.Alternates = new GameObject[this.Alternates.Length];
            Array.Copy(Alternates, target.Alternates, Alternates.Length);
        }
    }
}