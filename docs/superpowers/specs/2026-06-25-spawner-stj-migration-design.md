# Remove `DynamicJson`, migrate spawners to typed System.Text.Json

- **Date:** 2026-06-25
- **Status:** Approved (design); **revised mid-implementation — see §0 Revision**
- **Author:** Kamron Batman (with Claude)
- **Scope:** `Projects/Server/Json/`, `Projects/UOContent/Engines/Spawners/`, `Distribution/Data/Spawns/`

## 0. Revision (2026-06-25): Approach A → B (DTO records)

The original design (§5 below) made each live spawner `Item` the STJ deserialization
target via inert shadow properties + an `OnAfterJsonDeserialize` hook ("Approach A").
During implementation (after Tasks 1–7 of the v1 plan) we hit a structural flaw:

> **STJ constructs each spawner `Item` — registered in `World` at `Map.Internal` — as it
> streams the JSON, *before* the data is validated.** A `JsonException` mid-array (or any
> converter / post-construct failure) leaves already-constructed spawner Items orphaned in
> the world with no reference to delete them. On a malformed/hand-edited spawn file this is
> a **world-save leak**, and it cannot be fully closed while the `Item` is the deserialization
> target (even per-element deserialization constructs the Item before a property failure).

**Resolution — Approach B:** deserialize to a **plain `record` DTO** (no `Item`, no world
registration), validate by virtue of a successful parse, then construct the real spawner via
a factory. A parse failure is now pure GC — zero world state touched. This supersedes §5's
"shadow property" mechanism. Decisions D1–D5 stand; the `$type` discriminator, auto-discovery,
the `[JsonDiscoverableType]` marker, exact sparse output (now via nullable DTO properties +
`WhenWritingNull`), legacy `homeRange` read, and the data migration are all unchanged. Only
the *binding mechanism* changes. Note: the regions #1400 "DTOs are pure overhead" lesson does
**not** transfer — a `Region` is itself a POCO, whereas a spawner is an `Item` with a world
lifecycle, so the DTO is what keeps STJ away from world state rather than redundant ceremony.

### 0.1 Approach B architecture (authoritative; supersedes §5.1–5.4)

**Polymorphic DTO hierarchy** (`Projects/UOContent/Engines/Spawners/Json/`), one DTO per
spawner type, each carrying the `[JsonDiscoverableType]` marker:

- `abstract record SpawnerDto` — the common `BaseSpawner` JSON fields (guid, location, map,
  count, name, minDelay, maxDelay, team, walkingRange, **homeRange** [legacy read],
  spawnLocationIsHome, spawnPositionMode, maxSpawnAttempts, entries). Sparse fields are
  nullable (`int?`, `TimeSpan?`, …) so `WhenWritingNull` omits domain-defaults, matching
  today's `ToJson` exactly. Declares `abstract BaseSpawner CreateEmpty()` (constructs the
  right empty `Item`) and a concrete `BaseSpawner ToSpawner()` that calls `CreateEmpty()`,
  applies the common fields (the former `OnAfterJsonDeserialize` body: `InitSpawn`, entries,
  `homeRange`→`spawnBounds`), and returns the live spawner.
- `record SpawnerDataDto : SpawnerDto` (`$type` = `"Spawner"`) — adds `spawnBounds`.
- `record RegionSpawnerDto : SpawnerDto` (`$type` = `"RegionSpawner"`) — adds `region`
  (resolved against the DTO's `map` in `ToSpawner`).
- `record ProximitySpawnerDto : SpawnerDto` (`$type` = `"ProximitySpawner"`) — adds
  `spawnBounds`, `triggerRange`, `spawnMessage`, `instant`.

Each concrete DTO overrides `CreateEmpty()` and extends `ToSpawner()` to apply its extra
fields (calling base first).

**Symmetric export mapping** lives on the spawner as `virtual SpawnerDto BaseSpawner.ToDto()`
(each concrete spawner overrides it). This keeps extensibility open and symmetric: a custom
spawner type ships a paired DTO (`[JsonDiscoverableType]` + `ToSpawner`) and overrides
`ToDto()` — no central registry, no closed switch. Export builds a `List<SpawnerDto>` by
calling `ToDto()` on each spawner; STJ writes `$type` via polymorphism. Import deserializes
`List<SpawnerDto>` then calls `ToSpawner()` per element.

**`SpawnerJsonSerializer` simplifies.** It now discovers `[JsonDiscoverableType]` types
assignable to **`SpawnerDto`** (not `BaseSpawner`) and wires STJ polymorphism on the
`SpawnerDto` root. The **property-pruning resolver modifier and the `OnDeserialized` hook
are removed** — a DTO record exposes only its declared JSON properties, and `ToSpawner()`
is invoked explicitly by the importer rather than by STJ. Discovery, collision/constructibility
validation, and the discriminator rules are unchanged.

**Import cleanup contract becomes trivial and complete.** DTO deserialization never
constructs an `Item`; a malformed file fails as GC-only. `ToSpawner()` constructs exactly
one `Item` per validated DTO, under our control: the importer places it (`MoveToWorld` +
`Respawn`) or, on a `ToSpawner`/placement failure, `Delete()`s the single referenced spawner.
No mid-array orphan is possible.

The remainder of §5 (data migration §5.5, deletion §5.6) is unchanged. §6–§11 stand, with
"shadow property" read as "DTO property" and `OnAfterJsonDeserialize` read as `ToSpawner()`.

## 1. Problem

`Projects/Server/Json/DynamicJson.cs` is a dated, hand-rolled JSON envelope —
a `string Type` discriminator plus a `[JsonExtensionData] Dictionary<string, JsonElement>`
bag with manual `GetProperty<T>` / `SetProperty<T>` helpers. `SetProperty` round-trips
every value through `JsonSerializer` → `JsonDocument.Parse` → `.Clone()`, and the file
itself carries a `// TODO: Use JSON Node in .NET 6` note. It is the **only** remaining
consumer of this pattern; regions were migrated off it in #1400.

`DynamicJson` is used exclusively by spawners:

- `BaseSpawner`/`Spawner`/`RegionSpawner`/`ProximitySpawner` each hand-write a
  `(DynamicJson, options)` constructor **and** a `ToJson(...)` method, reading/writing
  every field by string key.
- `ImportSpawnersCommand` deserializes `List<DynamicJson>`, resolves the concrete type
  via `AssemblyHandler.FindTypeByName(json.Type)`, and reflection-instantiates via
  `type.CreateInstance<ISpawner>(json, options)`.
- `ExportSpawnersCommand` builds `DynamicJson.Create(...)` + `spawner.ToJson(...)`.

The codebase already has the better pattern in two places: **regions**
(`RegionJsonSerializer` — typed polymorphism + a `TypeInfoResolver`) and, more directly,
**`SpawnerEntry`**, which is already a typed POCO deserialized by STJ via `[JsonConstructor]`
and `[SerializedJsonPropertyName]` (a source-generator hook that emits `[JsonPropertyName]`).
Spawners are the last holdout.

### Why now / why not "because .NET 10"

The enabling STJ features — polymorphism, `[JsonConstructor]`, resolver `ShouldSerialize`
modifiers — landed in .NET 7–8 and regions already use them. **This work is justified by
consistency, compile-time type safety, and deleting dated code, not by a new .NET 10
feature.** The PR should be framed that way.

## 2. Goals

- Delete `DynamicJson` entirely.
- Make spawners directly STJ-(de)serializable, mirroring the regions pattern and the
  existing `SpawnerEntry` precedent.
- Preserve **exact** sparse export output (no field churn vs. today).
- Preserve **zero-config extensibility** — operators must not be forced to maintain a
  manual registration list (unlike regions' `RegionJsonRegistration`).
- Migrate the checked-in spawn data to the `$type` discriminator, aligning with regions.

## 3. Non-goals

- Retrofitting **regions** with the new discovery/opt-in attribute (possible follow-up;
  the attribute is designed to allow it, but it is not wired up here).
- Changing spawner **binary world-save** serialization (the `[SerializationGenerator]`
  layer) — untouched.
- Changing spawner runtime behavior, spawn logic, or the import/export command UX.

## 4. Decisions (locked)

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | **Match current sparse output exactly** via `JsonPropertyInfo.ShouldSerialize` modifiers in a `TypeInfoResolver`. | `WhenWritingDefault` only omits CLR defaults; spawners omit *domain* defaults (5-min delay, `maxSpawnAttempts == 10`, `spawnPositionMode` Automatic/Abandoned, `walkingRange == -1`, …). The same predicates already exist for the binary layer (`ShouldSerializeMinDelay()` etc.) and are reused. |
| D2 | **Migrate data files to `$type`** (one-time conversion). | Aligns spawners with regions' STJ-default discriminator. Existing `$type` values stay the short type name (`"Spawner"`), so only the key changes. |
| D3 | **Legacy `homeRange`: read-legacy / write-modern.** | The runtime keeps reading `homeRange` (→ `spawnBounds`) for old/external files; regenerated files contain only `spawnBounds`. No input-compat break, clean output forward. |
| D4 | **Auto-discovery, no manual registration.** | Today's `FindTypeByName` path needs zero registration; a `Register<T>()` list (regions-style) would be a *regression* and re-introduce the known footgun ("people forget to register"). |
| D5 | **Opt-in via a dedicated, reusable marker attribute** with optional discriminator override. Wire discovery for the `BaseSpawner` hierarchy only in this PR. | Decouples opt-in from STJ's constructor-count rules (so it generalizes), is copy-paste-safe, and the optional override resolves name collisions without class renames. |

## 5. Architecture

Six components. Only one Server-side change is a deletion (`DynamicJson.cs`); the new
spawner serializer lives in UOContent.

### 5.1 Reusable opt-in marker attribute (`Projects/Server/Json/`)

A class-level attribute marking a concrete type as a **discoverable polymorphic JSON
derived type**, with an optional discriminator override. Lives in `Server.Json` alongside
`SerializedJsonPropertyNameAttribute`.

```csharp
// Name is a plan-level detail; e.g. JsonDiscoverableTypeAttribute / DiscoverableJsonTypeAttribute.
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class JsonDiscoverableTypeAttribute : Attribute
{
    public JsonDiscoverableTypeAttribute(string discriminator = null) => Discriminator = discriminator;
    public string Discriminator { get; } // null => use Type.Name
}
```

- **Not** coupled to spawners — it only declares "this concrete class is discoverable for
  STJ polymorphism." The *consumer* (the spawner serializer) decides which base hierarchy
  to scan.
- `Inherited = false`: a subclass must declare its own intent (no accidental inheritance).
- This is a **runtime-discovery** attribute, distinct from the codegen `Serialized*`
  family — it does not drive the source generator.

### 5.2 `SpawnerJsonSerializer` (`Projects/UOContent/Engines/Spawners/`)

Mirrors `RegionJsonSerializer`, but discovers derived types instead of requiring
registration. Placed in UOContent because `BaseSpawner` is UOContent — keeps Server edits
to the `DynamicJson` deletion only.

Responsibilities:

1. **Discovery (`Configure` phase).** Scan `AssemblyHandler.GetTypeCache(asm).Types`
   across all assemblies (same enumeration `Main.cs:VerifyType` uses) for types where
   `IsAssignableTo(typeof(BaseSpawner)) && !IsAbstract` **and** bearing the marker
   attribute. Build the `JsonDerivedType[]` as `new(t, attr.Discriminator ?? t.Name)`.
   - Triggered via the standard `Invoke("Configure")` bootstrap (a static `Configure()`).
   - Timing is safe: spawner JSON is only (de)serialized at import/export command time,
     long after `Configure`.

2. **`TypeInfoResolver`** (a `DefaultJsonTypeInfoResolver` with modifiers):
   - *Modifier A — polymorphism:* on `typeInfo.Type == typeof(BaseSpawner)`, set
     `PolymorphismOptions` and add the discovered `DerivedTypes`. Discriminator key stays
     STJ-default `$type`.
   - *Modifier B — sparse output (D1):* on types assignable to `BaseSpawner`, set
     `JsonPropertyInfo.ShouldSerialize` for the domain-default fields, reusing the existing
     default conditions (`_minDelay != DefaultMinDelay`, `_maxDelay != DefaultMaxDelay`,
     `_team != 0`, `_maxSpawnAttempts != DefaultMaxSpawnAttempts`, `spawnPositionMode`
     not Automatic/Abandoned, `walkingRange != -1`, optional name/etc.).
   - *Modifier C — post-construct (`OnDeserialized`):* run `InitSpawn(...)`, wire up
     `entries` (`AddEntry` per entry), and perform the legacy `homeRange` → `spawnBounds`
     conversion (D3) using the captured location.

3. **Options:** `new(JsonConfig.DefaultOptions) { DefaultIgnoreCondition =
   WhenWritingDefault, TypeInfoResolver = … }` — same shape as `RegionJsonSerializer._options`.
   The `TextDefinitionConverterFactory` (used by `ProximitySpawner.spawnMessage`) is added
   as it is today.

4. **Startup validation (loud):**
   - *Collision:* two discovered types resolving to the same discriminator → throw at
     `Configure` with both full names; resolvable by setting an explicit override.
   - *Constructibility:* a discovered (opted-in) type that STJ cannot construct (no single
     public ctor and no `[JsonConstructor]`) → throw at `Configure` with the full name,
     converting a first-import failure into a boot-time failure.

### 5.3 Spawner class changes

`Spawner` / `RegionSpawner` / `ProximitySpawner` become STJ-deserializable like
`SpawnerEntry`:

- Add the **marker attribute** to each concrete class.
- Annotate JSON-bound fields with `[SerializedJsonPropertyName("…")]` (generator emits
  `[JsonPropertyName]`); add a minimal `[JsonConstructor]` per concrete class chaining the
  `Item` base ctor. (Each is required because Items always have multiple constructors, so
  STJ needs the attribute to disambiguate.)
- **Delete** every `(DynamicJson, options)` constructor and every `ToJson(...)` method.
- `location` / `map`: expose JSON-only accessors on `BaseSpawner` that **read** live
  `Item.Location`/`Item.Map` for export and **capture** an import target for placement.
  These are *not* `[SerializableField]` (binary save untouched) — plain
  `[JsonInclude]`/`[JsonPropertyName]` members backed by transient fields.
- `homeRange`: a settable, write-ignored legacy property feeding the `OnDeserialized`
  conversion; `spawnBounds` is the modern field.

### 5.4 Import / Export command rewire

- **Export** (`ExportSpawnersCommand`): `JsonConfig.Serialize(spawners, options)` over the
  filtered `List<BaseSpawner>` directly. Remove `DynamicJson.Create`/`ToJson`.
- **Import** (`ImportSpawnersCommand`): `JsonConfig.Deserialize<List<BaseSpawner>>(file,
  options)` — polymorphism yields concrete spawner Items directly; remove `FindTypeByName`
  + `CreateInstance`. Read placement from each spawner's JSON `location`/`map` accessors,
  then `MoveToWorld` + `Respawn`, preserving today's dedup-by-location/type replacement.
  - **Cleanup contract:** STJ constructs each Item (serial assigned, World-registered)
    during deserialize, before validation. Any spawner the importer then rejects/replaces
    must be `Delete()`d so no orphaned Items leak. A malformed file mid-parse throws
    `JsonException` (already caught) — any partially-constructed items from that file must
    also be cleaned up.

### 5.5 One-time data migration (`Distribution/Data/Spawns/**`)

A **pure JSON transform** (no game world), preserving the per-file directory layout:

- Rename `"type"` → `"$type"`, kept as the **first** property (STJ requires the
  discriminator first for polymorphic reads).
- Convert `homeRange` (+ `location`) → `spawnBounds` via the documented formula
  (`homeRange == 0` → `z = location.Z, depth = 0`; else `z = -128, depth = 256`;
  `Rectangle3D(x-hr, y-hr, z, hr*2+1, hr*2+1, depth)`), then drop `homeRange`.
- Chosen over round-tripping through the in-game import/export commands because those
  flatten everything into a single output file and would destroy the
  `felucca/Vendors.json` directory structure.
- **Safety net:** because the runtime retains the `homeRange` read path (D3), an imperfect
  conversion cannot silently break loading — a stale `homeRange` still loads correctly.
  A test asserts converter output equals the runtime legacy read for a sample.

### 5.6 Delete `DynamicJson`

Remove `Projects/Server/Json/DynamicJson.cs` and all references. Keep `JsonConfig`, the
converters, and `SerializedJsonPropertyNameAttribute`. `type.CreateInstance<>` stays as a
general utility (just no longer used by import).

## 6. Data flow

- **Export:** live `List<BaseSpawner>` → `JsonConfig.Serialize` (resolver: polymorphism
  writes `$type`; `ShouldSerialize` omits domain defaults) → file.
- **Import / `[GenerateSpawners`:** file → `Deserialize<List<BaseSpawner>>` → STJ picks
  concrete type by `$type`, sets properties, runs `OnDeserialized` (`InitSpawn`, entries,
  `homeRange`→`spawnBounds`) → importer places (`MoveToWorld`) + `Respawn` + dedup.
- **World save/load:** unchanged — binary `[SerializationGenerator]` path, independent of
  JSON.

## 7. Backward compatibility

- Existing files must load after the one-time `$type` migration. The discriminator values
  are unchanged (short type names).
- External/un-migrated files still using `homeRange` continue to load (D3). External files
  still using `"type"` (not `$type`) will **not** load — an accepted consequence of D2.
- `SpawnerEntry` JSON shape is unchanged.

## 8. Error handling

- **Discriminator collision** → throw at `Configure` (both full names; fix via override).
- **Opted-in but not STJ-constructible** → throw at `Configure` (full name).
- **Malformed import file** → `JsonException`, caught as today; partial Items cleaned up.
- **Unknown `$type`** (type not discovered/opted-in) → STJ throws; surfaced as an import
  failure for that file, logged.

## 9. Testing

- **Round-trip:** deserialize a migrated sample → serialize → assert structural equality
  and `$type` first.
- **Load-all:** every migrated file under `Spawns/` deserializes without throwing.
- **Legacy read:** a `homeRange`-only file (no `spawnBounds`) yields the correct bounds.
- **Migration equivalence:** converter output equals the runtime legacy read for a sample.
- **Sparse output:** domain-default fields omitted; non-defaults written (per-field).
- **Polymorphism:** each concrete type round-trips to the correct `$type`; a custom
  marked subclass is discovered and round-trips.
- **Validation:** duplicate discriminator and non-constructible opted-in type each throw
  at `Configure`.
- **Import cleanup:** a malformed/rejected entry leaves no orphaned Items in the World.

## 10. Risks & mitigations

- **Sparse-output parity drift** → driven by the same predicates as the binary layer;
  covered by per-field tests.
- **`$type`-first requirement** → the migration keeps the discriminator first; round-trip
  test asserts it.
- **Item-construction-during-deserialize side effects** → explicit cleanup contract (5.4)
  + import-cleanup test.
- **`homeRange` formula duplicated in the migration tool** → small/pure; equivalence test
  cross-checks against the canonical runtime read.
- **Discoverability of opt-in** → loud `Configure`-time validation; copy-paste convention
  carries the marker.

## 11. Out of scope / follow-ups

- Retire regions' `RegionJsonRegistration.Register<T>()` list using the same discovery +
  marker attribute.
- Replace `DynamicJson.SetProperty`'s round-trip with `JsonNode` — moot once deleted.
