using System.Linq;

namespace QubicNS
{
    /// <summary> Set of standard Wall tags </summary>
    public partial class WallTags : BaseTagSet<WallTags>
    {
        public static Tag Wall;
        public static Tag Steps;
        public static Tag Passage;
        public static Tag Door;
        public static Tag Parapet;
        public static Tag Content;
        public static Tag Transparent;
    }

    /// <summary> Set of standard Floor/Ceiling tags </summary>
    public partial class FloorTags : BaseTagSet<FloorTags>
    {
        public static Tag Floor;
        public static Tag Ceiling;
    }

    /// <summary> Set of standard Cells/Style tags </summary>
    public partial class CellTags : BaseTagSet<CellTags>
    {
        public static Tag Outside;
        public static Tag Inside;
        public static Tag Roof;
        public static Tag Impassible;
    }

    public static class UITagsProvider
    {
        static string[] edgeTags;
        static string[] cellTags;

        static string[] AdditionalEdgeTags = new string[] { "Rails", "Window", "Arch", "Partition" };
        static string[] AdditionalCellTags = new string[] { "Any", "Room", "Closet", "Stairs", "Balcony", "Bridge", "Fence", "Atrium" };

        public static string[] GetEdgeTags()
        {
            if (edgeTags == null)
            {
                edgeTags =
                WallTags.GetTags()
                .Union(FloorTags.GetTags())
                .Select(t => t.Name)
                .Union(AdditionalEdgeTags)
                .OrderBy(t => t)
                .ToArray();
            }

            return edgeTags;
        }

        public static string[] GetCellTags()
        {
            if (cellTags == null)
            {
                cellTags =
                CellTags.GetTags()
                .Select(t => t.Name)
                .Union(AdditionalCellTags)
                .OrderBy(t => t)
                .ToArray();
            }

            return cellTags;
        }
    }
}