# ModernUO Region System

This document covers ModernUO's region system: spatial areas on the map that control gameplay rules, spawning, spell restrictions, housing, combat, lighting, and more.

## Overview

Regions are named, polygonal volumes on a map. When a mobile enters a region, the server calls that region's virtual hooks to control behavior. Regions are hierarchical — a child region inherits its parent's behavior and can selectively override it.

Regions are loaded from `Data/regions.json` at startup via polymorphic JSON deserialization, but they can also be created **dynamically** at runtime (e.g., house regions, champion spawn areas).

## Class Hierarchy

```
Region (Server)                        ← Engine-level base, 30+ virtual hooks
  └─ BaseRegion (UOContent)            ← Game-level base: CheckTravel, spawn weights, RuneName
       ├─ GuardedRegion                ← NPC guards, vendor access, spell restrictions in town
       │   ├─ NoHousingGuardedRegion   ← GuardedRegion that also blocks housing overlap checks
       │   └─ TownRegion              ← Type marker for towns (adds Entrance property)
       ├─ DungeonRegion               ← Dungeon lighting, no housing, young not protected
       │   └─ NoTravelSpellsAllowedRegion  ← Blocks all travel spells for players
       │       └─ MondainRegion       ← Mondain's Legacy dungeon base
       │           ├─ CrystalFieldRegion, IcyRiverRegion, ...  (damage boost regions)
       ├─ NoHousingRegion             ← Blocks housing placement (manual overlap check)
       ├─ GreenAcresRegion            ← No housing, no travel, no Mark spell
       ├─ JailRegion                  ← Full lockdown: no skills, spells, travel, combat
       ├─ HouseRegion                 ← Dynamic: one per house, access/ban/lockdown control
       ├─ ChampionSpawnRegion         ← Dynamic: champion spawn area effects
       └─ (many more specialized regions)
```

## Region (Server Engine)

`Server.Region` is the engine-level base class in `Projects/Server/Regions/Region.cs`. It provides:

### Key Properties

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Region identifier (unique per map for JSON regions) |
| `Map` | `Map` | The map this region belongs to |
| `Parent` | `Region` | Parent region (null for top-level) |
| `Children` | `List<Region>` | Child regions nested inside this one |
| `Area` | `Rectangle3D[]` | Spatial bounds (one or more 3D rectangles) |
| `Priority` | `int` | Sort priority (default 50); higher = checked first |
| `ChildLevel` | `int` | Nesting depth (0 for top-level) |
| `Dynamic` | `bool` | True if created via constructor (not from JSON) |
| `Registered` | `bool` | Whether the region is active on the map |
| `GoLocation` | `Point3D` | Recall/bind target location |
| `Music` | `MusicName` | Music played for players in this region |

### Sort Order

Regions in each map sector are sorted by:
1. **Dynamic regions first** — runtime regions take precedence over JSON-defined ones
2. **Higher priority first** — `DefaultPriority` is 50
3. **Higher child level first** — more deeply nested regions win

This means a child region always takes precedence over its parent, and dynamic regions (like `HouseRegion`) override static JSON regions.

### Static Lookup Methods

```csharp
// Find the most specific region at a world location (fast — uses sector index)
Region region = Region.Find(point3D, map);

// Find a named region on a map (slow — linear scan, use sparingly)
Region region = Region.Find("Britain", Map.Felucca);
```

### Hierarchy Traversal

```csharp
// Walk up the parent chain looking for a specific type
var dungeon = region.GetRegion<DungeonRegion>();     // null if not in a dungeon
var guarded = region.GetRegion<GuardedRegion>();

// Check if this region is a child of (or equal to) a specific region/type
bool inDungeon = region.IsPartOf<DungeonRegion>();
bool inBritain = region.IsPartOf("Britain");

// Check two types at once (avoids double traversal)
bool inDungeonOrGuarded = region.IsPartOf<DungeonRegion, GuardedRegion>();
```

### Registration / Unregistration

```csharp
region.Register();    // Adds to map sectors, Region.Regions list, parent's Children
region.Unregister();  // Removes from all of the above
```

Both are **idempotent** — calling `Register()` on an already-registered region is a no-op.

`Register()` does:
1. Calls `OnRegister()` virtual
2. Adds self to `Parent.Children` (if parent exists)
3. Adds self to `Region.Regions` global list
4. Adds self to relevant `Map.Sector` lists
5. Stores sector references in `Sectors` array

`Unregister()` does the reverse. **Warning**: unregistering a region that still has children logs a warning — unregister children first.

### Querying Region Contents

```csharp
// Get all players in this region (pooled — zero-alloc)
using var players = region.GetPlayersPooled();

// Get all mobiles in this region (pooled)
using var mobiles = region.GetMobilesPooled();

// Get count without allocating
int count = region.GetPlayerCount();
```

## Virtual Hooks

Region provides 30+ virtual methods. Each default implementation **delegates to Parent** (or returns a default if no parent). This means child regions automatically inherit parent behavior — you only override what you need.

### Lifecycle

| Method | Called when |
|---|---|
| `OnRegister()` | Region is registered on the map |
| `OnUnregister()` | Region is unregistered |
| `OnChildAdded(Region)` | A child region registers under this parent |
| `OnChildRemoved(Region)` | A child region unregisters |

### Movement & Entry

| Method | Called when |
|---|---|
| `OnMoveInto(Mobile, Direction, newLoc, oldLoc)` | Mobile attempts to enter; return false to block |
| `OnEnter(Mobile)` | Mobile enters this region |
| `OnExit(Mobile)` | Mobile leaves this region |
| `OnLocationChanged(Mobile, oldLoc)` | Mobile moves within the region |

### Combat & Interaction

| Method | Called when |
|---|---|
| `AllowHarmful(Mobile from, Mobile target)` | PvP/PvM harm attempt |
| `AllowBeneficial(Mobile from, Mobile target)` | Healing/buffing attempt |
| `OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)` | Aggression committed |
| `OnDidHarmful(Mobile harmer, Mobile harmed)` | Harmful action completed |
| `OnCriminalAction(Mobile, bool message)` | Criminal act committed |
| `OnCombatantChange(Mobile, old, new)` | Target change; return false to block |
| `SpellDamageScalar(Mobile caster, Mobile target, ref double damage)` | Modify spell damage |

### Spells & Skills

| Method | Called when |
|---|---|
| `OnBeginSpellCast(Mobile, ISpell)` | Spell cast attempt; return false to block |
| `OnSpellCast(Mobile, ISpell)` | Spell successfully cast |
| `OnSkillUse(Mobile, int skill)` | Skill use attempt; return false to block |
| `AllowGain(Mobile, Skill, object)` | Skill gain attempt; return false to block |

### World Rules

| Method | Called when |
|---|---|
| `AllowHousing(Mobile, Point3D)` | Housing placement check; return false to block |
| `AllowSpawn()` | Creature spawn check |
| `AcceptsSpawnsFrom(Region)` | Whether spawns from another region are accepted |
| `OnDecay(Item)` | Item decay; return false to prevent |
| `CheckAccessibility(Item, Mobile)` | Item access check; return false to deny |
| `GetResource(Type)` | Alter resource type for mining/harvesting |
| `MakeGuard(Mobile focus)` | Spawn a guard at a location |

### Environment

| Method | Called when |
|---|---|
| `AlterLightLevel(Mobile, ref int global, ref int personal)` | Modify light levels |
| `GetLogoutDelay(Mobile)` | Get logout timer duration |
| `CanUseStuckMenu(Mobile)` | Whether stuck menu is available |
| `OnSpeech(SpeechEventArgs)` | Speech in region (used for guard calls, house commands) |
| `OnResurrect(Mobile)` | Resurrection attempt; return false to block |
| `OnBeforeDeath(Mobile)` / `OnDeath(Mobile)` | Death handling |

## BaseRegion (UOContent)

`Server.Regions.BaseRegion` in `Projects/UOContent/Regions/BaseRegion.cs` extends `Region` with game-specific features:

### Additional Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `YoungProtected` | `virtual bool` | `true` | Young players get warning gump on entry |
| `YoungMayEnter` | `virtual bool` | `true` | Young players allowed to enter |
| `MountsAllowed` | `virtual bool` | `true` | Mounts allowed in region |
| `DeadMayEnter` | `virtual bool` | `true` | Dead players can enter |
| `ResurrectionAllowed` | `virtual bool` | `true` | Resurrection permitted |
| `LogoutAllowed` | `virtual bool` | `true` | Logout permitted |
| `ExcludeFromParentSpawns` | `bool` | `false` | Blocks parent region spawners from placing here |
| `RuneName` | `string` | `null` | Name shown on recall runes |
| `NoLogoutDelay` | `bool` | `false` | Zero logout delay if not in combat |

### CheckTravel

`BaseRegion.CheckTravel` is the key hook for spell travel restrictions:

```csharp
public virtual bool CheckTravel(
    Mobile m,
    Point3D newLocation,
    TravelCheckType travelType,
    out TextDefinition message)
```

`TravelCheckType` values: `RecallFrom`, `RecallTo`, `GateFrom`, `GateTo`, `Mark`, `TeleportFrom`, `TeleportTo`.

Both the **current** region and the **destination** region are checked by `SpellHelper.CheckTravel`. If either returns false, the spell is blocked.

### Spawn Distribution

`BaseRegion` provides `InitRectangles()` which breaks overlapping `Area` rectangles into non-overlapping pieces and calculates weights for uniform spawn distribution. Used by `RegionSpawner`.

## JSON Region Loading

Regions are defined in `Distribution/Data/regions.json` and loaded at startup by `RegionJsonSerializer.LoadRegions()`.

### JSON Schema

```json
{
  "$type": "DungeonRegion",
  "Name": "Shame",
  "Map": "Felucca",
  "Priority": 50,
  "Parent": { "Name": "Felucca", "Map": "Felucca" },
  "Area": [
    { "x1": 5369, "y1": 1, "x2": 5627, "y2": 127 }
  ],
  "GoLocation": { "x": 511, "y": 1565, "z": 0 },
  "Music": "Dungeon9",
  "Entrance": { "x": 511, "y": 1565, "z": 0 },
  "MinExpansion": "None",
  "MaxExpansion": "EJ"
}
```

Key fields:
- `$type` — Polymorphic type discriminator (must be a registered type name)
- `Parent` — Resolved by `RegionByNameConverter` via `Region.Find(name, map)`
- `MinExpansion` / `MaxExpansion` — Region only loads if server expansion is within range
- `Area` — Array of 2D or 3D rectangles

### Type Registration

All region types that appear in JSON must be registered in `RegionJsonRegistration.Configure()`:

```csharp
RegionJsonSerializer.Register<BaseRegion>();
RegionJsonSerializer.Register<TownRegion>();
RegionJsonSerializer.Register<DungeonRegion>();
RegionJsonSerializer.Register<GuardedRegion>();
// ... 30+ more types
```

On deserialization, each region's `Register()` is called automatically if the server expansion is in range.

## Child Regions

Child regions are regions with a `Parent`. They form a hierarchy:

```
Felucca (default region)
  └─ Britain (TownRegion, guarded)
       └─ Britain Bank (BaseRegion, NoLogoutDelay)
```

### How Children Work

1. **Behavior inheritance**: Every virtual hook delegates to `Parent` by default. A child only needs to override what it changes.
2. **Priority**: Children have higher `ChildLevel` than parents, so they're checked first in sector lookups.
3. **Entry/exit events**: When a mobile moves from parent to child (or vice versa), `OnExit` is called on regions being left and `OnEnter` on regions being entered, walking the hierarchy.
4. **`IsPartOf<T>()`**: Walks up the parent chain. A region inside "Britain Bank" returns true for `IsPartOf<GuardedRegion>()` because `TownRegion` (Britain) is a `GuardedRegion`.

### When to Use Child Regions

Use a child region when you need to **modify behavior within an existing region** without replacing it:

- **Dungeon sub-areas**: A treasure room inside a dungeon that has different light or spawn rules
- **Town districts**: A bank area with no logout delay inside a guarded town
- **Boss arenas**: A champion spawn area inside a dungeon that adds lighting effects and player ejection
- **Restricted zones**: A no-spell zone within a larger dungeon

**Example — dungeon chest with a localized effect zone**:

If you need a dungeon chest that creates a "cursed area" around it (e.g., damage over time, spell restrictions), create a child region of the dungeon:

```csharp
public class CursedChestRegion : BaseRegion
{
    private readonly CursedChest _chest;

    public CursedChestRegion(CursedChest chest)
        : base(null, chest.Map, Region.Find(chest.Location, chest.Map), // parent = dungeon
            new Rectangle2D(chest.X - 5, chest.Y - 5, 11, 11))
    {
        _chest = chest;
    }

    // Inherits DungeonRegion behavior (lighting, no housing, etc.)
    // Only adds curse-specific effects

    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);
        if (m.Player)
            m.SendMessage("You feel a dark presence...");
    }

    public override void SpellDamageScalar(Mobile caster, Mobile target, ref double damage)
    {
        base.SpellDamageScalar(caster, target, ref damage);
        damage *= 1.25; // 25% more spell damage in cursed area
    }
}
```

Because this is a **child** of the dungeon region, it inherits all dungeon rules (lighting, no housing, young protection = false) and only adds the curse effect on top.

## Dynamic Regions

Dynamic regions are created at runtime by game code rather than loaded from JSON. They're identified by `Dynamic = true` (set automatically in the constructor) and sort before static regions.

### Pattern 1: Item-Tracked Region (Register/Unregister)

The most common pattern — an item creates a region around itself and manages its lifecycle:

```csharp
public partial class ChampionSpawn : Item
{
    private ChampionSpawnRegion m_Region;

    public void UpdateRegion()
    {
        m_Region?.Unregister();       // Remove old region

        if (!Deleted && Map != Map.Internal)
        {
            m_Region = GetRegion();   // Create new region
            m_Region.Register();      // Add to map
        }
    }

    public override void OnLocationChange(Point3D oldLoc)
    {
        // ... update spawn area coordinates ...
        UpdateRegion();               // Re-register at new location
    }

    public override void OnMapChange()
    {
        // ... update child items' maps ...
        UpdateRegion();               // Re-register on new map
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        // ... cleanup child items ...
        UpdateRegion();               // Unregisters (Deleted == true)
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        // Defer registration to next tick (world may not be fully loaded)
        Timer.StartTimer(TimeSpan.Zero, UpdateRegion);
    }
}
```

**Key points:**
- Call `UpdateRegion()` on `OnLocationChange`, `OnMapChange`, and `OnAfterDelete`
- In `AfterDeserialization`, defer registration with `Timer.StartTimer(TimeSpan.Zero, ...)` — regions rely on the map and parent regions being loaded first
- The `UpdateRegion()` method is idempotent: unregisters old, creates new if not deleted

### Pattern 2: House Region

Houses create a region with priority `DefaultPriority + 1` (higher than normal regions):

```csharp
public virtual void UpdateRegion()
{
    m_Region?.Unregister();

    if (Map != null)
    {
        m_Region = new HouseRegion(this);
        m_Region.Register();
    }
    else
    {
        m_Region = null;
    }
}
```

### Pattern 3: Parent-Aware Dynamic Child

When creating a dynamic region that should be a child of whatever region exists at a location:

```csharp
// Find the existing region at the item's location and use it as parent
var parent = Region.Find(item.Location, item.Map);
var childRegion = new MyCustomRegion(item, parent);
childRegion.Register();
```

The `ChampionSpawnRegion` constructor does exactly this:

```csharp
public ChampionSpawnRegion(ChampionSpawn spawn)
    : base(null, spawn.Map, Region.Find(spawn.Location, spawn.Map), spawn.SpawnArea)
```

This makes the champion spawn region a child of whatever region it's placed in (dungeon, wilderness, etc.), inheriting that region's rules.

## Existing Region Types Quick Reference

### Base Types (Use for Most Tasks)

| Type | Inherits | Key Behavior |
|---|---|---|
| `BaseRegion` | `Region` | Game-level base; CheckTravel, RuneName, spawn weights |
| `GuardedRegion` | `BaseRegion` | NPC guards, no housing, town spell restrictions |
| `TownRegion` | `GuardedRegion` | Town marker (adds Entrance); inherits guard behavior |
| `DungeonRegion` | `BaseRegion` | Dungeon lighting, no housing, young not protected, no stuck menu on Felucca |
| `NoHousingRegion` | `BaseRegion` | Blocks housing placement (manual overlap check) |
| `NoHousingGuardedRegion` | `GuardedRegion` | Guards + no housing overlap check |
| `NoTravelSpellsAllowedRegion` | `DungeonRegion` | Blocks all travel spells for players |
| `GreenAcresRegion` | `BaseRegion` | No housing, no travel, no Mark |
| `JailRegion` | `BaseRegion` | Full lockdown: no skills, spells, combat, travel, or gain |
| `HouseRegion` | `BaseRegion` | Dynamic; per-house access control, lockdown, secure items |

### Choosing a Base for Your Region

| You Need | Inherit From |
|---|---|
| Standard overworld area with custom rules | `BaseRegion` |
| Town/city with guards | `TownRegion` or `GuardedRegion` |
| Dungeon area (dark, no housing) | `DungeonRegion` |
| Dungeon + no travel spells | `NoTravelSpellsAllowedRegion` |
| No housing only | `NoHousingRegion` |
| Full spell/skill lockdown | `JailRegion` (or custom `BaseRegion`) |
| Item-controlled dynamic area | `BaseRegion` (with parent = `Region.Find(...)`) |

### Specialized Regions (Registered for JSON)

These are used in `regions.json` for specific areas and can serve as references for custom regions:

| Type | Purpose |
|---|---|
| `MondainRegion` | Mondain's Legacy dungeon base (no travel) |
| `CrystalFieldRegion` | Cold damage boost zone |
| `IcyRiverRegion` | Cold damage boost zone |
| `AcidRiverRegion` | Poison damage boost zone |
| `PoisonedTreeRegion` | Poison damage boost zone |
| `PoisonedCemeteryRegion` | Poison damage boost zone |
| `LostCityEntranceRegion` | Special dungeon entrance |
| `BlackthornDungeonRegion` | Blackthorn-specific rules |
| `ExodusDungeonRegion` | Exodus dungeon rules |
| `DoomGuardianRegion` | Doom gauntlet area |
| `UnderwaterRegion` | Underwater mechanics |
| `ApprenticeRegion` | Apprentice quest zone |
| `SeaMarketRegion` | Sea market mechanics |
| `BattleRegion` | Myrmidex battle zone |
| `NewMaginciaRegion` | New Magincia rules |
| `TokunoDocksRegion` | Tokuno docks mechanics |
| `TombOfKingsRegion` / `ToKBridgeRegion` | Tomb of Kings areas |
| `WrongLevel3Region` / `WrongJailRegion` | Wrong dungeon jail |
| `CousteauPerronHouseRegion` | Special house region |

## Common Patterns

### Blocking Travel Spells

```csharp
public override bool CheckTravel(
    Mobile m, Point3D newLocation, TravelCheckType travelType, out TextDefinition message)
{
    message = null; // null = use default "Thy spell doth not appear to work"
    return m.AccessLevel > AccessLevel.Player; // Staff can always travel
}
```

### Blocking Specific Spells (e.g., Mark)

```csharp
public override bool OnBeginSpellCast(Mobile m, ISpell s)
{
    if (m.AccessLevel == AccessLevel.Player && s is MarkSpell)
    {
        m.SendLocalizedMessage(501802); // Thy spell doth not appear to work...
        return false;
    }
    return base.OnBeginSpellCast(m, s);
}
```

### Blocking Housing

```csharp
public override bool AllowHousing(Mobile from, Point3D p) => false;
```

### Custom Light Level

```csharp
public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
{
    global = LightCycle.DungeonLevel; // Dungeon darkness
}
```

### No Logout Delay (Safe Zone)

Set `NoLogoutDelay = true` on a `BaseRegion`. The delay is zero only if the mobile has no aggressors and is not criminal.

### Damage Modification

```csharp
public override void SpellDamageScalar(Mobile caster, Mobile target, ref double damage)
{
    base.SpellDamageScalar(caster, target, ref damage);
    damage *= 1.5; // 50% more spell damage
}
```

## Common Mistakes

| Mistake | Problem | Fix |
|---|---|---|
| Not calling `Unregister()` before `Register()` | Duplicate region entries | Always unregister old before creating new |
| Registering in deserialization constructor | Map/parent may not exist yet | Use `[AfterDeserialization]` with `Timer.StartTimer(TimeSpan.Zero, ...)` |
| Forgetting to unregister on item delete | Ghost region remains on map | Call `UpdateRegion()` or `Unregister()` in `OnAfterDelete()` |
| Creating a child without finding the parent | Region has no parent hierarchy benefits | Use `Region.Find(location, map)` as parent |
| Not registering type for JSON | Deserialization fails silently | Add `RegionJsonSerializer.Register<T>()` in `RegionJsonRegistration.Configure()` |
| Using `Region.Find(string, Map)` in hot paths | Linear scan, O(n) | Use `Region.Find(Point3D, Map)` which uses sector index |
| Unregistering parent before children | Warning logged, orphaned children | Unregister children first |

## Key File References

| File | Description |
|---|---|
| `Projects/Server/Regions/Region.cs` | Engine-level base class (30+ virtual hooks) |
| `Projects/Server/Regions/RegionJsonSerializer.cs` | JSON loading, type registration |
| `Projects/Server/Json/Converters/RegionByNameConverter.cs` | Parent resolution from JSON |
| `Projects/UOContent/Regions/BaseRegion.cs` | Game-level base: CheckTravel, spawn weights |
| `Projects/UOContent/Regions/GuardedRegion.cs` | NPC guards, town mechanics |
| `Projects/UOContent/Regions/DungeonRegion.cs` | Dungeon lighting and rules |
| `Projects/UOContent/Regions/TownRegion.cs` | Town type marker |
| `Projects/UOContent/Regions/NoHousingRegion.cs` | Housing block |
| `Projects/UOContent/Regions/NoHousingGuardedRegion.cs` | Guarded + housing block |
| `Projects/UOContent/Regions/NoTravelSpellsAllowedRegion.cs` | Travel spell block |
| `Projects/UOContent/Regions/GreenAcresRegion.cs` | Multi-restriction region |
| `Projects/UOContent/Regions/HouseRegion.cs` | Dynamic per-house region |
| `Projects/UOContent/Regions/RegionJsonRegistration.cs` | All registered JSON types |
| `Projects/UOContent/Engines/CannedEvil/ChampionSpawn.cs` | Dynamic region lifecycle example |
| `Projects/UOContent/Multis/Houses/BaseHouse.cs` | House region lifecycle example |
| `Projects/UOContent/Spells/Base/SpellHelper.cs` | Travel check dispatch |
| `Distribution/Data/regions.json` | Static region definitions |
