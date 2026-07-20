# Foundation Changes (All Scripts)

## Overview

These changes apply to every RunUO script being migrated. Apply them first before tackling system-specific changes (serialization, timers, gumps, etc.).

## 1. File-Scoped Namespaces

RunUO uses block-scoped namespaces. ModernUO uses file-scoped (C# 10+):

```csharp
// RunUO
namespace Server.Items
{
    public class MyItem : Item
    {
        // ...
    }
}

// ModernUO
namespace Server.Items;

public class MyItem : Item
{
    // ...
}
```

## 2. Naming Conventions

### Private Fields
RunUO uses `m_` prefix. ModernUO uses `_camelCase`:

```csharp
// RunUO
private int m_Charges;
private Mobile m_Owner;
private string m_Name;

// ModernUO
private int _charges;
private Mobile _owner;
private string _name;
```

**Note**: Don't rename existing `m_` fields in legacy code you're not otherwise modifying. Only use `_` for new code and code you're actively migrating.

### Properties
Both use `PascalCase`. RunUO often has verbose syntax:

```csharp
// RunUO
public int Charges { get { return m_Charges; } set { m_Charges = value; } }

// ModernUO (expression-bodied or auto-property)
public int Charges { get => _charges; set => _charges = value; }
// Or if serialized, [SerializableField] generates it automatically
```

## 3. [Constructable] → [Constructible]

Spelling change for the attribute on parameterless constructors:

```csharp
// RunUO
[Constructable]
public MyItem() : base(0x1234) { }

// ModernUO
[Constructible]
public MyItem() : base(0x1234) { }
```

The `using` also changes:
```csharp
// RunUO — no using needed, it's in Server namespace
// ModernUO — still in Server namespace, but ensure you have:
using ModernUO.Serialization;  // for [SerializationGenerator], [SerializableField]
```

## 4. Logging

`Console.WriteLine` is never used in ModernUO. Use structured logging:

```csharp
// RunUO
Console.WriteLine("Player {0} logged in from {1}", name, ip);

// ModernUO
using Server.Logging;

private static readonly ILogger logger = LogFactory.GetLogger(typeof(MyClass));

logger.Information("Player {Name} logged in from {IP}", name, ip);
```

Log levels: `Debug`, `Information`, `Warning`, `Error`, `Fatal`

## 5. DateTime.UtcNow → Core.Now

```csharp
// RunUO
DateTime.UtcNow

// ModernUO
Core.Now
```

`Core.Now` is the server's authoritative time source, consistent within each game tick.

## 6. World Iteration → Spatial Queries

Never iterate `World.Mobiles` or `World.Items`. Use map-based spatial queries:

```csharp
// RunUO
foreach (Mobile m in World.Mobiles.Values)
{
    if (m.InRange(location, 10))
        DoSomething(m);
}

// ModernUO
foreach (var m in map.GetMobilesInRange<Mobile>(location, 10))
{
    DoSomething(m);
}
```

Available spatial queries (on `Map`):
- `GetMobilesAt<T>(Point3D)` — exact location
- `GetMobilesInRange<T>(Point3D, int range)` — within range
- `GetMobilesInBounds<T>(Rectangle2D)` — within rectangle
- Same for `GetItemsAt`, `GetItemsInRange`, `GetItemsInBounds`

## 7. Remove Concurrency Primitives

ModernUO's game loop is single-threaded. Remove all threading constructs:

```csharp
// RunUO (remove ALL of these)
lock (_syncObj) { }
volatile int _counter;
ConcurrentDictionary<int, Item> _items;
Mutex m = new Mutex();
Semaphore s = new Semaphore(1, 1);

// ModernUO (single-threaded replacements)
// lock → remove entirely
// volatile → remove keyword
// ConcurrentDictionary → Dictionary
// Mutex/Semaphore → remove entirely
```

## 8. No Task.Run / new Thread

Game code must not spawn threads:

```csharp
// RunUO (FORBIDDEN in ModernUO)
Task.Run(() => ProcessItems());
new Thread(BackgroundWork).Start();
ThreadPool.QueueUserWorkItem(Work);

// ModernUO — use timers or await
Timer.StartTimer(TimeSpan.FromSeconds(1), ProcessItems);
await Timer.Pause(1000);  // for async/await patterns
```

## 9. ArrayPool → STArrayPool

```csharp
// RunUO
var buffer = ArrayPool<byte>.Shared.Rent(1024);
// ...
ArrayPool<byte>.Shared.Return(buffer);

// ModernUO (single-threaded, no locks)
var buffer = STArrayPool<byte>.Shared.Rent(1024);
// ...
STArrayPool<byte>.Shared.Return(buffer);
```

## 10. new List<T>() on Hot Paths → PooledRefList<T>

```csharp
// RunUO
var list = new List<Mobile>();
foreach (var m in nearbyMobiles)
{
    if (m.Alive)
        list.Add(m);
}
// list goes to GC

// ModernUO (zero-alloc)
using var list = PooledRefList<Mobile>.Create();
foreach (var m in nearbyMobiles)
{
    if (m.Alive)
        list.Add(m);
}
// Automatically returns array to pool on Dispose
```

## 11. LINQ Restrictions

ModernUO has tiered LINQ rules. On hot paths:
- **Tier 1 (allowed)**: `foreach` over `IEnumerable<T>`, `.Contains()` after LINQ operators, `.OrderBy().First()`, `.Count()` on sized collections
- **Tier 2 (acceptable on warm paths)**: `.Skip().Take().ToArray()`, `.Where()` on arrays
- **Tier 3 (forbidden on hot paths)**: `.Select().Where()` chains, `.GroupBy()`, `.ToDictionary()`, `.Aggregate()`, `.Sum()`/`.Min()`/`.Max()`

```csharp
// RunUO (common LINQ patterns — FORBIDDEN on hot paths in ModernUO)
var targets = nearbyMobiles.Where(m => m.Alive).ToList();
var count = items.Count(i => i.Stackable);

// ModernUO (manual loops)
using var targets = PooledRefList<Mobile>.Create();
foreach (var m in nearbyMobiles)
{
    if (m.Alive)
        targets.Add(m);
}

var count = 0;
foreach (var i in items)
{
    if (i.Stackable)
        count++;
}
```

See `dev-docs/code-standards.md` for full LINQ tier details.

## 12. Property Syntax Modernization

```csharp
// RunUO (verbose)
public int Charges
{
    get { return m_Charges; }
    set { m_Charges = value; }
}

public override string Name
{
    get { return "An Item"; }
}

// ModernUO (modern C#)
public int Charges { get => _charges; set => _charges = value; }

public override string DefaultName => "an item";
```

## 13. Using Directives

Common new usings in ModernUO:
```csharp
using ModernUO.Serialization;  // [SerializationGenerator], [SerializableField], etc.
using Server.Logging;           // ILogger, LogFactory
using Server.Gumps;            // SendGump, HasGump, etc. extension methods
using Server.Collections;       // PooledRefList
```

Removed/changed usings:
```csharp
// RunUO (no longer exists/changed)
using Server.Network;  // Packet classes removed — use extension methods
```

## 14. Serial Constructor Removal

RunUO items have a deserialization constructor `MyItem(Serial serial) : base(serial)`. In ModernUO with `[SerializationGenerator]`, this constructor is generated automatically. **Remove it.**

```csharp
// RunUO
public MyItem(Serial serial) : base(serial) { }

// ModernUO — DELETE THIS CONSTRUCTOR. The source generator creates it.
```

## 15. Static `Parse(string)` → `IParsable<T>` / `ISpanParsable<T>`

RunUO predates `IParsable<T>`/`ISpanParsable<T>` (C# 11 / .NET 7 static-abstract interface
members), so RunUO types that convert from a string expose a bare `public static T Parse(string value)`.
**ModernUO expects any such type to implement `IParsable<T>` (string) and, where practical,
`ISpanParsable<T>` (span; it extends `IParsable<T>`, so implement span and you get both).**

This matters because the engine's string→value converter, `Server.Types.TryParse` — used by `[set`,
`[props`, spawner property assignment, the conditional-command compiler (`[where`), and Advanced
Search — binds to the `Parse(string, IFormatProvider)` signature. A type with **only** a legacy
`Parse(string)` is discovered by `Types` through a reflection fallback, but that fallback is a safety
net, not the intended path: a bare `Parse(string)` is easy to miss, doesn't participate in the
span-based fast paths, and (if it returns `null` instead of throwing) makes `[set` silently assign
`null` on bad input. Convert it.

The `Parse` overloads throw `FormatException` on failure; `TryParse` returns `false`. Delegate the
string overloads to a span core (see `Race`, `Poison`, `Point3D` for the established pattern):

```csharp
// RunUO
public abstract class Faction : IComparable<Faction>
{
    public static Faction Parse(string name)  // returns null on no-match — wrong contract, not IParsable
    {
        // ... linear search by name ...
        return null;
    }
}

// ModernUO
public abstract class Faction : IComparable<Faction>, ISpanParsable<Faction>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Faction Parse(string s) => Parse(s, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Faction Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string s, IFormatProvider provider, out Faction result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Faction Parse(ReadOnlySpan<char> s, IFormatProvider provider) =>
        TryParse(s, provider, out var result)
            ? result
            : throw new FormatException($"The input string '{s}' was not in a correct format.");

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out Faction result)
    {
        // ... linear search by name using s.InsensitiveEquals(...) ...
        result = null;
        return false;
    }
}
```

To find un-migrated types: search for `public static [A-Za-z0-9_<>]+ Parse\(string ` and check whether
the declaring type lists `IParsable<T>`/`ISpanParsable<T>`.

## Quick Checklist

When migrating any RunUO script, apply these changes in order:

1. [ ] Change to file-scoped namespace
2. [ ] Add `using ModernUO.Serialization;`
3. [ ] Rename `m_` fields to `_camelCase`
4. [ ] Change `[Constructable]` to `[Constructible]`
5. [ ] Replace `Console.WriteLine` with structured logging
6. [ ] Replace `DateTime.UtcNow` with `Core.Now`
7. [ ] Replace `World.Mobiles`/`World.Items` iteration with spatial queries
8. [ ] Remove concurrency primitives
9. [ ] Remove threading code
10. [ ] Replace `ArrayPool` with `STArrayPool`
11. [ ] Replace `new List<T>()` on hot paths with `PooledRefList<T>`
12. [ ] Modernize property syntax
13. [ ] Remove `Serial` constructor (handled by serialization generator)
14. [ ] Update usings
15. [ ] Convert bare static `Parse(string)` to `IParsable<T>`/`ISpanParsable<T>`

## See Also

- `dev-docs/code-standards.md` — Full coding standards and LINQ tiers
- `dev-docs/threading-model.md` — Threading model details
- `02-serialization.md` — Next step: converting serialization
