using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Diagnostics;

namespace Server.Commands
{
    public static class Profiling
    {
        public static void Initialize()
        {
            CommandSystem.Register("DumpTimers", AccessLevel.Administrator, DumpTimers_OnCommand);
            CommandSystem.Register("CountObjects", AccessLevel.Administrator, CountObjects_OnCommand);
            CommandSystem.Register("ProfileWorld", AccessLevel.Administrator, ProfileWorld_OnCommand);
            CommandSystem.Register("TraceInternal", AccessLevel.Administrator, TraceInternal_OnCommand);
            CommandSystem.Register("TraceExpanded", AccessLevel.Administrator, TraceExpanded_OnCommand);
            CommandSystem.Register("WriteProfiles", AccessLevel.Administrator, WriteProfiles_OnCommand);
            CommandSystem.Register("SetProfiles", AccessLevel.Administrator, SetProfiles_OnCommand);
        }

        [Usage("WriteProfiles")]
        [Description("Generates a log files containing performance diagnostic information.")]
        public static void WriteProfiles_OnCommand(CommandEventArgs e)
        {
            try
            {
                using var sw = new StreamWriter("profiles.log", true);
                sw.WriteLine("# Dump on {0:f}", Core.Now);
                sw.WriteLine($"# Core profiling for {Core.ProfileTime}");

                sw.WriteLine("# Packet send");
                BaseProfile.WriteAll(sw, PacketSendProfile.Profiles);
                sw.WriteLine();

                sw.WriteLine("# Packet receive");
                BaseProfile.WriteAll(sw, PacketReceiveProfile.Profiles);
                sw.WriteLine();

                sw.WriteLine("# Timer");
                BaseProfile.WriteAll(sw, TimerProfile.Profiles);
                sw.WriteLine();

                sw.WriteLine("# Gump response");
                BaseProfile.WriteAll(sw, GumpProfile.Profiles);
                sw.WriteLine();

                sw.WriteLine("# Target response");
                BaseProfile.WriteAll(sw, TargetProfile.Profiles);
                sw.WriteLine();
            }
            catch
            {
                // ignored
            }
        }

        [Usage("SetProfiles [true | false]"),
         Description("Enables, disables, or toggles the state of core packet and timer profiling.")]
        public static void SetProfiles_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                Core.Profiling = e.GetBoolean(0);
            }
            else
            {
                Core.Profiling = !Core.Profiling;
            }

            if (Core.Profiling)
            {
                e.Mobile.SendMessage($"Profiling has been enabled.");
            }
            else
            {
                e.Mobile.SendMessage($"Profiling has been disabled.");
            }
        }

        [Usage("DumpTimers"),
         Description("Generates a log file of all currently executing timers. Used for tracing timer leaks.")]
        public static void DumpTimers_OnCommand(CommandEventArgs e)
        {
            try
            {
                using var sw = new StreamWriter("timerdump.log", true);
                Timer.DumpInfo(sw);
                e.Mobile.SendMessage("Timers dumped to timerdump.log");
            }
            catch
            {
                // ignored
            }
        }

        [Usage("CountObjects")]
        [Description("Generates a log file detailing all item and mobile types in the world.")]
        public static void CountObjects_OnCommand(CommandEventArgs e)
        {
            using (var op = new StreamWriter("objects.log"))
            {
                var table = new Dictionary<Type, int>();

                foreach (var item in World.Items.Values)
                {
                    var type = item.GetType();

                    table[type] = (table.TryGetValue(type, out var value) ? value : 0) + 1;
                }

                var items = table.ToList();
                table.Clear();

                foreach (var m in World.Mobiles.Values)
                {
                    var type = m.GetType();

                    table[type] = (table.TryGetValue(type, out var value) ? value : 0) + 1;
                }

                var mobiles = table.ToList();

                items.Sort(new CountSorter());
                mobiles.Sort(new CountSorter());

                op.WriteLine("# Object count table generated on {0}", Core.Now);
                op.WriteLine();
                op.WriteLine();

                op.WriteLine("# Items:");

                items.ForEach(
                    kvp =>
                        op.WriteLine("{0}\t{1:F2}%\t{2}", kvp.Value, 100.0 * kvp.Value / World.Items.Count, kvp.Key)
                );

                op.WriteLine();
                op.WriteLine();

                op.WriteLine("#Mobiles:");

                mobiles.ForEach(
                    kvp =>
                        op.WriteLine("{0}\t{1:F2}%\t{2}", kvp.Value, 100.0 * kvp.Value / World.Mobiles.Count, kvp.Key)
                );
            }

            e.Mobile.SendMessage("Object table has been generated. See the file : objects.log");
        }

        [Usage("TraceExpanded")]
        [Description("Generates a log file describing all items using expanded memory.")]
        public static void TraceExpanded_OnCommand(CommandEventArgs e)
        {
            var typeTable = new Dictionary<Type, int[]>();

            foreach (var item in World.Items.Values)
            {
                var flags = item.GetExpandFlags();

                if ((flags & ~(ExpandFlag.TempFlag | ExpandFlag.SaveFlag)) == 0)
                {
                    continue;
                }

                var itemType = item.GetType();

                do
                {
                    if (!typeTable.TryGetValue(itemType, out var countTable))
                    {
                        typeTable[itemType] = countTable = new int[9];
                    }

                    if ((flags & ExpandFlag.Name) != 0)
                    {
                        ++countTable[0];
                    }

                    if ((flags & ExpandFlag.Items) != 0)
                    {
                        ++countTable[1];
                    }

                    if ((flags & ExpandFlag.Bounce) != 0)
                    {
                        ++countTable[2];
                    }

                    if ((flags & ExpandFlag.Holder) != 0)
                    {
                        ++countTable[3];
                    }

                    if ((flags & ExpandFlag.Blessed) != 0)
                    {
                        ++countTable[4];
                    }

                    /*if (( flags & ExpandFlag.TempFlag ) != 0)
                      ++countTable[5];

                    if (( flags & ExpandFlag.SaveFlag ) != 0)
                      ++countTable[6];*/

                    if ((flags & ExpandFlag.Weight) != 0)
                    {
                        ++countTable[7];
                    }

                    if ((flags & ExpandFlag.Spawner) != 0)
                    {
                        ++countTable[8];
                    }

                    itemType = itemType.BaseType;
                } while (itemType != typeof(object));
            }

            try
            {
                using var op = new StreamWriter("expandedItems.log", true);
                string[] names =
                {
                    "Name",
                    "Items",
                    "Bounce",
                    "Holder",
                    "Blessed",
                    "TempFlag",
                    "SaveFlag",
                    "Weight",
                    "Spawner"
                };

                var list = typeTable.ToList();

                list.Sort(new CountsSorter());

                foreach (var kvp in list)
                {
                    var countTable = kvp.Value;

                    op.WriteLine("# {0}", kvp.Key.FullName);

                    for (var i = 0; i < countTable.Length; ++i)
                    {
                        if (countTable[i] > 0)
                        {
                            op.WriteLine("{0}\t{1:N0}", names[i], countTable[i]);
                        }
                    }

                    op.WriteLine();
                }
            }
            catch
            {
                // ignored
            }
        }

        [Usage("TraceInternal")]
        [Description("Generates a log file describing all items in the 'internal' map.")]
        public static void TraceInternal_OnCommand(CommandEventArgs e)
        {
            var totalCount = 0;
            var table = new Dictionary<Type, int[]>();

            foreach (var item in World.Items.Values)
            {
                if (item.Parent != null || item.Map != Map.Internal)
                {
                    continue;
                }

                ++totalCount;

                var type = item.GetType();

                if (table.TryGetValue(type, out var parms))
                {
                    parms[0]++;
                    parms[1] += item.Amount;
                }
                else
                {
                    table[type] = new[] { 1, item.Amount };
                }
            }

            using var op = new StreamWriter("internal.log");
            op.WriteLine("# {0} items found", totalCount);
            op.WriteLine("# {0} different types", table.Count);
            op.WriteLine();
            op.WriteLine();
            op.WriteLine("Type\t\tCount\t\tAmount\t\tAvg. Amount");

            foreach (var de in table)
            {
                var parms = de.Value;

                op.WriteLine("{0}\t\t{1}\t\t{2}\t\t{3:F2}", de.Key.Name, parms[0], parms[1], (double)parms[1] / parms[0]);
            }
        }

        [Usage("ProfileWorld"),
         Description("Prints the amount of data serialized for every object type in your world file.")]
        public static void ProfileWorld_OnCommand(CommandEventArgs e)
        {
            ProfileWorld("items", "worldprofile_items.log");
            ProfileWorld("mobiles", "worldprofile_mobiles.log");
        }

        public static void ProfileWorld(string type, string opFile)
        {
            try
            {
                var types = new List<Type>();

                using (var bin = new BinaryReader(
                    new FileStream(
                        string.Format("Saves/{0}/{0}.tdb", type),
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read
                    )
                ))
                {
                    var count = bin.ReadInt32();

                    for (var i = 0; i < count; ++i)
                    {
                        types.Add(AssemblyHandler.FindTypeByFullName(bin.ReadString()));
                    }
                }

                long total = 0;

                var table = new Dictionary<Type, int>();

                using (var bin = new BinaryReader(
                    new FileStream(
                        string.Format("Saves/{0}/{0}.idx", type),
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read
                    )
                ))
                {
                    var count = bin.ReadInt32();

                    for (var i = 0; i < count; ++i)
                    {
                        var typeID = bin.ReadInt32();
                        var serial = bin.ReadInt32();
                        var pos = bin.ReadInt64();
                        var length = bin.ReadInt32();
                        var objType = types[typeID];

                        while (objType != null && objType != typeof(object))
                        {
                            table[objType] = length + (table.TryGetValue(objType, out var value) ? value : 0);
                            objType = objType.BaseType;
                            total += length;
                        }
                    }
                }

                var list = table.ToList();

                list.Sort(new CountSorter());

                using var op = new StreamWriter(opFile);
                op.WriteLine("# Profile of world {0}", type);
                op.WriteLine("# Generated on {0}", Core.Now);
                op.WriteLine();
                op.WriteLine();

                list.ForEach(
                    kvp =>
                        op.WriteLine("{0}\t{1:F2}%\t{2}", kvp.Value, 100.0 * kvp.Value / total, kvp.Key)
                );
            }
            catch
            {
                // ignored
            }
        }

        private class CountSorter : IComparer<KeyValuePair<Type, int>>
        {
            public int Compare(KeyValuePair<Type, int> x, KeyValuePair<Type, int> y)
            {
                var aCount = x.Value;
                var bCount = y.Value;

                var v = -aCount.CompareTo(bCount);

                return v != 0 ? v : string.CompareOrdinal(x.Key.FullName, y.Key.FullName);
            }
        }

        private class CountsSorter : IComparer<KeyValuePair<Type, int[]>>
        {
            public int Compare(KeyValuePair<Type, int[]> x, KeyValuePair<Type, int[]> y)
            {
                var aCount = 0;
                foreach (var val in x.Value)
                {
                    aCount += val;
                }

                var bCount = 0;
                foreach (var val in y.Value)
                {
                    bCount += val;
                }

                var v = -aCount.CompareTo(bCount);

                return v != 0 ? v : string.CompareOrdinal(x.Key.FullName, y.Key.FullName);
            }
        }
    }
}
