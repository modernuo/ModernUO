using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SpawnProcessor;

public static class SpawnProcessor
{
    // NPC types that should use spawnBounds (town folk, vendors, stationary NPCs)
    private static readonly HashSet<string> TownNpcTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Guards
        "OrderGuard", "ChaosGuard", "WarriorGuard",

        // Town Criers
        "TownCrier",

        // Escortables and Town NPCs
        "EscortableMage", "SeekerOfAdventure", "Noble", "Peasant", "Beggar",
        "HireBard", "HireBardArcher", "HireBeggar", "HireFighter", "HireMage",
        "HireRanger", "HireRangerArcher", "HireSailor", "HireThief", "HirePaladin",

        // Vendors (general pattern)
        "Alchemist", "AnimalTrainer", "Architect", "Armorer", "Baker", "Banker",
        "Bard", "Barkeeper", "Blacksmith", "Bowyer", "Butcher", "Carpenter",
        "Cobbler", "Cook", "Farmer", "Fisherman", "Fletcher", "Furtrader",
        "Glassblower", "Guildmaster", "Healer", "Herbalist", "HolyMage", "Innkeeper",
        "Jeweler", "Judge", "Leatherworker", "Librarian", "Mage", "Mapmaker",
        "Miller", "Miner", "Monk", "Necromancer", "Paladin", "Provisioner",
        "Ranger", "Reagentvendor", "Sage", "Scribe", "Sculptor", "Shipwright",
        "Stablemaster", "Stoneworker", "Tailor", "Tanner", "Thief", "Tinker",
        "TownGuard", "Veterinarian", "Waiter", "Waitress", "Weaponsmith", "Weaver",

        // Specific named NPCs (Heartwood elves etc.)
        "Olaeni", "Bolaevin", "athialon", "abbein", "taellia", "vicaie", "mallew",
        "jothan", "alethanian", "Tyeelor", "aneen", "Alelle", "Daelas", "Aluniol", "Rebinil",

        // Static treasure chests
        "TreasureChestLevel1", "TreasureChestLevel2", "TreasureChestLevel3",
        "TreasureChestLevel4", "TreasureChestLevel5"
    };

    // NPC types that should NOT use spawnBounds (wildlife, monsters with large roaming areas)
    private static readonly HashSet<string> WildlifeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Wildlife
        "Boar", "Cougar", "Goat", "Horse", "Panther", "Pig", "Sheep",
        "BlackBear", "GrizzlyBear", "BrownBear", "TimberWolf", "GreyWolf",
        "WanderingHealer", "Bird", "Eagle", "GreatHart", "Hind", "Bull", "Cow",
        "Chicken", "Rabbit", "Cat", "Dog", "Rat", "Snake", "Gorilla", "Llama",
        "PackHorse", "PackLlama", "Walrus", "Dolphin", "SeaSerpent", "Kraken",

        // Monsters that roam
        "Ettin", "Ogre", "Troll", "Orc", "OrcBomber", "OrcBrute", "OrcCaptain",
        "Lizardman", "Ratman", "RatmanArcher", "RatmanMage",
        "Harpy", "StoneHarpy", "Mongbat", "GiantSpider", "Wisp",
        "Skeleton", "Zombie", "Ghoul", "Spectre", "Wraith", "Shade", "Mummy",
        "Lich", "LichLord", "BoneKnight", "BoneMagi", "SkeletalKnight", "SkeletalMage",
        "Daemon", "Imp", "Gargoyle", "StoneGargoyle", "FireGargoyle",
        "Dragon", "Drake", "Wyrm", "GreaterDragon", "AncientWyrm",
        "Elemental", "EarthElemental", "FireElemental", "WaterElemental", "AirElemental",
        "HordeMinion", "GiantRat", "Slime", "DireWolf", "HellHound", "HellCat"
    };

    // Files that contain outdoor/wildlife spawns (don't convert these)
    private static readonly HashSet<string> OutdoorFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "WildLife.json", "Outdoors.json", "SeaLife.json", "LostLands.json",
        "Reagents.json", "FelCropsLS.json", "TramCropsLS.json"
    };

    // Files that contain town/vendor spawns (should convert)
    private static readonly HashSet<string> TownFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "TownsPeople.json", "TownsLife.json", "Vendors.json", "Towns.json"
    };

    public static void Main(string[] args)
    {
        var basePath = args.Length > 0 ? args[0] : @"C:\Repositories\ModernUO\Distribution\Data\Spawns";
        var reportPath = Path.Combine(basePath, "spawn_changes_report.md");

        Console.WriteLine($"Processing spawns in: {basePath}");

        var postUomlPath = Path.Combine(basePath, "post-uoml");
        var uomlPath = Path.Combine(basePath, "uoml");
        var sharedPath = Path.Combine(basePath, "shared");

        // Collect all spawns
        var postUomlSpawns = CollectSpawns(postUomlPath);
        var uomlSpawns = CollectSpawns(uomlPath);

        Console.WriteLine($"Found {postUomlSpawns.Count} spawns in post-uoml");
        Console.WriteLine($"Found {uomlSpawns.Count} spawns in uoml");

        // Find shared spawns (same GUID in both)
        var sharedGuids = postUomlSpawns.Keys.Intersect(uomlSpawns.Keys).ToHashSet();
        Console.WriteLine($"Found {sharedGuids.Count} shared GUIDs");

        // Build report
        var report = new List<string>
        {
            "# Spawn Changes Report",
            "",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "",
            "## Summary",
            "",
            $"- Post-UOML spawns: {postUomlSpawns.Count}",
            $"- UOML spawns: {uomlSpawns.Count}",
            $"- Shared spawns (same GUID): {sharedGuids.Count}",
            "",
            "## Categories",
            "",
            "### Spawns to Convert to SpawnBounds",
            "",
            "These spawns have small homeRange OR are town folk/vendors:",
            "",
            "| GUID | File | Type | HomeRange | Entries | Reason |",
            "|------|------|------|-----------|---------|--------|"
        };

        var changedSpawns = new List<(string guid, string file, string reason, SpawnInfo info)>();
        var unchangedSpawns = new List<(string guid, string file, string reason, SpawnInfo info)>();
        var ambiguousSpawns = new List<(string guid, string file, string reason, SpawnInfo info)>();

        // Process all spawns
        var allSpawns = postUomlSpawns.Concat(uomlSpawns)
            .GroupBy(x => x.Key)
            .Select(g => g.First())
            .ToList();

        foreach (var kvp in allSpawns)
        {
            var guid = kvp.Key;
            var info = kvp.Value;
            var category = CategorizeSpawn(info);

            switch (category.action)
            {
                case "convert":
                    changedSpawns.Add((guid, info.RelativePath, category.reason, info));
                    break;
                case "unchanged":
                    unchangedSpawns.Add((guid, info.RelativePath, category.reason, info));
                    break;
                default:
                    ambiguousSpawns.Add((guid, info.RelativePath, category.reason, info));
                    break;
            }
        }

        // Add changed spawns to report
        foreach (var (guid, file, reason, info) in changedSpawns.OrderBy(x => x.file))
        {
            var entries = string.Join(", ", info.EntryNames.Take(3));
            if (info.EntryNames.Count > 3) entries += "...";
            report.Add($"| `{guid[..8]}...` | {file} | Spawner | {info.HomeRange} | {entries} | {reason} |");
        }

        report.Add("");
        report.Add("### Spawns NOT Converting (Large Area Wildlife/Monsters)");
        report.Add("");
        report.Add("These spawns have large homeRange AND are wildlife/monsters:");
        report.Add("");
        report.Add("| GUID | File | HomeRange | Entries | Reason |");
        report.Add("|------|------|-----------|---------|--------|");

        foreach (var (guid, file, reason, info) in unchangedSpawns.Take(50).OrderBy(x => x.file))
        {
            var entries = string.Join(", ", info.EntryNames.Take(3));
            if (info.EntryNames.Count > 3) entries += "...";
            report.Add($"| `{guid[..8]}...` | {file} | {info.HomeRange} | {entries} | {reason} |");
        }

        if (unchangedSpawns.Count > 50)
        {
            report.Add($"| ... | ... | ... | ... | ({unchangedSpawns.Count - 50} more) |");
        }

        report.Add("");
        report.Add("### Ambiguous Spawns (Need Review)");
        report.Add("");
        report.Add("| GUID | File | HomeRange | Entries | Notes |");
        report.Add("|------|------|-----------|---------|-------|");

        foreach (var (guid, file, reason, info) in ambiguousSpawns.OrderBy(x => x.file))
        {
            var entries = string.Join(", ", info.EntryNames.Take(3));
            if (info.EntryNames.Count > 3) entries += "...";
            report.Add($"| `{guid[..8]}...` | {file} | {info.HomeRange} | {entries} | {reason} |");
        }

        report.Add("");
        report.Add("## Shared Spawns (Deduplicated)");
        report.Add("");
        report.Add($"The following {sharedGuids.Count} spawns appear in both post-uoml and uoml with the same GUID.");
        report.Add("These should be moved to a `shared` folder.");
        report.Add("");

        // Group shared spawns by file
        var sharedByFile = sharedGuids
            .Select(g => postUomlSpawns[g])
            .GroupBy(s => Path.GetFileName(s.RelativePath))
            .OrderBy(g => g.Key);

        foreach (var group in sharedByFile)
        {
            report.Add($"### {group.Key}: {group.Count()} spawns");
        }

        // Write report
        File.WriteAllLines(reportPath, report);
        Console.WriteLine($"\nReport written to: {reportPath}");

        // Now actually process the files
        Console.WriteLine("\nProcessing spawn files...");
        ProcessSpawnFiles(basePath, sharedGuids, postUomlSpawns, uomlSpawns, changedSpawns);

        Console.WriteLine("\nDone!");
    }

    private static Dictionary<string, SpawnInfo> CollectSpawns(string basePath)
    {
        var spawns = new Dictionary<string, SpawnInfo>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(basePath))
            return spawns;

        foreach (var file in Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(file);
                var array = JsonNode.Parse(json)?.AsArray();
                if (array == null) continue;

                var relativePath = Path.GetRelativePath(Path.GetDirectoryName(basePath)!, file);

                foreach (var node in array)
                {
                    if (node == null) continue;

                    var guid = node["guid"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(guid)) continue;

                    var info = new SpawnInfo
                    {
                        Guid = guid,
                        FilePath = file,
                        RelativePath = relativePath,
                        Location = ParseLocation(node["location"]),
                        Map = node["map"]?.GetValue<string>() ?? "",
                        HomeRange = node["homeRange"]?.GetValue<int>() ?? 0,
                        WalkingRange = node["walkingRange"]?.GetValue<int>() ?? 0,
                        Count = node["count"]?.GetValue<int>() ?? 1,
                        EntryNames = new List<string>(),
                        JsonNode = node
                    };

                    var entries = node["entries"]?.AsArray();
                    if (entries != null)
                    {
                        foreach (var entry in entries)
                        {
                            var name = entry?["name"]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(name))
                                info.EntryNames.Add(name);
                        }
                    }

                    spawns[guid] = info;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {file}: {ex.Message}");
            }
        }

        return spawns;
    }

    private static (int x, int y, int z) ParseLocation(JsonNode? node)
    {
        if (node is JsonArray arr && arr.Count >= 3)
        {
            return (arr[0]!.GetValue<int>(), arr[1]!.GetValue<int>(), arr[2]!.GetValue<int>());
        }
        return (0, 0, 0);
    }

    private static (string action, string reason) CategorizeSpawn(SpawnInfo info)
    {
        var fileName = Path.GetFileName(info.FilePath);

        // Check if it's in a file we know should/shouldn't be converted
        if (OutdoorFiles.Contains(fileName))
        {
            return ("unchanged", "Outdoor/Wildlife file");
        }

        if (TownFiles.Contains(fileName))
        {
            return ("convert", "Town/Vendor file");
        }

        // Check homeRange
        if (info.HomeRange <= 5)
        {
            // Small homeRange - likely town NPCs or stationary spawns
            return ("convert", $"Small homeRange ({info.HomeRange})");
        }

        if (info.HomeRange >= 30)
        {
            // Large homeRange - likely roaming wildlife/monsters
            return ("unchanged", $"Large homeRange ({info.HomeRange})");
        }

        // Check entry types
        var isTownNpc = info.EntryNames.Any(e => TownNpcTypes.Contains(e));
        var isWildlife = info.EntryNames.Any(e => WildlifeTypes.Contains(e));

        if (isTownNpc && !isWildlife)
        {
            return ("convert", "Town NPC type");
        }

        if (isWildlife && !isTownNpc)
        {
            return ("unchanged", "Wildlife/Monster type");
        }

        // Medium homeRange with mixed or unknown types
        if (info.HomeRange <= 12 && isTownNpc)
        {
            return ("convert", $"Medium homeRange ({info.HomeRange}) with town NPCs");
        }

        if (info.HomeRange > 12 && isWildlife)
        {
            return ("unchanged", $"Medium-large homeRange ({info.HomeRange}) with wildlife");
        }

        // Dungeon files - generally keep homeRange unless very small
        if (fileName.Contains("Dungeon", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Deceit", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Despise", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Destard", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Shame", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Wrong", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Covetous", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Hythloth", StringComparison.OrdinalIgnoreCase))
        {
            if (info.HomeRange <= 8)
            {
                return ("convert", $"Dungeon with small homeRange ({info.HomeRange})");
            }
            return ("unchanged", $"Dungeon spawn");
        }

        // Default based on homeRange
        if (info.HomeRange <= 10)
        {
            return ("convert", $"HomeRange {info.HomeRange} <= 10");
        }

        return ("ambiguous", $"Mixed/unknown (homeRange={info.HomeRange})");
    }

    private static void ProcessSpawnFiles(
        string basePath,
        HashSet<string> sharedGuids,
        Dictionary<string, SpawnInfo> postUomlSpawns,
        Dictionary<string, SpawnInfo> uomlSpawns,
        List<(string guid, string file, string reason, SpawnInfo info)> spawnsToConvert)
    {
        var postUomlPath = Path.Combine(basePath, "post-uoml");
        var uomlPath = Path.Combine(basePath, "uoml");
        var sharedPath = Path.Combine(basePath, "shared");

        var spawnsToConvertSet = spawnsToConvert.Select(x => x.guid).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Process each JSON file
        foreach (var folder in new[] { postUomlPath, uomlPath })
        {
            if (!Directory.Exists(folder)) continue;

            foreach (var file in Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories))
            {
                ProcessSingleFile(file, basePath, sharedGuids, spawnsToConvertSet, folder == postUomlPath);
            }
        }

        // Create shared folder structure and move shared spawns
        CreateSharedFolder(basePath, sharedGuids, postUomlSpawns, spawnsToConvertSet);
    }

    private static void ProcessSingleFile(
        string filePath,
        string basePath,
        HashSet<string> sharedGuids,
        HashSet<string> spawnsToConvert,
        bool isPostUoml)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var array = JsonNode.Parse(json)?.AsArray();
            if (array == null) return;

            var modified = false;
            var toRemove = new List<int>();

            for (var i = 0; i < array.Count; i++)
            {
                var node = array[i];
                if (node == null) continue;

                var guid = node["guid"]?.GetValue<string>();
                if (string.IsNullOrEmpty(guid)) continue;

                // If shared, mark for removal from individual folders
                if (sharedGuids.Contains(guid))
                {
                    toRemove.Add(i);
                    modified = true;
                    continue;
                }

                // If should convert to spawnBounds
                if (spawnsToConvert.Contains(guid) && node["homeRange"] != null && node["spawnBounds"] == null)
                {
                    ConvertToSpawnBounds(node);
                    modified = true;
                }
            }

            // Remove shared spawns from individual files
            foreach (var idx in toRemove.OrderByDescending(x => x))
            {
                array.RemoveAt(idx);
            }

            if (modified)
            {
                // If all spawns were removed, delete the file
                if (array.Count == 0)
                {
                    File.Delete(filePath);
                    Console.WriteLine($"Deleted (empty): {Path.GetRelativePath(basePath, filePath)}");
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    File.WriteAllText(filePath, array.ToJsonString(options));
                    Console.WriteLine($"Modified: {Path.GetRelativePath(basePath, filePath)}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing {filePath}: {ex.Message}");
        }
    }

    private static void ConvertToSpawnBounds(JsonNode node)
    {
        var location = ParseLocation(node["location"]);
        var homeRange = node["homeRange"]?.GetValue<int>() ?? 0;

        // Create spawnBounds
        var spawnBounds = new JsonObject
        {
            ["x1"] = location.x - homeRange,
            ["y1"] = location.y - homeRange,
            ["z1"] = location.z,
            ["x2"] = location.x + homeRange + 1,
            ["y2"] = location.y + homeRange + 1,
            ["z2"] = location.z + 16
        };

        node.AsObject()["spawnBounds"] = spawnBounds;
        node.AsObject().Remove("homeRange");
    }

    private static void CreateSharedFolder(
        string basePath,
        HashSet<string> sharedGuids,
        Dictionary<string, SpawnInfo> spawns,
        HashSet<string> spawnsToConvert)
    {
        var sharedPath = Path.Combine(basePath, "shared");

        // Group shared spawns by their relative folder structure
        var sharedByFolder = sharedGuids
            .Where(g => spawns.ContainsKey(g))
            .Select(g => spawns[g])
            .GroupBy(s =>
            {
                // Get the folder structure after post-uoml/ or uoml/
                var rel = s.RelativePath;
                var parts = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (parts.Length >= 2)
                {
                    // Return everything except the first folder (post-uoml/uoml) and the filename
                    return string.Join(Path.DirectorySeparatorChar, parts.Skip(1).Take(parts.Length - 2));
                }
                return "";
            });

        foreach (var folderGroup in sharedByFolder)
        {
            var folderPath = Path.Combine(sharedPath, folderGroup.Key);
            Directory.CreateDirectory(folderPath);

            // Group by original filename
            var byFile = folderGroup.GroupBy(s => Path.GetFileName(s.FilePath));

            foreach (var fileGroup in byFile)
            {
                var outputFile = Path.Combine(folderPath, fileGroup.Key);
                var spawnsArray = new JsonArray();

                foreach (var info in fileGroup.OrderBy(s => s.Guid))
                {
                    var node = info.JsonNode!.DeepClone();

                    // Convert to spawnBounds if applicable
                    if (spawnsToConvert.Contains(info.Guid) && node["homeRange"] != null && node["spawnBounds"] == null)
                    {
                        ConvertToSpawnBounds(node);
                    }

                    spawnsArray.Add(node);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                File.WriteAllText(outputFile, spawnsArray.ToJsonString(options));
                Console.WriteLine($"Created shared: {Path.GetRelativePath(basePath, outputFile)} ({spawnsArray.Count} spawns)");
            }
        }
    }

    private class SpawnInfo
    {
        public string Guid { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public (int x, int y, int z) Location { get; set; }
        public string Map { get; set; } = "";
        public int HomeRange { get; set; }
        public int WalkingRange { get; set; }
        public int Count { get; set; }
        public List<string> EntryNames { get; set; } = new();
        public JsonNode? JsonNode { get; set; }
    }
}
