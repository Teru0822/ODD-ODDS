using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace QubicNS
{
    [HelpURL("https://docs.google.com/document/d/1dSxqUGTbihdTqLsPRBO3JFuZ1Aij3nN15FFACrcJ5mM/edit?tab=t.0#heading=h.qer4wexh2fia")]
    [Serializable]
    public class Prefab
    {
        [ReadOnly]
        public PrefabType Type = PrefabType.Wall;
        [SerializeField, HideInInspector] protected string _inspector_name;
        public PrefabInfo PrefabInfo = new PrefabInfo();
        [FormerlySerializedAs("Features")]
        public WallPrefabFeatures WallFeatures = new WallPrefabFeatures();
        public ContentPrefabFeatures ContentFeatures = new ContentPrefabFeatures();
        public ColumnPrefabFeatures ColumnFeatures = new ColumnPrefabFeatures();
        [Delayed] public string SetTags = "";
        [FieldButtons(Min = 0, Max = 10)] public int SetTagsRadius = 1;
        [RangeMode(Min = -20, Max = 40, CanBeOutside = true)] public RangeInt Levels = new RangeInt(-20, 20);
        public Rule[] Rules = new Rule[1];

        [HideInInspector] public int Seed = 0;
        [NonSerialized] public FastBounds[] BoundsByDirs;
        [NonSerialized] public bool HasVariants;
        public Bounds Bounds => PrefabInfo.Bounds.RotateY(Vector3.zero, PrefabInfo.RotationY / 90);
        [NonSerialized] public bool IsLeftColumn;
        [NonSerialized] public bool IsRightColumn;
        [NonSerialized] public bool IsFullDepth;
        [NonSerialized] public int WidthInEdges;
        [NonSerialized] public int HeightInEdges;
        public IEnumerable<GameObject> Prefabs => Enumerable.Range(0, 1).Select(i => PrefabInfo?.Prefab).Concat(PrefabInfo?.Alternates == null ? Array.Empty<GameObject>() : PrefabInfo?.Alternates).Where(p => p != null);
        [NonSerialized] public UInt64 SetTagsMask;
        [NonSerialized] public UInt64 AvoidTagsMask;

        public bool IsNone { get; private set; }

        public static DateTime collapseRequestTime;

        public Prefab(GameObject prefab, PrefabType wall, string setTags = "")
        {
            PrefabInfo = new PrefabInfo() { Prefab = prefab };
            Type = wall;
            SetTags = setTags;
            SetTagsRadius = 1;
        }

        public const string DefaultCollisionLayer = "Default";

        public void OnValidate()
        {
            if (PrefabInfo == null)
                PrefabInfo = new PrefabInfo();

            if (WallFeatures == null)
                WallFeatures = new WallPrefabFeatures();

            if (ContentFeatures == null)
                ContentFeatures = new ContentPrefabFeatures();
            if (ContentFeatures.CollisionLayer == null)
                ContentFeatures.CollisionLayer = DefaultCollisionLayer;

            if (ColumnFeatures == null)
                ColumnFeatures = new ColumnPrefabFeatures();

            if (Seed <= 0 && PrefabInfo.Prefab != null)
                Seed = Mathf.Abs(PrefabInfo.Prefab.name.GetHashCode());

            _inspector_name = PrefabInfo.Prefab == null ? "null" : PrefabInfo.Prefab.name;
        }

        public void Prepare(QubicBuilder builder)
        {
            HasVariants = Prefabs.Any(p => p?.GetComponentInChildren<Variant>() != null);
            IsNone = PrefabInfo.Prefab == null || PrefabInfo.Prefab.name == "None";

            // prepare bounds
            if (Type == PrefabType.Content)
            {
                if (BoundsByDirs == null)
                    BoundsByDirs = new FastBounds[4];
                var collisionMask = builder.CollisionLayerTagMapper.GetOrCreate(ContentFeatures.CollisionLayer.SplitAndTrim());
                for (int iDir = 0; iDir < 4; iDir++)
                {
                    var dirRotation = QubicHelper.Quaternions4[iDir];
                    var rotation = dirRotation * Quaternion.Euler(0, PrefabInfo.RotationY, 0);
                    var edgePos = builder.Map.EdgeToPosLocal(QubicHelper.Dirs4[iDir]);
                    var position = edgePos + dirRotation * (PrefabInfo.Anchor - Vector3.forward * builder.PrefabDatabase.Features.DefaultContentPadding);
                    var bounds = QubicHelper.RotateBounds90(PrefabInfo.Bounds, position, rotation);
                    BoundsByDirs[iDir] = new FastBounds(bounds, collisionMask);
                }

                CalcAdditionalProperties(builder.PrefabDatabase);
            }

            SetTagsMask = builder.TagsMapper.GetOrCreate(SetTags.SplitAndTrim());
            if (Type == PrefabType.Content)
            {
                AvoidTagsMask = builder.TagsMapper.GetOrCreate(ContentFeatures.AvoidTags.SplitAndTrim());
                if (ContentFeatures.AvoidCellsWithSetTags)
                    AvoidTagsMask |= SetTagsMask;
            } else
                AvoidTagsMask = 0ul;

            // calc width in edges
            WidthInEdges = Mathf.Max(1, Mathf.RoundToInt(Bounds.size.x / builder.CellSize));
            HeightInEdges = Mathf.Max(1, Mathf.RoundToInt(Bounds.size.y / builder.CellHeight));

            // prepare rules
            foreach (var rule in Rules)
                rule.Prepare(builder, this);
        }

        public void CalcAdditionalProperties(PrefabDatabase database)
        {
            var c = Bounds.center + PrefabInfo.Anchor;
            var x = c.x / database.CellSize;
            IsLeftColumn = x < -0.5f + database.Features.ColumnCoeff;
            IsRightColumn = x > 0.5f - database.Features.ColumnCoeff;

            IsFullDepth = Bounds.size.z > database.CellSize * database.Features.FullDepthCoeff;
        }

        public void CaptureBounds(bool changeAnchor)
        {
            if (PrefabInfo.Prefab == null)
                return;
            var temp = GameObject.Instantiate(PrefabInfo.Prefab);
            var splitter = new MeshQuadrantSplitter();
            splitter.SplitMeshIntoQuadrants(temp);
            PrefabInfo.Bounds = splitter.totalBounds;
            if (PrefabInfo.Bounds.size != Vector3.zero)
                PrefabInfo.Bounds.center -= PrefabInfo.Prefab.transform.position;

            var anchor = Vector3.zero;
            switch (Type)
            {
                case PrefabType.Content:
                    anchor = new Vector3(0, -PrefabInfo.Bounds.min.y + 0.01f, -PrefabInfo.Bounds.max.z - 0.2f); break;
                default:
                {
                    anchor = -PrefabInfo.Bounds.center;
                    if (PrefabInfo.Bounds.size.z > PrefabInfo.Bounds.size.x * 2)
                    {
                        anchor = new Vector3(-anchor.z, 0, -anchor.x);
                    }

                    var rule = default(Rule);
                    if (Rules != null && Rules.Length > 0)
                        rule = Rules[0];
                    if (rule.Wall.Contains("Floor"))
                        anchor.y = PrefabInfo.Bounds.min.y;
                    else
                        anchor.y = -PrefabInfo.Bounds.min.y;
                    break;
                }
            }
            
            if(changeAnchor)
                PrefabInfo.Anchor = anchor;

            Helper.DestroySafe(temp);
        }
    }

    public enum PrefabType
    {
        Wall = 1, Corner = 5, Column = 8, Content = 10
    }

    [Serializable]
    public partial class WallPrefabFeatures
    {
        public float ContentPadding = PrefabDatabaseFeatures.PADDING;
        public bool RequiresColumn = false;
        public SideWall Side = SideWall.Default;
        public bool IsOneSideWall => Side == SideWall.IsOneSide;
    }

    [Serializable]
    public enum SideWall
    {
        Default = 0,
        IsOneSide = 1,
        IsDoubleSide = 2,
    }

    [Serializable]
    public partial class ColumnPrefabFeatures
    {
        //public float ContentPadding = 0.1f;
        public ColumnCornerType CornerType = ColumnCornerType.Any;
    }


    [Serializable, Flags]
    public enum ColumnCornerType
    {
        Any = 0x0,
        Straight = 0x1,
        LCornerConvex = 0x2,
        LCornerConcave = 0x4,
        End = 0x8,
        AllowT = 0x10,
        AllowX = 0x20,
    }

    //[Serializable, Flags]
    //public enum ColumnCornerType : UInt16
    //{
    //    Any = 1,
    //    Straight = 2,
    //    LCorner = 3,
    //    LCornerConvex = 4,
    //    LCornerConcave = 5,
    //    LCornerOrEnd = 6,
    //    End = 7,
    //}

    [Serializable]
    public partial class ContentPrefabFeatures
    {
        [Delayed]
        public string CollisionLayer = Prefab.DefaultCollisionLayer;
        public TriBoolValue AvoidNarrow;
        [WideCheckbox] public bool SpawnInsideRoom = false;
        [WideCheckbox] public bool AvoidConvexCorners;
        [WideCheckbox] public bool AvoidConcaveCorners;
        [WideCheckbox] public bool NeedCeiling;
        [WideCheckbox] public bool NeedFloor = true;
        [WideCheckbox] public bool NeedColumn = false;
        [WideCheckbox] public bool IgnoreWallPadding = false;
        [WideCheckbox] public bool IgnoreFloorPadding = false;
        [WideCheckbox] public bool IgnoreCollisions = false;
        public TriBoolValue CheckWallsAround;
        public bool SingleInCell => Decimation != Decimation.None;
        public Decimation Decimation = Decimation.OnePerCell;
        [Delayed] public string AvoidTags = "";
        [WideCheckbox] public bool AvoidCellsWithSetTags = true;
    }

    [Serializable]
    public enum Decimation : byte
    {
        None = 0,
        OnePerCell = 1,
        SpawnEach2 = 2,
        SpawnEach3 = 3,
        SpawnEach4 = 4,
        SpawnEach5 = 5,
        SpawnEach7 = 7,
        SpawnEach10 = 10,
        SpawnEach13 = 13,
        SpawnEach16 = 16,
    }

    [Serializable]
    public enum TriBoolValue : byte
    {
        Auto = 0, On = 2, Off = 5
    }
}