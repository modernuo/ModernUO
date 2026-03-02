---
name: modernuo-code-audit
description: >
  Auto-trigger whenever writing or modifying .cs files under Projects/. Audits code for ModernUO convention violations. Warnings only - flag issues and ask before fixing.
---

# ModernUO Code Audit

## When This Activates
- Any time you write, edit, or modify a `.cs` file under `Projects/`
- After generating code snippets for the user
- During code review

## Audit Rules (Warnings Only)

Flag these issues but do NOT auto-fix. Ask the user before making changes.

### 1. LINQ: Know What's Optimized (.NET 10)
Not all LINQ is banned. .NET 10 JIT/PGO eliminates overhead for specific patterns. Anything not listed below is still forbidden on hot paths.

**Tier 1 — Zero-cost (use freely on hot paths):**
- `foreach` over `IEnumerable<T>` backed by `T[]`, `List<T>`, `Stack<T>`, `Queue<T>` — PGO devirtualizes the enumerator, zero heap allocation
- `.Contains()` after a preceding LINQ operator (`.Distinct()`, `.OrderBy()`, `.Reverse()`, `.Union()`, `.Intersect()`, `.Except()`, `.Concat()`, `.SelectMany()`, `.Where().Select()`, `.Skip()`, `.Take()`, `.OfType()`, `.Cast()`, `.Shuffle()`) — LINQ has ~30 specialized overrides that skip the intermediate work (no sort, no HashSet, no buffering)
- `.Count()` on sized collections (`ICollection<T>`, or after `Range`/`Repeat`/`Skip`/`Take`/`Append`) — O(1) property access, no enumeration
- `.OrderBy().First()` / `.OrderByDescending().First()` / `.OrderBy().Last()` — O(N) min/max scan, no sort performed
- `.Shuffle().Take(n)` — reservoir sampling, single pass, O(n) memory
- `Enumerable.Range()` / `Enumerable.Sequence()` followed by `.Count()`, `.Contains()`, `.ToArray()`, `.ToList()`, `.ElementAt()`, `.Last()` — arithmetic, not enumeration

**Tier 2 — Low overhead (acceptable on warm paths, benchmark if critical):**
- `.Skip(n).Take(m).ToArray()` on `T[]`/`List<T>` — vectorized `Span<T>.CopyTo` (still allocates output)
- `.LeftJoin()` / `.RightJoin()` — ~2x faster than manual `GroupJoin`+`SelectMany`+`DefaultIfEmpty`
- `.Where(predicate)` on `T[]`/`List<T>` — `WhereIterator` still heap-allocates, but enumeration is PGO-optimized. Manual `foreach`+`if` is still faster for true hot paths.

**Tier 3 — Still forbidden on hot paths (write manual code):**
- `.Select(f).Where(p)` (this order — each intermediate iterator allocates)
- `.GroupBy()`, `.ToDictionary()`, `.ToHashSet()`, `.ToLookup()` (always allocate internal structures)
- `.Aggregate()` (delegate overhead per element)
- `.Sum()` / `.Min()` / `.Max()` on `float`/`double` (no SIMD in LINQ on ARM)
- `.SelectMany()` when iterating results (not `.Contains()`) — multiple enumerator allocations
- `.Zip()` when iterating — enumerator allocations
- Any LINQ over `IAsyncEnumerable<T>` — no PGO/escape analysis
- Long chains like `.Where().Select().OrderBy().Take()` — each step allocates an iterator

**Prerequisites**: .NET 10, tiered compilation + Dynamic PGO enabled (default). Tier 1 optimizations require ~30+ calls for JIT warmup.

**Quick decision**: If the exact pattern is in Tier 1 → use it. If it's in Tier 2 → acceptable unless profiling shows it's a bottleneck. If it's anything else → manual `for`/`foreach` + `PooledRefList<T>`.

### 2. No Console.WriteLine
**Bad**: `Console.WriteLine(...)`, `Console.Write(...)`
**Good**: `private static readonly ILogger logger = LogFactory.GetLogger(typeof(MyClass));` then `logger.Information(...)`, `logger.Warning(...)`, `logger.Error(...)`
**Requires**: `using Server.Logging;`

### 3. No Concurrency Primitives in Game Code
**Bad**: `ConcurrentDictionary`, `ConcurrentQueue`, `ConcurrentBag`, `volatile`, `lock(...)`, `Mutex`, `Semaphore`, `Monitor`, `Interlocked`, `ReaderWriterLock`
**Why**: Server is single-threaded. These add overhead for no benefit.
**Instead**: Use regular `Dictionary<K,V>`, `List<T>`, plain fields.

### 4. Never Iterate World.Mobiles or World.Items Directly
**Bad**: `foreach (var m in World.Mobiles.Values)`, `World.Items.Values.Where(...)`
**Good**: `map.GetMobilesInBounds<T>(bounds)`, `map.GetMobilesInRange<T>(point, range)`, `map.GetItemsInRange<T>(point, range)`
**Why**: Full world iteration is O(n) over all entities. Spatial queries use sector indexing.

### 5. Clean Up References in OnDelete/OnAfterDelete
**Check**: Classes with `Item` or `Mobile` references should clean them in `OnDelete()` or `OnAfterDelete()`.
**Pattern**:
```csharp
public override void OnAfterDelete()
{
    _someReference = null;
    base.OnAfterDelete();
}
```

### 6. Cancel Timers in OnDelete/OnAfterDelete
**Check**: Any class with `TimerExecutionToken` or `Timer` fields must cancel them on deletion.
**Pattern**:
```csharp
public override void OnAfterDelete()
{
    _timerToken.Cancel();  // For TimerExecutionToken
    _timer?.Stop();        // For Timer references
    _timer = null;
    base.OnAfterDelete();
}
```

### 7. Use STArrayPool, Not ArrayPool
**Bad**: `ArrayPool<T>.Shared.Rent(...)` in game logic
**Good**: `STArrayPool<T>.Shared.Rent(...)` in game logic
**Why**: STArrayPool is single-threaded optimized (no locks). Use ArrayPool only in explicitly multi-threaded code.
**Also**: Always return rented arrays in a `finally` block.

### 8. No new List in Hot Paths
**Bad**: `var list = new List<Mobile>();` in frequently-called methods
**Good**: `using var list = PooledRefList<Mobile>.Create();`
**Why**: PooledRefList uses pooled arrays, zero GC pressure. It's a ref struct (stack-allocated).

### 9. Serialization Class Requirements
**Check**: Classes with `[SerializationGenerator]` MUST be `partial`.
**Check**: `[Constructible]` on parameterless constructors for items/mobiles.
**Check**: `TimerExecutionToken` fields must NOT have `[SerializableField]`.
**Check**: Use `using ModernUO.Serialization;` when using serialization attributes.

### 10. No Task.Run or new Thread
**Bad**: `Task.Run(...)`, `new Thread(...)`, `ThreadPool.QueueUserWorkItem(...)` in game code
**Why**: Game logic runs on the single-threaded event loop. Background threads cause race conditions.
**Exception**: Server infrastructure code (Projects/Server/Main.cs, World saves) may use threading.

### 11. Never Assume Era
**Check**: If code uses era-conditional logic (`Core.AOS`, `Core.SE`, etc.) and the user hasn't specified a target era, ASK which expansion to target.
**Why**: Different eras have dramatically different mechanics.

### 12. Naming Conventions
**Check**: `_camelCase` for private fields, `PascalCase` for properties/methods/classes.
**Note**: Legacy code may use `m_` prefix -- don't flag existing `m_` fields but use `_` for new code.

### 13. No Empty Gumps
**Check**: Any gump (legacy `Gump` constructor, or `BuildLayout`) must not have a code path that produces zero visual elements (no `AddBackground`, no `AddPage` with content, etc.).
**Why**: The client has no way to close an empty gump — no close button, no right-click dismiss. This leaks a gump slot on both client and server until relog.
**Common cause**: Early `return` in a constructor or `BuildLayout` when prerequisites aren't met.
**Fix**: Use a static `DisplayTo(Mobile from)` method that validates prerequisites **before** constructing the gump. Make the constructor `private`. See `Projects/UOContent/Gumps/Go/GoGump.cs` for the canonical pattern.

### 14. PropertyList String Literals Must Be Holes
**Check**: In any `IPropertyList.Add()` interpolated string, string constants must be wrapped as holes `{"text"}`, not bare literals.
**Bad**: `list.Add(1060658, $"Chances\t{_charges}");` — "Chances" becomes a delimiter, not an argument.
**Good**: `list.Add(1060658, $"{"Chances"}\t{_charges}");` — "Chances" is an argument.
**Why**: The handler treats bare text as delimiters and `{}` contents as arguments. The property list system is used beyond the game client (e.g., web rendering) which must distinguish arguments from delimiters. Only `\t` should be a bare literal.
**Also**: If you don't know the text for a cliloc number, see `Projects/Server/Localization/Localization.cs` `LoadClilocs()` to learn the binary format, and ask the user where their `cliloc.enu` file is.

## Severity Levels
- **ERROR**: Rules 3, 9, 10, 13 (will cause bugs, build failures, or client-side leaks)
- **WARNING**: Rules 1 (Tier 3 LINQ), 2, 4, 5, 6, 7, 8, 12, 14 (performance/convention issues)
- **INFO**: Rule 1 (Tier 2 LINQ on warm paths — note it but don't flag as violation)
- **ASK**: Rule 11 (need user input)

## How to Report
When you find violations, report them as:
```
[AUDIT] {SEVERITY}: {Description}
  File: {path}:{line}
  Suggestion: {fix}
```

Do NOT silently fix issues. Always flag and ask.

## See Also
- `dev-docs/code-standards.md` - Full coding standards documentation
- `dev-docs/claude-skills/modernuo-serialization.md` - Serialization rules
- `dev-docs/claude-skills/modernuo-timers.md` - Timer cleanup rules
- `dev-docs/claude-skills/modernuo-threading.md` - Threading model details
- `dev-docs/claude-skills/modernuo-property-lists.md` - PropertyList interpolation rules
