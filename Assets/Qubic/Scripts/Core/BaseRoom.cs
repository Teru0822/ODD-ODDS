using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    public class BaseRoom : BaseSpawner
    {
        [ShowIf(nameof(CanBeRotated)), Range(0, 3)]
        public int Rotation = 0;

        //[Popup(nameof(PossibleStyles), IsEditable = true)]
        [Delayed]
        public string Tags;
        public UInt64 SetTagsMask { get; protected set; }

        [FieldButtons] public int Seed;
        public RectOffset Size = new RectOffset();

        public Vector3IntSet MyCells = new Vector3IntSet();
        public virtual bool CanBeRotated => false;
        public virtual bool CanHaveCustomCells => true;
        public RectOffset SizeRotated
        {
            get => Size?.Rotate(Rotation);
            set => Size = value.Rotate(-Rotation);
        }

        [FieldButtons(Min = 1, Max = 20)] public int LevelCount = 1;

        [HideInInspector]
        public List<Vector3Int> customCells = new List<Vector3Int>();// in relative coordinates and with zero Y
        [HideInInspector]
        public List<Vector3Int> forbiddenCells = new List<Vector3Int>();// in relative coordinates and with zero Y

        //IEnumerable<string> PossibleStyles => AssemblerDatabase.Instance.WallStyles;
        protected int TagsSeed = 0;
        public override int Order => 100;

        public override IEnumerator OnPrepare(QubicBuilder builder)
        {
            yield return base.OnPrepare(builder);

            //
            RootRnd = RootRnd.GetBranch(Seed);
            TagsSeed = Seed == 0 ? 0 : RootRnd.GetBranch(23).Int();

            StartCell = Map.PosToCell(transform.position);

            MyCells.Clear();

            Size = new RectOffset(Mathf.Max(0, Size.left), Mathf.Max(0, Size.right), Mathf.Max(0, Size.top), Mathf.Max(0, Size.bottom));

            SetTagsMask = TagsMapper.GetOrCreate(Tags.SplitAndTrim());
        }

        protected virtual IEnumerable<Vector3Int> CellsToCapture(IntersectionMode IntersectionMode)
        {
            var forbiddenHash = new Vector3IntSet();//TODO: pool
            forbiddenHash.Clear();
            forbiddenHash.AddRange(forbiddenCells);
            var originalRoom = Map[StartCell * 2].Tags;

            for (int iLevel = 0; iLevel < LevelCount; iLevel++)
            {
                foreach (var cell in (StartCell + Vector3Int.up * iLevel).Neighbors(SizeRotated))
                {
                    if (Check(cell))
                        yield return cell;
                }

                foreach (var cell in customCells.Select(c => c.RotateY(Rotation) + StartCell + Vector3Int.up * iLevel))
                    if (Check(cell))
                        yield return cell;

                bool Check(Vector3Int cell)
                {
                    var relative = new Vector3Int(cell.x - StartCell.x, 0, cell.z - StartCell.z);
                    relative = relative.RotateY(-Rotation);
                    if (forbiddenHash.Contains(relative))
                        return false;

                    switch (IntersectionMode)
                    {
                        case IntersectionMode.Fill:
                            if (originalRoom == Map[cell * 2].Tags)
                                return true;
                            break;
                        case IntersectionMode.Overlap:
                        case IntersectionMode.Weak:
                        case IntersectionMode.Aggressive:
                            return true;
                        case IntersectionMode.Complement:
                            if (Map[cell * 2].Tags == 0)
                                return true;
                            break;
                    }

                    return false;
                }
            }
        }

        public void ClearCustomCells()
        {
            forbiddenCells.Clear();
            customCells.Clear();
        }

        public Vector3Int GetRelativeCellHex(Vector3Int hex)
        {
            var relative = hex - Builder.Map.PosToCell(transform.position);
            relative.y = 0;
            relative = relative.RotateY(-Rotation);
            return relative;
        }

        public bool AddCustomCell(Vector3Int relative)
        {
            var changed = false;

            if (forbiddenCells.Contains(relative))
            {
                forbiddenCells.Remove(relative);
                changed = true;
            }

            if (!customCells.Contains(relative))
            {
                customCells.Add(relative);
                changed = true;
            }

            return changed;
        }

        public bool RemoveCustomCell(Vector3Int relative)
        {
            var changed = false;

            if (customCells.Contains(relative))
            {
                customCells.Remove(relative);
                changed = true;
            }
            else
            if (!forbiddenCells.Contains(relative))
            {
                forbiddenCells.Add(relative);
                changed = true;
            }

            return changed;
        }

        [HideInInspector]
        public Vector3Int StartCell;

        [SerializeField, HideInInspector]
        Color gizmosColor = Color.clear;

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (SceneLoadTracker.SceneIsOpening)
                return;

            var builder = GetComponentInParent<QubicBuilder>();
            if (!builder || builder.Lock)
                return;

            if (MyCells == null || Map == null) return;

            builder = UnityEditor.Selection.activeGameObject?.GetComponentInParent<QubicBuilder>();
            var isMMselected = builder != null;
            if (gizmosColor == Color.clear)
            {
                UnityEngine.Random.InitState(GetHashCode());
                gizmosColor = UnityEngine.Random.ColorHSV(0, 1, 1f, 1f, 0.5f, 1f, 1, 1);
            }
            var isSelected = UnityEditor.Selection.activeTransform == transform;
            gizmosColor.a = isMMselected ? 0.15f : 0f;
            var color = gizmosColor;
            const float h = 0.11f;

            if (!isSelected || (Preferences.Instance.HighlightCellsOfSelectedRoom || Preferences.Instance.HighlightCells))
            {
                if (isSelected)
                    color = new Color(0.2f, 0.2f, 1, 0.3f);
                if (builder != null && !Preferences.Instance.HighlightCells && !isSelected)
                    color = Color.black * 0.03f;

                foreach (var c in MyCells)
                {
                    var pos = Map.CellToPos(c);
                    if (!MyCells.Contains(c + Vector3Int.up))
                    {
                        Gizmos.color = color;
                        Gizmos.DrawCube(pos + Vector3.up * Map.CellHeight, new Vector3(Map.CellSize, h, Map.CellSize));
                    }
                }
            }
        }
#endif

        protected override void OnValidate()
        {
            base.OnValidate();

#if UNITY_EDITOR
            if (Tags.IsNullOrEmpty())
                Tags = GetType().Name;

            if (Size == null)
                Size = new RectOffset();

            if (Seed == 0)
                Seed = new Rnd().Int(10000);
#endif
        }
    }

    [Serializable]
    public enum IntersectionMode
    {
        Fill = 0, Overlap = 1, Complement = 2, Aggressive = 3, Weak = 4
    }
}