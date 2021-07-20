using Server.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Server
{
    public static class packLoader
    {
        public class InitStats
        {
            [JsonIgnore]
            public string Name {get;set;}
            [CommandProperty(AccessLevel.GameMaster)]
            public int minStr { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int maxStr { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int minDex { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int maxDex { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int minInt { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int maxInt { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int minDmg { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int maxDmg { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int minHits { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int maxHits { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public double ActiveSpeed { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public double PassiveSpeed { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int AgrRange { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public double atkSkill { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int FightMode { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int VirtualArmor { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int HitPoisonChance { get; set; } = -1;
            [CommandProperty(AccessLevel.GameMaster)]
            public int HitPoison { get; set; } = -1;
        }
        public class LootItem
        {
            [JsonIgnore]
            public string PackName { get; set; }
            [JsonIgnore]
            public List<LootItem> _savedpack { get; set; }

            public string TypeName { get; set; }
            [CommandProperty(AccessLevel.GameMaster)]
            public bool AtSpawnTime { get; set; }
            [CommandProperty(AccessLevel.GameMaster)]
            [JsonIgnore]
            public string Name
            {
                get
                {
                    return TypeName;
                }
            }
            [JsonIgnore]
            public Type Item { get; set; }
            [CommandProperty(AccessLevel.GameMaster)]
            public double DropChance { get; set; }
            [CommandProperty(AccessLevel.GameMaster)]
            public string Quantity { get; set; } = "1";
            [CommandProperty(AccessLevel.GameMaster)]
            public int maxProps { get; set; }
            [CommandProperty(AccessLevel.GameMaster)]
            public int minIntensity { get; set; }
            [CommandProperty(AccessLevel.GameMaster)]
            public int maxIntensity { get; set; }
            public int Idx { get; set; }
            public bool IsDestPack { get; set; }
            public DateTime CreateDT { get; set; }

        }
        public class Pack
        {
            public List<LootItem> LootItems { get; set; }
            public InitStats Stats { get; set; } = new InitStats();

        }

        private static string path = Path.Combine(Core.BaseDirectory, "Data/LootPacks.json");
        private static Dictionary<string, Pack> Packs { get;set;}
        private static void Save() => JsonConfig.Serialize(path, Packs);
        public static Dictionary<string, Pack> GetPacks() => Packs;
        public static List<LootItem> GetPackLootByName(string Name) => Packs.GetValueOrDefault(Name)?.LootItems;
        public static Pack GetPackByName(string Name) => Packs.GetValueOrDefault(Name);
        public static bool PackExist(string Name) => Packs.ContainsKey(Name);
        public static void DeletePack(string Name) { if (Packs.Remove(Name)) Save(); }
        

        internal static void LoadPacks()
        {
            Console.Write("Lootpack: Loading...");
            var stopwatch = Stopwatch.StartNew();
            Packs = JsonConfig.Deserialize<Dictionary<string, Pack>>(path);
            //File not found
            if (Packs == null) Packs = new Dictionary<string, Pack>();
            Utility.PushColor(ConsoleColor.Green);
            Console.Write("done");
            Utility.PopColor();
            Console.WriteLine(
                " ({0} packs) ({1:F2} seconds)",
                Packs.Count,
                stopwatch.Elapsed.TotalSeconds
            );
        }

        public static void AddToPack(string Name, object Pack)
        {
            if (Packs.ContainsKey(Name))
            {
                if (Pack is List<LootItem> pck)
                {
                    Packs[Name].LootItems = pck;
                }
                else if (Pack is InitStats stat)
                {
                    Packs[Name].Stats = stat;
                }
            }
            else
            {
                if (Pack is List<LootItem> pck)
                {
                    Packs.Add(Name, new Pack() { LootItems = pck });
                }
                else if (Pack is InitStats stat)
                {
                    Packs.Add(Name, new Pack() { Stats = stat });
                }

            }
            Save();
        }


    }
}
