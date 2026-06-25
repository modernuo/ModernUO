# Spawner STJ Migration (Remove DynamicJson) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Delete `DynamicJson` and make spawners directly System.Text.Json-(de)serializable via a discovery-based polymorphic serializer, with the data files migrated to the `$type` discriminator.

**Architecture:** Each concrete spawner stays the STJ target (no DTOs). JSON binds to inert, hand-written **shadow properties** (backed by transient `_json*` fields; getters read live state for export, setters stash raw values for import). All imperative wiring (`InitSpawn`, `AddEntry`, legacy `homeRange`→`spawnBounds`, region lookup) runs in a single `OnAfterJsonDeserialize()` virtual hook fired by the resolver's `OnDeserialized`. A `SpawnerJsonSerializer` (mirroring `RegionJsonSerializer`) auto-discovers `[JsonDiscoverableType]`-marked `BaseSpawner` subclasses at the `Configure` phase, wires STJ polymorphism, prunes non-JSON engine properties, and validates loudly.

**Tech Stack:** .NET 10, System.Text.Json, xUnit, ModernUO source-generated serialization.

## Global Constraints

- **Single-threaded game loop.** No `lock`/`volatile`/`Concurrent*`/`Task.Run`/`new Thread()` in game code. (The one-time migration tool is a standalone offline utility and may use ordinary file I/O.)
- **Do not modify `Projects/Server/` beyond:** adding `JsonDiscoverableTypeAttribute` and **deleting** `DynamicJson.cs`. Everything else lives in `Projects/UOContent/`.
- **Do not touch binary world-save serialization** (`[SerializationGenerator]`, `[SerializableField]`, `Deserialize(reader, version)`, `MigrateFrom`). The JSON path is independent.
- **Braces on all control flow.** `_camelCase` private fields, `PascalCase` public members.
- **No `Console.WriteLine`** — use `LogFactory.GetLogger(typeof(X))`.
- **Sparse export must match today's `ToJson` output exactly** (which fields are present/omitted), achieved via nullable shadow getters + `JsonIgnoreCondition.WhenWritingNull` (already the default in `JsonConfig.DefaultOptions`).
- **Discriminator:** STJ-default `$type`; discriminator value = `type.Name` unless overridden. Existing values (`"Spawner"`, `"RegionSpawner"`, `"ProximitySpawner"`) are unchanged; only the key changes from `"type"`.
- **Tests:** xUnit. World-dependent tests use `[Collection("Sequential UOContent Tests")]`. `SpawnerJsonSerializer.Configure()` must be called once in `TestServerInitializer` (production calls it via `AssemblyHandler.Invoke("Configure")`).
- **Build:** `dotnet build ModernUO.sln` from repo root. **Test:** `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj`.

> **Note (refines spec D1):** the spec proposed `ShouldSerialize` modifiers for sparse output. During planning we found nullable shadow getters + `WhenWritingNull` produce identical output with less machinery and no resolver property-iteration for serialization. This plan uses that. All other spec decisions (D2–D5) stand.

---

## File Structure

**Create:**
- `Projects/Server/Json/JsonDiscoverableTypeAttribute.cs` — reusable opt-in marker.
- `Projects/UOContent/Engines/Spawners/SpawnerJsonSerializer.cs` — discovery, options, resolver.
- `Projects/UOContent/Engines/Spawners/BaseSpawner.Json.cs` — BaseSpawner shadow props + `OnAfterJsonDeserialize` (partial; keeps JSON concerns out of the main file).
- `Projects/UOContent/Engines/Spawners/Spawner.Json.cs`
- `Projects/UOContent/Engines/Spawners/RegionSpawner.Json.cs`
- `Projects/UOContent/Engines/Spawners/ProximitySpawner.Json.cs`
- `tools/spawner-json-migrate/` — one-time offline conversion utility (throwaway; not part of the server build).
- Tests under `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/`.

**Modify:**
- `Projects/UOContent/Engines/Spawners/BaseSpawner.cs` — remove `(DynamicJson, options)` ctor and `ToJson`; add `[JsonDiscoverableType]` is **not** placed here (abstract). Add `[JsonConstructor]` to the parameterless ctor.
- `Projects/UOContent/Engines/Spawners/Spawner.cs`, `RegionSpawner.cs`, `ProximitySpawner.cs` — remove `(DynamicJson,…)` ctor + `ToJson`; add `[JsonDiscoverableType]` + `[JsonConstructor]`.
- `Projects/UOContent/Engines/Spawners/Commands/ExportSpawnersCommand.cs`, `ImportSpawnersCommand.cs` — rewire to typed (de)serialization.
- `Projects/UOContent.Tests/Fixtures/TestServerInitializer.cs` — call `SpawnerJsonSerializer.Configure()`.
- `Distribution/Data/Spawns/**/*.json` — one-time migrated output (Task 8).

**Delete:**
- `Projects/Server/Json/DynamicJson.cs` (Task 9).

---

## Task 1: Reusable opt-in marker attribute

**Files:**
- Create: `Projects/Server/Json/JsonDiscoverableTypeAttribute.cs`
- Test: `Projects/Server.Tests/Tests/Json/JsonDiscoverableTypeAttributeTests.cs`

**Interfaces:**
- Produces: `Server.Json.JsonDiscoverableTypeAttribute(string discriminator = null)` with `string Discriminator { get; }`.

- [ ] **Step 1: Write the failing test**

```csharp
using Server.Json;
using Xunit;

namespace Server.Tests.Json;

public class JsonDiscoverableTypeAttributeTests
{
    [JsonDiscoverableType]
    private sealed class DefaultName { }

    [JsonDiscoverableType("custom")]
    private sealed class Overridden { }

    [Fact]
    public void DefaultDiscriminator_IsNull()
    {
        var attr = (JsonDiscoverableTypeAttribute)System.Attribute.GetCustomAttribute(
            typeof(DefaultName), typeof(JsonDiscoverableTypeAttribute), false);
        Assert.NotNull(attr);
        Assert.Null(attr.Discriminator);
    }

    [Fact]
    public void OverrideDiscriminator_IsReturned()
    {
        var attr = (JsonDiscoverableTypeAttribute)System.Attribute.GetCustomAttribute(
            typeof(Overridden), typeof(JsonDiscoverableTypeAttribute), false);
        Assert.Equal("custom", attr.Discriminator);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Projects/Server.Tests/Server.Tests.csproj --filter JsonDiscoverableTypeAttributeTests`
Expected: FAIL — `JsonDiscoverableTypeAttribute` does not exist (compile error).

- [ ] **Step 3: Write minimal implementation**

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: JsonDiscoverableTypeAttribute.cs                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace Server.Json;

/// <summary>
/// Marks a concrete class as a discoverable polymorphic JSON derived type. A consumer
/// (e.g. <c>SpawnerJsonSerializer</c>) scans assemblies for marked subclasses of a chosen
/// base and registers them for System.Text.Json polymorphism. Optionally overrides the
/// <c>$type</c> discriminator value (defaults to the type's <see cref="System.Type.Name"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class JsonDiscoverableTypeAttribute : Attribute
{
    public JsonDiscoverableTypeAttribute(string discriminator = null) => Discriminator = discriminator;

    public string Discriminator { get; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Projects/Server.Tests/Server.Tests.csproj --filter JsonDiscoverableTypeAttributeTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add Projects/Server/Json/JsonDiscoverableTypeAttribute.cs \
        Projects/Server.Tests/Tests/Json/JsonDiscoverableTypeAttributeTests.cs
git commit -m "feat(json): add reusable JsonDiscoverableType opt-in marker attribute"
```

---

## Task 2: SpawnerJsonSerializer + BaseSpawner/Spawner JSON binding (core round-trip)

This is the foundational slice: the serializer plus the first end-to-end round-trip. It validates discovery, polymorphism, property pruning, the shadow-property pattern, and `OnAfterJsonDeserialize`.

**Files:**
- Create: `Projects/UOContent/Engines/Spawners/SpawnerJsonSerializer.cs`
- Create: `Projects/UOContent/Engines/Spawners/BaseSpawner.Json.cs`
- Create: `Projects/UOContent/Engines/Spawners/Spawner.Json.cs`
- Modify: `Projects/UOContent/Engines/Spawners/Spawner.cs` (add `[JsonDiscoverableType]` + `[JsonConstructor]`; remove `(DynamicJson,…)` ctor + `ToJson` — see Task 9 if you prefer to defer deletion, but doing it here keeps the file compiling without `DynamicJson` once it is gone; for now leave the old members in place and remove in Task 9). **In this task, only ADD the attributes and the Json partial; do not delete the old `DynamicJson` members yet** so the project keeps building.
- Modify: `Projects/UOContent.Tests/Fixtures/TestServerInitializer.cs`
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/SpawnerRoundTripTests.cs`

**Interfaces:**
- Produces:
  - `Server.Engines.Spawners.SpawnerJsonSerializer` with `static void Configure()`, `static JsonSerializerOptions Options { get; }`.
  - `BaseSpawner.OnAfterJsonDeserialize()` — `protected internal virtual void`.
  - BaseSpawner transient fields consumed by derived hooks: `_jsonLocation` (`Point3D`), `_jsonMap` (`Map`).
- Consumes: `Server.Json.JsonDiscoverableTypeAttribute` (Task 1); `Server.Json.JsonConfig`.

- [ ] **Step 1: Write the failing test**

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class SpawnerRoundTripTests
{
    private static Map Map => Map.Felucca;

    [Fact]
    public void Spawner_RoundTrips_TypeAndCoreFields()
    {
        var spawner = new Spawner(2, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(7), 0,
            new Rectangle3D(100, 100, 0, 5, 5, 0), "Fisherman");
        spawner.MoveToWorld(new Point3D(105, 105, 0), Map);

        var json = JsonSerializer.Serialize<List<BaseSpawner>>(
            new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);

        Assert.Contains("\"$type\": \"Spawner\"", json);
        Assert.Contains("\"count\": 2", json);

        var roundTripped = JsonSerializer.Deserialize<List<BaseSpawner>>(json, SpawnerJsonSerializer.Options);
        var s = Assert.IsType<Spawner>(Assert.Single(roundTripped));
        Assert.Equal(2, s.Count);
        Assert.Equal(TimeSpan.FromMinutes(3), s.MinDelay);
        Assert.Equal(TimeSpan.FromMinutes(7), s.MaxDelay);
        Assert.Equal(new Rectangle3D(100, 100, 0, 5, 5, 0), s.SpawnBounds);
        Assert.Single(s.Entries);
        Assert.Equal("Fisherman", s.Entries[0].SpawnedName);

        s.Delete();
        spawner.Delete();
    }

    [Fact]
    public void Spawner_OmitsDomainDefaults()
    {
        // Default delays (5/10 min), team 0, default maxSpawnAttempts → omitted.
        var spawner = new Spawner("Fisherman");
        spawner.MoveToWorld(new Point3D(110, 110, 0), Map);

        var json = JsonSerializer.Serialize<List<BaseSpawner>>(
            new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);

        Assert.DoesNotContain("minDelay", json);
        Assert.DoesNotContain("maxDelay", json);
        Assert.DoesNotContain("\"team\"", json);
        Assert.DoesNotContain("maxSpawnAttempts", json);

        spawner.Delete();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter SpawnerRoundTripTests`
Expected: FAIL — `SpawnerJsonSerializer` does not exist; `Spawner` not discoverable.

- [ ] **Step 3a: Create the serializer**

`Projects/UOContent/Engines/Spawners/SpawnerJsonSerializer.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpawnerJsonSerializer.cs                                        *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Server.Json;
using Server.Logging;

namespace Server.Engines.Spawners;

public static class SpawnerJsonSerializer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SpawnerJsonSerializer));

    private static JsonDerivedType[] _derivedTypes = Array.Empty<JsonDerivedType>();
    private static JsonSerializerOptions _options;

    /// <summary>
    /// Invoked automatically during the Configure bootstrap phase
    /// (AssemblyHandler.Invoke("Configure")). Discovers every concrete BaseSpawner subclass
    /// marked with [JsonDiscoverableType] and registers it for STJ polymorphism.
    /// </summary>
    public static void Configure()
    {
        var discovered = new List<JsonDerivedType>();
        var byDiscriminator = new Dictionary<string, Type>();

        foreach (var asm in AssemblyHandler.Assemblies)
        {
            Collect(AssemblyHandler.GetTypeCache(asm).Types, discovered, byDiscriminator);
        }

        Collect(AssemblyHandler.GetTypeCache(Core.Assembly).Types, discovered, byDiscriminator);

        _derivedTypes = discovered.ToArray();
        _options = null; // force rebuild with the discovered types

        logger.Information("Discovered {Count} spawner JSON type(s)", _derivedTypes.Length);
    }

    private static void Collect(Type[] types, List<JsonDerivedType> discovered, Dictionary<string, Type> byDiscriminator)
    {
        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            if (type.IsAbstract || !type.IsAssignableTo(typeof(BaseSpawner)))
            {
                continue;
            }

            var attr = (JsonDiscoverableTypeAttribute)Attribute.GetCustomAttribute(
                type, typeof(JsonDiscoverableTypeAttribute), false);
            if (attr == null)
            {
                continue;
            }

            if (!IsJsonConstructible(type))
            {
                throw new Exception(
                    $"Spawner type '{type.FullName}' is marked [JsonDiscoverableType] but System.Text.Json cannot construct it. " +
                    "Add a public parameterless constructor marked [JsonConstructor]."
                );
            }

            var discriminator = attr.Discriminator ?? type.Name;
            if (byDiscriminator.TryGetValue(discriminator, out var existing))
            {
                throw new Exception(
                    $"Spawner JSON discriminator '{discriminator}' is claimed by both '{existing.FullName}' and " +
                    $"'{type.FullName}'. Set an explicit discriminator via [JsonDiscoverableType(\"...\")] on one."
                );
            }

            byDiscriminator[discriminator] = type;
            discovered.Add(new JsonDerivedType(type, discriminator));
        }
    }

    private static bool IsJsonConstructible(Type type)
    {
        foreach (var ctor in type.GetConstructors())
        {
            if (ctor.GetParameters().Length == 0)
            {
                return true;
            }

            if (Attribute.IsDefined(ctor, typeof(JsonConstructorAttribute)))
            {
                return true;
            }
        }

        return false;
    }

    public static JsonSerializerOptions Options =>
        _options ??= new JsonSerializerOptions(JsonConfig.GetOptions(new TextDefinitionConverterFactory()))
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    AddPolymorphism,
                    PruneToJsonProperties,
                    AddOnDeserialized
                }
            }
        };

    private static void AddPolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(BaseSpawner))
        {
            return;
        }

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions();
        for (var i = 0; i < _derivedTypes.Length; i++)
        {
            typeInfo.PolymorphismOptions.DerivedTypes.Add(_derivedTypes[i]);
        }
    }

    // Spawners are Items with many public engine properties STJ would otherwise (de)serialize.
    // Keep ONLY properties explicitly annotated with [JsonPropertyName] (our shadow properties).
    private static void PruneToJsonProperties(JsonTypeInfo typeInfo)
    {
        if (!typeInfo.Type.IsAssignableTo(typeof(BaseSpawner)))
        {
            return;
        }

        for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            var provider = typeInfo.Properties[i].AttributeProvider;
            var keep = provider?.IsDefined(typeof(JsonPropertyNameAttribute), true) ?? false;
            if (!keep)
            {
                typeInfo.Properties.RemoveAt(i);
            }
        }
    }

    private static void AddOnDeserialized(JsonTypeInfo typeInfo)
    {
        if (!typeInfo.Type.IsAssignableTo(typeof(BaseSpawner)))
        {
            return;
        }

        typeInfo.OnDeserialized = static o => ((BaseSpawner)o).OnAfterJsonDeserialize();
    }
}
```

- [ ] **Step 3b: Add the BaseSpawner JSON partial**

`Projects/UOContent/Engines/Spawners/BaseSpawner.Json.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseSpawner.Json.cs                                             *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Server.Engines.Spawners;

public abstract partial class BaseSpawner
{
    // Transient import-only state (NOT [SerializableField]; never part of the binary save).
    private Guid _jsonGuid;
    private bool _jsonHasGuid;
    private int _jsonCount;
    private TimeSpan _jsonMinDelay = DefaultMinDelay;
    private TimeSpan _jsonMaxDelay = DefaultMaxDelay;
    private int _jsonTeam;
    private int _jsonWalkingRange = -1;
    private int _jsonHomeRange = -1;
    private bool _jsonSpawnLocationIsHome;
    private SpawnPositionMode _jsonSpawnPositionMode;
    private int _jsonMaxSpawnAttempts = DefaultMaxSpawnAttempts;
    private List<SpawnerEntry> _jsonEntries;
    private protected Point3D _jsonLocation;
    private protected Map _jsonMap;

    // --- Always-written fields ---

    [JsonInclude]
    [JsonPropertyName("guid")]
    public Guid JsonGuid
    {
        get => _guid;
        set
        {
            _jsonGuid = value;
            _jsonHasGuid = true;
        }
    }

    [JsonInclude]
    [JsonPropertyName("location")]
    public Point3D JsonLocation
    {
        get => Location;
        set => _jsonLocation = value;
    }

    [JsonInclude]
    [JsonPropertyName("map")]
    public Map JsonMap
    {
        get => Map;
        set => _jsonMap = value;
    }

    [JsonInclude]
    [JsonPropertyName("count")]
    public int JsonCount
    {
        get => Count;
        set => _jsonCount = value;
    }

    [JsonInclude]
    [JsonPropertyName("entries")]
    public List<SpawnerEntry> JsonEntries
    {
        get => Entries;
        set => _jsonEntries = value;
    }

    // --- Conditionally-written fields (null getter => omitted under WhenWritingNull) ---

    [JsonInclude]
    [JsonPropertyName("name")]
    public string JsonName
    {
        get => string.IsNullOrEmpty(Name) ? null : Name;
        set => Name = value;
    }

    [JsonInclude]
    [JsonPropertyName("minDelay")]
    public TimeSpan? JsonMinDelay
    {
        get => _minDelay == DefaultMinDelay ? null : _minDelay;
        set => _jsonMinDelay = value ?? DefaultMinDelay;
    }

    [JsonInclude]
    [JsonPropertyName("maxDelay")]
    public TimeSpan? JsonMaxDelay
    {
        get => _maxDelay == DefaultMaxDelay ? null : _maxDelay;
        set => _jsonMaxDelay = value ?? DefaultMaxDelay;
    }

    [JsonInclude]
    [JsonPropertyName("team")]
    public int? JsonTeam
    {
        get => _team == 0 ? null : _team;
        set => _jsonTeam = value ?? 0;
    }

    // Mirrors today's ToJson exactly: written when _walkingRange != 0, emitting the WalkingRange property.
    [JsonInclude]
    [JsonPropertyName("walkingRange")]
    public int? JsonWalkingRange
    {
        get => _walkingRange != 0 ? WalkingRange : null;
        set => _jsonWalkingRange = value ?? -1;
    }

    [JsonInclude]
    [JsonPropertyName("spawnLocationIsHome")]
    public bool? JsonSpawnLocationIsHome
    {
        get => _spawnLocationIsHome ? true : null;
        set => _jsonSpawnLocationIsHome = value ?? false;
    }

    [JsonInclude]
    [JsonPropertyName("spawnPositionMode")]
    public SpawnPositionMode? JsonSpawnPositionMode
    {
        get => _spawnPositionMode is not SpawnPositionMode.Automatic and not SpawnPositionMode.Abandoned
            ? _spawnPositionMode
            : null;
        set => _jsonSpawnPositionMode = value ?? SpawnPositionMode.Automatic;
    }

    [JsonInclude]
    [JsonPropertyName("maxSpawnAttempts")]
    public int? JsonMaxSpawnAttempts
    {
        get => _maxSpawnAttempts != DefaultMaxSpawnAttempts ? _maxSpawnAttempts : null;
        set => _jsonMaxSpawnAttempts = value ?? DefaultMaxSpawnAttempts;
    }

    // Legacy read-only: present in old files; converted in OnAfterJsonDeserialize. Never written
    // (getter always null) — modern files carry spawnBounds instead.
    [JsonInclude]
    [JsonPropertyName("homeRange")]
    public int? JsonHomeRange
    {
        get => null;
        set => _jsonHomeRange = value ?? -1;
    }

    /// <summary>
    /// Applies the deserialized JSON state to this live spawner. Fired by the resolver's
    /// OnDeserialized after all shadow properties are set. Overrides MUST call base first.
    /// This replaces the former (DynamicJson, options) constructor body.
    /// </summary>
    protected internal virtual void OnAfterJsonDeserialize()
    {
        if (_jsonHasGuid)
        {
            _guid = _jsonGuid;
        }

        // Legacy homeRange -> spawnBounds (Map not available yet; use the deserialized location).
        if (_jsonHomeRange >= 0)
        {
            int z;
            int depth;
            if (_jsonHomeRange == 0)
            {
                z = _jsonLocation.Z;
                depth = 0;
            }
            else
            {
                z = -128;
                depth = 256;
            }

            SpawnBounds = new Rectangle3D(
                _jsonLocation.X - _jsonHomeRange,
                _jsonLocation.Y - _jsonHomeRange,
                z,
                _jsonHomeRange * 2 + 1,
                _jsonHomeRange * 2 + 1,
                depth
            );
        }

        InitSpawn(_jsonCount, _jsonMinDelay, _jsonMaxDelay, _jsonTeam, SpawnBounds);

        _walkingRange = _jsonWalkingRange;
        _spawnLocationIsHome = _jsonSpawnLocationIsHome;
        _spawnPositionMode = _jsonSpawnPositionMode;
        _maxSpawnAttempts = _jsonMaxSpawnAttempts;

        if (_jsonEntries != null)
        {
            for (var i = 0; i < _jsonEntries.Count; i++)
            {
                var entry = _jsonEntries[i];
                AddEntry(entry.SpawnedName, entry.SpawnedProbability, entry.SpawnedMaxCount, false, entry.Properties, entry.Parameters);
            }
        }
    }
}
```

> The `private` modifiers `_guid`, `_minDelay`, `_maxDelay`, `_team`, `_walkingRange`, `_spawnLocationIsHome`, `_spawnPositionMode`, `_maxSpawnAttempts`, `Entries`, `WalkingRange`, `Name`, `Count`, `Location`, `Map`, `SpawnBounds`, `InitSpawn`, `AddEntry`, `DefaultMinDelay`, `DefaultMaxDelay`, `DefaultMaxSpawnAttempts` are all defined on `BaseSpawner` in the same assembly; the partial sees them. `DefaultMaxSpawnAttempts` and `DefaultMinDelay`/`DefaultMaxDelay` are existing `private`/`private const` members — accessible to the partial.

- [ ] **Step 3c: Add the Spawner JSON partial**

`Projects/UOContent/Engines/Spawners/Spawner.Json.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Spawner.Json.cs                                                 *
 *************************************************************************/

using System.Text.Json.Serialization;

namespace Server.Engines.Spawners;

public partial class Spawner
{
    private Rectangle3D _jsonSpawnBounds;

    [JsonInclude]
    [JsonPropertyName("spawnBounds")]
    public Rectangle3D? JsonSpawnBounds
    {
        get => _spawnBounds == default ? null : _spawnBounds;
        set => _jsonSpawnBounds = value ?? default;
    }

    protected internal override void OnAfterJsonDeserialize()
    {
        base.OnAfterJsonDeserialize();

        if (_jsonSpawnBounds != default)
        {
            SpawnBounds = _jsonSpawnBounds;
        }
    }
}
```

- [ ] **Step 3d: Mark `Spawner` discoverable + constructible**

In `Projects/UOContent/Engines/Spawners/Spawner.cs`, add `using Server.Json;` and the class attribute, and `[JsonConstructor]` on the parameterless ctor:

```csharp
[SerializationGenerator(1)]
[JsonDiscoverableType]
public partial class Spawner : BaseSpawner
```

```csharp
    [Constructible(AccessLevel.Developer)]
    [System.Text.Json.Serialization.JsonConstructor]
    public Spawner()
    {
    }
```

> Leave the existing `(DynamicJson, options)` ctor and `ToJson` override in place for now (Task 9 deletes them). The new shadow properties live in the `.Json.cs` partial and do not collide.

- [ ] **Step 3e: Wire discovery into the test bootstrap**

In `Projects/UOContent.Tests/Fixtures/TestServerInitializer.cs`, add the call alongside the other curated `Configure()` calls (after `World.Load()` is fine; discovery only reads loaded assemblies):

```csharp
            DecayScheduler.Configure();
            Server.Engines.Spawners.SpawnerJsonSerializer.Configure();
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter SpawnerRoundTripTests`
Expected: PASS (2 tests). If `$type` is not emitted, confirm the list is serialized as `List<BaseSpawner>` (the static type must be the polymorphic base).

- [ ] **Step 5: Commit**

```bash
git add Projects/UOContent/Engines/Spawners/SpawnerJsonSerializer.cs \
        Projects/UOContent/Engines/Spawners/BaseSpawner.Json.cs \
        Projects/UOContent/Engines/Spawners/Spawner.Json.cs \
        Projects/UOContent/Engines/Spawners/Spawner.cs \
        Projects/UOContent.Tests/Fixtures/TestServerInitializer.cs \
        Projects/UOContent.Tests/Tests/Engines/Spawners/Json/SpawnerRoundTripTests.cs
git commit -m "feat(spawners): add SpawnerJsonSerializer + typed JSON binding for Spawner"
```

---

## Task 3: RegionSpawner JSON binding

**Files:**
- Create: `Projects/UOContent/Engines/Spawners/RegionSpawner.Json.cs`
- Modify: `Projects/UOContent/Engines/Spawners/RegionSpawner.cs` (attributes + `[JsonConstructor]`)
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/RegionSpawnerRoundTripTests.cs`

**Interfaces:**
- Consumes: `BaseSpawner.OnAfterJsonDeserialize`, `_jsonMap` (Task 2).

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Server.Regions;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class RegionSpawnerRoundTripTests
{
    [Fact]
    public void RegionSpawner_RoundTrips_RegionByName()
    {
        // Pick any registered region on Felucca for the round-trip.
        var region = Region.Find(new Point3D(1416, 1683, 0), Map.Felucca) as BaseRegion;
        Assert.NotNull(region);

        var spawner = new RegionSpawner("Fisherman") { SpawnRegion = region };
        spawner.MoveToWorld(new Point3D(1416, 1683, 0), Map.Felucca);

        var json = JsonSerializer.Serialize<List<BaseSpawner>>(
            new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);
        Assert.Contains("\"$type\": \"RegionSpawner\"", json);
        Assert.Contains(region.Name, json);

        var rt = JsonSerializer.Deserialize<List<BaseSpawner>>(json, SpawnerJsonSerializer.Options);
        var s = Assert.IsType<RegionSpawner>(Assert.Single(rt));
        Assert.Equal(region.Name, s.SpawnRegion?.Name);

        s.Delete();
        spawner.Delete();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter RegionSpawnerRoundTripTests`
Expected: FAIL — `RegionSpawner` not discoverable (no `[JsonDiscoverableType]`), `region` not bound.

- [ ] **Step 3a: Add the RegionSpawner JSON partial**

`Projects/UOContent/Engines/Spawners/RegionSpawner.Json.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * File: RegionSpawner.Json.cs                                           *
 *************************************************************************/

using System.Text.Json.Serialization;
using Server.Regions;

namespace Server.Engines.Spawners;

public partial class RegionSpawner
{
    private string _jsonRegion;

    [JsonInclude]
    [JsonPropertyName("region")]
    public string JsonRegion
    {
        get => SpawnRegion?.Name;
        set => _jsonRegion = value;
    }

    protected internal override void OnAfterJsonDeserialize()
    {
        base.OnAfterJsonDeserialize();

        _spawnRegion = Region.Find(_jsonRegion, _jsonMap) as BaseRegion;
        _spawnRegion?.InitRectangles();
        SpawnRegionName = _spawnRegion?.Name;
    }
}
```

- [ ] **Step 3b: Mark `RegionSpawner` discoverable + constructible**

In `Projects/UOContent/Engines/Spawners/RegionSpawner.cs`, add `using Server.Json;`, the class attribute, and `[JsonConstructor]` on the parameterless ctor:

```csharp
[SerializationGenerator(0)]
[JsonDiscoverableType]
public partial class RegionSpawner : Spawner
```

```csharp
    [Constructible(AccessLevel.Developer)]
    [System.Text.Json.Serialization.JsonConstructor]
    public RegionSpawner()
    {
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter RegionSpawnerRoundTripTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Projects/UOContent/Engines/Spawners/RegionSpawner.Json.cs \
        Projects/UOContent/Engines/Spawners/RegionSpawner.cs \
        Projects/UOContent.Tests/Tests/Engines/Spawners/Json/RegionSpawnerRoundTripTests.cs
git commit -m "feat(spawners): add typed JSON binding for RegionSpawner"
```

---

## Task 4: ProximitySpawner JSON binding

**Files:**
- Create: `Projects/UOContent/Engines/Spawners/ProximitySpawner.Json.cs`
- Modify: `Projects/UOContent/Engines/Spawners/ProximitySpawner.cs`
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ProximitySpawnerRoundTripTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class ProximitySpawnerRoundTripTests
{
    [Fact]
    public void ProximitySpawner_RoundTrips_ProximityFields()
    {
        var spawner = new ProximitySpawner("Fisherman") { TriggerRange = 4, InstantFlag = true };
        spawner.SpawnMessage = 500000;
        spawner.MoveToWorld(new Point3D(120, 120, 0), Map.Felucca);

        var json = JsonSerializer.Serialize<List<BaseSpawner>>(
            new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);
        Assert.Contains("\"$type\": \"ProximitySpawner\"", json);
        Assert.Contains("\"triggerRange\": 4", json);
        Assert.Contains("\"instant\": true", json);

        var rt = JsonSerializer.Deserialize<List<BaseSpawner>>(json, SpawnerJsonSerializer.Options);
        var s = Assert.IsType<ProximitySpawner>(Assert.Single(rt));
        Assert.Equal(4, s.TriggerRange);
        Assert.True(s.InstantFlag);
        Assert.Equal(500000, s.SpawnMessage.Number);

        s.Delete();
        spawner.Delete();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter ProximitySpawnerRoundTripTests`
Expected: FAIL — `ProximitySpawner` not discoverable.

- [ ] **Step 3a: Add the ProximitySpawner JSON partial**

`Projects/UOContent/Engines/Spawners/ProximitySpawner.Json.cs`:

```csharp
/*************************************************************************
 * ModernUO                                                              *
 * File: ProximitySpawner.Json.cs                                        *
 *************************************************************************/

using System.Text.Json.Serialization;

namespace Server.Engines.Spawners;

public partial class ProximitySpawner
{
    private int _jsonTriggerRange;
    private TextDefinition _jsonSpawnMessage;
    private bool _jsonInstant;

    [JsonInclude]
    [JsonPropertyName("triggerRange")]
    public int JsonTriggerRange
    {
        get => TriggerRange;
        set => _jsonTriggerRange = value;
    }

    [JsonInclude]
    [JsonPropertyName("spawnMessage")]
    public TextDefinition JsonSpawnMessage
    {
        get => SpawnMessage;
        set => _jsonSpawnMessage = value;
    }

    [JsonInclude]
    [JsonPropertyName("instant")]
    public bool JsonInstant
    {
        get => InstantFlag;
        set => _jsonInstant = value;
    }

    protected internal override void OnAfterJsonDeserialize()
    {
        base.OnAfterJsonDeserialize();

        TriggerRange = _jsonTriggerRange;
        SpawnMessage = _jsonSpawnMessage;
        InstantFlag = _jsonInstant;
    }
}
```

- [ ] **Step 3b: Mark `ProximitySpawner` discoverable + constructible**

In `Projects/UOContent/Engines/Spawners/ProximitySpawner.cs`, add `using Server.Json;`, the class attribute, and `[JsonConstructor]` on the parameterless ctor:

```csharp
[SerializationGenerator(0)]
[JsonDiscoverableType]
public partial class ProximitySpawner : Spawner
```

```csharp
    [Constructible(AccessLevel.Developer)]
    [System.Text.Json.Serialization.JsonConstructor]
    public ProximitySpawner()
    {
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter ProximitySpawnerRoundTripTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Projects/UOContent/Engines/Spawners/ProximitySpawner.Json.cs \
        Projects/UOContent/Engines/Spawners/ProximitySpawner.cs \
        Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ProximitySpawnerRoundTripTests.cs
git commit -m "feat(spawners): add typed JSON binding for ProximitySpawner"
```

---

## Task 5: Legacy `homeRange` read path

**Files:**
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/LegacyHomeRangeTests.cs`

The implementation already exists in `BaseSpawner.OnAfterJsonDeserialize` (Task 2). This task adds the regression test that locks the legacy contract (D3).

- [ ] **Step 1: Write the failing test**

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
    [Fact]
    public void HomeRange_NoSpawnBounds_ProducesCenteredBounds()
    {
        const string legacy = """
        [
          {
            "$type": "Spawner",
            "guid": "3df0543a-373c-4673-a98b-8191686f4ab3",
            "location": [100, 200, 5],
            "map": "Felucca",
            "count": 1,
            "homeRange": 3,
            "entries": [ { "name": "Fisherman", "maxCount": 1, "probability": 100 } ]
          }
        ]
        """;

        var rt = JsonSerializer.Deserialize<List<BaseSpawner>>(legacy, SpawnerJsonSerializer.Options);
        var s = Assert.IsType<Spawner>(Assert.Single(rt));

        // homeRange 3 -> Rectangle3D(100-3, 200-3, -128, 7, 7, 256)
        Assert.Equal(new Rectangle3D(97, 197, -128, 7, 7, 256), s.SpawnBounds);

        s.Delete();
    }

    [Fact]
    public void HomeRangeZero_ProducesSingleTileBounds()
    {
        const string legacy = """
        [
          {
            "$type": "Spawner",
            "location": [100, 200, 5],
            "map": "Felucca",
            "count": 1,
            "homeRange": 0,
            "entries": [ { "name": "Fisherman", "maxCount": 1, "probability": 100 } ]
          }
        ]
        """;

        var rt = JsonSerializer.Deserialize<List<BaseSpawner>>(legacy, SpawnerJsonSerializer.Options);
        var s = Assert.IsType<Spawner>(Assert.Single(rt));

        // homeRange 0 -> Rectangle3D(100, 200, 5, 1, 1, 0)
        Assert.Equal(new Rectangle3D(100, 200, 5, 1, 1, 0), s.SpawnBounds);

        s.Delete();
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (implementation already present)**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter LegacyHomeRangeTests`
Expected: PASS. If FAIL, the bug is in `BaseSpawner.OnAfterJsonDeserialize`'s homeRange block — fix there, not in the test.

- [ ] **Step 3: Commit**

```bash
git add Projects/UOContent.Tests/Tests/Engines/Spawners/Json/LegacyHomeRangeTests.cs
git commit -m "test(spawners): lock legacy homeRange->spawnBounds JSON read"
```

---

## Task 6: Rewire `ExportSpawnersCommand` to typed serialization

**Files:**
- Modify: `Projects/UOContent/Engines/Spawners/Commands/ExportSpawnersCommand.cs:71-101`

**Interfaces:**
- Consumes: `SpawnerJsonSerializer.Options`.

- [ ] **Step 1: Replace the DynamicJson export loop**

In `ExportSpawnersCommand.ExecuteList`, replace the block that builds `List<DynamicJson>` (lines ~71-101) with a direct typed list. The current code:

```csharp
        var options = JsonConfig.GetOptions(new TextDefinitionConverterFactory());

        var spawnRecords = new List<DynamicJson>(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is not BaseSpawner spawner || spawner.Map == Map.Internal || spawner.Parent != null)
            {
                continue;
            }

            var dynamicJson = DynamicJson.Create(spawner.GetType());
            spawner.ToJson(dynamicJson, options);
            spawnRecords.Add(dynamicJson);
        }

        if (spawnRecords.Count == 0)
        {
            LogFailure("No matching spawners found.");
            return;
        }

        e.Mobile.SendMessage("Exporting spawners...");

        JsonConfig.Serialize(path, spawnRecords, options);
```

becomes:

```csharp
        var spawnRecords = new List<BaseSpawner>(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is not BaseSpawner spawner || spawner.Map == Map.Internal || spawner.Parent != null)
            {
                continue;
            }

            spawnRecords.Add(spawner);
        }

        if (spawnRecords.Count == 0)
        {
            LogFailure("No matching spawners found.");
            return;
        }

        e.Mobile.SendMessage("Exporting spawners...");

        JsonConfig.Serialize(path, spawnRecords, SpawnerJsonSerializer.Options);
```

Remove now-unused `using` of `DynamicJson` if present and any unused `options` local. Keep `using Server.Json;` (for `JsonConfig`).

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build Projects/UOContent/UOContent.csproj`
Expected: SUCCESS (the `Spawner.ToJson` overrides still exist; they are simply no longer called here).

- [ ] **Step 3: Add an export round-trip test**

`Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ExportImportFileTests.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        var spawner = new Spawner(3, System.TimeSpan.FromMinutes(4), System.TimeSpan.FromMinutes(8), 1,
            new Rectangle3D(200, 200, 0, 9, 9, 0), "Tanner");
        spawner.MoveToWorld(new Point3D(204, 204, 0), Map.Felucca);

        var path = Path.GetTempFileName();
        try
        {
            JsonConfig.Serialize(path, new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);

            var loaded = JsonConfig.Deserialize<List<BaseSpawner>>(path, SpawnerJsonSerializer.Options);
            var s = Assert.IsType<Spawner>(Assert.Single(loaded));
            Assert.Equal(3, s.Count);
            Assert.Equal(1, s.Team);
            Assert.Equal(new Rectangle3D(200, 200, 0, 9, 9, 0), s.SpawnBounds);

            s.Delete();
        }
        finally
        {
            File.Delete(path);
        }

        spawner.Delete();
    }
}
```

- [ ] **Step 4: Run the test**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter ExportImportFileTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Projects/UOContent/Engines/Spawners/Commands/ExportSpawnersCommand.cs \
        Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ExportImportFileTests.cs
git commit -m "refactor(spawners): export via typed SpawnerJsonSerializer"
```

---

## Task 7: Rewire `ImportSpawnersCommand` to typed deserialization

**Files:**
- Modify: `Projects/UOContent/Engines/Spawners/Commands/ImportSpawnersCommand.cs:193-274`
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ImportCleanupTests.cs`

**Interfaces:**
- Consumes: `SpawnerJsonSerializer.Options`; `BaseSpawner.JsonLocation`/`JsonMap` are read back via public `Location`/`Map`? No — the deserialized spawner is not yet placed; read placement from the public `Location`/`Map` is invalid. Use the transient values exposed below.
- Produces: `BaseSpawner.ImportLocation` (`Point3D`) and `BaseSpawner.ImportMap` (`Map`) read-only accessors so the importer can place the deserialized spawner.

- [ ] **Step 1: Expose import placement accessors on BaseSpawner**

Add to `Projects/UOContent/Engines/Spawners/BaseSpawner.Json.cs` (these surface the captured transient values for the importer; not serialized):

```csharp
    [JsonIgnore]
    public Point3D ImportLocation => _jsonLocation;

    [JsonIgnore]
    public Map ImportMap => _jsonMap;
```

- [ ] **Step 2: Replace the import body**

Replace `ImportJsonSpawners` (lines 193-274) with the typed flow. New body:

```csharp
    private static void ImportJsonSpawners(
        Mobile from,
        FileInfo file,
        Dictionary<Guid, ISpawner> allSpawners,
        ref int totalGenerated,
        ref int totalFailures
    )
    {
        List<BaseSpawner> spawners;
        try
        {
            spawners = JsonConfig.Deserialize<List<BaseSpawner>>(file.FullName, SpawnerJsonSerializer.Options);
        }
        catch (JsonException)
        {
            from.SendMessage(
                $"GenerateSpawners: Exception parsing {file.FullName}, file may not be in the correct format."
            );
            return;
        }

        if (spawners == null || spawners.Count == 0)
        {
            from.SendMessage($"GenerateSpawners: Skipping empty spawner file {file.Name}");
            logger.Information("{User} is skipping empty spawner file {File}", from, file.FullName);
            return;
        }

        using var queue = PooledRefQueue<Item>.Create();
        for (var i = 0; i < spawners.Count; i++)
        {
            var spawner = spawners[i];
            var location = spawner.ImportLocation;
            var map = spawner.ImportMap;

            if (map == null || map == Map.Internal)
            {
                logger.Error($"Spawner {spawner.Guid} ({i}) has no valid map; skipping.");
                spawner.Delete();
                totalFailures++;
                continue;
            }

            var type = spawner.GetType();

            // Delete all spawners of the same concrete type already at this location.
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

Remove the now-unused `var options = JsonConfig.GetOptions();` and the `AssemblyHandler`/`CreateInstance` usings if they become unused. Keep `using Server.Json;`.

> **Cleanup contract:** every deserialized spawner is either placed (`MoveToWorld`) or `Delete()`d — no orphaned Items. STJ constructs Items during deserialize (registered in World at `Map.Internal`); the `map == Map.Internal` filter and the `catch` both `Delete()`.

- [ ] **Step 3: Write the cleanup test**

```csharp
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
        var dir = Path.Combine(Path.GetTempPath(), "muo-spawner-import-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test.json");
        File.WriteAllText(path, """
        [
          {
            "$type": "Spawner",
            "guid": "11111111-1111-1111-1111-111111111111",
            "location": [305, 305, 0],
            "map": "Felucca",
            "count": 1,
            "spawnBounds": { "x1": 300, "y1": 300, "x2": 310, "y2": 310 },
            "entries": [ { "name": "Fisherman", "maxCount": 1, "probability": 100 } ]
          }
        ]
        """);

        try
        {
            ImportSpawnersCommand.GenerateFromFolder(dir); // see Step 4 note
            var placed = Map.Felucca.GetItemsAt<BaseSpawner>(new Point3D(305, 305, 0));
            var found = false;
            foreach (var s in placed)
            {
                if (s.Guid == new System.Guid("11111111-1111-1111-1111-111111111111"))
                {
                    found = true;
                    s.Delete();
                }
            }

            Assert.True(found);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
```

> **Note:** if `ImportSpawnersCommand` has no folder-level public entry usable from a test, add an `internal static` test seam (e.g. `internal static void ImportFile(FileInfo file, Dictionary<Guid, ISpawner> all)`) that wraps `ImportJsonSpawners`, and call that instead. Pick whichever already-exposed method maps cleanly; do not broaden visibility more than needed. Update the test call accordingly.

- [ ] **Step 4: Run the test**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter ImportCleanupTests`
Expected: PASS. Adjust the test seam per the note if the build flags an inaccessible method.

- [ ] **Step 5: Commit**

```bash
git add Projects/UOContent/Engines/Spawners/Commands/ImportSpawnersCommand.cs \
        Projects/UOContent/Engines/Spawners/BaseSpawner.Json.cs \
        Projects/UOContent.Tests/Tests/Engines/Spawners/Json/ImportCleanupTests.cs
git commit -m "refactor(spawners): import via typed polymorphic deserialization with cleanup"
```

---

## Task 8: One-time data migration to `$type`

**Files:**
- Create: `tools/spawner-json-migrate/migrate.mjs` (Node, no server dependency) OR a `dotnet-script`/console one-off — use Node for zero build friction.
- Modify (output): `Distribution/Data/Spawns/**/*.json`
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/MigratedDataLoadTests.cs`

> **Rationale (spec §5.5):** a pure JSON transform preserves the per-file directory layout, which round-tripping through the in-game commands would not. The runtime keeps the `homeRange` read path (Task 5), so this transform cannot silently break loading.

- [ ] **Step 1: Write the migration script**

`tools/spawner-json-migrate/migrate.mjs`:

```javascript
// One-time migration: rename "type" -> "$type" (kept first), and convert legacy
// homeRange (+location) -> spawnBounds, dropping homeRange. Run once, then delete this tool.
//
// Usage: node tools/spawner-json-migrate/migrate.mjs Distribution/Data/Spawns
import { readdirSync, readFileSync, writeFileSync, statSync } from "node:fs";
import { join } from "node:path";

function* walk(dir) {
  for (const name of readdirSync(dir)) {
    const p = join(dir, name);
    if (statSync(p).isDirectory()) {
      yield* walk(p);
    } else if (name.endsWith(".json")) {
      yield p;
    }
  }
}

function toBounds(loc, hr) {
  const [x, y, z] = loc;
  if (hr === 0) {
    return { x1: x, y1: y, z1: z, x2: x, y2: y, z2: z };
  }
  // z = -128, depth 256 -> z2 = 127. Rectangle3D start..end inclusive of width/height.
  return { x1: x - hr, y1: y - hr, z1: -128, x2: x + hr, y2: y + hr, z2: 127 };
}

function migrateOne(obj) {
  const out = {};
  // $type first.
  if ("type" in obj) {
    out["$type"] = obj["type"];
  } else if ("$type" in obj) {
    out["$type"] = obj["$type"];
  }
  for (const [k, v] of Object.entries(obj)) {
    if (k === "type" || k === "$type") {
      continue;
    }
    if (k === "homeRange") {
      if (obj.spawnBounds === undefined && Array.isArray(obj.location)) {
        out.spawnBounds = toBounds(obj.location, v);
      }
      continue; // drop homeRange
    }
    out[k] = v;
  }
  return out;
}

let files = 0;
let records = 0;
for (const file of walk(process.argv[2])) {
  const data = JSON.parse(readFileSync(file, "utf8"));
  const arr = Array.isArray(data) ? data : [data];
  const migrated = arr.map((o) => {
    records++;
    return migrateOne(o);
  });
  const result = Array.isArray(data) ? migrated : migrated[0];
  writeFileSync(file, JSON.stringify(result, null, 2) + "\n", "utf8");
  files++;
}
console.log(`Migrated ${records} record(s) across ${files} file(s).`);
```

> **Bounds formula note:** the runtime homeRange→bounds uses `Rectangle3D(x-hr, y-hr, -128, hr*2+1, hr*2+1, 256)`. The `Rectangle3DConverter` object form is `{x1,y1,z1,x2,y2,z2}` where the rectangle spans start..end. `x2 = x+hr` gives width `hr*2+1` (inclusive), `z1=-128,z2=127` gives depth 256. Task 5's equivalence test (Step 4) validates this against the runtime read; if it disagrees, fix `toBounds` to match `BaseSpawner.OnAfterJsonDeserialize`.

- [ ] **Step 2: Run the migration**

Run: `node tools/spawner-json-migrate/migrate.mjs Distribution/Data/Spawns`
Expected: prints a non-zero migrated count. `git status` shows many modified `Distribution/Data/Spawns/**/*.json`.

- [ ] **Step 3: Spot-check a migrated file**

Run: `git diff -- Distribution/Data/Spawns/post-uoml/felucca/Vendors.json | head -40`
Expected: `"type"` → `"$type"` (first key); any `homeRange` replaced by a `spawnBounds` object; no other semantic changes.

- [ ] **Step 4: Write the migrated-data load + equivalence test**

```csharp
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class MigratedDataLoadTests
{
    [Fact]
    public void MigratedFile_LoadsWithDollarType()
    {
        var path = Path.Combine(Core.BaseDirectory, "Data", "Spawns", "post-uoml", "felucca", "Vendors.json");
        if (!File.Exists(path))
        {
            return; // distribution data not present in this checkout
        }

        var loaded = JsonConfig_DeserializeList(path);
        Assert.NotEmpty(loaded);
        foreach (var s in loaded)
        {
            Assert.IsAssignableFrom<BaseSpawner>(s);
            s.Delete();
        }
    }

    private static List<BaseSpawner> JsonConfig_DeserializeList(string path) =>
        JsonSerializer.Deserialize<List<BaseSpawner>>(File.ReadAllText(path), SpawnerJsonSerializer.Options);
}
```

- [ ] **Step 5: Run the test**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter MigratedDataLoadTests`
Expected: PASS.

- [ ] **Step 6: Commit (data + tool together, then remove the tool)**

```bash
git add Distribution/Data/Spawns tools/spawner-json-migrate \
        Projects/UOContent.Tests/Tests/Engines/Spawners/Json/MigratedDataLoadTests.cs
git commit -m "chore(spawners): migrate spawn data to \$type discriminator"
git rm -r tools/spawner-json-migrate
git commit -m "chore(spawners): remove one-time spawn migration tool"
```

---

## Task 9: Delete `DynamicJson` and the old per-class JSON members

**Files:**
- Delete: `Projects/Server/Json/DynamicJson.cs`
- Modify: `Projects/UOContent/Engines/Spawners/BaseSpawner.cs` (remove `(DynamicJson, options)` ctor + `ToJson`)
- Modify: `Projects/UOContent/Engines/Spawners/Spawner.cs`, `RegionSpawner.cs`, `ProximitySpawner.cs` (remove `(DynamicJson,…)` ctor + `ToJson` override)

- [ ] **Step 1: Remove the old members**

In `BaseSpawner.cs`, delete the entire `public BaseSpawner(DynamicJson json, JsonSerializerOptions options)` constructor (lines ~297-356) and the `public virtual void ToJson(DynamicJson json, JsonSerializerOptions options)` method (lines ~496-547). In `Spawner.cs`, `RegionSpawner.cs`, and `ProximitySpawner.cs`, delete each `(DynamicJson, options)` constructor and each `ToJson` override. Remove now-unused `using Server.Json;` / `using System.Text.Json;` lines flagged by the compiler.

- [ ] **Step 2: Delete the file**

Run: `git rm Projects/Server/Json/DynamicJson.cs`

- [ ] **Step 3: Verify no remaining references**

Run: `git grep -n "DynamicJson"`
Expected: no matches (empty output).

- [ ] **Step 4: Build the solution**

Run: `dotnet build ModernUO.sln`
Expected: SUCCESS, no errors.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(json): delete DynamicJson; spawners use typed STJ exclusively"
```

---

## Task 10: Load-all regression over `Spawns/`

**Files:**
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/AllSpawnFilesLoadTests.cs`

- [ ] **Step 1: Write the test**

```csharp
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
    public void EverySpawnFile_DeserializesWithoutThrowing()
    {
        var root = Path.Combine(Core.BaseDirectory, "Data", "Spawns");
        if (!Directory.Exists(root))
        {
            return; // distribution data not present
        }

        var failures = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<BaseSpawner>>(
                    File.ReadAllText(file), SpawnerJsonSerializer.Options);
                if (list != null)
                {
                    foreach (var s in list)
                    {
                        s?.Delete();
                    }
                }
            }
            catch (JsonException ex)
            {
                failures.Add($"{file}: {ex.Message}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
```

- [ ] **Step 2: Run the test**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter AllSpawnFilesLoadTests`
Expected: PASS. Any failure names the offending file — fix the migration (Task 8) or converter, not the test.

- [ ] **Step 3: Commit**

```bash
git add Projects/UOContent.Tests/Tests/Engines/Spawners/Json/AllSpawnFilesLoadTests.cs
git commit -m "test(spawners): assert all migrated spawn files deserialize"
```

---

## Task 11: Startup validation tests (collision + constructibility)

**Files:**
- Test: `Projects/UOContent.Tests/Tests/Engines/Spawners/Json/SpawnerDiscoveryValidationTests.cs`

These exercise the `Collect` validation in isolation via a small reflection-free helper. Because `Configure()` scans real assemblies, test the validation logic by calling a refactored internal helper.

- [ ] **Step 1: Expose an internal validation helper**

Refactor `SpawnerJsonSerializer.Collect` to delegate discriminator/constructibility validation to an `internal static` method so tests can call it without a full assembly scan:

```csharp
    internal static (string discriminator, JsonDerivedType derived) Validate(
        Type type, Dictionary<string, Type> byDiscriminator)
    {
        if (!IsJsonConstructible(type))
        {
            throw new Exception(
                $"Spawner type '{type.FullName}' is marked [JsonDiscoverableType] but System.Text.Json cannot construct it. " +
                "Add a public parameterless constructor marked [JsonConstructor].");
        }

        var attr = (JsonDiscoverableTypeAttribute)Attribute.GetCustomAttribute(
            type, typeof(JsonDiscoverableTypeAttribute), false);
        var discriminator = attr?.Discriminator ?? type.Name;
        if (byDiscriminator.TryGetValue(discriminator, out var existing))
        {
            throw new Exception(
                $"Spawner JSON discriminator '{discriminator}' is claimed by both '{existing.FullName}' and " +
                $"'{type.FullName}'. Set an explicit discriminator via [JsonDiscoverableType(\"...\")] on one.");
        }

        return (discriminator, new JsonDerivedType(type, discriminator));
    }
```

Have `Collect` call `Validate` and add to its maps/list. Add `[assembly: InternalsVisibleTo("UOContent.Tests")]` to UOContent if not already present (check `Projects/UOContent/Properties/` or an existing `AssemblyInfo`; if `SectorSpawnCacheTests` already touches internals, it is present).

- [ ] **Step 2: Write the tests**

```csharp
using System;
using System.Collections.Generic;
using Server.Engines.Spawners;
using Server.Json;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

public class SpawnerDiscoveryValidationTests
{
    [JsonDiscoverableType("dup")]
    private sealed class DupA : Spawner { [System.Text.Json.Serialization.JsonConstructor] public DupA() { } }

    [JsonDiscoverableType("dup")]
    private sealed class DupB : Spawner { [System.Text.Json.Serialization.JsonConstructor] public DupB() { } }

    [Fact]
    public void DuplicateDiscriminator_Throws()
    {
        var map = new Dictionary<string, Type>();
        var (disc, _) = SpawnerJsonSerializer.Validate(typeof(DupA), map);
        map[disc] = typeof(DupA);

        var ex = Assert.Throws<Exception>(() => SpawnerJsonSerializer.Validate(typeof(DupB), map));
        Assert.Contains("discriminator 'dup'", ex.Message);
    }
}
```

> A non-constructible negative test is hard to express cleanly (any nested class can declare a ctor). The constructibility branch is covered by the production `Configure()` path; the duplicate-discriminator test is the high-value case. Do not add a contrived non-constructible type just to hit the branch.

- [ ] **Step 3: Run the tests**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj --filter SpawnerDiscoveryValidationTests`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add Projects/UOContent/Engines/Spawners/SpawnerJsonSerializer.cs \
        Projects/UOContent.Tests/Tests/Engines/Spawners/Json/SpawnerDiscoveryValidationTests.cs
git commit -m "test(spawners): validate duplicate JSON discriminator detection"
```

---

## Task 12: Full suite + spec sync

**Files:**
- Modify: `docs/superpowers/specs/2026-06-25-spawner-stj-migration-design.md` (note the D1 refinement)

- [ ] **Step 1: Run the full UOContent + Server suites**

Run: `dotnet test Projects/UOContent.Tests/UOContent.Tests.csproj` and `dotnet test Projects/Server.Tests/Server.Tests.csproj`
Expected: PASS (no regressions). Pay attention to any spawner-related world-save tests.

- [ ] **Step 2: Update the spec's D1 note**

In the design doc, append to the D1 row / §5.2 that sparse output is realized via nullable shadow getters + `WhenWritingNull` (equivalent output, simpler than `ShouldSerialize`).

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/specs/2026-06-25-spawner-stj-migration-design.md
git commit -m "docs(spawners): note D1 sparse-output mechanism refinement"
```

---

## Self-Review

**Spec coverage:**
- Delete DynamicJson → Task 9. ✓
- Typed STJ spawners (no DTOs) → Tasks 2–4. ✓
- Exact sparse output (D1) → nullable shadow getters (Tasks 2–4) + omission tests (Task 2 Step 1). ✓
- Auto-discovery, no registration (D4) → `SpawnerJsonSerializer.Configure` (Task 2). ✓
- Reusable opt-in marker (D5) → Task 1; wired for spawners only. ✓
- `$type` migration (D2) → Task 8. ✓
- Legacy `homeRange` read-legacy/write-modern (D3) → Tasks 2 (write-modern: getter always null) + 5 (read-legacy test). ✓
- Loud collision/constructibility validation → Tasks 2 (`Collect`) + 11. ✓
- Import cleanup contract → Task 7. ✓
- Testing (round-trip, load-all, legacy, sparse, polymorphism, validation) → Tasks 2–7, 10, 11. ✓

**Placeholder scan:** No "TBD"/"handle edge cases"/"similar to Task N". The one conditional (Task 7 test seam) gives an explicit decision rule, not a vague instruction.

**Type consistency:** `OnAfterJsonDeserialize` (`protected internal virtual`/`override`) consistent across Tasks 2–4. `SpawnerJsonSerializer.Options`/`Configure`/`Validate` signatures consistent across Tasks 2, 6, 7, 11. Shadow property names (`Json*` + `[JsonPropertyName]` lowercase keys) consistent. `ImportLocation`/`ImportMap` defined in Task 7 Step 1, used Task 7 Step 2.

**Open risk to watch during execution (flag, do not pre-solve):** STJ constructor selection — `[JsonConstructor]` on a parameterless ctor that is *also* `[Constructible]`. If STJ rejects it or picks the generated `(Serial)` ctor, the Task 2 round-trip test fails at deserialize; the fix is to ensure exactly one parameterless `[JsonConstructor]` per concrete type (already specified). The `[Collection("Sequential UOContent Tests")]` fixture must expose `Map.Felucca` with tile data — `TestServerInitializer` loads maps, so this holds.
