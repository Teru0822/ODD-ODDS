using System;
using System.Collections;
using UnityEngine;

namespace QubicNS
{
    public class BaseSpawner : MonoBehaviour
    {
        public QubicBuilder Builder { get; protected set; }
        public Map Map { get; protected set; }
        public StringFlagMapper TagsMapper { get; protected set; }
        public int InstanceIndex { get; set; }
        public virtual int Order => 100;
        public Rnd RootRnd { get; protected set; }

        [SerializeField, HideInInspector]
        protected byte Initialized = 0;

        #region Build phases
        public virtual IEnumerator OnPrepare(QubicBuilder builder)
        {
            RootRnd = builder.RootRnd.GetBranch(GetType().Name);
            Builder = builder;
            Map = builder.Map;
            TagsMapper = builder.TagsMapper;
            yield break;
        }
        public virtual IEnumerator OnCaptureCells() { yield break; }
        public virtual IEnumerator OnCellsCaptured() { yield break; }
        public virtual IEnumerator OnTagsPostprocess() { yield break; }
        public virtual IEnumerator OnPrepareSpawnPrefabs() { yield break; }
        public virtual IEnumerator OnSpawnWalls() { yield break; }
        public virtual IEnumerator OnSpawnColumns() { yield break; }
        public virtual IEnumerator OnSpawnContent() { yield break; }
        public virtual IEnumerator OnSpawnCompleted() { yield break; }
        public virtual IEnumerator OnBuildCompleted() { yield break; }
        #endregion

        protected Rnd CreateRnd(int id, int id2, Vector3Int indexXZ)
            => new Rnd(Rnd.CombineHashCodes(stackalloc[] { id, id2, indexXZ.x, indexXZ.z, Builder.Seed }));

        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (SceneLoadTracker.SceneIsOpening)
            {
                transform.hasChanged = false;
                return;
            }

            var builder = GetComponentInParent<QubicBuilder>();
            if (builder)
                builder.RebuildNeeded();
#endif
        }
    }

    /// <summary> This class will be automatically attached to builder </summary>
    public abstract class AutoSpawner : BaseSpawner
    {
    }
}