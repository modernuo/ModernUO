using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SpawnProcessor;

/// <summary>
/// Deduplicates spawners between uoml and post-uoml folders.
/// Spawners with the same GUID in both folders are moved to a shared folder.
/// </summary>
public static class SpawnDeduplicator
{
    public static void Main(string[] args)
    {
        var spawnsPath = args.Length > 0 ? args[0] : @"C:\Repositories\ModernUO\Distribution\Data\Spawns";

        Console.WriteLine("=== Spawn Deduplicator ===");
        Console.WriteLine($"Spawns Path: {spawnsPath}");
        Console.WriteLine();

        var postUomlPath = Path.Combine(spawnsPath, "post-uoml");
        var uomlPath = Path.Combine(spawnsPath, "uoml");
        var sharedPath = Path.Combine(spawnsPath, "shared");

        // Collect all spawns from both folders
        var postUomlSpawns = CollectSpawns(postUomlPath, "post-uoml");
        var uomlSpawns = CollectSpawns(uomlPath, "uoml");

        Console.WriteLine($"Found {postUomlSpawns.Count} spawns in post-uoml");
        Console.WriteLine($"Found {uomlSpawns.Count} spawns in uoml");

        // Find spawns with same GUID in both folders
        var sharedGuids = postUomlSpawns.Keys.Intersect(uomlSpawns.Keys).ToHashSet();
        Console.WriteLine($"Found {sharedGuids.Count} shared GUIDs (same spawner in both folders)");

        // Check if the spawns are actually identical
        var identicalSpawns = new List<string>();
        var differentSpawns = new List<(string guid, string diff)>();

        foreach (var guid in sharedGuids)
        {
            var postSpawn = postUomlSpawns[guid];
            var uomlSpawn = uomlSpawns[guid];

            var diff = CompareSpawns(postSpawn, uomlSpawn);
            if (string.IsNullOrEmpty(diff))
            {
                identicalSpawns.Add(guid);
            }
            else
            {
                differentSpawns.Add((guid, diff));
            }
        }

        Console.WriteLine($"  - Identical spawns: {identicalSpawns.Count}");
        Console.WriteLine($"  - Different spawns: {differentSpawns.Count}");

        if (differentSpawns.Any())
        {
            Console.WriteLine("\nDifferent spawns (same GUID but different data):");
            foreach (var (guid, diff) in differentSpawns.Take(20))
            {
                Console.WriteLine($"  {guid}: {diff}");
            }
            if (differentSpawns.Count > 20)
            {
                Console.WriteLine($"  ... and {differentSpawns.Count - 20} more");
            }
        }

        // Move identical spawns to shared folder
        if (identicalSpawns.Any())
        {
            Console.WriteLine($"\nMoving {identicalSpawns.Count} identical spawns to shared folder...");
            MoveToShared(identicalSpawns, postUomlSpawns, uomlSpawns, spawnsPath, sharedPath);
        }

        // Generate report
        var reportPath = Path.Combine(spawnsPath, "deduplication_report.md");
        WriteReport(reportPath, postUomlSpawns, uomlSpawns, identicalSpawns, differentSpawns);
        Console.WriteLine($"\nReport written to: {reportPath}");

        Console.WriteLine("\nDone!");
    }

    private static Dictionary<string, SpawnData> CollectSpawns(string basePath, string folderName)
    {
        var spawns = new Dictionary<string, SpawnData>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(basePath))
            return spawns;

        foreach (var file in Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(file);
                var array = JsonNode.Parse(json)?.AsArray();
                if (array == null) continue;

                // Get relative path within the uoml/post-uoml folder (e.g., felucca/Shame.json)
                var relativePath = Path.GetRelativePath(basePath, file);

                foreach (var node in array)
                {
                    if (node == null) continue;

                    var guid = node["guid"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(guid)) continue;

                    spawns[guid] = new SpawnData
                    {
                        Guid = guid,
                        FilePath = file,
                        RelativePath = relativePath,
                        FolderName = folderName,
                        Node = node
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {file}: {ex.Message}");
            }
        }

        return spawns;
    }

    private static string CompareSpawns(SpawnData a, SpawnData b)
    {
        // Compare key properties
        var aLoc = a.Node["location"]?.AsArray();
        var bLoc = b.Node["location"]?.AsArray();

        if (aLoc != null && bLoc != null)
        {
            if (aLoc.Count >= 3 && bLoc.Count >= 3)
            {
                var ax = aLoc[0]?.GetValue<int>() ?? 0;
                var ay = aLoc[1]?.GetValue<int>() ?? 0;
                var az = aLoc[2]?.GetValue<int>() ?? 0;
                var bx = bLoc[0]?.GetValue<int>() ?? 0;
                var by = bLoc[1]?.GetValue<int>() ?? 0;
                var bz = bLoc[2]?.GetValue<int>() ?? 0;

                if (ax != bx || ay != by || az != bz)
                {
                    return $"Location: ({ax},{ay},{az}) vs ({bx},{by},{bz})";
                }
            }
        }

        var aMap = a.Node["map"]?.GetValue<string>() ?? "";
        var bMap = b.Node["map"]?.GetValue<string>() ?? "";
        if (!aMap.Equals(bMap, StringComparison.OrdinalIgnoreCase))
        {
            return $"Map: {aMap} vs {bMap}";
        }

        var aHomeRange = a.Node["homeRange"]?.GetValue<int>() ?? 0;
        var bHomeRange = b.Node["homeRange"]?.GetValue<int>() ?? 0;
        if (aHomeRange != bHomeRange)
        {
            return $"HomeRange: {aHomeRange} vs {bHomeRange}";
        }

        // Compare entries
        var aEntries = a.Node["entries"]?.AsArray();
        var bEntries = b.Node["entries"]?.AsArray();

        if (aEntries != null && bEntries != null)
        {
            if (aEntries.Count != bEntries.Count)
            {
                return $"Entry count: {aEntries.Count} vs {bEntries.Count}";
            }

            for (int i = 0; i < aEntries.Count; i++)
            {
                var aName = aEntries[i]?["name"]?.GetValue<string>() ?? "";
                var bName = bEntries[i]?["name"]?.GetValue<string>() ?? "";
                if (!aName.Equals(bName, StringComparison.OrdinalIgnoreCase))
                {
                    return $"Entry[{i}]: {aName} vs {bName}";
                }
            }
        }

        return ""; // Identical
    }

    private static void MoveToShared(
        List<string> identicalGuids,
        Dictionary<string, SpawnData> postUomlSpawns,
        Dictionary<string, SpawnData> uomlSpawns,
        string spawnsPath,
        string sharedPath)
    {
        // Group by relative path (e.g., felucca/Shame.json)
        var byRelativePath = identicalGuids
            .Select(g => postUomlSpawns[g])
            .GroupBy(s => s.RelativePath)
            .ToList();

        var filesCreated = 0;
        var spawnsMovedCount = 0;

        foreach (var group in byRelativePath)
        {
            var relativePath = group.Key;
            var outputPath = Path.Combine(sharedPath, relativePath);
            var outputDir = Path.GetDirectoryName(outputPath)!;
            Directory.CreateDirectory(outputDir);

            // Build the JSON array for this file
            var spawnsArray = new JsonArray();
            foreach (var spawn in group.OrderBy(s => s.Guid))
            {
                spawnsArray.Add(spawn.Node.DeepClone());
                spawnsMovedCount++;
            }

            // Write to shared folder
            WriteFormattedJson(outputPath, spawnsArray);
            filesCreated++;
            Console.WriteLine($"  Created: shared/{relativePath} ({group.Count()} spawns)");

            // Remove from original files
            RemoveSpawnsFromFile(postUomlSpawns, group.Select(s => s.Guid).ToHashSet(), spawnsPath);
            RemoveSpawnsFromFile(uomlSpawns, group.Select(s => s.Guid).ToHashSet(), spawnsPath);
        }

        Console.WriteLine($"\nCreated {filesCreated} files in shared folder");
        Console.WriteLine($"Moved {spawnsMovedCount} spawns total");
    }

    private static void RemoveSpawnsFromFile(
        Dictionary<string, SpawnData> spawns,
        HashSet<string> guidsToRemove,
        string spawnsPath)
    {
        // Group by file
        var byFile = guidsToRemove
            .Where(g => spawns.ContainsKey(g))
            .Select(g => spawns[g])
            .GroupBy(s => s.FilePath)
            .ToList();

        foreach (var fileGroup in byFile)
        {
            var filePath = fileGroup.Key;
            if (!File.Exists(filePath)) continue;

            var guidsInFile = fileGroup.Select(s => s.Guid).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var json = File.ReadAllText(filePath);
            var array = JsonNode.Parse(json)?.AsArray();
            if (array == null) continue;

            // Remove matching spawns
            var indicesToRemove = new List<int>();
            for (int i = 0; i < array.Count; i++)
            {
                var guid = array[i]?["guid"]?.GetValue<string>();
                if (guid != null && guidsInFile.Contains(guid))
                {
                    indicesToRemove.Add(i);
                }
            }

            foreach (var idx in indicesToRemove.OrderByDescending(x => x))
            {
                array.RemoveAt(idx);
            }

            if (array.Count == 0)
            {
                // Delete empty file
                File.Delete(filePath);
                Console.WriteLine($"  Deleted (empty): {Path.GetRelativePath(spawnsPath, filePath)}");
            }
            else if (indicesToRemove.Any())
            {
                // Write back
                WriteFormattedJson(filePath, array);
            }
        }
    }

    private static void WriteFormattedJson(string path, JsonArray array)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[");

        for (int i = 0; i < array.Count; i++)
        {
            var node = array[i];
            if (node == null) continue;

            sb.AppendLine("  {");
            WriteSpawnObject(sb, node.AsObject(), "    ");
            sb.Append("  }");
            if (i < array.Count - 1)
                sb.Append(",");
            sb.AppendLine();
        }

        sb.AppendLine("]");
        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteSpawnObject(StringBuilder sb, JsonObject obj, string indent)
    {
        var keys = obj.Select(kvp => kvp.Key).ToList();
        var orderedKeys = new[] { "guid", "type", "location", "map", "homeRange", "walkingRange",
            "minDelay", "maxDelay", "team", "count", "spawnBounds", "entries" };

        var sortedKeys = orderedKeys.Where(k => keys.Contains(k)).Concat(keys.Except(orderedKeys)).ToList();

        for (int i = 0; i < sortedKeys.Count; i++)
        {
            var key = sortedKeys[i];
            var value = obj[key];
            var isLast = i == sortedKeys.Count - 1;

            if (key == "location" && value is JsonArray locArr && locArr.Count >= 3)
            {
                // Compact location on one line
                sb.Append($"{indent}\"{key}\": [{locArr[0]}, {locArr[1]}, {locArr[2]}]");
            }
            else if (key == "spawnBounds" && value is JsonArray boundsArr && boundsArr.Count >= 6)
            {
                // Compact spawnBounds on one line
                sb.Append($"{indent}\"{key}\": [{boundsArr[0]}, {boundsArr[1]}, {boundsArr[2]}, {boundsArr[3]}, {boundsArr[4]}, {boundsArr[5]}]");
            }
            else if (key == "entries" && value is JsonArray entriesArr)
            {
                // Each entry on its own line
                sb.AppendLine($"{indent}\"{key}\": [");
                for (int j = 0; j < entriesArr.Count; j++)
                {
                    var entry = entriesArr[j]?.AsObject();
                    if (entry != null)
                    {
                        var name = entry["name"]?.GetValue<string>() ?? "";
                        var maxCount = entry["maxCount"]?.GetValue<int>() ?? 1;
                        var probability = entry["probability"]?.GetValue<int>() ?? 100;
                        sb.Append($"{indent}  {{ \"name\": \"{name}\", \"maxCount\": {maxCount}, \"probability\": {probability} }}");
                        if (j < entriesArr.Count - 1)
                            sb.Append(",");
                        sb.AppendLine();
                    }
                }
                sb.Append($"{indent}]");
            }
            else
            {
                // Regular key-value
                var valueStr = value?.ToJsonString() ?? "null";
                sb.Append($"{indent}\"{key}\": {valueStr}");
            }

            if (!isLast)
                sb.Append(",");
            sb.AppendLine();
        }
    }

    private static void WriteReport(
        string reportPath,
        Dictionary<string, SpawnData> postUomlSpawns,
        Dictionary<string, SpawnData> uomlSpawns,
        List<string> identicalSpawns,
        List<(string guid, string diff)> differentSpawns)
    {
        var report = new List<string>
        {
            "# Spawn Deduplication Report",
            "",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "",
            "## Summary",
            "",
            $"- Post-UOML spawns: {postUomlSpawns.Count}",
            $"- UOML spawns: {uomlSpawns.Count}",
            $"- Identical spawns (moved to shared): {identicalSpawns.Count}",
            $"- Different spawns (same GUID, different data): {differentSpawns.Count}",
            ""
        };

        if (differentSpawns.Any())
        {
            report.Add("## Spawns with Same GUID but Different Data");
            report.Add("");
            report.Add("These spawns exist in both folders with the same GUID but have differences:");
            report.Add("");
            report.Add("| GUID | Post-UOML File | UOML File | Difference |");
            report.Add("|------|----------------|-----------|------------|");

            foreach (var (guid, diff) in differentSpawns)
            {
                var postFile = postUomlSpawns.TryGetValue(guid, out var ps) ? ps.RelativePath : "N/A";
                var uomlFile = uomlSpawns.TryGetValue(guid, out var us) ? us.RelativePath : "N/A";
                var guidShort = guid.Length > 8 ? guid[..8] + "..." : guid;
                report.Add($"| `{guidShort}` | {postFile} | {uomlFile} | {diff} |");
            }
        }

        report.Add("");
        report.Add("## Files Modified");
        report.Add("");

        // Group identical spawns by file
        var byFile = identicalSpawns
            .Where(g => postUomlSpawns.ContainsKey(g))
            .Select(g => postUomlSpawns[g])
            .GroupBy(s => s.RelativePath)
            .OrderBy(g => g.Key);

        report.Add("| Relative Path | Spawns Moved |");
        report.Add("|---------------|--------------|");

        foreach (var group in byFile)
        {
            report.Add($"| {group.Key} | {group.Count()} |");
        }

        File.WriteAllLines(reportPath, report);
    }

    private class SpawnData
    {
        public string Guid { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public string FolderName { get; set; } = "";
        public JsonNode Node { get; set; } = null!;
    }
}
