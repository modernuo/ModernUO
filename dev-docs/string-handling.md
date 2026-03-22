# String Handling in ModernUO

This document covers the string building utilities in `Projects/Server/Text/` and `Projects/Server/Buffers/`, when to use each, and how to avoid common allocation pitfalls.

## Table of Contents
1. [ValueStringBuilder](#valuestringbuilder)
2. [RawInterpolatedStringHandler](#rawinterpolatedstringhandler)
3. [StringHelpers](#stringhelpers)
4. [TextEncoding](#textencoding)
5. [Decision Guide](#decision-guide)

---

## ValueStringBuilder

**Location**: `Projects/Server/Buffers/ValueStringBuilder.cs`
**Namespace**: `Server.Text`

A `ref struct` string builder that avoids heap allocations entirely when backed by `stackalloc`. This is the **preferred** string builder for all ModernUO code — do not use `System.Text.StringBuilder`.

### Construction Patterns

**Stackalloc (preferred for bounded output)**:
```csharp
// Best: zero heap allocation, zero pool overhead
using var sb = new ValueStringBuilder(stackalloc char[128]);
sb.Append($"Hello {name}, score: {score}");
return sb.ToString();
```

**Pooled (for unbounded or large output)**:
```csharp
// Rents from STArrayPool — returned on Dispose
using var sb = ValueStringBuilder.Create(256);
// or with default capacity (64):
using var sb = ValueStringBuilder.Create();
```

### Choosing Capacity

| Output size | Pattern |
|---|---|
| Known, <=256 chars | `new ValueStringBuilder(stackalloc char[N])` |
| Known, >256 chars | `ValueStringBuilder.Create(N)` |
| Unbounded/unknown | `ValueStringBuilder.Create()` (grows automatically) |

If the stackalloc buffer is too small, the builder automatically grows to a pooled array. This is safe but costs a pool rent — size the stackalloc to fit the expected output.

### String Interpolation (`$"..."`)

`ValueStringBuilder` supports `$"..."` syntax via a copy-and-reconcile `InterpolationHandler`. This writes directly into the builder's buffer — **no intermediate allocation**, even with stackalloc.

```csharp
// Works with stackalloc — zero allocation
using var sb = new ValueStringBuilder(stackalloc char[64]);
sb.Append($"Player {name} has {kills} kills");
sb.Append($" and {bounty} gold bounty");
```

**How it works**: The compiler passes a value copy of the builder to the handler. The copy shares the same underlying buffer (Span points to the same memory), so writes go to the original buffer. `Append()` reconciles by copying the handler's updated state back to the original. If the handler triggers a `Grow()`, the reconciliation updates the buffer reference.

### Reusing a Builder

Use `Reset()` to clear the builder for reuse instead of creating a new one:

```csharp
using var sb = new ValueStringBuilder(stackalloc char[128]);

foreach (var item in items)
{
    sb.Reset(); // clear for next iteration
    sb.Append($"{item.Name}: {item.Value}");
    Process(sb.ToString());
}
```

### Reading the Result

| Method | Use when |
|---|---|
| `sb.ToString()` | You need a `string` (allocates) |
| `sb.AsSpan()` | You can consume a `ReadOnlySpan<char>` (zero-alloc) |
| `sb.AsSpan(terminate: true)` | You need a null-terminated span |

### Disposal

Always use `using var` for automatic disposal:
```csharp
using var sb = new ValueStringBuilder(stackalloc char[64]);
```

If `using var` is not possible (e.g., the builder is passed by `ref` to extension methods, or `goto case` in switch blocks), use manual `Dispose()`:
```csharp
var sb = new ValueStringBuilder(stackalloc char[64]);
sb.AppendSpaceWithArticle(text, articleAn); // takes ref ValueStringBuilder
var result = sb.ToString();
sb.Dispose();
```

For stackalloc-only builders that never grow, `Dispose()` is a no-op. But always call it defensively — if a future change triggers growth, the pooled array needs returning.

### Limitations

- **No `AppendFormat`**: Use `$"..."` interpolation instead — it's more readable and zero-allocation:
  ```csharp
  // StringBuilder (old):
  sb.AppendFormat("{0:N0} points, {1:N0} kills", score, kills);

  // ValueStringBuilder:
  sb.Append($"{score:N0} points, {kills:N0} kills");
  ```
  There is no `object[] params` equivalent for format strings. All formatting goes through `$"..."` interpolation which uses `ISpanFormattable.TryFormat` directly — zero boxing, zero intermediate strings.

- **No chained Append**: `Append()` returns `void`, not `this`. Write `sb.Append(a); sb.Append(b);` instead of `sb.Append(a).Append(b)`.
- **Ref struct constraints**: Cannot be stored in fields, captured by lambdas, or used in `async` methods. Scoped to the declaring method.
- **`using var` + `ref` conflict**: A `using` variable cannot be passed by `ref`. If extension methods take `ref ValueStringBuilder`, use manual `Dispose()` instead.

---

## RawInterpolatedStringHandler

**Location**: `Projects/Server/Buffers/RawInterpolatedStringHandler.cs`
**Namespace**: `Server.Buffers`

A `[InterpolatedStringHandler]` ref struct used internally by `ValueStringBuilder`'s interpolation support. You should not need to use this directly — use `sb.Append($"...")` instead.

---

## StringHelpers

**Location**: `Projects/Server/Text/StringHelpers.cs`
**Namespace**: `Server.Text`

Extension methods for common string operations:

| Method | Description |
|---|---|
| `Wrap(string, int perLine, int maxLines)` | Word-wrap text into lines |
| `AppendSpaceWithArticle(ref ValueStringBuilder, string, bool)` | Append with "a"/"an" article prefix |
| `Remove(ReadOnlySpan<char>, ...)` | Filter substrings from spans |
| `Capitalize(string)` | Title-case with "the" handling |
| `TrimMultiline(string)` | Trim each line in multiline text |

---

## TextEncoding

**Location**: `Projects/Server/Text/TextEncoding.cs`
**Namespace**: `Server.Text`

UTF-8/Unicode encoding utilities used by the networking layer:

| Method | Description |
|---|---|
| `GetBytesUtf8(string, Span<byte>)` | Encode string to UTF-8 in buffer |
| `GetBytesUtf8(ReadOnlySpan<char>, Span<byte>)` | Encode char span to UTF-8 |
| `GetStringUtf8(Span<byte>)` | Decode UTF-8 bytes to string (with filtering) |

---

## Decision Guide

### When to use what

```
Need to build a string?
├── In a hot path (packets, ticks, spatial queries)?
│   └── ValueStringBuilder with stackalloc
├── In game content (gumps, messages, commands)?
│   └── ValueStringBuilder with stackalloc (bounded) or Create() (unbounded)
├── Building an ObjectPropertyList tooltip?
│   └── Use IPropertyList.Add($"...") — has its own handler
├── In async/multi-threaded code (rare)?
│   └── ValueStringBuilder.CreateMT() or System.Text.StringBuilder
└── Never → System.Text.StringBuilder
```

### Do NOT use `System.Text.StringBuilder`

`ValueStringBuilder` replaces `StringBuilder` in all game code. It avoids:
- GC pressure from `StringBuilder`'s internal `char[]` allocations
- The `StringBuilder` object allocation itself (24+ bytes on heap)
- Thread-safe overhead in `ArrayPool<char>.Shared` (VSB uses lock-free `STArrayPool`)

### Common patterns

**Instead of string concatenation**:
```csharp
// BAD: allocates intermediate strings
var msg = "Player " + name + " has " + kills + " kills";

// GOOD: zero allocation with stackalloc
using var sb = new ValueStringBuilder(stackalloc char[64]);
sb.Append($"Player {name} has {kills} kills");
var msg = sb.ToString(); // single allocation for the final string
```

**Instead of StringBuilder**:
```csharp
// BAD: StringBuilder allocates on heap
var sb = new StringBuilder();
sb.Append(name);
sb.Append(": ");
sb.Append(value);
return sb.ToString();

// GOOD: ValueStringBuilder with stackalloc
using var sb = new ValueStringBuilder(stackalloc char[64]);
sb.Append($"{name}: {value}");
return sb.ToString();
```

**For packet string construction** (hot path):
```csharp
// Use AsSpan() to avoid ToString() allocation when the consumer accepts spans
using var sb = new ValueStringBuilder(stackalloc char[32]);
sb.Append(bounty);
sb.Append(" gold");
writer.WriteString(sb.AsSpan(), textBuffer); // zero-copy
```
