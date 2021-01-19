using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.BulkOrders
{
    public class SmallBulkEntry
    {
        private static Dictionary<string, Dictionary<string, SmallBulkEntry[]>> m_Cache;

        public SmallBulkEntry(Type type, int number, int graphic)
        {
            Type = type;
            Number = number;
            Graphic = graphic;
        }

        public Type Type { get; }

        public int Number { get; }

        public int Graphic { get; }

        public static SmallBulkEntry[] BlacksmithWeapons => GetEntries("Blacksmith", "weapons");

        public static SmallBulkEntry[] BlacksmithArmor => GetEntries("Blacksmith", "armor");

        public static SmallBulkEntry[] TailorCloth => GetEntries("Tailoring", "cloth");

        public static SmallBulkEntry[] TailorLeather => GetEntries("Tailoring", "leather");

        public static SmallBulkEntry[] GetEntries(string type, string name)
        {
            if (m_Cache == null)
            {
                m_Cache = new Dictionary<string, Dictionary<string, SmallBulkEntry[]>>();
            }

            if (!m_Cache.TryGetValue(type, out var table))
            {
                m_Cache[type] = table = new Dictionary<string, SmallBulkEntry[]>();
            }

            if (!table.TryGetValue(name, out var entries))
            {
                table[name] = entries = LoadEntries(type, name);
            }

            return entries;
        }

        public static SmallBulkEntry[] LoadEntries(string type, string name) =>
            LoadEntries($"Data/Bulk Orders/{type}/{name}.cfg");

        public static SmallBulkEntry[] LoadEntries(string path)
        {
            path = Path.Combine(Core.BaseDirectory, path);

            var list = new List<SmallBulkEntry>();

            if (File.Exists(path))
            {
                using var ip = new StreamReader(path);
                string line;

                while ((line = ip.ReadLine()) != null)
                {
                    if (line.Length == 0 || line.StartsWithOrdinal("#"))
                    {
                        continue;
                    }

                    try
                    {
                        var split = line.Split('\t');

                        if (split.Length >= 2)
                        {
                            var type = AssemblyHandler.FindTypeByName(split[0]);
                            var graphic = Utility.ToInt32(split[^1]);

                            if (type != null && graphic > 0)
                            {
                                list.Add(
                                    new SmallBulkEntry(
                                        type,
                                        graphic < 0x4000 ? 1020000 + graphic : 1078872 + graphic,
                                        graphic
                                    )
                                );
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            return list.ToArray();
        }
    }
}
