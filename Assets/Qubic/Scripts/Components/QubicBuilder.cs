using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace QubicNS
{
    /// <summary>  
    /// Root class for building. Contains all spawners, a constructed building, a map, and controls the calling of spawners.яё
    /// </summary>
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.v2z27f424of9")]
    [ExecuteInEditMode]
    public partial class QubicBuilder : MonoBehaviour
    {
        [Info(messageType = InfoAttribute.MessageType.WARNING)]
        [SerializeField] string Warning;
        [field: SerializeField] public bool Lock { get; private set; }
        public PrefabDatabase PrefabDatabase;
        public float CellSize => PrefabDatabase.CellSize;
        public float CellHeight => PrefabDatabase.CellHeight;
        [FieldButtons]
        public int Seed;
        [Tooltip("Rotate building around Y axis.")]
        public float RotationY = 0f;
        public QubicDebug Debug;
        //[field: SerializeField, HideInInspector]
        public Map Map { get; set; } = new Map();
        [field: SerializeField, HideInInspector]
        public StringFlagMapper TagsMapper { get; set; } = new StringFlagMapper();
        public List<BaseSpawner> Spawners { get; protected set; } = new List<BaseSpawner>();
        public List<ColumnRequest> ColumnRequestQueue { get; protected set; } = new List<ColumnRequest>();
        public Rnd RootRnd { get; private set; }
        public Transform Holder { get; private set; }
        public Pool Pool { get; private set; }
        [field: SerializeField, HideInInspector]
        public List<Vector3> PortalPositions { get; set; } = new List<Vector3>();
        public static List<Type> AutoSpawners { get; private set; }
        public StringFlagMapper CollisionLayerTagMapper { get; private set; } = new StringFlagMapper();
        [field: SerializeField, HideInInspector]
        public SerializedDictionary<GameObject, SpawnedObjectInfo> SpawnedObjects { get; private set; } = new SerializedDictionary<GameObject, SpawnedObjectInfo>();

        public event Action<QubicBuilder> OnStart;
        public event Action<QubicBuilder> OnBuildCompleted;
        public UnityEvent<QubicBuilder> OnBuilt;

        public InspectorButton _BuildInEditor_Build_CleanBuild_AddRoom;

        public int BuildType { get; private set; }

#if UNITY_EDITOR
        /// <param name="type">0 - rebuild, 1 - clean build, 2 - add room, 3 - rebuild and collapse hierarchy</param>
        public void BuildInEditor(int type)
        {
            if (Lock || this == null)
                return;

            if (Application.isPlaying)
                return;

            if (PrefabDatabase == null)
                return;

            BuildType = type;

            if (type == 2)
            {
                var newRoom = Templates.Room();
                var go = newRoom.gameObject;
                go.transform.SetParent(transform, false);
                go.isStatic = gameObject.isStatic;
                newRoom.transform.localPosition = Vector3.zero;
                RebuildNeeded();

                if (!Application.isPlaying)
                {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Spawn Room");
                    UnityEditor.Selection.activeObject = go;
                }

                return;
            }

            // large map ?
            var sb = new StringBuilder();
            if (Map != null && Map.Count > 5000)
                sb.AppendLine($"The building contains too many cells: ~{Map.Count}\r\nThis reduces the speed of building!\r\nCreate a new {nameof(QubicBuilder)}, please.");

            Warning = sb.ToString();

            // clean build
            if (type == 1)
            {
                var holder = GetComponentInChildren<QubicHolder>(true);
                if (holder) Helper.DestroySafe(holder.gameObject);
                Pool = new Pool();
            }

            if (Debug.LogBuildTime) TimeLogger.Start("Build", true);

            var e = BuildInternal();
            while (e.Enumerate()) ;

            if (Debug.LogBuildTime)
            {
                TimeLogger.LogStop();
                MethodTimer.PrintResults();
            }

            // collapse holder
            if (type == 3 || type == 1)
                QubicHelper.CollapseHierarchy(Holder.gameObject);

            //TODO: optimize size of serialized
            //Map?.SyncSerialized();

            if (Holder?.gameObject != null)
            {
                if (Debug.DisablePickingSpawnedObjects)
                    UnityEditor.SceneVisibilityManager.instance.DisablePicking(Holder.gameObject, true);
                else
                    UnityEditor.SceneVisibilityManager.instance.EnablePicking(Holder.gameObject, true);
            }

            OnSelectionChanged();

            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

#endif

        public IEnumerator BuildInternal()
        {
            if (Lock || this == null)
                yield break;

            if (PrefabDatabase == null)
                throw new Exception("PrefabDatabase is not assigned!");

            AddAutoSpawners();

            RootRnd = new Rnd(Seed);
            CreateHolder();
            yield return null;

            if (Map == null) Map = new Map();
            if (Spawners == null) Spawners = new List<BaseSpawner>();

            Map.Clear();
            TagsMapper.Clear();
            Spawners.Clear();
            CollisionLayerTagMapper.Clear();
            ColumnRequestQueue.Clear();
            SpawnedObjects.Clear();

            InitMap();

            OnStart?.Invoke(this);

            // get spawners list
            Spawners.AddRange(GetComponentsInChildren<BaseSpawner>()
                .Where(s => s.enabled && s.gameObject.activeSelf)
                .OrderBy(s => s.Order));

            yield return null;

            // assign InstanceIndex to spawners
            for (int i = 0; i < Spawners.Count; i++)
                Spawners[i].InstanceIndex = i;

            yield return null;

            // prepare tags
            WallTags.Prepare(this);
            FloorTags.Prepare(this);
            CellTags.Prepare(this);

            // ===== Build phases
            foreach (var s in Spawners) yield return s.OnPrepare(this);
            foreach (var s in Spawners) yield return s.OnCaptureCells();
            foreach (var s in Spawners) yield return s.OnCellsCaptured();
            foreach (var s in Spawners) yield return s.OnTagsPostprocess();
            foreach (var s in Spawners) yield return s.OnPrepareSpawnPrefabs();
            foreach (var s in Spawners) yield return s.OnSpawnWalls();
            foreach (var s in Spawners) yield return s.OnSpawnColumns();
            foreach (var s in Spawners) yield return s.OnSpawnContent();
            foreach (var s in Spawners) yield return s.OnSpawnCompleted();
            foreach (var s in Spawners) yield return s.OnBuildCompleted();

            // finalize prefab pool
            Pool.RemoveUnused();

            OnBuildCompleted?.Invoke(this);
            OnBuilt?.Invoke(this);
        }

        public void InitMap()
        {
            Map.Clear();

            // align pos by grid
            var x = transform.position.x;
            var y = transform.position.y;
            var z = transform.position.z;
            var startWorldPos = new Vector3(Mathf.FloorToInt(x / CellSize) * CellSize, y, Mathf.FloorToInt(z / CellSize) * CellSize);
            transform.position = startWorldPos;

            Map.WorldPos = startWorldPos;
            Map.CellSize = CellSize;
            Map.CellHeight = CellHeight;
        }

        private void CreateHolder()
        {
            Holder = GetComponentInChildren<QubicHolder>(true)?.transform;
            if (!Holder)
            {
                Holder = new GameObject("Holder", typeof(QubicHolder)).transform;
                Holder.SetParent(transform);
                Holder.transform.SetSiblingIndex(0);
            }
            if (Pool == null)
                Pool = new Pool();

            Holder.localPosition = Vector3.zero;
            Holder.localRotation = Quaternion.identity;
            Pool.Reset(Holder);
        }

        public void RebuildNeeded(bool forced = false) 
        {
            if (Preferences.Instance.AutoRebuild || forced)
                lastRebuildRequestFrame = Time.frameCount;
        }

        public BaseRoom ObjToRoom(GameObject obj)
        {
            if (SpawnedObjects != null)
            if (SpawnedObjects.TryGetValue(obj, out var info))
                return info.FromRoom ?? info.ToRoom;

            return null;
        }

        public BaseRoom ObjToRoomGlobal(GameObject obj)
        {
            foreach (var builder in AllEnabledBuilders)
            if (builder)
            {
                var room = builder.ObjToRoom(obj);
                if (room != null)
                    return room;
            }

            return null;
        }

        public BaseRoom PosToRoom(Vector3 pos, float radius = 0f)
        {
            if (Map == null) return null;

            var room = Map[Map.PosToCell(pos) * 2].Room;
            if (room != null) return room;

            var dY = Map.CellHeight / 2;

            var offset = new Vector3(radius, dY, radius);
            room = Map[Map.PosToCell(pos + offset) * 2].Room;
            if (room != null) return room;

            offset = new Vector3(-radius, dY, -radius);
            room = Map[Map.PosToCell(pos + offset) * 2].Room;
            if (room != null) return room;

            offset = new Vector3(radius, -dY, radius);
            room = Map[Map.PosToCell(pos + offset) * 2].Room;
            if (room != null) return room;

            offset = new Vector3(-radius, -dY, -radius);
            room = Map[Map.PosToCell(pos + offset) * 2].Room;
            if (room != null) return room;

            return null;

        }

        public static HashSet<QubicBuilder> AllEnabledBuilders { get; } = new HashSet<QubicBuilder>();

        public static BaseRoom PosToRoomGlobal(Vector3 pos)
        {
            foreach (var builder in AllEnabledBuilders)
            if (builder && builder.Map != null)
            {
                var room = builder.PosToRoom(pos);
                if (room != null)
                    return room;
            }

            return null;
        }

        public static bool PosToCellGlobal(Vector3 pos, out QubicBuilder otherBuilder, out QubicEdge otherCell)
        {
            foreach (var builder in AllEnabledBuilders)
            if (builder && builder.Map != null)
            {
                var cell = builder.Map[builder.Map.PosToCell(pos)];
                if (cell.Room != null)
                {
                    otherBuilder = builder;
                    otherCell = cell;
                    return true;
                }
            }
            otherBuilder = null;
            otherCell = null;
            return false;
        }

        private void OnEnable()
        {
            AllEnabledBuilders.Add(this);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += OnEditorUpdate;
#endif
        }

        private void OnDisable()
        {
            AllEnabledBuilders.Remove(this);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= OnEditorUpdate;
#endif
        }

        public void SetHolderRotation(float angleY)
        {
            var holder = GetComponentInChildren<QubicHolder>(true);
            if (holder == null)
                return;
            holder.transform.localRotation = Quaternion.Euler(0, angleY, 0);
        }

        #region Editor service

#if UNITY_EDITOR
        private void OnEditorUpdate()
        {
            if (lastRebuildRequestFrame != 0/* && lastRebuildRequestFrame < Time.frameCount*/)
            {
                lastRebuildRequestFrame = 0;
                BuildInEditor(0);
            }
        }

        public void OnValidate()
        {
            transform.hasChanged = false;

            if (Debug == null)
                Debug = new QubicDebug();

            AddAutoSpawners();

            AllEnabledBuilders.Add(this);

            if (!SceneLoadTracker.SceneIsOpening)
            {
                RebuildNeeded();
            }

            UnityEditor.Selection.selectionChanged -= OnSelectionChanged;
            UnityEditor.Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (this == null)
                return;

            var selectedBuilder = UnityEditor.Selection.activeGameObject?.GetComponentInParent<QubicBuilder>();

            if (selectedBuilder == this && UnityEditor.Selection.activeGameObject != this.gameObject)
                SetHolderRotation(0);
            else
                SetHolderRotation(RotationY);
        }
#endif

        private void AddAutoSpawners()
        {
            // add autospawners
            if (AutoSpawners == null)
                AutoSpawners = TypesHelper.GetDerivedTypes<AutoSpawner>().ToList();
            foreach (var a in AutoSpawners)
                if (!GetComponent(a))
                    gameObject.AddComponent(a);
        }

        int lastRebuildRequestFrame;

#if UNITY_EDITOR

        public static QubicBuilder LastSelectedBuilder { get; set; }
        public BaseRoom LastSelectedRoom { get; set; }

        private void OnDrawGizmos()
        {
            if (SceneLoadTracker.SceneIsOpening)
            {
                transform.hasChanged = false;
                return;
            }

            if (Map == null)
                return;

            var selectedBuilder = UnityEditor.Selection.activeGameObject?.GetComponentInParent<QubicBuilder>();

            if (selectedBuilder != this)
            {
                transform.hasChanged = false;
                return;
            }

            if (transform.hasChanged)
            {
                RebuildNeeded();
                transform.hasChanged = false;
            }

            var sb = new StringBuilder();

            if (Debug.DrawRoomName)
            {
                foreach (var cell in Map.Values)
                {
                    sb.Clear();
                    var p = Map.EdgeToPos(cell.Index);

                    //if (Debug.DrawCellIndex)
                    //    sb.AppendLine(cell.Index.ToString());

                    if (Debug.DrawRoomName)
                        sb.AppendLine(cell.Room?.name ?? "");                   

                    if (sb.Length > 0)
                        UnityEditor.Handles.Label(p, sb.ToString());
                }
            }

            if (Debug.DrawDoorsAndStairs)
            {
                Gizmos.color = Color.yellow;
                foreach (var s in Spawners.OfType<Stairs>().SelectMany(s => s.MyCells))
                    Gizmos.DrawWireCube(Map.CellToPos(s) + Vector3.up * Map.CellHeight / 2, new Vector3(Map.CellSize, Map.CellHeight, Map.CellSize));
            }

            if (Debug.DrawCellTags || Debug.DrawOutsideTags || Debug.DrawEdgeTags || Debug.DrawFloorTags)
            {
                WallTags.Prepare(this);
                FloorTags.Prepare(this);
                CellTags.Prepare(this);

                ulong showTagsMask = ~(CellTags.Inside);
                if (!Debug.DrawOutsideTags) showTagsMask &= ~(CellTags.Outside);
                if (!Debug.DrawCellTags) showTagsMask &= (CellTags.Outside | CellTags.Inside);

                foreach (var edgeIndex in Map.Keys)
                {
                    var edge = Map[edgeIndex];
                    sb.Clear();
                    var p = Map.EdgeToPos(edgeIndex);
                    var isCell = edgeIndex.IsCell();
                    if (isCell)
                        p += Vector3.up / 4;

                    //TODO: draw tags of selected room only
                    if (Debug.DrawEdgeTags && edge.Type == EdgeType.Wall && edge.Tags != 0)
                        sb.AppendLine(string.Join(", ", TagsMapper.ToStrings(edge.Tags)));

                    if (Debug.DrawFloorTags && edge.Type == EdgeType.Floor && edge.Tags != 0)
                        sb.AppendLine(string.Join(", ", TagsMapper.ToStrings(edge.Tags)));

                    if (Debug.DrawCellTags || Debug.DrawOutsideTags)
                    if (edge.Type == EdgeType.Cell && edge.Tags != 0)
                        sb.AppendLine(string.Join(", ", TagsMapper.ToStrings(edge.Tags & showTagsMask)));

                    if (sb.Length > 0)
                        UnityEditor.Handles.Label(p, sb.ToString());
                }
            }
        }
#endif

        public void GrabFrom(QubicBuilder lastSpawned)
        {
            if (lastSpawned == null) return;
            PrefabDatabase = lastSpawned.PrefabDatabase;
            Debug = lastSpawned.Debug.CloneDeep();
        }
        #endregion
    }

    [Serializable]
    public class QubicDebug
    {
        [WideCheckbox] public bool DrawRoomName;
        [WideCheckbox] public bool DrawCellTags;
        [WideCheckbox] public bool DrawEdgeTags;
        [WideCheckbox] public bool DrawFloorTags;
        [WideCheckbox] public bool DrawOutsideTags;
        [WideCheckbox] public bool DrawDoorsAndStairs;
        //[WideCheckbox] public bool DrawEdgeIndex;
        //[WideCheckbox] public bool DrawCellIndex;

        [WideCheckbox] public bool LogBuildTime;
        [WideCheckbox] public bool DisablePickingSpawnedObjects;
        [WideCheckbox] public bool DoNotSpawnRoof;
        [WideCheckbox(Label = "Forced Isometric View (F6)")] public bool ForcedIsometricView;
    }

    [Serializable]
    public struct SpawnedObjectInfo
    {
        [SerializeReference]
        public GameObject Object;
        [SerializeReference]
        public Prefab Prefab;
        [SerializeReference]
        public Room FromRoom;
        [SerializeReference]
        public Room ToRoom;
    }
}