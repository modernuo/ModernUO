# ModernUO Coding Standards

This document defines the coding conventions and standards for ModernUO content development. All code under `Projects/UOContent/` and `Projects/Server/` must follow these guidelines.

## Table of Contents
1. [Naming Conventions](#naming-conventions)
2. [Performance Rules](#performance-rules)
3. [Serialization Requirements](#serialization-requirements)
4. [Logging](#logging)
5. [Threading Model](#threading-model)
6. [Memory Management](#memory-management)
7. [Entity Lifecycle](#entity-lifecycle)
8. [Era-Conditional Code](#era-conditional-code)
9. [File Organization](#file-organization)

---

## Naming Conventions

### Fields and Properties
- **Private fields**: `_camelCase` prefix with underscore
  ```csharp
  private int _charges;
  private Mobile _owner;
  private TimerExecutionToken _timerToken;
  ```
- **Properties**: `PascalCase`
  ```csharp
  public int Charges { get; set; }
  public Mobile Owner => _owner;
  ```
- **Methods**: `PascalCase`
  ```csharp
  public void OnDoubleClick(Mobile from) { }
  private void CheckExpiry() { }
  ```
- **Constants**: `PascalCase`
  ```csharp
  public const int MaxCharges = 20;
  ```
- **Local variables**: `camelCase`
  ```csharp
  var damage = Utility.RandomMinMax(10, 20);
  ```

### Legacy Code
Older code uses `m_` prefix for private fields (e.g., `m_Amount`). Do not change existing `m_` fields, but always use `_` prefix for new code.

### Access Levels
```csharp
public enum AccessLevel
{
    Player,       // Regular players
    Counselor,    // Support staff
    GameMaster,   // GMs
    Seer,         // Event coordinators
    Administrator,// Server admins
    Developer,    // Developers
    Owner         // Server owner
}
```
Reference: `Projects/Server/Mobiles/Mobile.cs`

---

## Performance Rules

### LINQ: Know What's Optimized (.NET 10)

.NET 10's JIT and Dynamic PGO can now eliminate abstraction overhead for specific LINQ patterns. Not all LINQ is banned — but most still is. This section defines exactly what's allowed and what isn't.

**Prerequisites**: All optimizations below require .NET 10 with tiered compilation and Dynamic PGO enabled (both on by default — don't disable them). Tier 1 optimizations need ~30+ calls for the JIT to recompile at Tier 1 with PGO data.

#### Tier 1 — Zero-Cost Abstractions (use freely on hot paths)

These patterns produce **zero heap allocations** after JIT warmup, performing as well as hand-written code.

**`foreach` over `IEnumerable<T>` backed by known collection types:**

PGO profiles the concrete type. Guarded devirtualization (GDV) emits a specialized path. The enumerator is devirtualized, inlined, and stack-allocated.

Optimized backing types: `T[]`, `List<T>`, `Stack<T>`, `Queue<T>`, `ConcurrentDictionary<TKey,TValue>`, `PriorityQueue<TElement,TPriority>`.

```csharp
// ✅ ALLOWED — JIT eliminates all abstraction overhead
int Sum(IEnumerable<int> values)  // caller passes int[] or List<int>
{
    int sum = 0;
    foreach (int v in values) sum += v;
    return sum;
}
```

Falls back to normal virtual dispatch + heap-allocated enumerator for unknown/uncommon collection types or before JIT warmup.

**`.Contains()` after a preceding LINQ operator:**

LINQ has ~30 specialized `Contains` overrides that bypass intermediate processing entirely. No sort, no HashSet, no buffering — the source is searched directly.

| Preceding operator | What `.Contains()` does | Speedup vs .NET 9 |
|---|---|---|
| `.Distinct()` | Searches source directly — no HashSet built | ~363x |
| `.Union(other)` | Searches both sources — no HashSet | ~302x |
| `.OrderBy()` / `.OrderByDescending()` | Searches source directly — no sort | ~258x |
| `.ThenBy()` / `.ThenByDescending()` | Same — no sort | ~258x |
| `.Append()` / `.Prepend()` / `.Concat()` | Searches sequentially | ~56x |
| `.SelectMany(f)` | Searches each sub-source | ~49x |
| `.Reverse()` | Searches source directly — no buffering | ~9x |
| `.Where(p).Select(f)` | Applies predicate+projection inline | ~7x |
| `.Select(f)` | Applies projection inline | moderate |
| `.Skip(n)` / `.Take(n)` | Searches within bounds | moderate |
| `.OfType<T>()` / `.Cast<T>()` | Filters/casts and searches | moderate |
| `.Intersect(other)` / `.Except(other)` | Searches appropriately | large |
| `.Shuffle()` | Searches source directly — no shuffle | large |
| `.Shuffle().Take(n)` | Hypergeometric probability — near O(1) math | massive |

```csharp
// ✅ ALLOWED — no sort performed, source searched directly
bool exists = source.OrderBy(x => x.Name).Contains(target);

// ✅ ALLOWED — no HashSet built
bool exists = source.Distinct().Contains(target);

// ✅ ALLOWED — no buffering/reversing
bool exists = source.Reverse().Contains(target);
```

Falls back to normal enumeration for custom `IEnumerable<T>` implementations that LINQ doesn't recognize.

**`.Count()` on sized collections:**

Returns `.Count` property directly when source implements `ICollection<T>` or is a known LINQ iterator with tracked count (after `Range`, `Repeat`, `Skip`, `Take`, `Append`, etc.). O(1), no enumeration.

```csharp
// ✅ ALLOWED — O(1) property access
int count = myList.Count();
int count = Enumerable.Range(0, 1000).Skip(10).Take(50).Count();
```

**`.OrderBy().First()` / `.OrderByDescending().First()` / `.OrderBy().Last()`:**

LINQ performs O(N) min/max scan instead of O(N log N) sort. No sort buffer allocated.

```csharp
// ✅ ALLOWED — O(N) scan, no sort
var cheapest = products.OrderBy(p => p.Price).First();
var newest = events.OrderByDescending(e => e.Timestamp).First();
```

**`.Shuffle().Take(n)`:**

Uses reservoir sampling — single pass over source, O(n) memory. Does NOT shuffle the entire collection.

```csharp
// ✅ ALLOWED — reservoir sampling, not full shuffle
var sample = population.Shuffle().Take(10).ToArray();
```

**`Enumerable.Range()` / `Enumerable.Sequence()` terminal operations:**

When followed by `.Count()`, `.Contains()`, `.ToArray()`, `.ToList()`, `.Skip()`, `.Take()`, `.ElementAt()`, `.Last()`. Specialized iterators compute results from arithmetic, not enumeration.

```csharp
// ✅ ALLOWED — range check, no enumeration
bool has = Enumerable.Range(0, 1000).Contains(500);

// ✅ ALLOWED — single allocation, span fill
int[] arr = Enumerable.Range(0, 100).ToArray();
```

#### Tier 2 — Low Overhead (acceptable on warm paths, benchmark if critical)

These patterns have some overhead but are significantly optimized in .NET 10.

**`.Skip(n).Take(m).ToArray()` / `.ToList()` on `T[]` or `List<T>`:**

Uses vectorized `Span<T>.CopyTo` (~5x faster than .NET 9). Still allocates the output array/list.

```csharp
// ✅ Acceptable on warm paths — vectorized copy
var page = items.Skip(offset).Take(pageSize).ToArray();
```

**`.LeftJoin()` / `.RightJoin()` (new in .NET 10):**

~2x faster and ~2x less memory than the manual `GroupJoin`+`SelectMany`+`DefaultIfEmpty` pattern.

```csharp
// ✅ Prefer over manual GroupJoin chain
var results = orders.LeftJoin(customers, o => o.CustomerId, c => c.Id,
    (order, customer) => new { order, customer });
```

**`.Where(predicate)` on `T[]` or `List<T>`:**

The `WhereIterator` still heap-allocates, but enumerating it is cheaper due to PGO. For true hot paths, manual `foreach`+`if` is still faster.

```csharp
// ⚠️ Acceptable but not zero-cost — WhereIterator allocates
foreach (var item in items.Where(x => x.IsActive))
    Process(item);

// 🏆 Faster manual alternative for true hot paths:
foreach (var item in items)
    if (item.IsActive) Process(item);
```

#### Tier 3 — Still Forbidden on Hot Paths

These patterns still carry meaningful abstraction overhead. Use manual code.

| Pattern | Why it's still slow | Manual alternative |
|---|---|---|
| `.Select(f).Where(p)` (this order) | Each intermediate iterator allocates | `foreach` + `if` + inline transform |
| `.GroupBy(k)` | Builds dictionary internally | Manual dictionary loop |
| `.ToDictionary()` / `.ToHashSet()` | Always allocates the collection | Pre-size and fill manually |
| `.ToLookup()` | Always builds grouping structure | Manual dictionary of lists |
| `.Aggregate(f)` | Delegate overhead per element | Manual accumulator loop |
| `.Sum()` / `.Min()` / `.Max()` on `float`/`double` | No SIMD vectorization in LINQ (ARM) | `TensorPrimitives.Sum()` etc. |
| `.SelectMany(f)` (iterating results, not `.Contains()`) | Multiple enumerator allocations | Nested manual loops |
| `.Zip()` iterating | Enumerator allocations | Dual-index `for` loop |
| Any LINQ over `IAsyncEnumerable<T>` | No PGO/escape analysis for async | Manual `await foreach` |
| Long chains: `.Where().Select().OrderBy().Take()` | Each step allocates an iterator | Manual loop with sort |

```csharp
// ❌ STILL FORBIDDEN — allocates iterator + delegate per step
var targets = nearbyMobiles.Where(m => m.Alive).ToList();
var count = items.Count(i => i.Stackable);  // Count with predicate is NOT .Count()
var first = mobiles.FirstOrDefault(m => m is PlayerMobile);

// ✅ CORRECT — zero allocations
using var targets = PooledRefList<Mobile>.Create();
foreach (var m in nearbyMobiles)
{
    if (m.Alive)
        targets.Add(m);
}

// ✅ CORRECT — manual count
var count = 0;
foreach (var i in items)
{
    if (i.Stackable)
        count++;
}
```

#### Quick Decision Flowchart

```
Is it .Contains() after another LINQ operator?
  YES → ✅ Use it (see Tier 1 table)

Is it foreach over IEnumerable<T> backed by T[]/List<T>/Stack<T>/Queue<T>?
  YES → ✅ Use it (zero-alloc with PGO)

Is it .OrderBy().First() or .OrderBy().Last()?
  YES → ✅ Use it (O(N) not O(N log N))

Is it .Shuffle().Take(n)?
  YES → ✅ Use it (reservoir sampling)

Is it .Count() on a sized collection or Range?
  YES → ✅ Use it (O(1))

Is it .Skip().Take().ToArray() on T[]/List<T>?
  YES → ✅ Acceptable (vectorized copy)

Is it anything else on a hot path?
  → ❌ Write manual code
```

*Reference: Stephen Toub, "Performance Improvements in .NET 10", September 2025. dotnet/runtime PRs: #112684, #108153, #111473, #116978, #112173, #118425.*

### Array Pooling
Use `STArrayPool<T>.Shared` instead of `ArrayPool<T>.Shared` in game logic. STArrayPool is optimized for single-threaded access (no locks).

```csharp
var buffer = STArrayPool<byte>.Shared.Rent(1024);
try
{
    // Use buffer...
}
finally
{
    STArrayPool<byte>.Shared.Return(buffer);
}
```

Reference: `Projects/Server/Buffers/STArrayPool.cs`

### PooledRefList
For temporary lists in methods, use `PooledRefList<T>` instead of `new List<T>()`:

```csharp
using var list = PooledRefList<Mobile>.Create();
// list is stack-allocated, uses pooled backing array
list.Add(mobile);
// Automatically returns array to pool on Dispose
```

Reference: `Projects/Server/Collections/PooledRefList.cs`

### Spatial Queries
Never iterate `World.Mobiles` or `World.Items` directly. Use map-based spatial queries:

```csharp
// BAD - O(n) over ALL mobiles in the world
foreach (var m in World.Mobiles.Values)
{
    if (m.InRange(location, 10))
        DoSomething(m);
}

// GOOD - O(1) sector lookup
foreach (var m in map.GetMobilesInRange<Mobile>(location, 10))
{
    DoSomething(m);
}
```

Available spatial queries (on `Map`):
- `GetMobilesAt<T>(Point3D p)` - exact location
- `GetMobilesInRange<T>(Point3D p, int range)` - within range
- `GetMobilesInBounds<T>(Rectangle2D bounds)` - within rectangle
- Same patterns for `GetItemsAt`, `GetItemsInRange`, `GetItemsInBounds`

---

## Serialization Requirements

### Partial Classes
Any class with `[SerializationGenerator]` **must** be declared `partial`:
```csharp
[SerializationGenerator(0, false)]
public partial class MyItem : Item  // MUST be partial
{
}
```

### Constructible Attribute
Items and Mobiles must have `[Constructible]` on their parameterless constructor:
```csharp
[Constructible]
public MyItem() : base(0x1234)
{
}
```

### Timer Fields Are Not Serialized
`TimerExecutionToken` fields must NOT have `[SerializableField]`:
```csharp
// CORRECT
private TimerExecutionToken _timerToken;  // No serialization attribute

// WRONG
[SerializableField(2)]
private TimerExecutionToken _timerToken;  // Will cause errors
```

Timers are restored in `[AfterDeserialization]` methods.

See: `dev-docs/serialization.md` for complete serialization guide.

---

## Logging

Use structured logging via `ILogger`, never `Console.WriteLine`.

### Setup
```csharp
using Server.Logging;

public class MySystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MySystem));
}
```

### Usage
```csharp
logger.Debug("Processing {Count} items for {Player}", items.Count, player.Name);
logger.Information("Player {Name} logged in from {IP}", name, ip);
logger.Warning("Unexpected state in {System}: {Details}", "Combat", details);
logger.Error(exception, "Failed to process {Action}", action);
logger.Fatal(exception, "Unrecoverable error in {System}", system);
```

### Levels
- `Debug` - Detailed diagnostic information
- `Information` - General operational events
- `Warning` - Unexpected but recoverable situations
- `Error` - Failures that affect specific operations
- `Fatal` - Unrecoverable errors

Reference: `Projects/Logger/ILogger.cs`

---

## Threading Model

ModernUO uses a **single-threaded game loop**. All game logic runs on one thread.

### Forbidden in Game Code
```csharp
// ALL of these are WRONG in game code:
Task.Run(() => ProcessItems());
new Thread(BackgroundWork).Start();
ThreadPool.QueueUserWorkItem(Work);
lock (_syncObj) { }
volatile int _counter;
ConcurrentDictionary<int, Item> _items;
```

### Why It Works
- `EventLoopContext` (SynchronizationContext) routes all `await` continuations to the main thread
- `await` is safe because it always resumes on the game thread
- No data races possible in single-threaded code

### Exceptions
Only server infrastructure code may use threading:
- `Projects/Server/Main.cs` - Event loop setup
- World save disk I/O (serialization on main thread, writes may be background)
- Network I/O

See: `dev-docs/threading-model.md` for complete threading documentation.

---

## Memory Management

### Array Returns
Always return pooled arrays:
```csharp
var arr = STArrayPool<int>.Shared.Rent(size);
try
{
    // Use arr
}
finally
{
    STArrayPool<int>.Shared.Return(arr);
}
```

### Avoid Allocations in Hot Paths
- Use `PooledRefList<T>` instead of `new List<T>()`
- Use `stackalloc` for small fixed-size buffers
- Use `STArrayPool<T>` for larger buffers
- Avoid string concatenation in loops (use `StringBuilder` or string interpolation in `IPropertyList`)

---

## Entity Lifecycle

### Two-Phase Deletion
Items and Mobiles use two deletion hooks:

1. **`OnDelete()`** - Called first. Cancel timers, remove from tracking systems.
   ```csharp
   public override void OnDelete()
   {
       _timerToken.Cancel();
       base.OnDelete();
   }
   ```

2. **`OnAfterDelete()`** - Called after entity is removed from world. Clean up references.
   ```csharp
   public override void OnAfterDelete()
   {
       _timer?.Stop();
       _timer = null;
       _owner = null;
       base.OnAfterDelete();
   }
   ```

### Reference Cleanup
Any field holding an `Item` or `Mobile` reference should be nulled in deletion:
```csharp
public override void OnAfterDelete()
{
    _target = null;
    _owner = null;
    base.OnAfterDelete();
}
```

---

## Era-Conditional Code

### Always Ask for Target Era
If the user hasn't specified which expansion to target, **always ask**. Different eras have dramatically different mechanics.

### Pattern
```csharp
if (Core.AOS)  // Age of Shadows or later
{
    damage = GetNewAosDamage(10, 1, 4, target);
}
else  // Pre-AOS
{
    damage = Utility.Random(4, 4);
}
```

### Available Checks
```csharp
Core.T2A   // >= The Second Age
Core.UOR   // >= Renaissance
Core.UOTD  // >= Third Dawn
Core.LBR   // >= Blackthorn's Revenge
Core.AOS   // >= Age of Shadows
Core.SE    // >= Samurai Empire
Core.ML    // >= Mondain's Legacy
Core.SA    // >= Stygian Abyss
Core.HS    // >= High Seas
Core.TOL   // >= Time of Legends
Core.EJ    // >= Endless Journey
```

See: `dev-docs/era-expansion.md` for complete expansion guide.

---

## File Organization

### Directory Structure
```
Projects/UOContent/
├── Items/
│   ├── Weapons/       # BaseWeapon, Swords/, Maces/, etc.
│   ├── Armor/         # BaseArmor, Plate/, Chain/, etc.
│   ├── Clothing/      # Shirts, hats, etc.
│   ├── Misc/          # General items
│   └── Special/       # Unique/quest items
├── Mobiles/
│   ├── Animals/       # Bears/, Birds/, etc.
│   ├── Monsters/      # AOS/, SE/, ML/ by era
│   ├── Special/       # Champions, bosses
│   └── Vendors/       # NPC vendors
├── Spells/
│   ├── Base/          # Spell base classes
│   ├── First/ - Eighth/  # Magery circles
│   ├── Necromancy/    # Necro spells
│   └── Spellweaving/  # Spellweaving
├── Skills/            # Skill implementations
├── Gumps/             # UI dialogs
│   └── Base/          # Gump base classes
└── Engines/           # Complex systems
```

### File Naming
- One class per file (generally)
- File name matches class name
- Group related items in subdirectories

---

## Quick Reference: Common Anti-Patterns

| Anti-Pattern | Correct Pattern |
|---|---|
| `list.Where(x => x.Alive)` | `foreach` + `if` (Tier 2 — acceptable on warm paths) |
| `.GroupBy()` / `.ToDictionary()` / `.ToHashSet()` | Manual dictionary loop (Tier 3 — still forbidden) |
| `.Select(f).Where(p)` chain | `foreach` + `if` + inline transform (Tier 3) |
| `.OrderBy().First()` | ✅ Allowed — O(N) scan, no sort (Tier 1) |
| `.Distinct().Contains()` | ✅ Allowed — no HashSet built (Tier 1) |
| `Console.WriteLine(msg)` | `logger.Information(msg)` |
| `new List<T>()` in hot path | `PooledRefList<T>.Create()` |
| `ArrayPool<T>.Shared` | `STArrayPool<T>.Shared` |
| `ConcurrentDictionary` | `Dictionary` |
| `Task.Run(...)` | Don't. Use timers. |
| `World.Mobiles.Values` iteration | `map.GetMobilesInRange<T>()` |
| Missing `partial` on serialized class | Add `partial` keyword |
| Serializing `TimerExecutionToken` | Leave unserialized, restore in `[AfterDeserialization]` |
