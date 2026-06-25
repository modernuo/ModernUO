# Spawner STJ Migration v2 (DTO records) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

> **Supersedes** `2026-06-25-spawner-stj-migration.md` (Approach A). See the design spec §0 Revision for why: deserializing directly into the live spawner `Item` lets STJ construct world-registered objects before validation, leaking them on malformed input. Approach B deserializes to plain `record` DTOs (failure = GC only), then a factory builds the spawner.

**Goal:** Delete `DynamicJson`; spawners (de)serialize through a polymorphic `SpawnerDto` record hierarchy — STJ never touches a live `Item`.

**Architecture:** `abstract record SpawnerDto` + one concrete record per spawner type, each `[JsonDiscoverableType]`. Import: `Deserialize<List<SpawnerDto>>` → `dto.ToSpawner()`. Export: `spawner.ToDto()` → `Serialize(List<SpawnerDto>)`. `SpawnerJsonSerializer` discovers `SpawnerDto` subtypes and wires `$type` polymorphism (no property pruning, no `OnDeserialized`).

**Tech Stack:** .NET 10, System.Text.Json, xUnit, ModernUO source-generated serialization.

## Current state (Approach A already on the branch)

Tasks 1–7 of the v1 plan are committed (HEAD `30d154286`). **Task 1's marker attribute (`Server.Json.JsonDiscoverableTypeAttribute`) carries over unchanged.** The Approach-A artifacts below are REPLACED by this plan and must be removed where noted:
- `BaseSpawner.Json.cs`, `Spawner.Json.cs`, `RegionSpawner.Json.cs`, `ProximitySpawner.Json.cs` (shadow properties + `OnAfterJsonDeserialize` + `ImportLocation`/`ImportMap`) → **deleted**, replaced by DTO records + `BaseSpawner.Dto.cs`.
- `[JsonDiscoverableType]` + `[JsonConstructor]` on `Spawner`/`RegionSpawner`/`ProximitySpawner` → **removed** (the marker moves to the DTO records; STJ never constructs the Items).
- `SpawnerJsonSerializer` prune + `OnDeserialized` resolver modifiers → **removed**; discovery retargets to `SpawnerDto`.
- Export/import command rewires (A) → **replaced** with the DTO versions here.
- A round-trip tests (`SpawnerRoundTripTests`, `RegionSpawnerRoundTripTests`, `ProximitySpawnerRoundTripTests`, `ExportImportFileTests`, `ImportCleanupTests`, `LegacyHomeRangeTests`) → **replaced/adapted** to DTOs.

The net branch diff vs `main` will be Approach B; the A commits remain in history.

## Global Constraints

- **Single-threaded.** No `lock`/`volatile`/`Concurrent*`/`Task.Run`/`new Thread()` in game code. (Migration tool is offline.)
- **Server changes:** keep `JsonDiscoverableTypeAttribute`; **delete** `DynamicJson.cs` (Task 7). No other Server edits.
- **Do not touch binary world-save serialization** (`[SerializationGenerator]`, `[SerializableField]`, `Deserialize(reader, version)`, `MigrateFrom`).
- **Braces on all control flow.** `_camelCase` private fields, `PascalCase` public/record members.
- **No `Console.WriteLine`** — `LogFactory.GetLogger(...)`.
- **Sparse output must match today's `ToJson`** — realized via **nullable DTO properties** + `WhenWritingNull` (the default in `JsonConfig.GetOptions`). The `maxSpawnAttempts` getter omits BOTH `0` and `DefaultMaxSpawnAttempts(10)` (runtime treats `0` as default — `BaseSpawner.cs:~568`).
- **Discriminator:** STJ-default `$type`; value = `type.Name` of the **DTO** unless overridden, but the wire value must be the SPAWNER name (`"Spawner"`, `"RegionSpawner"`, `"ProximitySpawner"`) for data compatibility — so each DTO sets an explicit discriminator override matching the spawner type name.
- **Records use init-only properties** (not positional) so STJ uses the public parameterless ctor; no `[JsonConstructor]` needed.
- **Test hygiene:** any spawner `MoveToWorld`'d OR produced by `ToSpawner()` must be `?.Delete()`d in a `finally` block (declared before `try`); temp files/dirs too. Tests share `[Collection("Sequential UOContent Tests")]`.
- **Build:** `dotnet build ModernUO.sln`. **Test:** `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj`.

---

## File Structure

**Create:**
- `Projects/UOContent/Engines/Spawners/Json/SpawnerDto.cs` — `abstract record SpawnerDto` + `SpawnerDataDto`, `RegionSpawnerDto`, `ProximitySpawnerDto`.
- `Projects/UOContent/Engines/Spawners/BaseSpawner.Dto.cs` — `internal void ApplyDto(SpawnerDto)` (import population) + `private protected` `Dto*` export getters.
- `Projects/UOContent/Engines/Spawners/Spawner.Dto.cs`, `RegionSpawner.Dto.cs`, `ProximitySpawner.Dto.cs` — `public override SpawnerDto ToDto()`.

**Modify:**
- `SpawnerJsonSerializer.cs` — retarget discovery to `SpawnerDto`; drop prune + `OnDeserialized`.
- `Spawner.cs`/`RegionSpawner.cs`/`ProximitySpawner.cs` — remove `[JsonDiscoverableType]` + `[JsonConstructor]` (added in A); add `abstract SpawnerDto ToDto()` on `BaseSpawner`.
- Export/Import commands.
- Tests.

**Delete:** `BaseSpawner.Json.cs`, `Spawner.Json.cs`, `RegionSpawner.Json.cs`, `ProximitySpawner.Json.cs` (A artifacts); `DynamicJson.cs` (Task 7).

---

## Task 2: DTO record hierarchy + serializer retarget (core conversion)

Supersedes A Tasks 2–4. Replaces the shadow-property mechanism with DTO records.

**Files:**
- Create: `Projects/UOContent/Engines/Spawners/Json/SpawnerDto.cs`
- Create: `Projects/UOContent/Engines/Spawners/BaseSpawner.Dto.cs`
- Create: `Projects/UOContent/Engines/Spawners/Spawner.Dto.cs`, `RegionSpawner.Dto.cs`, `ProximitySpawner.Dto.cs`
- Modify: `SpawnerJsonSerializer.cs`; `BaseSpawner.cs` (add `abstract ToDto`); `Spawner.cs`/`RegionSpawner.cs`/`ProximitySpawner.cs` (remove A attributes)
- Delete: `BaseSpawner.Json.cs`, `Spawner.Json.cs`, `RegionSpawner.Json.cs`, `ProximitySpawner.Json.cs`
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/SpawnerDtoRoundTripTests.cs` (replaces the three A round-trip test files — delete those)

**Interfaces:**
- Produces: `Server.Engines.Spawners.SpawnerDto` (abstract, `[JsonDerivedType]`-discovered) with `BaseSpawner ToSpawner()`; `BaseSpawner.ToDto()` (`public abstract SpawnerDto`); `BaseSpawner.ApplyDto(SpawnerDto)` (`internal`); `SpawnerJsonSerializer.Options`/`Configure()` retargeted to `SpawnerDto`.
- Consumes: `Server.Json.JsonDiscoverableTypeAttribute` (Task 1).

- [ ] **Step 1: Write the failing test**

`SpawnerDtoRoundTripTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Server.Regions;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class SpawnerDtoRoundTripTests
{
    [Fact]
    public void Spawner_RoundTrips_ThroughDto()
    {
        Spawner original = null;
        BaseSpawner rebuilt = null;
        try
        {
            original = new Spawner(2, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(7), 0,
                new Rectangle3D(100, 100, 0, 5, 5, 0), "Fisherman");
            original.MoveToWorld(new Point3D(105, 105, 0), Map.Felucca);

            var json = JsonSerializer.Serialize(new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);
            Assert.Contains("\"$type\": \"Spawner\"", json);
            Assert.Contains("\"count\": 2", json);

            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            var s = Assert.IsType<Spawner>(rebuilt);
            Assert.Equal(2, s.Count);
            Assert.Equal(TimeSpan.FromMinutes(3), s.MinDelay);
            Assert.Equal(new Rectangle3D(100, 100, 0, 5, 5, 0), s.SpawnBounds);
            Assert.Equal("Fisherman", Assert.Single(s.Entries).SpawnedName);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
        }
    }

    [Fact]
    public void Spawner_OmitsDomainDefaults()
    {
        Spawner original = null;
        try
        {
            original = new Spawner("Fisherman");
            original.MoveToWorld(new Point3D(110, 110, 0), Map.Felucca);
            var json = JsonSerializer.Serialize(new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);
            Assert.DoesNotContain("minDelay", json);
            Assert.DoesNotContain("maxDelay", json);
            Assert.DoesNotContain("\"team\"", json);
            Assert.DoesNotContain("maxSpawnAttempts", json);
        }
        finally
        {
            original?.Delete();
        }
    }

    [Fact]
    public void RegionSpawner_RoundTrips_RegionByName()
    {
        var region = new BaseRegion("DtoTestRegion", Map.Felucca, 50, new Rectangle3D(1400, 1670, 0, 40, 40, 0));
        region.Register();
        RegionSpawner original = null;
        BaseSpawner rebuilt = null;
        try
        {
            original = new RegionSpawner("Fisherman") { SpawnRegion = region };
            original.MoveToWorld(new Point3D(1416, 1683, 0), Map.Felucca);
            var json = JsonSerializer.Serialize(new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);
            Assert.Contains("\"$type\": \"RegionSpawner\"", json);
            Assert.Contains("DtoTestRegion", json);

            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            Assert.Equal("DtoTestRegion", Assert.IsType<RegionSpawner>(rebuilt).SpawnRegion?.Name);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
            region.Unregister();
        }
    }

    [Fact]
    public void ProximitySpawner_RoundTrips_Fields()
    {
        ProximitySpawner original = null;
        BaseSpawner rebuilt = null;
        try
        {
            original = new ProximitySpawner("Fisherman") { TriggerRange = 4, InstantFlag = true, SpawnMessage = 500000 };
            original.MoveToWorld(new Point3D(120, 120, 0), Map.Felucca);
            var json = JsonSerializer.Serialize(new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);
            Assert.Contains("\"$type\": \"ProximitySpawner\"", json);
            Assert.Contains("\"triggerRange\": 4", json);
            Assert.Contains("\"instant\": true", json);

            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            var p = Assert.IsType<ProximitySpawner>(rebuilt);
            Assert.Equal(4, p.TriggerRange);
            Assert.True(p.InstantFlag);
            Assert.Equal(500000, p.SpawnMessage.Number);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter SpawnerDtoRoundTripTests`
Expected: FAIL — `SpawnerDto`, `ToDto`, `ToSpawner` do not exist.

- [ ] **Step 3a: Create the DTO records**

`Projects/UOContent/Engines/Spawners/Json/SpawnerDto.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * File: SpawnerDto.cs                                                   *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Server.Json;
using Server.Regions;

namespace Server.Engines.Spawners;

/// <summary>
/// Plain data carrier for spawner JSON. System.Text.Json deserializes into these records
/// (never a live Item), so a malformed file fails as GC only. <see cref="ToSpawner"/> builds
/// the real spawner from a fully-validated DTO. Sparse fields are nullable so WhenWritingNull
/// omits domain defaults, matching the legacy ToJson output.
/// </summary>
public abstract record SpawnerDto
{
    [JsonPropertyName("guid")] public Guid? Guid { get; init; }
    [JsonPropertyName("location")] public Point3D Location { get; init; }
    [JsonPropertyName("map")] public Map Map { get; init; }
    [JsonPropertyName("count")] public int Count { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; }
    [JsonPropertyName("minDelay")] public TimeSpan? MinDelay { get; init; }
    [JsonPropertyName("maxDelay")] public TimeSpan? MaxDelay { get; init; }
    [JsonPropertyName("team")] public int? Team { get; init; }
    [JsonPropertyName("walkingRange")] public int? WalkingRange { get; init; }
    [JsonPropertyName("homeRange")] public int? HomeRange { get; init; } // legacy read; never written
    [JsonPropertyName("spawnLocationIsHome")] public bool? SpawnLocationIsHome { get; init; }
    [JsonPropertyName("spawnPositionMode")] public SpawnPositionMode? SpawnPositionMode { get; init; }
    [JsonPropertyName("maxSpawnAttempts")] public int? MaxSpawnAttempts { get; init; }
    [JsonPropertyName("entries")] public List<SpawnerEntry> Entries { get; init; }

    /// <summary>Constructs the empty concrete spawner Item for this DTO.</summary>
    protected abstract BaseSpawner CreateEmpty();

    /// <summary>Builds and populates the live spawner. Override to apply subtype fields after base.</summary>
    public virtual BaseSpawner ToSpawner()
    {
        var spawner = CreateEmpty();
        spawner.ApplyDto(this);
        return spawner;
    }
}

[JsonDiscoverableType("Spawner")]
public sealed record SpawnerDataDto : SpawnerDto
{
    [JsonPropertyName("spawnBounds")] public Rectangle3D? SpawnBounds { get; init; }

    protected override BaseSpawner CreateEmpty() => new Spawner();

    public override BaseSpawner ToSpawner()
    {
        var spawner = (Spawner)base.ToSpawner();
        if (SpawnBounds is { } bounds && bounds != default)
        {
            spawner.SpawnBounds = bounds;
        }

        return spawner;
    }
}

[JsonDiscoverableType("RegionSpawner")]
public sealed record RegionSpawnerDto : SpawnerDto
{
    [JsonPropertyName("region")] public string Region { get; init; }

    protected override BaseSpawner CreateEmpty() => new RegionSpawner();

    public override BaseSpawner ToSpawner()
    {
        var spawner = (RegionSpawner)base.ToSpawner();
        spawner.SpawnRegion = Server.Regions.Region.Find(Region, Map) as BaseRegion;
        return spawner;
    }
}

[JsonDiscoverableType("ProximitySpawner")]
public sealed record ProximitySpawnerDto : SpawnerDto
{
    [JsonPropertyName("spawnBounds")] public Rectangle3D? SpawnBounds { get; init; }
    [JsonPropertyName("triggerRange")] public int TriggerRange { get; init; }
    [JsonPropertyName("spawnMessage")] public TextDefinition SpawnMessage { get; init; }
    [JsonPropertyName("instant")] public bool Instant { get; init; }

    protected override BaseSpawner CreateEmpty() => new ProximitySpawner();

    public override BaseSpawner ToSpawner()
    {
        var spawner = (ProximitySpawner)base.ToSpawner();
        if (SpawnBounds is { } bounds && bounds != default)
        {
            spawner.SpawnBounds = bounds;
        }

        spawner.TriggerRange = TriggerRange;
        spawner.SpawnMessage = SpawnMessage;
        spawner.InstantFlag = Instant;
        return spawner;
    }
}
```

- [ ] **Step 3b: Add `BaseSpawner.Dto.cs` (import population + export getters)**

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * File: BaseSpawner.Dto.cs                                              *
 *************************************************************************/

using System;

namespace Server.Engines.Spawners;

public abstract partial class BaseSpawner
{
    /// <summary>Applies the common DTO fields to this freshly-created spawner (import path).</summary>
    internal void ApplyDto(SpawnerDto dto)
    {
        _guid = dto.Guid ?? Guid.NewGuid();

        if (!string.IsNullOrEmpty(dto.Name))
        {
            Name = dto.Name;
        }

        // Legacy homeRange -> spawnBounds (Map not available yet; use the DTO location).
        if (dto.HomeRange is int homeRange && homeRange >= 0)
        {
            int z;
            int depth;
            if (homeRange == 0)
            {
                z = dto.Location.Z;
                depth = 0;
            }
            else
            {
                z = -128;
                depth = 256;
            }

            SpawnBounds = new Rectangle3D(
                dto.Location.X - homeRange,
                dto.Location.Y - homeRange,
                z,
                homeRange * 2 + 1,
                homeRange * 2 + 1,
                depth
            );
        }

        InitSpawn(dto.Count, dto.MinDelay ?? DefaultMinDelay, dto.MaxDelay ?? DefaultMaxDelay, dto.Team ?? 0, SpawnBounds);

        _walkingRange = dto.WalkingRange ?? -1;
        _spawnLocationIsHome = dto.SpawnLocationIsHome ?? false;
        _spawnPositionMode = dto.SpawnPositionMode ?? SpawnPositionMode.Automatic;
        _maxSpawnAttempts = dto.MaxSpawnAttempts ?? DefaultMaxSpawnAttempts;

        if (dto.Entries != null)
        {
            for (var i = 0; i < dto.Entries.Count; i++)
            {
                var entry = dto.Entries[i];
                AddEntry(entry.SpawnedName, entry.SpawnedProbability, entry.SpawnedMaxCount, false, entry.Properties, entry.Parameters);
            }
        }
    }

    // Export helpers — nullable so WhenWritingNull omits domain defaults, matching legacy ToJson.
    private protected Guid? DtoGuid => _guid;
    private protected string DtoName => string.IsNullOrEmpty(Name) ? null : Name;
    private protected TimeSpan? DtoMinDelay => _minDelay == DefaultMinDelay ? null : _minDelay;
    private protected TimeSpan? DtoMaxDelay => _maxDelay == DefaultMaxDelay ? null : _maxDelay;
    private protected int? DtoTeam => _team == 0 ? null : _team;
    private protected int? DtoWalkingRange => _walkingRange != 0 ? WalkingRange : null;
    private protected bool? DtoSpawnLocationIsHome => _spawnLocationIsHome ? true : null;

    private protected SpawnPositionMode? DtoSpawnPositionMode =>
        _spawnPositionMode is not SpawnPositionMode.Automatic and not SpawnPositionMode.Abandoned
            ? _spawnPositionMode
            : null;

    // Runtime treats 0 identically to DefaultMaxSpawnAttempts (BaseSpawner.cs maxAttempts clamp),
    // so both are omitted — fresh spawners have _maxSpawnAttempts == 0.
    private protected int? DtoMaxSpawnAttempts =>
        _maxSpawnAttempts > 0 && _maxSpawnAttempts != DefaultMaxSpawnAttempts ? _maxSpawnAttempts : null;
}
```

- [ ] **Step 3c: Add `BaseSpawner.ToDto()` abstract + per-type overrides**

In `BaseSpawner.cs`, add the abstract declaration (near `SpawnBounds`):

```csharp
    /// <summary>Builds the JSON DTO for this spawner (export path).</summary>
    public abstract SpawnerDto ToDto();
```

`Projects/UOContent/Engines/Spawners/Spawner.Dto.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * File: Spawner.Dto.cs                                                  *
 *************************************************************************/

namespace Server.Engines.Spawners;

public partial class Spawner
{
    public override SpawnerDto ToDto() => new SpawnerDataDto
    {
        Guid = DtoGuid,
        Location = Location,
        Map = Map,
        Count = Count,
        Name = DtoName,
        MinDelay = DtoMinDelay,
        MaxDelay = DtoMaxDelay,
        Team = DtoTeam,
        WalkingRange = DtoWalkingRange,
        SpawnLocationIsHome = DtoSpawnLocationIsHome,
        SpawnPositionMode = DtoSpawnPositionMode,
        MaxSpawnAttempts = DtoMaxSpawnAttempts,
        Entries = Entries,
        SpawnBounds = SpawnBounds == default ? null : SpawnBounds
    };
}
```

`RegionSpawner.Dto.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * File: RegionSpawner.Dto.cs                                            *
 *************************************************************************/

namespace Server.Engines.Spawners;

public partial class RegionSpawner
{
    public override SpawnerDto ToDto() => new RegionSpawnerDto
    {
        Guid = DtoGuid,
        Location = Location,
        Map = Map,
        Count = Count,
        Name = DtoName,
        MinDelay = DtoMinDelay,
        MaxDelay = DtoMaxDelay,
        Team = DtoTeam,
        WalkingRange = DtoWalkingRange,
        SpawnLocationIsHome = DtoSpawnLocationIsHome,
        SpawnPositionMode = DtoSpawnPositionMode,
        MaxSpawnAttempts = DtoMaxSpawnAttempts,
        Entries = Entries,
        Region = SpawnRegion?.Name
    };
}
```

`ProximitySpawner.Dto.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * File: ProximitySpawner.Dto.cs                                         *
 *************************************************************************/

namespace Server.Engines.Spawners;

public partial class ProximitySpawner
{
    public override SpawnerDto ToDto() => new ProximitySpawnerDto
    {
        Guid = DtoGuid,
        Location = Location,
        Map = Map,
        Count = Count,
        Name = DtoName,
        MinDelay = DtoMinDelay,
        MaxDelay = DtoMaxDelay,
        Team = DtoTeam,
        WalkingRange = DtoWalkingRange,
        SpawnLocationIsHome = DtoSpawnLocationIsHome,
        SpawnPositionMode = DtoSpawnPositionMode,
        MaxSpawnAttempts = DtoMaxSpawnAttempts,
        Entries = Entries,
        SpawnBounds = SpawnBounds == default ? null : SpawnBounds,
        TriggerRange = TriggerRange,
        SpawnMessage = SpawnMessage,
        Instant = InstantFlag
    };
}
```

- [ ] **Step 3d: Retarget + simplify `SpawnerJsonSerializer`**

Edit `SpawnerJsonSerializer.cs`: change the discovery filter from `typeof(BaseSpawner)` to `typeof(SpawnerDto)` (in `Collect`/`Validate`), change `AddPolymorphism` to gate on `typeInfo.Type == typeof(SpawnerDto)`, and **remove** the `PruneToJsonProperties` and `AddOnDeserialized` modifiers (and their entries in the `Modifiers` list). Result — `Options` resolver has a single modifier (polymorphism); `Configure`/`Collect`/`Validate`/`IsJsonConstructible` otherwise unchanged but operating on `SpawnerDto` subtypes. Keep the `TextDefinitionConverterFactory` registration (for `ProximitySpawnerDto.SpawnMessage`).

- [ ] **Step 3e: Remove the Approach-A artifacts**

```bash
git rm Projects/UOContent/Engines/Spawners/BaseSpawner.Json.cs \
       Projects/UOContent/Engines/Spawners/Spawner.Json.cs \
       Projects/UOContent/Engines/Spawners/RegionSpawner.Json.cs \
       Projects/UOContent/Engines/Spawners/ProximitySpawner.Json.cs \
       Projects/UOContent.Tests/Tests/Engines/Spawners/Json/SpawnerRoundTripTests.cs \
       Projects/UOContent.Tests/Tests/Engines/Spawners/Json/RegionSpawnerRoundTripTests.cs \
       Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ProximitySpawnerRoundTripTests.cs
```

In `Spawner.cs`, `RegionSpawner.cs`, `ProximitySpawner.cs`: remove the `[JsonDiscoverableType]` class attribute and the `[JsonConstructor]` on the parameterless ctor that Approach A added (leave `[Constructible]`). Remove now-unused `using Server.Json;`/`using System.Text.Json;` if the compiler flags them.

- [ ] **Step 4: Build, then run the tests**

Run: `dotnet build ModernUO.sln` then `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter SpawnerDtoRoundTripTests`
Expected: build SUCCESS; 4/4 pass. If `$type` is missing, confirm the list element type is `SpawnerDto` and the discriminator override strings match.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(spawners): deserialize via SpawnerDto records instead of live Items"
```

---

## Task 3: Legacy homeRange read regression (DTO)

**Files:**
- Create: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/LegacyHomeRangeTests.cs` (replaces the A version if it still exists — `git rm` it first if present)

- [ ] **Step 1: Write the test**

```csharp
using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class LegacyHomeRangeTests
{
    [Theory]
    [InlineData(3, 97, 197, -128, 7, 7, 256)]
    [InlineData(0, 100, 200, 5, 1, 1, 0)]
    public void HomeRange_ConvertsToCenteredBounds(int homeRange, int x, int y, int z, int w, int h, int d)
    {
        var json = $$"""
        [ { "$type": "Spawner", "location": [100, 200, 5], "map": "Felucca", "count": 1,
            "homeRange": {{homeRange}},
            "entries": [ { "name": "Fisherman", "maxCount": 1, "probability": 100 } ] } ]
        """;

        BaseSpawner s = null;
        try
        {
            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            s = Assert.Single(dtos).ToSpawner();
            Assert.Equal(new Rectangle3D(x, y, z, w, h, d), s.SpawnBounds);
        }
        finally
        {
            s?.Delete();
        }
    }
}
```

- [ ] **Step 2: Run (implementation exists from Task 2)**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter LegacyHomeRangeTests`
Expected: PASS (2 cases). If FAIL, fix `BaseSpawner.ApplyDto`'s homeRange block, not the test.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "test(spawners): lock legacy homeRange->spawnBounds via DTO"
```

---

## Task 4: Rewire `ExportSpawnersCommand` to DTOs

**Files:**
- Modify: `Projects/UOContent/Engines/Spawners/Commands/ExportSpawnersCommand.cs`
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ExportImportFileTests.cs` (replace the A version — `git rm` it first)

- [ ] **Step 1: Edit the export loop**

Build a `List<SpawnerDto>` by calling `ToDto()` (preserve the existing `Map.Internal`/`Parent`/name-prefix selection guards), then `JsonConfig.Serialize(path, spawnRecords, SpawnerJsonSerializer.Options)`:

```csharp
        var spawnRecords = new List<SpawnerDto>(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is not BaseSpawner spawner || spawner.Map == Map.Internal || spawner.Parent != null)
            {
                continue;
            }

            // (preserve any existing name-prefix filter here)
            spawnRecords.Add(spawner.ToDto());
        }

        if (spawnRecords.Count == 0)
        {
            LogFailure("No matching spawners found.");
            return;
        }

        e.Mobile.SendMessage("Exporting spawners...");
        JsonConfig.Serialize(path, spawnRecords, SpawnerJsonSerializer.Options);
```

Remove any leftover `DynamicJson`/`ToJson` calls and unused `options` locals. Keep `using Server.Json;`.

- [ ] **Step 2: Build**

Run: `dotnet build Projects/UOContent/UOContent.csproj` → SUCCESS.

- [ ] **Step 3: Write the file round-trip test**

```csharp
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Engines.Spawners;
using Server.Json;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class ExportImportFileTests
{
    [Fact]
    public void Serialize_ThenDeserialize_File_PreservesSpawner()
    {
        Spawner original = null;
        BaseSpawner rebuilt = null;
        var path = Path.GetTempFileName();
        try
        {
            original = new Spawner(3, System.TimeSpan.FromMinutes(4), System.TimeSpan.FromMinutes(8), 1,
                new Rectangle3D(200, 200, 0, 9, 9, 0), "Tanner");
            original.MoveToWorld(new Point3D(204, 204, 0), Map.Felucca);

            JsonConfig.Serialize(path, new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);

            var dtos = JsonConfig.Deserialize<List<SpawnerDto>>(path, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            var s = Assert.IsType<Spawner>(rebuilt);
            Assert.Equal(3, s.Count);
            Assert.Equal(1, s.Team);
            Assert.Equal(new Rectangle3D(200, 200, 0, 9, 9, 0), s.SpawnBounds);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
            File.Delete(path);
        }
    }
}
```

- [ ] **Step 4: Run**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter ExportImportFileTests` → PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(spawners): export via SpawnerDto"
```

---

## Task 5: Rewire `ImportSpawnersCommand` to DTOs (leak-free)

**Files:**
- Modify: `Projects/UOContent/Engines/Spawners/Commands/ImportSpawnersCommand.cs`
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ImportCleanupTests.cs` (replace the A version)

**Interfaces:**
- Consumes: `SpawnerDto.ToSpawner()`, `SpawnerDto.Location`/`.Map`, `SpawnerJsonSerializer.Options`.

- [ ] **Step 1: Rewrite `ImportJsonSpawners`**

```csharp
    private static void ImportJsonSpawners(
        Mobile from,
        FileInfo file,
        Dictionary<Guid, ISpawner> allSpawners,
        ref int totalGenerated,
        ref int totalFailures
    )
    {
        List<SpawnerDto> dtos;
        try
        {
            // DTO deserialization constructs NO world objects — a malformed file fails as GC only.
            dtos = JsonConfig.Deserialize<List<SpawnerDto>>(file.FullName, SpawnerJsonSerializer.Options);
        }
        catch (JsonException)
        {
            from?.SendMessage(
                $"GenerateSpawners: Exception parsing {file.FullName}, file may not be in the correct format."
            );
            return;
        }

        if (dtos == null || dtos.Count == 0)
        {
            from?.SendMessage($"GenerateSpawners: Skipping empty spawner file {file.Name}");
            logger.Information("{User} is skipping empty spawner file {File}", from, file.FullName);
            return;
        }

        using var queue = PooledRefQueue<Item>.Create();
        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var location = dto.Location;
            var map = dto.Map;

            if (map == null || map == Map.Internal)
            {
                logger.Error("Spawner {Guid} ({Index}) has no valid map; skipping.", dto.Guid, i);
                totalFailures++;
                continue;
            }

            BaseSpawner spawner;
            try
            {
                spawner = dto.ToSpawner(); // constructs the single Item, now referenced
            }
            catch (Exception ex)
            {
                TraceException(ex, $"Failed to build spawner {dto.Guid}.");
                totalFailures++;
                continue;
            }

            var type = spawner.GetType();
            foreach (var existing in map.GetItemsAt<BaseSpawner>(location))
            {
                if (existing.GetType() == type && existing != spawner)
                {
                    queue.Enqueue(existing);
                    allSpawners.Remove(existing.Guid);
                }
            }

            while (queue.Count > 0)
            {
                queue.Dequeue().Delete();
            }

            try
            {
                spawner.MoveToWorld(location, map);
                spawner.Respawn();

                if (allSpawners.Remove(spawner.Guid, out var oldSpawner))
                {
                    oldSpawner.Delete();
                }

                allSpawners.Add(spawner.Guid, spawner);
                totalGenerated++;
            }
            catch (Exception ex)
            {
                TraceException(ex, $"Failed to generate spawner {spawner.Guid}.");
                spawner.Delete();
                totalFailures++;
            }
        }
    }
```

Remove now-unused `using` for `AssemblyHandler`/`System.Reflection` if flagged. Keep `using Server.Json;`. If a test seam is needed (no public file entry), add a minimal `internal static void ImportFile(FileInfo, Dictionary<Guid, ISpawner>)` wrapping this; verify `[assembly: InternalsVisibleTo("UOContent.Tests")]` exists.

- [ ] **Step 2: Build** → `dotnet build ModernUO.sln` SUCCESS.

- [ ] **Step 3: Tests — placement + malformed-no-leak**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class ImportCleanupTests
{
    [Fact]
    public void Import_ValidFile_PlacesSpawner()
    {
        var guid = new Guid("11111111-1111-1111-1111-111111111111");
        var dir = Path.Combine(Path.GetTempPath(), "muo-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "t.json"), $$"""
        [ { "$type": "Spawner", "guid": "{{guid}}", "location": [305, 305, 0], "map": "Felucca",
            "count": 1, "spawnBounds": { "x1": 300, "y1": 300, "x2": 310, "y2": 310 },
            "entries": [ { "name": "Fisherman", "maxCount": 1, "probability": 100 } ] } ]
        """);
        try
        {
            ImportSpawnersCommand.ImportFile(new FileInfo(Path.Combine(dir, "t.json")), new Dictionary<Guid, ISpawner>());
            var found = false;
            foreach (var s in Map.Felucca.GetItemsAt<BaseSpawner>(new Point3D(305, 305, 0)))
            {
                if (s.Guid == guid) { found = true; s.Delete(); }
            }
            Assert.True(found);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Import_MalformedFile_LeaksNoWorldItems()
    {
        var dir = Path.Combine(Path.GetTempPath(), "muo-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        // First entry valid, then truncated/garbage — STJ throws mid-array.
        File.WriteAllText(Path.Combine(dir, "bad.json"), """
        [ { "$type": "Spawner", "location": [320, 320, 0], "map": "Felucca", "count": 1,
            "entries": [ { "name": "Fisherman", "maxCount": 1, "probability": 100 } ] },
          { "$type": "Spawner", "location": [ THIS IS NOT JSON
        """);
        try
        {
            var before = CountSpawnersNear(new Point3D(320, 320, 0));
            ImportSpawnersCommand.ImportFile(new FileInfo(Path.Combine(dir, "bad.json")), new Dictionary<Guid, ISpawner>());
            // Malformed parse must construct zero Items (DTO is GC-only); nothing placed or orphaned.
            Assert.Equal(before, CountSpawnersNear(new Point3D(320, 320, 0)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static int CountSpawnersNear(Point3D p)
    {
        var n = 0;
        foreach (var _ in Map.Felucca.GetItemsAt<BaseSpawner>(p)) { n++; }
        return n;
    }
}
```

> If `Map.GetItemsAt` is not the right API for an Internal-orphan check, the malformed test's intent is: after importing a file that throws mid-array, no new `BaseSpawner` exists at the valid entry's location (320,320,0) — because DTO parse builds no Items. Adapt the query to whatever the codebase exposes; the assertion is "zero spawners constructed from a malformed file."

- [ ] **Step 4: Run**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter ImportCleanupTests` → PASS (both). Then full suite once.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(spawners): import via SpawnerDto (leak-free on malformed input)"
```

---

## Task 6: One-time data migration to `$type`

Identical to v1 plan Task 8 (the data format is independent of A vs B). Run the `tools/spawner-json-migrate/migrate.mjs` transform (rename `type`→`$type` first-key; `homeRange`+`location`→`spawnBounds`), validate, commit data, remove the tool. **Adapt the verification test** to deserialize `List<SpawnerDto>` (not `List<BaseSpawner>`) and call `ToSpawner()` on each, cleaning up in `finally`. Use the migration script and steps from v1 Task 8 verbatim except the test's deserialize target.

- [ ] Steps: see v1 plan Task 8, Steps 1–6, with the test changed to `JsonSerializer.Deserialize<List<SpawnerDto>>(...)` + `dto.ToSpawner()` + `?.Delete()`.

---

## Task 7: Delete `DynamicJson` + old spawner JSON members

**Files:**
- Delete: `Projects/Server/Json/DynamicJson.cs`
- Modify: `BaseSpawner.cs`, `Spawner.cs`, `RegionSpawner.cs`, `ProximitySpawner.cs` (remove `(DynamicJson, options)` ctors + `ToJson` methods)

- [ ] **Step 1:** Remove every `(DynamicJson, options)` constructor and `ToJson(DynamicJson, ...)` method from the four spawner classes. Remove unused `using Server.Json;`/`using System.Text.Json;`.
- [ ] **Step 2:** `git rm Projects/Server/Json/DynamicJson.cs`
- [ ] **Step 3:** `git grep -n "DynamicJson"` → no matches.
- [ ] **Step 4:** `dotnet build ModernUO.sln` → SUCCESS.
- [ ] **Step 5:** Commit: `refactor(json): delete DynamicJson; spawners use SpawnerDto exclusively`

---

## Task 8: Load-all regression over `Spawns/`

Identical to v1 Task 10, with the deserialize target `List<SpawnerDto>` and a `ToSpawner()` build per entry (cleaned up in `finally`):

- [ ] **Step 1:** Test `AllSpawnFilesLoadTests`: for every `Data/Spawns/**/*.json`, `JsonSerializer.Deserialize<List<SpawnerDto>>(...)`; for each DTO call `ToSpawner()` then `Delete()`; collect any `JsonException` into a failures list; `Assert.True(failures.Count == 0, join)`. Guard `if (!Directory.Exists(root)) return;`.
- [ ] **Step 2:** Run → PASS.
- [ ] **Step 3:** Commit: `test(spawners): assert all migrated spawn files build via DTO`

---

## Task 9: Discovery validation tests

As v1 Task 11 but against `SpawnerDto` subtypes. Refactor `SpawnerJsonSerializer.Collect` to delegate to `internal static (string, JsonDerivedType) Validate(Type, Dictionary<string,Type>)`; test duplicate-discriminator detection with two nested `[JsonDiscoverableType("dup")]` `SpawnerDto` records.

- [ ] **Step 1:** Expose `Validate` (internal). Confirm `InternalsVisibleTo("UOContent.Tests")`.
- [ ] **Step 2:** Test `DuplicateDiscriminator_Throws`: two `record DupA/DupB : SpawnerDto` with `[JsonDiscoverableType("dup")]` and a trivial `CreateEmpty()` (e.g. `=> new Spawner()`); first `Validate` registers, second throws containing `"dup"`.
- [ ] **Step 3:** Run → PASS.
- [ ] **Step 4:** Commit: `test(spawners): validate duplicate SpawnerDto discriminator detection`

---

## Task 10: Full suite + spec sync + final review

- [ ] **Step 1:** `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj` and `dotnet test Projects/Server.Tests/Server.Tests.csproj` → all PASS.
- [ ] **Step 2:** Confirm the design spec §0 Revision matches what shipped; fix any drift.
- [ ] **Step 3:** Commit any doc edits. Then the controller runs the final whole-branch review.

---

## Self-Review

**Spec coverage (Approach B):**
- Delete DynamicJson → Task 7. ✓
- DTO records, no live-Item deserialize → Task 2. ✓
- Exact sparse output via nullable DTO props + WhenWritingNull → Task 2 (`Dto*` getters) + omission test. ✓
- Auto-discovery, no registration; reusable marker → Task 1 (kept) + Task 2 retarget to `SpawnerDto`. ✓
- `$type` migration → Task 6. ✓
- Legacy homeRange read → Task 2 (`ApplyDto`) + Task 3 test. ✓
- Leak-free import (the whole reason for B) → Task 5 + malformed-no-leak test. ✓
- Collision/constructibility validation → Task 2 (`Collect`) + Task 9. ✓
- Open extensibility (custom spawner = custom DTO + `ToDto` override) → DTO design. ✓

**Placeholder scan:** No TBD/"handle edge cases". The Task 5 malformed-test API caveat and Task 4 name-filter note give explicit decision rules.

**Type consistency:** `SpawnerDto.ToSpawner()`, `BaseSpawner.ToDto()`/`ApplyDto()`, `Dto*` getters, `SpawnerJsonSerializer.Options/Configure/Validate` consistent across tasks. DTO discriminator overrides (`"Spawner"`/`"RegionSpawner"`/`"ProximitySpawner"`) match the migrated data and the round-trip assertions.

**Risk to watch during execution:** record init-property deserialization with the custom converters (Point3D/Map/Rectangle3D/TextDefinition) — confirm they bind through `[JsonPropertyName]` init props (Task 2 round-trip test is the gate). `Region.Find` in `RegionSpawnerDto.ToSpawner` needs the region registered (test registers one). `InitSpawn` is called once in `ApplyDto`; derived `ToSpawner` overrides set subtype fields AFTER base — matches legacy ordering.
