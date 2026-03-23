using System;
using System.Diagnostics;
using UnityEngine;

namespace QubicNS
{
    /// <summary> Contains methods to generate templates of structures and prefabs </summary>
    public static partial class Templates
    {
        #region Prefabs

        public static Prefab Wall(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall);

            res.Rules = new []
            {
                new Rule(res, "Wall", "Any", "Any"),
                new Rule(res, "Wall", "Closet", "Any", 1),
                new Rule(res, "Wall", "Stairs,UStairs", "Outside", 1),
            };
            return res;
        }

        public static Prefab Partition(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall);
            res.Rules = new[]
            {
                new Rule(res, "Wall", "Inside", "Inside", 1)
            };
            return res;
        }

        public static Prefab Window(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall, "Window");
            res.Rules = new[]
            {
                new Rule(res, "Wall", "Inside", "Outside"),
                new Rule(res, "Window", "Any", "Any", 3)
            };
            return res;
        }

        public static Prefab Door(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall, "Door");
            res.Rules = new[]
            {
                new Rule(res, "Door", "Any", "Any")
            };
            return res;
        }

        public static Prefab Arch(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall, "Arch");
            res.Rules = new[]
            {
                new Rule(res, "Wall", "Bridge", "Outside", 3),
                new Rule(res, "Wall", "Bridge", "Inside", 2, SteepCondition.NoSteep),
                new Rule(res, "Arch", "Any", "Any", 3),
                new Rule(res, "Wall", "Atrium", "Inside", 3),
            };
            return res;
        }

        public static Prefab Steps(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall);
            res.Rules = new[]
            {
                new Rule(res, "Steps", "Stairs", "Any", 4)
            };
            return res;
        }

        public static Prefab Rails(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall, "Rails");
            res.Rules = new[]
            {
                new Rule(res, "Wall", "Inside", "Inside", 4, SteepCondition.Steep),
                new Rule(res, "Wall", "Balcony", "Outside", 3),
                new Rule(res, "Wall", "Bridge", "Outside", 4, SteepCondition.Steep),
                new Rule(res, "Wall", "Fence", "Outside", 3),
            };
            return res;
        }

        [InspectorTitle("Floor/Ceiling")]
        public static Prefab Floor(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall);
            res.Rules = new[]
            {
                new Rule(res, "Floor,Ceiling", "Any", "Any")
            };
            return res;
        }

        public static Prefab Corner(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Corner);
            res.Rules = new[]
            {
                new Rule(res, "Wall", "Any", "Any", 2)
            };
            return res;
        }

        public static Prefab Column(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Column);
            res.Rules = new[]
            {
                new Rule(res, "Wall,Window,Door,Arch,Partition", "Any", "Any")
            };
            return res;
        }

        public static Prefab RailsColumn(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Column);
            res.Rules = new[]
            {
                new Rule(res, "Rails", "Any", "Any", -1)
            };
            return res;
        }

        public static Prefab Parapet(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Wall);
            res.Rules = new[]
            {
                new Rule(res, "Parapet", "Roof", "Any"),
            };
            return res;
        }


        [InspectorTitle("▣ Content")]
        public static Prefab Content(GameObject prefab)
        {
            var res = new Prefab(prefab, PrefabType.Content);
            res.Rules = new[]
            {
                new Rule(res, "Wall", "Room", "Any")
            };
            return res;
        }
        #endregion

        #region Rooms

        public static BaseRoom Room()
        {
            var room = CreateRoom<Room>();
            
            return room;
        }

        public static BaseRoom Closet()
        {
            var room = CreateRoom<Room>();
            room.Tags = "Closet";
            room.IntersectionMode = IntersectionMode.Fill;

            return room;
        }

        [InspectorTitle("Stairs/Simple")]
        public static BaseRoom StairsSimple()
        {
            var room = CreateRoom<Stairs>();
            room.Tags = "Stairs";
            room.Size = new RectOffset(0, 0, 1, 0);
            room.LevelCount = 2;
            room.IntersectionMode = IntersectionMode.Weak;
            room.Features.Walls = false;
            room.Features.FloorOnFirstLevel = false;
            room.Features.FloorOnIntermediateLevels = false;
            room.Features.Roof = false;
            room.Features.DoorsStrategy = DoorStrategy.NoDoors;
            room.Features.SetTagsStrategy = SetTagsStrategy.Add;
            room.Features.WallContentCount = ContentCount.Zero;

            return room;
        }

        [InspectorTitle("Stairs/Straight")]
        public static BaseRoom Stairs()
        {
            var room = CreateRoom<Stairs>();
            room.Tags = "Stairs";
            room.Size = new RectOffset(0, 0, 1, 0);
            room.LevelCount = 2;
            room.IntersectionMode = IntersectionMode.Aggressive;

            return room;
        }

        [InspectorTitle("Stairs/U-Shaped")]
        public static BaseRoom StairsU()
        {
            var room = CreateRoom<Stairs>();
            room.Tags = "UStairs";
            room.Size = new RectOffset(0, 0, 1, 0);
            room.LevelCount = 2;
            room.IntersectionMode = IntersectionMode.Aggressive;
            room.Type = StairsType.UShaped;

            return room;
        }

        public static BaseRoom Atrium()
        {
            var room = CreateRoom<Atrium>();
            room.Tags = "Atrium";
            room.LevelCount = 2;
            room.IntersectionMode = IntersectionMode.Fill;

            return room;
        }

        public static BaseRoom Balcony()
        {
            var room = CreateRoom<Room>();
            room.Tags = "Room,Balcony,Outside";
            room.IntersectionMode = IntersectionMode.Aggressive;

            return room;
        }

        public static BaseRoom Fence()
        {
            var room = CreateRoom<Room>();
            room.Tags = "Fence,Outside";
            room.IntersectionMode = IntersectionMode.Complement;
            room.Features.Roof = false;

            return room;
        }

        [InspectorTitle("Area/Content")]
        public static BaseRoom ContentArea()
        {
            var room = CreateRoom<Room>();
            room.Tags = "Room";
            room.Size = new RectOffset(1, 0, 0, 1);
            room.IntersectionMode = IntersectionMode.Fill;
            room.Features.Walls = false;
            room.Features.FloorOnFirstLevel = false;
            room.Features.FloorOnIntermediateLevels = false;
            room.Features.Roof = false;
            room.Features.DoorsStrategy = DoorStrategy.NoDoors;
            room.Features.SetTagsStrategy = SetTagsStrategy.Add;
            room.InsideContent.Decimation = 1;
            room.InsideContent.DoNotAffectWalls = false;

            return room;
        }

        [InspectorTitle("Bridge/Regular")]
        public static BaseRoom Bridge()
        {
            var room = CreateRoom<Room>();
            room.Tags = "Bridge,Outside";
            room.IntersectionMode = IntersectionMode.Complement;
            room.Features.Roof = false;
            room.Features.DoorsStrategy = DoorStrategy.NoDoors;

            return room;
        }

        [InspectorTitle("Bridge/Arcaded")]
        public static BaseRoom BridgeArcaded()
        {
            var room = CreateRoom<Room>();
            room.Tags = "Bridge,Outside";
            room.LevelCount = 2;
            room.IntersectionMode = IntersectionMode.Complement;
            room.Features.FloorOnFirstLevel = false;
            room.Features.Roof = false;
            room.Features.DoorsStrategy = DoorStrategy.NoDoors;
            
            return room;
        }

        [InspectorTitle("Wall/Door")]
        public static BaseRoom Door()
        {
            var room = CreateRoom<Wall>();
            room.Tags = "Door";

            return room;
        }

        [InspectorTitle("Wall/Arch")]
        public static BaseRoom Arch()
        {
            var room = CreateRoom<Wall>();
            room.Tags = "Arch";

            return room;
        }

        [InspectorTitle("Wall/Window")]
        public static BaseRoom Window()
        {
            var room = CreateRoom<Wall>();
            room.Tags = "Window";

            return room;
        }

        [InspectorTitle("Wall/Rails")]
        public static BaseRoom Rails()
        {
            var room = CreateRoom<Wall>();
            room.Tags = "Rails";

            return room;
        }

        

        [InspectorTitle("Wall/Content")]
        public static BaseRoom Content()
        {
            var room = CreateRoom<Wall>();
            room.Tags = "Content";

            return room;
        }

        [InspectorTitle("Area/No Doors")]
        public static BaseRoom NoDoorsArea()
        {
            var room = CreateRoom<Room>();
            room.Tags = " ";
            room.IntersectionMode = IntersectionMode.Weak;
            room.Features.Walls = false;
            room.Features.FloorOnFirstLevel = false;
            room.Features.FloorOnIntermediateLevels = false;
            room.Features.Roof = false;
            room.Features.DoorsStrategy = DoorStrategy.Impassable;
            room.Features.SetTagsStrategy = SetTagsStrategy.Add;

            return room;
        }

        [InspectorTitle("Area/No Content")]
        public static BaseRoom NoContentArea()
        {
            var room = CreateRoom<Room>();
            room.Tags = " ";
            room.IntersectionMode = IntersectionMode.Fill;
            room.Features.Walls = false;
            room.Features.FloorOnFirstLevel = false;
            room.Features.FloorOnIntermediateLevels = false;
            room.Features.Roof = false;
            room.Features.DoorsStrategy = DoorStrategy.NoDoors;
            room.Features.SetTagsStrategy = SetTagsStrategy.Add;
            room.Features.WallContentCount = ContentCount.Zero;

            return room;
        }
        #endregion

        #region Utils

        private static T CreateRoom<T>() where T : BaseRoom
        {
            var name = new StackTrace().GetFrame(1).GetMethod().Name;// get method name
            var res = new GameObject(name, typeof(T));
            return res.GetComponent<T>();
        }

        #endregion
    }

    public class OrderAttribute : Attribute
    {
        public int Order;
        public OrderAttribute(int order) { Order = order; }
    }
}