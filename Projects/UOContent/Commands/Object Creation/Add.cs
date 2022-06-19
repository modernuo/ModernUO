using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Server.Items;
using CPA = Server.CommandPropertyAttribute;

using static Server.Attributes;
using static Server.Types;

namespace Server.Commands
{
    public static class Add
    {
        public static void Initialize()
        {
            CommandSystem.Register("Tile", AccessLevel.GameMaster, Tile_OnCommand);
            CommandSystem.Register("TileRXYZ", AccessLevel.GameMaster, TileRXYZ_OnCommand);
            CommandSystem.Register("TileXYZ", AccessLevel.GameMaster, TileXYZ_OnCommand);
            CommandSystem.Register("TileZ", AccessLevel.GameMaster, TileZ_OnCommand);
            CommandSystem.Register("TileAvg", AccessLevel.GameMaster, TileAvg_OnCommand);

            CommandSystem.Register("Outline", AccessLevel.GameMaster, Outline_OnCommand);
            CommandSystem.Register("OutlineRXYZ", AccessLevel.GameMaster, OutlineRXYZ_OnCommand);
            CommandSystem.Register("OutlineXYZ", AccessLevel.GameMaster, OutlineXYZ_OnCommand);
            CommandSystem.Register("OutlineZ", AccessLevel.GameMaster, OutlineZ_OnCommand);
            CommandSystem.Register("OutlineAvg", AccessLevel.GameMaster, OutlineAvg_OnCommand);
        }

        public static void Invoke(
            Mobile from, Point3D start, Point3D end, string[] args, List<Container> packs = null,
            bool outline = false, bool mapAvg = false
        )
        {
            var sb = new StringBuilder();

            sb.Append($"{from.AccessLevel} {CommandLogging.Format(from)} building ");

            if (start == end)
            {
                sb.Append($"at {start} in {from.Map}");
            }
            else
            {
                sb.Append($"from {start} to {end} in {from.Map}");
            }

            sb.Append(':');

            for (var i = 0; i < args.Length; ++i)
            {
                sb.Append($" \"{args[i]}\"");
            }

            CommandLogging.WriteLine(from, sb.ToString());

            var name = args[0];

            FixArgs(ref args);

            string[,] props = null;

            for (var i = 0; i < args.Length; ++i)
            {
                if (args[i].InsensitiveEquals("set"))
                {
                    var remains = args.Length - i - 1;

                    if (remains >= 2)
                    {
                        props = new string[remains / 2, 2];

                        remains /= 2;

                        for (var j = 0; j < remains; ++j)
                        {
                            props[j, 0] = args[i + j * 2 + 1];
                            props[j, 1] = args[i + j * 2 + 2];
                        }

                        FixSetString(ref args, i);
                    }

                    break;
                }
            }

            var type = AssemblyHandler.FindTypeByName(name);

            if (!IsEntity(type))
            {
                from.SendMessage("No type with that name was found.");
                return;
            }

            var watch = new Stopwatch();
            watch.Start();

            var built = BuildObjects(from, type, start, end, args, props, packs, outline, mapAvg);

            if (built > 0)
            {
                watch.Stop();
                from.SendMessage(
                    "{0} object{1} generated in {2:F2} seconds.",
                    built,
                    built != 1 ? "s" : "",
                    watch.Elapsed.TotalSeconds
                );
            }
            else
            {
                SendUsage(type, from);
            }
        }

        public static void FixSetString(ref string[] args, int index)
        {
            var old = args;
            args = GC.AllocateUninitializedArray<string>(index);

            Array.Copy(old, 0, args, 0, index);
        }

        public static void FixArgs(ref string[] args)
        {
            var old = args;
            args = GC.AllocateUninitializedArray<string>(args.Length - 1);

            Array.Copy(old, 1, args, 0, args.Length);
        }

        public static int BuildObjects(
            Mobile from, Type type, Point3D start, Point3D end, string[] args, string[,] props,
            List<Container> packs, bool outline = false, bool mapAvg = false
        )
        {
            Utility.FixPoints(ref start, ref end);

            PropertyInfo[] realProps = null;

            if (props != null)
            {
                realProps = new PropertyInfo[props.GetLength(0)];

                var allProps =
                    type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                for (var i = 0; i < realProps.Length; ++i)
                {
                    var propName = props[i, 0];
                    var thisProp = Properties.GetPropertyInfoByName(from, allProps, propName, PropertyAccess.Write, out var failReason);

                    if (failReason == null)
                    {
                        realProps[i] = thisProp;
                    }
                    else
                    {
                        from.SendMessage(failReason);
                    }
                }
            }

            var ctors = type.GetConstructors();

            for (var i = 0; i < ctors.Length; ++i)
            {
                var ctor = ctors[i];

                if (!IsConstructible(ctor, from.AccessLevel))
                {
                    continue;
                }

                // Handle optional constructors
                var paramList = ctor.GetParameters();
                var totalParams = 0;
                for (var j = 0; j < paramList.Length; j++)
                {
                    if (!paramList[j].HasDefaultValue)
                    {
                        totalParams++;
                    }
                }

                if (args.Length >= totalParams && args.Length <= paramList.Length)
                {
                    var paramValues = ParseValues(paramList, args);

                    if (paramValues == null)
                    {
                        continue;
                    }

                    var built = Build(from, start, end, ctor, paramValues, props, realProps, packs, outline, mapAvg);

                    if (built > 0)
                    {
                        return built;
                    }
                }
            }

            return 0;
        }

        public static object[] ParseValues(ParameterInfo[] paramList, string[] args)
        {
            var values = new object[paramList.Length];

            for (int i = 0, a = 0; i < paramList.Length; i++)
            {
                var param = paramList[i];
                TryParse(param.ParameterType, a < args.Length ? args[a++] : null, out var value);

                if (value != null)
                {
                    values[i] = value;
                }
                else if (param.HasDefaultValue)
                {
                    values[i] = Type.Missing;
                }
                else
                {
                    return null;
                }
            }

            return values;
        }

        public static IEntity Build(
            Mobile from, ConstructorInfo ctor, object[] values, string[,] props,
            PropertyInfo[] realProps, ref bool sendError
        )
        {
            var built = ctor.Invoke(values);

            if (realProps != null)
            {
                var hadError = false;

                for (var i = 0; i < realProps.Length; ++i)
                {
                    if (realProps[i] == null)
                    {
                        continue;
                    }

                    var result =
                        Properties.InternalSetValue(from, built, built, realProps[i], props[i, 1], props[i, 1], false);

                    if (result != "Property has been set.")
                    {
                        if (sendError)
                        {
                            from.SendMessage(result);
                        }

                        hadError = true;
                    }
                }

                if (hadError)
                {
                    sendError = false;
                }
            }

            return (IEntity)built;
        }

        public static int Build(
            Mobile from, Point3D start, Point3D end, ConstructorInfo ctor, object[] values,
            string[,] props, PropertyInfo[] realProps, List<Container> packs, bool outline = false, bool mapAvg = false
        )
        {
            try
            {
                var map = from.Map;

                var width = end.X - start.X + 1;
                var height = end.Y - start.Y + 1;

                if (outline && (width < 3 || height < 3))
                {
                    outline = false;
                }

                int objectCount;

                if (packs != null)
                {
                    objectCount = packs.Count;
                }
                else if (outline)
                {
                    objectCount = (width + height - 2) * 2;
                }
                else
                {
                    objectCount = width * height;
                }

                if (objectCount >= 20)
                {
                    from.SendMessage("Constructing {0} objects, please wait.", objectCount);
                }

                var sendError = true;

                var sb = new StringBuilder();
                sb.Append("Serials: ");

                if (packs != null)
                {
                    for (var i = 0; i < packs.Count; ++i)
                    {
                        var built = Build(from, ctor, values, props, realProps, ref sendError);

                        sb.Append($"{built.Serial}; ");

                        if (built is Item item)
                        {
                            packs[i].DropItem(item);
                        }
                        else if (built is Mobile m)
                        {
                            m.MoveToWorld(new Point3D(start.X, start.Y, start.Z), map);
                        }
                    }
                }
                else
                {
                    var z = start.Z;

                    for (var x = start.X; x <= end.X; ++x)
                    {
                        for (var y = start.Y; y <= end.Y; ++y)
                        {
                            if (outline && x != start.X && x != end.X && y != start.Y && y != end.Y)
                            {
                                continue;
                            }

                            if (mapAvg)
                            {
                                z = map.GetAverageZ(x, y);
                            }

                            var built = Build(from, ctor, values, props, realProps, ref sendError);

                            sb.Append($"{built.Serial}; ");

                            if (built is Item item)
                            {
                                item.MoveToWorld(new Point3D(x, y, z), map);
                            }
                            else if (built is Mobile m)
                            {
                                m.MoveToWorld(new Point3D(x, y, z), map);
                            }
                        }
                    }
                }

                CommandLogging.WriteLine(from, sb.ToString());

                return objectCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0;
            }
        }

        public static void SendUsage(Type type, Mobile from)
        {
            var ctors = type.GetConstructors();
            var foundCtor = false;

            for (var i = 0; i < ctors.Length; ++i)
            {
                var ctor = ctors[i];

                if (!IsConstructible(ctor, from.AccessLevel))
                {
                    continue;
                }

                if (!foundCtor)
                {
                    foundCtor = true;
                    from.SendMessage("Usage:");
                }

                SendCtor(type, ctor, from);
            }

            if (!foundCtor)
            {
                from.SendMessage("That type is not marked constructible.");
            }
        }

        public static void SendCtor(Type type, ConstructorInfo ctor, Mobile from)
        {
            var paramList = ctor.GetParameters();

            var sb = new StringBuilder();

            sb.Append(type.Name);

            for (var i = 0; i < paramList.Length; ++i)
            {
                if (i != 0)
                {
                    sb.Append(',');
                }

                sb.Append(' ');

                sb.Append(paramList[i].ParameterType.Name);
                sb.Append(' ');
                sb.Append(paramList[i].Name);
            }

            from.SendMessage(sb.ToString());
        }

        private static void TileBox_Callback(Mobile from, Point3D start, Point3D end, TileState ts)
        {
            var mapAvg = false;

            switch (ts.m_ZType)
            {
                case TileZType.Fixed:
                    {
                        start.Z = end.Z = ts.m_FixedZ;
                        break;
                    }
                case TileZType.MapAverage:
                    {
                        mapAvg = true;
                        break;
                    }
            }

            Invoke(from, start, end, ts.m_Args, null, ts.m_Outline, mapAvg);
        }

        private static void Internal_OnCommand(CommandEventArgs e, bool outline)
        {
            var from = e.Mobile;

            if (e.Length >= 1)
            {
                BoundingBoxPicker.Begin(
                    from,
                    (map, start, end) =>
                        TileBox_Callback(from, start, end, new TileState(TileZType.Start, 0, e.Arguments, outline))
                );
            }
            else
            {
                from.SendMessage(
                    "Format: {0} <type> [params] [set {{<propertyName> <value> ...}}]",
                    outline ? "Outline" : "Tile"
                );
            }
        }

        private static void InternalRXYZ_OnCommand(CommandEventArgs e, bool outline)
        {
            if (e.Length >= 6)
            {
                var p = new Point3D(e.Mobile.X + e.GetInt32(0), e.Mobile.Y + e.GetInt32(1), e.Mobile.Z + e.GetInt32(4));
                var p2 = new Point3D(p.X + e.GetInt32(2) - 1, p.Y + e.GetInt32(3) - 1, p.Z);

                var subArgs = new string[e.Length - 5];

                for (var i = 0; i < subArgs.Length; ++i)
                {
                    subArgs[i] = e.Arguments[i + 5];
                }

                Invoke(e.Mobile, p, p2, subArgs, null, outline);
            }
            else
            {
                e.Mobile.SendMessage(
                    "Format: {0}RXYZ <x> <y> <w> <h> <z> <type> [params] [set {{<propertyName> <value> ...}}]",
                    outline ? "Outline" : "Tile"
                );
            }
        }

        private static void InternalXYZ_OnCommand(CommandEventArgs e, bool outline)
        {
            if (e.Length >= 6)
            {
                var p = new Point3D(e.GetInt32(0), e.GetInt32(1), e.GetInt32(4));
                var p2 = new Point3D(p.X + e.GetInt32(2) - 1, p.Y + e.GetInt32(3) - 1, e.GetInt32(4));

                var subArgs = new string[e.Length - 5];

                for (var i = 0; i < subArgs.Length; ++i)
                {
                    subArgs[i] = e.Arguments[i + 5];
                }

                Invoke(e.Mobile, p, p2, subArgs, null, outline);
            }
            else
            {
                e.Mobile.SendMessage(
                    "Format: {0}XYZ <x> <y> <w> <h> <z> <type> [params] [set {{<propertyName> <value> ...}}]",
                    outline ? "Outline" : "Tile"
                );
            }
        }

        private static void InternalZ_OnCommand(CommandEventArgs e, bool outline)
        {
            var from = e.Mobile;

            if (e.Length >= 2)
            {
                var subArgs = new string[e.Length - 1];

                for (var i = 0; i < subArgs.Length; ++i)
                {
                    subArgs[i] = e.Arguments[i + 1];
                }

                BoundingBoxPicker.Begin(
                    from,
                    (map, start, end) =>
                        TileBox_Callback(from, start, end, new TileState(TileZType.Fixed, e.GetInt32(0), subArgs, outline))
                );
            }
            else
            {
                from.SendMessage(
                    "Format: {0}Z <z> <type> [params] [set {{<propertyName> <value> ...}}]",
                    outline ? "Outline" : "Tile"
                );
            }
        }

        private static void InternalAvg_OnCommand(CommandEventArgs e, bool outline)
        {
            var from = e.Mobile;

            if (e.Length >= 1)
            {
                BoundingBoxPicker.Begin(
                    from,
                    (map, start, end) =>
                        TileBox_Callback(from, start, end, new TileState(TileZType.MapAverage, 0, e.Arguments, outline))
                );
            }
            else
            {
                from.SendMessage(
                    "Format: {0}Avg <type> [params] [set {{<propertyName> <value> ...}}]",
                    outline ? "Outline" : "Tile"
                );
            }
        }

        [Usage("Tile <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name into a targeted bounding box. Optional constructor parameters. Optional set property list."
         )]
        public static void Tile_OnCommand(CommandEventArgs e)
        {
            Internal_OnCommand(e, false);
        }

        [Usage("TileRXYZ <x> <y> <w> <h> <z> <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name into a given bounding box, (x, y) parameters are relative to your characters position. Optional constructor parameters. Optional set property list."
         )]
        public static void TileRXYZ_OnCommand(CommandEventArgs e)
        {
            InternalRXYZ_OnCommand(e, false);
        }

        [Usage("TileXYZ <x> <y> <w> <h> <z> <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name into a given bounding box. Optional constructor parameters. Optional set property list."
         )]
        public static void TileXYZ_OnCommand(CommandEventArgs e)
        {
            InternalXYZ_OnCommand(e, false);
        }

        [Usage("TileZ <z> <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name into a targeted bounding box at a fixed Z location. Optional constructor parameters. Optional set property list."
         )]
        public static void TileZ_OnCommand(CommandEventArgs e)
        {
            InternalZ_OnCommand(e, false);
        }

        [Usage("TileAvg <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name into a targeted bounding box on the map's average Z elevation. Optional constructor parameters. Optional set property list."
         )]
        public static void TileAvg_OnCommand(CommandEventArgs e)
        {
            InternalAvg_OnCommand(e, false);
        }

        [Usage("Outline <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name around a targeted bounding box. Optional constructor parameters. Optional set property list."
         )]
        public static void Outline_OnCommand(CommandEventArgs e)
        {
            Internal_OnCommand(e, true);
        }

        [Usage("OutlineRXYZ <x> <y> <w> <h> <z> <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name around a given bounding box, (x, y) parameters are relative to your characters position. Optional constructor parameters. Optional set property list."
         )]
        public static void OutlineRXYZ_OnCommand(CommandEventArgs e)
        {
            InternalRXYZ_OnCommand(e, true);
        }

        [Usage("OutlineXYZ <x> <y> <w> <h> <z> <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name around a given bounding box. Optional constructor parameters. Optional set property list."
         )]
        public static void OutlineXYZ_OnCommand(CommandEventArgs e)
        {
            InternalXYZ_OnCommand(e, true);
        }

        [Usage("OutlineZ <z> <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name around a targeted bounding box at a fixed Z location. Optional constructor parameters. Optional set property list."
         )]
        public static void OutlineZ_OnCommand(CommandEventArgs e)
        {
            InternalZ_OnCommand(e, true);
        }

        [Usage("OutlineAvg <name> [params] [set {<propertyName> <value> ...}]"), Description(
             "Tiles an item or npc by name around a targeted bounding box on the map's average Z elevation. Optional constructor parameters. Optional set property list."
         )]
        public static void OutlineAvg_OnCommand(CommandEventArgs e)
        {
            InternalAvg_OnCommand(e, true);
        }

        private enum TileZType
        {
            Start,
            Fixed,
            MapAverage
        }

        private class TileState
        {
            public readonly string[] m_Args;
            public readonly int m_FixedZ;
            public readonly bool m_Outline;
            public readonly TileZType m_ZType;

            public TileState(TileZType zType, int fixedZ, string[] args, bool outline)
            {
                m_ZType = zType;
                m_FixedZ = fixedZ;
                m_Args = args;
                m_Outline = outline;
            }
        }
    }
}
