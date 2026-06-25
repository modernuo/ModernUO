// Regression test: verifies that EVERY migrated spawn file deserializes and builds spawners.
// This catches any migration defects where a spawn file is syntactically correct JSON
// but fails to build spawners via DTO.ToSpawner(). Any failure names the offending file
// and is a real migration defect, not a test weakness.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class AllSpawnFilesLoadTests
{
    [Fact]
    public void EverySpawnFile_DeserializesAndBuildsSpawners()
    {
        var root = Path.Combine(Core.BaseDirectory, "Data", "Spawns");
        if (!Directory.Exists(root))
        {
            return; // distribution data not present in this checkout
        }

        var failures = new List<string>();
        var fileCount = 0;
        var spawnerCount = 0;

        foreach (var file in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
        {
            fileCount++;
            try
            {
                var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(
                    File.ReadAllText(file), SpawnerJsonSerializer.Options);

                if (dtos != null)
                {
                    foreach (var dto in dtos)
                    {
                        try
                        {
                            var spawner = dto.ToSpawner();
                            spawnerCount++;
                            spawner?.Delete();
                        }
                        catch (Exception ex)
                        {
                            failures.Add($"{file}: ToSpawner failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                failures.Add($"{file}: JSON deserialization failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                failures.Add($"{file}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.True(
            failures.Count == 0,
            $"Loaded {fileCount} files, built {spawnerCount} spawners. Failures:\n{string.Join("\n", failures)}"
        );
    }
}
