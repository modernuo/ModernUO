using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SpawnProcessor;

[Flags]
public enum TileFlag : ulong
{
    None = 0x00000000,
    Background = 0x00000001,
    Weapon = 0x00000002,
    Transparent = 0x00000004,
    Translucent = 0x00000008,
    Wall = 0x00000010,
    Damaging = 0x00000020,
    Impassable = 0x00000040,
    Wet = 0x00000080,
    Surface = 0x00000200,
    Bridge = 0x00000400,
    Window = 0x00001000,
    NoShoot = 0x00002000,
    Internal = 0x00010000,  // Tile should not be rendered
    Foliage = 0x00020000,
    Roof = 0x10000000,
    Door = 0x20000000,
}

public class TileInfo
{
    public string Name { get; set; } = "";
    public TileFlag Flags { get; set; }
    public int Height { get; set; }

    public bool IsSurface => (Flags & TileFlag.Surface) != 0;
    public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
    public bool IsRoof => (Flags & TileFlag.Roof) != 0;
    public bool IsFloor => IsSurface && !IsImpassable;
    public bool IsWall => (Flags & TileFlag.Wall) != 0;
    public bool IsBackground => (Flags & TileFlag.Background) != 0;
    public bool IsInternal => (Flags & TileFlag.Internal) != 0;
}

public class StaticTile
{
    public ushort Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public sbyte Z { get; set; }
    public TileInfo? Info { get; set; }
}

public enum SpawnerCategory
{
    /// <summary>HomeRange >= 50, too large for meaningful z-restriction</summary>
    LargeHomeRange,

    /// <summary>Spawner is in a multi-floor building - NEEDS z-restricted spawnBounds</summary>
    MultiFloorBuilding,

    /// <summary>Spawner is in a single-floor building with visible ceiling - REVIEW recommended</summary>
    SingleFloorBuilding,

    /// <summary>Spawner has ceiling but it's just invisible nodraw tiles</summary>
    NoDrawCeiling,

    /// <summary>Spawner is on floor tiles but no ceiling (ruins, outdoor structures)</summary>
    OpenRuins,

    /// <summary>Spawner is in a dungeon level transition area</summary>
    DungeonTransition,

    /// <summary>Spawner appears to be outdoors with no structure</summary>
    Outdoor,

    /// <summary>Needs manual review - uncertain classification</summary>
    NeedsReview
}

public class SpawnerAnalysis
{
    public string Guid { get; set; } = "";
    public string File { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public string Map { get; set; } = "";
    public int HomeRange { get; set; }
    public bool HasSpawnBounds { get; set; }
    public List<string> Entries { get; set; } = new();

    // Structure analysis
    public bool HasFloorBelow { get; set; }
    public bool HasVisibleCeilingAbove { get; set; }
    public bool HasNoDrawCeilingAbove { get; set; }
    public bool HasMultipleFloorLevels { get; set; }
    public string FloorTileName { get; set; } = "";
    public string CeilingTileName { get; set; } = "";
    public int FloorZ { get; set; }
    public int CeilingZ { get; set; }
    public List<int> AllFloorLevels { get; set; } = new();

    // Region analysis
    public bool IsInTownRegion { get; set; }
    public string RegionName { get; set; } = "";

    // Categorization
    public SpawnerCategory Category { get; set; } = SpawnerCategory.Outdoor;

    // Action recommendations - only add spawnBounds if in a town region
    public bool NeedsSpawnBounds => Category == SpawnerCategory.MultiFloorBuilding && HomeRange > 0 && !HasSpawnBounds && IsInTownRegion;
    // Non-town multi-floor buildings should be reviewed (most are ok to spawn on all floors except roof)
    public bool ShouldReview => HomeRange > 0 && !HasSpawnBounds && (
        Category == SpawnerCategory.NeedsReview ||
        Category == SpawnerCategory.SingleFloorBuilding ||
        (Category == SpawnerCategory.MultiFloorBuilding && !IsInTownRegion));
    public bool ShouldRemoveSpawnBounds => HasSpawnBounds &&
                                           (HomeRange == 0 ||  // HomeRange 0 doesn't need spawnBounds
                                            Category == SpawnerCategory.LargeHomeRange ||
                                            Category == SpawnerCategory.Outdoor ||
                                            Category == SpawnerCategory.OpenRuins ||
                                            Category == SpawnerCategory.NoDrawCeiling);

    public string CategoryReason { get; set; } = "";
    public string Recommendation { get; set; } = "";
}

public static class BuildingAnalyzer
{
    // Separate tile data for each client version
    private static Dictionary<int, TileInfo> _itemTileDataModern = new();
    private static Dictionary<int, TileInfo> _landTileDataModern = new();
    private static Dictionary<int, TileInfo> _itemTileDataUoml = new();
    private static Dictionary<int, TileInfo> _landTileDataUoml = new();

    // NoDraw tile IDs - invisible tiles used to force "inside" rendering mode
    private static readonly HashSet<ushort> NoDrawTileIds = new()
    {
        0x0001,  // nodraw static
        0x21BC,  // nodraw static (8636)
        0x63D3,  // nodraw static (25555)
        0x2198, 0x2199, 0x21A0, 0x21A1, 0x21A2, 0x21A3, 0x21A4
    };

    private static readonly HashSet<ushort> ConditionalNoDrawTileIds = new()
    {
        0x9E4C, 0x9E64, 0x9E65, 0x9E7D
    };

    private static bool IsNoDrawTile(ushort tileId, TileInfo? info)
    {
        if (NoDrawTileIds.Contains(tileId)) return true;
        if (ConditionalNoDrawTileIds.Contains(tileId))
        {
            if (info == null) return true;
            return !info.IsBackground && !info.IsSurface;
        }
        if (info != null && info.IsInternal) return true;
        if (info != null && info.Name.Equals("nodraw", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // Cave/dungeon floor tile names (case insensitive partial match)
    private static readonly string[] CaveFloorNames = {
        "cave", "rock", "stone", "dirt", "dungeon", "lava"
    };

    private static bool IsCaveFloor(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var lower = name.ToLowerInvariant();
        return CaveFloorNames.Any(c => lower.Contains(c));
    }

    private static readonly Dictionary<string, int> MapIndices = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Felucca", 0 }, { "Trammel", 1 }, { "Ilshenar", 2 },
        { "Malas", 3 }, { "Tokuno", 4 }, { "TerMur", 5 }
    };

    private static readonly Dictionary<int, (int Width, int Height, int BlockWidth, int BlockHeight)> MapDimensions = new()
    {
        { 0, (6144, 4096, 768, 512) }, { 1, (6144, 4096, 768, 512) },
        { 2, (2304, 1600, 288, 200) }, { 3, (2560, 2048, 320, 256) },
        { 4, (1448, 1448, 181, 181) }, { 5, (1280, 4096, 160, 512) }
    };

    // TownRegion areas by map
    private static readonly Dictionary<string, List<RegionArea>> _townRegions = new(StringComparer.OrdinalIgnoreCase);

    private class RegionArea
    {
        public string Name { get; set; } = "";
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int Z1 { get; set; } = -128;
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public int Z2 { get; set; } = 127;
    }

    private static void LoadTownRegions(string dataPath)
    {
        var regionsFile = Path.Combine(dataPath, "regions.json");
        if (!File.Exists(regionsFile))
        {
            Console.WriteLine($"WARNING: regions.json not found at {regionsFile}");
            return;
        }

        try
        {
            var json = File.ReadAllText(regionsFile);
            var regions = JsonNode.Parse(json)?.AsArray();
            if (regions == null) return;

            foreach (var region in regions)
            {
                if (region == null) continue;

                var type = region["$type"]?.GetValue<string>() ?? "";
                if (!type.Equals("TownRegion", StringComparison.OrdinalIgnoreCase)) continue;

                var map = region["Map"]?.GetValue<string>() ?? "";
                var name = region["Name"]?.GetValue<string>() ?? "";
                var areas = region["Area"]?.AsArray();

                if (string.IsNullOrEmpty(map) || areas == null) continue;

                if (!_townRegions.TryGetValue(map, out var regionList))
                {
                    regionList = new List<RegionArea>();
                    _townRegions[map] = regionList;
                }

                foreach (var area in areas)
                {
                    if (area == null) continue;

                    var ra = new RegionArea
                    {
                        Name = name,
                        X1 = area["x1"]?.GetValue<int>() ?? 0,
                        Y1 = area["y1"]?.GetValue<int>() ?? 0,
                        X2 = area["x2"]?.GetValue<int>() ?? 0,
                        Y2 = area["y2"]?.GetValue<int>() ?? 0
                    };

                    // Z bounds are optional
                    if (area["z1"] != null) ra.Z1 = area["z1"].GetValue<int>();
                    if (area["z2"] != null) ra.Z2 = area["z2"].GetValue<int>();

                    regionList.Add(ra);
                }
            }

            var totalAreas = _townRegions.Values.Sum(l => l.Count);
            Console.WriteLine($"Loaded {totalAreas} TownRegion areas from regions.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading regions.json: {ex.Message}");
        }
    }

    private static (bool isInTown, string regionName) IsInTownRegion(string map, int x, int y, int z)
    {
        if (!_townRegions.TryGetValue(map, out var regions))
            return (false, "");

        foreach (var region in regions)
        {
            if (x >= region.X1 && x <= region.X2 &&
                y >= region.Y1 && y <= region.Y2 &&
                z >= region.Z1 && z <= region.Z2)
            {
                return (true, region.Name);
            }
        }

        return (false, "");
    }

    public static void Main(string[] args)
    {
        // Check for --remove and --add flags
        var removeMode = args.Contains("--remove");
        var addMode = args.Contains("--add");
        var otherArgs = args.Where(a => a != "--remove" && a != "--add").ToArray();

        // Modern UOP client path (for post-uoml and shared spawns)
        var modernUoPath = otherArgs.Length > 0 ? otherArgs[0] : @"C:\Ultima Online Classic";
        // UOML MUL client path (for uoml spawns)
        var uomlPath = otherArgs.Length > 1 ? otherArgs[1] : @"C:\UOML";
        var spawnsPath = otherArgs.Length > 2 ? otherArgs[2] : @"C:\Repositories\ModernUO\Distribution\Data\Spawns";

        Console.WriteLine("=== Comprehensive Spawner Analysis ===");
        Console.WriteLine($"Modern UO Path (UOP): {modernUoPath}");
        Console.WriteLine($"UOML Path (MUL): {uomlPath}");
        Console.WriteLine($"Spawns Path: {spawnsPath}");
        if (removeMode) Console.WriteLine("MODE: Remove unnecessary spawnBounds");
        if (addMode) Console.WriteLine("MODE: Add spawnBounds to spawners that need them");
        Console.WriteLine();

        Console.WriteLine("Loading Modern TileData (UOP)...");
        LoadTileData(modernUoPath, _itemTileDataModern, _landTileDataModern);
        Console.WriteLine($"Loaded {_itemTileDataModern.Count} item tiles and {_landTileDataModern.Count} land tiles");

        Console.WriteLine("Loading UOML TileData (MUL)...");
        LoadTileData(uomlPath, _itemTileDataUoml, _landTileDataUoml);
        Console.WriteLine($"Loaded {_itemTileDataUoml.Count} item tiles and {_landTileDataUoml.Count} land tiles");

        // Load TownRegions from regions.json
        var dataPath = Path.GetDirectoryName(spawnsPath)!;
        Console.WriteLine("\nLoading TownRegions...");
        LoadTownRegions(dataPath);

        Console.WriteLine("\nAnalyzing ALL spawners (including existing spawnBounds)...");
        var allSpawners = AnalyzeAllSpawners(modernUoPath, uomlPath, spawnsPath);

        // Categorize results
        var largeHomeRange = allSpawners.Where(s => s.Category == SpawnerCategory.LargeHomeRange).ToList();
        var needsSpawnBounds = allSpawners.Where(s => s.NeedsSpawnBounds).ToList();
        var needsReview = allSpawners.Where(s => s.ShouldReview).ToList();
        var shouldRemove = allSpawners.Where(s => s.ShouldRemoveSpawnBounds).ToList();
        var outdoor = allSpawners.Where(s => s.Category == SpawnerCategory.Outdoor && !s.HasSpawnBounds).ToList();

        Console.WriteLine($"\n=== RESULTS ===");
        Console.WriteLine($"Total spawners analyzed: {allSpawners.Count}");
        Console.WriteLine();
        Console.WriteLine($"Large homeRange (>=50, skip): {largeHomeRange.Count}");
        Console.WriteLine($"NEEDS z-restricted spawnBounds: {needsSpawnBounds.Count}");
        Console.WriteLine($"NEEDS REVIEW (uncertain): {needsReview.Count}");
        Console.WriteLine($"Should REMOVE existing spawnBounds: {shouldRemove.Count}");
        Console.WriteLine($"Outdoor (no changes needed): {outdoor.Count}");

        if (removeMode && shouldRemove.Any())
        {
            Console.WriteLine($"\n=== REMOVING spawnBounds from {shouldRemove.Count} spawners ===");
            RemoveSpawnBounds(shouldRemove, spawnsPath);
        }

        if (addMode && needsSpawnBounds.Any())
        {
            Console.WriteLine($"\n=== ADDING spawnBounds to {needsSpawnBounds.Count} spawners ===");
            AddSpawnBounds(needsSpawnBounds, spawnsPath);
        }

        // Write comprehensive report
        WriteComprehensiveReport(allSpawners, spawnsPath);

        Console.WriteLine("\nDone!");
    }

    private static void RemoveSpawnBounds(List<SpawnerAnalysis> toRemove, string spawnsPath)
    {
        // Group by file for efficient processing
        var byFile = toRemove.GroupBy(s => s.File).ToList();
        var totalRemoved = 0;

        foreach (var fileGroup in byFile)
        {
            var filePath = Path.Combine(spawnsPath, fileGroup.Key);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"  WARNING: File not found: {filePath}");
                continue;
            }

            var guidsToRemove = fileGroup.Select(s => s.Guid).ToHashSet();
            var json = File.ReadAllText(filePath);
            var array = JsonNode.Parse(json)?.AsArray();
            if (array == null) continue;

            var modified = false;
            foreach (var node in array)
            {
                if (node == null) continue;
                var guid = node["guid"]?.GetValue<string>();
                if (guid != null && guidsToRemove.Contains(guid))
                {
                    var obj = node.AsObject();
                    if (obj.ContainsKey("spawnBounds"))
                    {
                        obj.Remove("spawnBounds");
                        modified = true;
                        totalRemoved++;
                    }
                }
            }

            if (modified)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var newJson = JsonSerializer.Serialize(array, options);
                File.WriteAllText(filePath, newJson);
                Console.WriteLine($"  Removed {fileGroup.Count()} spawnBounds from {fileGroup.Key}");
            }
        }

        Console.WriteLine($"\nTotal spawnBounds removed: {totalRemoved}");
    }

    private static void AddSpawnBounds(List<SpawnerAnalysis> toAdd, string spawnsPath)
    {
        // Group by file for efficient processing
        var byFile = toAdd.GroupBy(s => s.File).ToList();
        var totalAdded = 0;

        foreach (var fileGroup in byFile)
        {
            var filePath = Path.Combine(spawnsPath, fileGroup.Key);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"  WARNING: File not found: {filePath}");
                continue;
            }

            // Build a lookup for spawner analysis by GUID
            var analysisLookup = fileGroup.ToDictionary(s => s.Guid, s => s);

            var json = File.ReadAllText(filePath);
            var array = JsonNode.Parse(json)?.AsArray();
            if (array == null) continue;

            var modified = false;
            foreach (var node in array)
            {
                if (node == null) continue;
                var guid = node["guid"]?.GetValue<string>();
                if (guid == null || !analysisLookup.TryGetValue(guid, out var analysis)) continue;

                var obj = node.AsObject();

                // Skip if already has spawnBounds
                if (obj.ContainsKey("spawnBounds")) continue;

                // Calculate spawnBounds
                var bounds = CalculateSpawnBounds(analysis);
                if (bounds != null)
                {
                    var boundsArray = new JsonArray(
                        JsonValue.Create(bounds.Value.x1),
                        JsonValue.Create(bounds.Value.y1),
                        JsonValue.Create(bounds.Value.z1),
                        JsonValue.Create(bounds.Value.x2),
                        JsonValue.Create(bounds.Value.y2),
                        JsonValue.Create(bounds.Value.z2)
                    );
                    obj.Add("spawnBounds", boundsArray);
                    modified = true;
                    totalAdded++;
                }
            }

            if (modified)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var newJson = JsonSerializer.Serialize(array, options);
                File.WriteAllText(filePath, newJson);
                Console.WriteLine($"  Added spawnBounds to {fileGroup.Count()} spawners in {fileGroup.Key}");
            }
        }

        Console.WriteLine($"\nTotal spawnBounds added: {totalAdded}");
    }

    private static (int x1, int y1, int z1, int x2, int y2, int z2)? CalculateSpawnBounds(SpawnerAnalysis analysis)
    {
        // Get the homeRange, default to 0 if not set
        var range = analysis.HomeRange;

        // Calculate x/y bounds based on homeRange
        var x1 = analysis.X - range;
        var y1 = analysis.Y - range;
        var x2 = analysis.X + range;
        var y2 = analysis.Y + range;

        // Calculate z bounds based on floor levels
        int z1, z2;

        if (analysis.AllFloorLevels.Count == 0)
        {
            // No floor data, use spawner Z with reasonable buffer
            z1 = analysis.Z - 10;
            z2 = analysis.Z + 15;
        }
        else
        {
            // Find the floor level at or below the spawner
            var currentFloor = analysis.AllFloorLevels
                .Where(z => z <= analysis.Z + 5)
                .OrderByDescending(z => z)
                .FirstOrDefault();

            // Find the next floor level above the spawner
            var nextFloor = analysis.AllFloorLevels
                .Where(z => z > analysis.Z + 10)
                .OrderBy(z => z)
                .FirstOrDefault();

            // z1: start from below current floor to catch standing on floor
            z1 = currentFloor != 0 ? currentFloor - 5 : analysis.Z - 10;

            // z2: stop before next floor, or use ceiling if available
            if (nextFloor != 0 && nextFloor > analysis.Z)
            {
                // Stop a few Z units before the next floor
                z2 = nextFloor - 5;
            }
            else if (analysis.CeilingZ > analysis.Z)
            {
                // Use ceiling height
                z2 = analysis.CeilingZ - 5;
            }
            else
            {
                // Default to 20 units above spawner (typical floor height)
                z2 = analysis.Z + 15;
            }

            // Ensure z2 is at least 10 above z1 for valid spawning
            if (z2 - z1 < 10)
            {
                z2 = z1 + 15;
            }
        }

        return (x1, y1, z1, x2, y2, z2);
    }

    private static void LoadTileData(string uoPath, Dictionary<int, TileInfo> itemData, Dictionary<int, TileInfo> landData)
    {
        var tiledataPath = Path.Combine(uoPath, "tiledata.mul");
        if (!File.Exists(tiledataPath))
        {
            Console.WriteLine($"WARNING: tiledata.mul not found at {tiledataPath}");
            return;
        }

        using var fs = new FileStream(tiledataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);

        // Detect format based on file size
        // New format (HS+): 3188736 bytes with 64-bit flags and 65536 items
        // Old format: smaller file with 32-bit flags
        var is64BitFlags = fs.Length >= 3188736;

        // Read land tiles (always 0x4000 = 16384 tiles)
        for (int i = 0; i < 0x4000; i++)
        {
            if (is64BitFlags)
            {
                if (i == 1 || (i > 0 && (i & 0x1F) == 0)) br.ReadInt32();
                var flags = (TileFlag)br.ReadUInt64();
                br.ReadInt16();
                var name = ReadTileName(br);
                landData[i] = new TileInfo { Name = name, Flags = flags };
            }
            else
            {
                if ((i & 0x1F) == 0) br.ReadInt32();
                var flags = (TileFlag)br.ReadUInt32();
                br.ReadInt16();
                var name = ReadTileName(br);
                landData[i] = new TileInfo { Name = name, Flags = flags };
            }
        }

        // Calculate item count from remaining file size
        var landBytesRead = fs.Position;
        var remainingBytes = fs.Length - landBytesRead;

        int itemLength;
        if (is64BitFlags)
        {
            // 64-bit: 41 bytes per item + 4 byte header per 32 items
            // Per group of 32: 32 * 41 + 4 = 1316 bytes
            var groups = remainingBytes / 1316;
            itemLength = (int)(groups * 32);
        }
        else
        {
            // 32-bit: 37 bytes per item + 4 byte header per 32 items
            // Per group of 32: 32 * 37 + 4 = 1188 bytes
            var groups = remainingBytes / 1188;
            itemLength = (int)(groups * 32);
        }

        for (int i = 0; i < itemLength && fs.Position < fs.Length - 30; i++)
        {
            if ((i & 0x1F) == 0) br.ReadInt32();

            var flags = (TileFlag)(is64BitFlags ? br.ReadUInt64() : br.ReadUInt32());
            br.ReadByte(); br.ReadByte(); br.ReadUInt16(); br.ReadByte();
            br.ReadByte(); br.ReadInt32(); br.ReadByte(); br.ReadByte();
            var height = br.ReadByte();
            var name = ReadTileName(br);

            itemData[i] = new TileInfo { Name = name, Flags = flags, Height = height };
        }
    }

    private static string ReadTileName(BinaryReader br)
    {
        var nameBytes = br.ReadBytes(20);
        var terminator = Array.IndexOf(nameBytes, (byte)0);
        if (terminator < 0) terminator = 20;
        return Encoding.ASCII.GetString(nameBytes, 0, terminator).Trim();
    }

    private static List<SpawnerAnalysis> AnalyzeAllSpawners(string modernUoPath, string uomlPath, string spawnsPath)
    {
        var results = new List<SpawnerAnalysis>();

        foreach (var subFolder in new[] { "shared", "post-uoml", "uoml" })
        {
            var folderPath = Path.Combine(spawnsPath, subFolder);
            if (!Directory.Exists(folderPath)) continue;

            // Use UOML path for uoml spawns, modern path for others
            var uoPath = subFolder == "uoml" ? uomlPath : modernUoPath;
            var itemData = subFolder == "uoml" ? _itemTileDataUoml : _itemTileDataModern;
            var landData = subFolder == "uoml" ? _landTileDataUoml : _landTileDataModern;

            foreach (var file in Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories))
            {
                var spawners = AnalyzeSpawnersInFile(uoPath, file, spawnsPath, itemData);
                results.AddRange(spawners);
            }
        }

        return results;
    }

    private static List<SpawnerAnalysis> AnalyzeSpawnersInFile(
        string uoPath,
        string filePath,
        string spawnsPath,
        Dictionary<int, TileInfo> itemTileData)
    {
        var results = new List<SpawnerAnalysis>();

        try
        {
            var json = File.ReadAllText(filePath);
            var array = JsonNode.Parse(json)?.AsArray();
            if (array == null) return results;

            var relativePath = Path.GetRelativePath(spawnsPath, filePath);

            foreach (var node in array)
            {
                if (node == null) continue;

                var guid = node["guid"]?.GetValue<string>() ?? "";
                var map = node["map"]?.GetValue<string>() ?? "";
                var location = node["location"]?.AsArray();
                var hasSpawnBounds = node["spawnBounds"] != null;
                var homeRange = node["homeRange"]?.GetValue<int>() ?? 0;

                if (location == null || location.Count < 3) continue;

                var x = location[0]?.GetValue<int>() ?? 0;
                var y = location[1]?.GetValue<int>() ?? 0;
                var z = location[2]?.GetValue<int>() ?? 0;

                var entries = new List<string>();
                var entriesNode = node["entries"]?.AsArray();
                if (entriesNode != null)
                {
                    foreach (var entry in entriesNode)
                    {
                        var name = entry?["name"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(name)) entries.Add(name);
                    }
                }

                var analysis = new SpawnerAnalysis
                {
                    Guid = guid,
                    File = relativePath,
                    X = x, Y = y, Z = z,
                    Map = map,
                    HomeRange = homeRange,
                    HasSpawnBounds = hasSpawnBounds,
                    Entries = entries
                };

                // Check if in TownRegion
                var (isInTown, regionName) = IsInTownRegion(map, x, y, z);
                analysis.IsInTownRegion = isInTown;
                analysis.RegionName = regionName;

                // Analyze ALL spawners, even those with existing spawnBounds
                AnalyzeSpawnerLocation(uoPath, analysis, itemTileData);
                CategorizeSpawner(analysis);

                results.Add(analysis);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing {filePath}: {ex.Message}");
        }

        return results;
    }

    private static void AnalyzeSpawnerLocation(
        string uoPath,
        SpawnerAnalysis analysis,
        Dictionary<int, TileInfo> itemTileData)
    {
        // Skip detailed analysis for large homeRange spawners
        if (analysis.HomeRange >= 50)
        {
            return;
        }

        if (!MapIndices.TryGetValue(analysis.Map, out var mapIndex)) return;

        // Get statics in a larger area to detect multi-floor structures
        var statics = GetStaticsInArea(uoPath, mapIndex, analysis.X, analysis.Y, 2, itemTileData);

        // Find all floor levels in the area
        var floorTiles = statics
            .Where(s => s.Info != null && s.Info.IsFloor)
            .ToList();

        // Group floor tiles by Z level (within 5 units = same floor)
        var floorLevels = floorTiles
            .GroupBy(s => s.Z / 5 * 5)
            .Select(g => g.Key)
            .OrderBy(z => z)
            .ToList();

        analysis.AllFloorLevels = floorLevels;
        analysis.HasMultipleFloorLevels = floorLevels.Count > 1 &&
            (floorLevels.Max() - floorLevels.Min()) >= 15; // At least 15 Z difference

        // Find floor directly below spawner
        var floorsBelow = floorTiles
            .Where(s => s.Z <= analysis.Z && s.Z >= analysis.Z - 10)
            .OrderByDescending(s => s.Z)
            .ToList();

        if (floorsBelow.Any())
        {
            var floor = floorsBelow.First();
            analysis.HasFloorBelow = true;
            analysis.FloorTileName = floor.Info?.Name ?? $"Tile {floor.Id}";
            analysis.FloorZ = floor.Z;
        }

        // Check for visible ceiling (not nodraw)
        var visibleCeilings = statics
            .Where(s => s.Info != null && (s.Info.IsRoof || s.Info.IsFloor))
            .Where(s => !IsNoDrawTile(s.Id, s.Info))
            .Where(s => s.Z >= analysis.Z + 10 && s.Z <= analysis.Z + 30)
            .OrderBy(s => s.Z)
            .ToList();

        if (visibleCeilings.Any())
        {
            var ceiling = visibleCeilings.First();
            analysis.HasVisibleCeilingAbove = true;
            analysis.CeilingTileName = ceiling.Info?.Name ?? $"Tile {ceiling.Id}";
            analysis.CeilingZ = ceiling.Z;
        }

        // Check for nodraw ceiling (invisible tiles forcing inside mode)
        var noDrawCeilings = statics
            .Where(s => IsNoDrawTile(s.Id, s.Info))
            .Where(s => s.Z >= analysis.Z + 10 && s.Z <= analysis.Z + 30)
            .ToList();

        analysis.HasNoDrawCeilingAbove = noDrawCeilings.Any() && !analysis.HasVisibleCeilingAbove;
    }

    private static void CategorizeSpawner(SpawnerAnalysis analysis)
    {
        // HomeRange == 0: code already restricts Z to spawner level, no spawnBounds needed
        if (analysis.HomeRange == 0)
        {
            analysis.Category = SpawnerCategory.Outdoor;
            analysis.CategoryReason = "HomeRange 0 - code restricts Z to spawner level";
            analysis.Recommendation = analysis.HasSpawnBounds
                ? "REMOVE spawnBounds - homeRange 0 doesn't need it"
                : "No changes needed";
            return;
        }

        // Large homeRange - skip detailed analysis, assume no spawnBounds needed
        if (analysis.HomeRange >= 50)
        {
            analysis.Category = SpawnerCategory.LargeHomeRange;
            analysis.CategoryReason = $"HomeRange {analysis.HomeRange} is >= 50, too large for z-restriction";
            analysis.Recommendation = analysis.HasSpawnBounds
                ? "REMOVE spawnBounds - homeRange is sufficient"
                : "No changes needed";
            return;
        }

        var townInfo = analysis.IsInTownRegion ? $" (in town: {analysis.RegionName})" : " (not in town)";

        // Multi-floor building or dungeon - may need z-restriction
        if (analysis.HasMultipleFloorLevels && analysis.HasFloorBelow)
        {
            // Check if it's a dungeon transition (cave floors both above and below)
            if (IsCaveFloor(analysis.FloorTileName) && IsCaveFloor(analysis.CeilingTileName))
            {
                analysis.Category = SpawnerCategory.DungeonTransition;
                analysis.CategoryReason = $"Cave floor at Z:{analysis.FloorZ}, cave ceiling at Z:{analysis.CeilingZ} - level transition";
                analysis.Recommendation = analysis.HasSpawnBounds
                    ? "Consider removing spawnBounds - dungeon transition"
                    : "No changes needed";
            }
            else if (analysis.HasVisibleCeilingAbove)
            {
                analysis.Category = SpawnerCategory.MultiFloorBuilding;
                analysis.CategoryReason = $"Multiple floor levels ({string.Join(", ", analysis.AllFloorLevels)}) with visible ceiling{townInfo}";

                if (analysis.IsInTownRegion)
                {
                    // Town buildings need spawnBounds
                    analysis.Recommendation = analysis.HasSpawnBounds
                        ? "Keep spawnBounds"
                        : "ADD spawnBounds with z-restriction";
                }
                else
                {
                    // Non-town buildings (like Shame dungeon surface building) usually ok without spawnBounds
                    analysis.Recommendation = analysis.HasSpawnBounds
                        ? "Keep spawnBounds - but verify if needed"
                        : "REVIEW - non-town multi-floor, usually ok to spawn on all floors except roof";
                }
            }
            else
            {
                analysis.Category = SpawnerCategory.DungeonTransition;
                analysis.CategoryReason = $"Multiple floor levels ({string.Join(", ", analysis.AllFloorLevels)}) but no visible ceiling";
                analysis.Recommendation = analysis.HasSpawnBounds
                    ? "Consider removing spawnBounds"
                    : "No changes needed";
            }
            return;
        }

        // Single floor with visible ceiling - might need spawnBounds, put in review
        if (analysis.HasFloorBelow && analysis.HasVisibleCeilingAbove)
        {
            analysis.Category = SpawnerCategory.SingleFloorBuilding;
            analysis.CategoryReason = $"Floor ({analysis.FloorTileName} Z:{analysis.FloorZ}) with ceiling ({analysis.CeilingTileName} Z:{analysis.CeilingZ}){townInfo}";
            analysis.Recommendation = "REVIEW - single floor building, may need spawnBounds";
            return;
        }

        // Has nodraw ceiling only - does NOT need spawnBounds
        if (analysis.HasNoDrawCeilingAbove)
        {
            analysis.Category = SpawnerCategory.NoDrawCeiling;
            analysis.CategoryReason = "Only has invisible nodraw tiles above";
            analysis.Recommendation = analysis.HasSpawnBounds
                ? "REMOVE spawnBounds - nodraw ceiling doesn't need restriction"
                : "No changes needed";
            return;
        }

        // Floor but no ceiling - ruins
        if (analysis.HasFloorBelow && !analysis.HasVisibleCeilingAbove)
        {
            analysis.Category = SpawnerCategory.OpenRuins;
            analysis.CategoryReason = $"Floor ({analysis.FloorTileName} Z:{analysis.FloorZ}) but no ceiling";
            analysis.Recommendation = analysis.HasSpawnBounds
                ? "REMOVE spawnBounds - open area"
                : "No changes needed";
            return;
        }

        // Default - outdoor
        analysis.Category = SpawnerCategory.Outdoor;
        analysis.CategoryReason = "No significant structure detected";
        analysis.Recommendation = analysis.HasSpawnBounds
            ? "REMOVE spawnBounds - outdoor area"
            : "No changes needed";
    }

    private static List<StaticTile> GetStaticsInArea(
        string uoPath,
        int mapIndex,
        int centerX,
        int centerY,
        int range,
        Dictionary<int, TileInfo> itemTileData)
    {
        var statics = new List<StaticTile>();

        var staidxPath = Path.Combine(uoPath, $"staidx{mapIndex}.mul");
        var staticsPath = Path.Combine(uoPath, $"statics{mapIndex}.mul");

        if (!File.Exists(staidxPath) || !File.Exists(staticsPath)) return statics;
        if (!MapDimensions.TryGetValue(mapIndex, out var dims)) return statics;

        var blocksToRead = new HashSet<(int blockX, int blockY)>();
        for (int dy = -range; dy <= range; dy++)
            for (int dx = -range; dx <= range; dx++)
            {
                var x = centerX + dx;
                var y = centerY + dy;
                blocksToRead.Add((x / 8, y / 8));
            }

        using var fsIdx = new FileStream(staidxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var brIdx = new BinaryReader(fsIdx);
        using var fsStatics = new FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var brStatics = new BinaryReader(fsStatics);

        foreach (var (blockX, blockY) in blocksToRead)
        {
            var blockIndex = blockX * dims.BlockHeight + blockY;
            var idxOffset = blockIndex * 12;
            if (idxOffset + 12 > fsIdx.Length) continue;

            fsIdx.Seek(idxOffset, SeekOrigin.Begin);
            var lookup = brIdx.ReadInt32();
            var length = brIdx.ReadInt32();
            brIdx.ReadInt32();

            if (lookup < 0 || length <= 0 || lookup + length > fsStatics.Length) continue;

            fsStatics.Seek(lookup, SeekOrigin.Begin);
            var count = length / 7;

            for (int i = 0; i < count; i++)
            {
                var tileId = brStatics.ReadUInt16();
                var sx = brStatics.ReadByte();
                var sy = brStatics.ReadByte();
                var sz = brStatics.ReadSByte();
                brStatics.ReadInt16();

                var worldX = blockX * 8 + sx;
                var worldY = blockY * 8 + sy;

                if (Math.Abs(worldX - centerX) <= range && Math.Abs(worldY - centerY) <= range)
                {
                    itemTileData.TryGetValue(tileId, out var info);
                    statics.Add(new StaticTile { Id = tileId, X = worldX, Y = worldY, Z = sz, Info = info });
                }
            }
        }

        return statics;
    }

    private static void WriteComprehensiveReport(List<SpawnerAnalysis> allSpawners, string spawnsPath)
    {
        var reportPath = Path.Combine(spawnsPath, "comprehensive_spawner_analysis.md");

        var largeHomeRange = allSpawners.Where(s => s.Category == SpawnerCategory.LargeHomeRange).ToList();
        var needsSpawnBounds = allSpawners.Where(s => s.NeedsSpawnBounds).ToList();
        var needsReview = allSpawners.Where(s => s.ShouldReview).ToList();
        var shouldRemove = allSpawners.Where(s => s.ShouldRemoveSpawnBounds).ToList();
        var dungeonTransition = allSpawners.Where(s => s.Category == SpawnerCategory.DungeonTransition).ToList();
        var outdoor = allSpawners.Where(s => s.Category == SpawnerCategory.Outdoor && !s.HasSpawnBounds).ToList();

        var report = new List<string>
        {
            "# Comprehensive Spawner Analysis Report",
            "",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "",
            "This report analyzes ALL spawners (including those with existing spawnBounds)",
            "to determine which ones need z-level restricted spawnBounds.",
            "",
            "## Rules Applied",
            "",
            "- **HomeRange >= 50**: Skip analysis, assume no spawnBounds needed (area too large)",
            "- **HomeRange < 50**: Analyze for multi-floor structures",
            "- **NPC type**: Used for recommendations only, not as a hard rule",
            "",
            "## Executive Summary",
            "",
            "| Category | Count | Action |",
            "|----------|-------|--------|",
            $"| Large HomeRange (>=50) | {largeHomeRange.Count} | No spawnBounds needed |",
            $"| **NEEDS spawnBounds** | {needsSpawnBounds.Count} | Add z-restricted spawnBounds |",
            $"| **NEEDS REVIEW** | {needsReview.Count} | Manual review recommended |",
            $"| Should REMOVE spawnBounds | {shouldRemove.Count} | Remove unnecessary spawnBounds |",
            $"| Dungeon transitions | {dungeonTransition.Count} | No changes needed |",
            $"| Outdoor (no structure) | {outdoor.Count} | No changes needed |",
            "",
            "---",
            ""
        };

        // Section: Spawners that NEED spawnBounds
        report.Add("## Spawners That NEED z-Restricted spawnBounds");
        report.Add("");
        report.Add("These spawners are in multi-floor structures where mobs could spawn on wrong floor.");
        report.Add("");

        if (needsSpawnBounds.Any())
        {
            report.Add("| GUID | File | Location | HomeRange | Floor Levels | Reason |");
            report.Add("|------|------|----------|-----------|--------------|--------|");

            foreach (var s in needsSpawnBounds.OrderBy(s => s.File).ThenBy(s => s.Map))
            {
                var levels = string.Join(",", s.AllFloorLevels.Take(5));
                if (s.AllFloorLevels.Count > 5) levels += "...";
                var guidShort = s.Guid.Length > 8 ? s.Guid[..8] + "..." : s.Guid;
                report.Add($"| `{guidShort}` | {s.File} | ({s.X},{s.Y},{s.Z}) {s.Map} | {s.HomeRange} | {levels} | {s.CategoryReason} |");
            }
        }
        else
        {
            report.Add("*None identified*");
        }

        // Section: Spawners that NEED REVIEW
        report.Add("");
        report.Add("---");
        report.Add("");
        report.Add("## Spawners That NEED REVIEW");
        report.Add("");
        report.Add("These spawners are in single-floor buildings or have uncertain classification.");
        report.Add("Manual review is recommended to determine if spawnBounds are needed.");
        report.Add("");

        if (needsReview.Any())
        {
            report.Add("| GUID | File | Location | HomeRange | Floor | Ceiling | Entries |");
            report.Add("|------|------|----------|-----------|-------|---------|---------|");

            foreach (var s in needsReview.OrderBy(s => s.File).ThenBy(s => s.Map))
            {
                var entries = string.Join(", ", s.Entries.Take(2));
                if (s.Entries.Count > 2) entries += "...";
                var guidShort = s.Guid.Length > 8 ? s.Guid[..8] + "..." : s.Guid;
                report.Add($"| `{guidShort}` | {s.File} | ({s.X},{s.Y},{s.Z}) | {s.HomeRange} | {s.FloorTileName} (Z:{s.FloorZ}) | {s.CeilingTileName} (Z:{s.CeilingZ}) | {entries} |");
            }
        }
        else
        {
            report.Add("*None identified*");
        }

        // Section: Spawners that should REMOVE spawnBounds
        report.Add("");
        report.Add("---");
        report.Add("");
        report.Add("## Spawners That Should REMOVE spawnBounds");
        report.Add("");
        report.Add("These spawners have spawnBounds defined but analysis suggests they don't need them.");
        report.Add("");

        if (shouldRemove.Any())
        {
            // Group by file for easier processing
            var byFile = shouldRemove.GroupBy(s => s.File).OrderBy(g => g.Key);

            report.Add("| File | Count | Categories |");
            report.Add("|------|-------|------------|");

            foreach (var g in byFile)
            {
                var categories = string.Join(", ", g.GroupBy(s => s.Category).Select(c => $"{c.Key}({c.Count()})"));
                report.Add($"| {g.Key} | {g.Count()} | {categories} |");
            }

            report.Add("");
            report.Add("### Detailed List");
            report.Add("");
            report.Add("| GUID | File | Location | HomeRange | Category | Reason |");
            report.Add("|------|------|----------|-----------|----------|--------|");

            foreach (var s in shouldRemove.Take(100).OrderBy(s => s.File))
            {
                var guidShort = s.Guid.Length > 8 ? s.Guid[..8] + "..." : s.Guid;
                report.Add($"| `{guidShort}` | {s.File} | ({s.X},{s.Y},{s.Z}) | {s.HomeRange} | {s.Category} | {s.CategoryReason} |");
            }

            if (shouldRemove.Count > 100)
            {
                report.Add($"| ... | ... | ... | ... | ... | *({shouldRemove.Count - 100} more)* |");
            }
        }
        else
        {
            report.Add("*None identified*");
        }

        // Section: Large HomeRange spawners
        report.Add("");
        report.Add("---");
        report.Add("");
        report.Add("## Large HomeRange Spawners (>=50)");
        report.Add("");
        report.Add("These spawners have homeRange >= 50 and are skipped from detailed analysis.");
        report.Add("The area is too large for meaningful z-restriction.");
        report.Add("");
        report.Add($"Total: {largeHomeRange.Count} spawners");
        report.Add("");

        if (largeHomeRange.Any())
        {
            var byFile = largeHomeRange.GroupBy(s => s.File).OrderByDescending(g => g.Count());
            report.Add("| File | Count | HomeRange Range |");
            report.Add("|------|-------|-----------------|");

            foreach (var g in byFile.Take(30))
            {
                var minHR = g.Min(s => s.HomeRange);
                var maxHR = g.Max(s => s.HomeRange);
                var hrRange = minHR == maxHR ? $"{minHR}" : $"{minHR}-{maxHR}";
                report.Add($"| {g.Key} | {g.Count()} | {hrRange} |");
            }

            if (byFile.Count() > 30)
            {
                report.Add($"| ... | ... | *({byFile.Count() - 30} more files)* |");
            }
        }

        // Section: Dungeon Transitions
        report.Add("");
        report.Add("---");
        report.Add("");
        report.Add("## Dungeon Level Transitions");
        report.Add("");
        report.Add("These spawners are in areas where multiple dungeon levels overlap.");
        report.Add("Spawning on either level is acceptable (stairwells, ramps, cave transitions).");
        report.Add("");
        report.Add($"Total: {dungeonTransition.Count} spawners");

        // Summary stats
        report.Add("");
        report.Add("---");
        report.Add("");
        report.Add("## Statistics by Folder");
        report.Add("");

        var byFolder = allSpawners.GroupBy(s => s.File.Split('\\')[0]).OrderBy(g => g.Key);
        report.Add("| Folder | Total | Needs SpawnBounds | Needs Review | Should Remove | Large HR | Outdoor |");
        report.Add("|--------|-------|-------------------|--------------|---------------|----------|---------|");

        foreach (var folder in byFolder)
        {
            var total = folder.Count();
            var needs = folder.Count(s => s.NeedsSpawnBounds);
            var review = folder.Count(s => s.ShouldReview);
            var remove = folder.Count(s => s.ShouldRemoveSpawnBounds);
            var large = folder.Count(s => s.Category == SpawnerCategory.LargeHomeRange);
            var outd = folder.Count(s => s.Category == SpawnerCategory.Outdoor && !s.HasSpawnBounds);
            report.Add($"| {folder.Key} | {total} | {needs} | {review} | {remove} | {large} | {outd} |");
        }

        File.WriteAllLines(reportPath, report);
        Console.WriteLine($"\nComprehensive report written to: {reportPath}");
    }
}
