# String Handling in ModernUO

This document covers the string building utilities in `Projects/Server/Text/` and `Projects/Server/Buffers/`, when to use each, and how to avoid common allocation pitfalls.

## Table of Contents
1. [ValueStringBuilder](#valuestringbuilder)
2. [RawInterpolatedStringHandler](#rawinterpolatedstringhandler)
3. [Interpolation Anti-Patterns](#interpolation-anti-patterns)
4. [StringHelpers](#stringhelpers)
5. [TextEncoding](#textencoding)
6. [Decision Guide](#decision-guide)

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

A `[InterpolatedStringHandler]` ref struct that renders an interpolated string directly into a `char[]` rented from `STArrayPool<char>.Shared`. Used as a parameter type to make zero-allocation interpolation overloads possible — when the caller writes `$"..."`, the compiler synthesizes the handler, fills it with the formatted chars, and the receiving method passes `handler.Text` to its underlying span path.

**You normally don't construct this directly**: it's used as a parameter type. Most ModernUO APIs that accept formatted text already provide a `ref RawInterpolatedStringHandler` overload alongside the `string` / `ReadOnlySpan<char>` overload. The compiler picks the handler overload automatically when the argument is a `$"..."` literal.

### APIs that accept `ref RawInterpolatedStringHandler`

- `SpanWriter.WriteAscii`, `WriteLatin1`, `Write(Encoding, …)` — packet building
- `Mobile.SendMessage`, `SendLocalizedMessage`, `SendAsciiMessage`, `Public/Local/Nonlocal/PrivateOverheadMessage`, `Say`, `Emote`, `Whisper`, `Yell` — player-facing chat
- `Item.PublicOverheadMessage`, `SendLocalizedMessageTo`, `SendMessageTo` — item-attributed messages
- `OutgoingMessagePackets.SendMessageLocalized`, `SendMessageLocalizedAffix`, `SendMessage` — direct NetState extensions
- `Html.Center`, `Html.Color`, `Html.Right` — gump HTML helpers

When in doubt, just write `$"..."` at the call site — if a handler overload exists, the compiler picks it.

### `:L` Lowercase Format Specifier

`RawInterpolatedStringHandler` recognizes `:L` as a custom format specifier that lowercases the value's output in-place using `char.ToLowerInvariant`. Handles surrogate pairs correctly via the BCL's vectorized `MemoryExtensions.ToLowerInvariant`.

```csharp
mob.SendMessage($"You earned a {rank:L} trophy!");          // "gold" instead of "Gold"
mob.SendMessage($"Welcome, {playerName:L}");                  // lowercased name
mob.SendMessage($"{count:L} kills");                          // ints unchanged ("42")
```

This eliminates the `value.ToString().ToLowerInvariant()` two-allocation idiom. Works for any type that goes through the handler (enums, strings, anything `ISpanFormattable`).

The format string is case-sensitive — `:l` is not recognized. Match the convention of standard format specifiers (`:N0`, `:F2`, etc.) and use uppercase `:L`.

### Pooled buffer lifecycle

`RawInterpolatedStringHandler` rents a `char[]` from `STArrayPool<char>.Shared` on construction (sized by the literal length + an estimate of formatted chars per hole). Methods that take `ref RawInterpolatedStringHandler` are responsible for calling `handler.Clear()` after consuming `handler.Text`, which returns the buffer to the pool. The rent is single-threaded and lock-free (~tens of nanoseconds), so the cost is negligible compared to the `string` allocation it replaces.

---

## Interpolation Anti-Patterns

ModernUO has many APIs with `ref RawInterpolatedStringHandler` overloads (messages, gumps, packets, OPL — see the list above). The handler overload is **only selected when the call-site argument is a `$"..."` literal directly in position**. Several patterns silently defeat handler binding and fall back to a `string`-allocating path. Each pattern below has a "before" and "after" — apply the "after" form when writing or reviewing code that interpolates into any handler-aware API.

### 1. Ternary with interpolated branches

```csharp
// BAD — the ternary unifies branches as `string`; handler overload not selected
mob.SendMessage(cond ? $"a {x}" : $"b {y}");
```
```csharp
// GOOD — each branch is a separate call, each binds to the handler overload
if (cond)
{
    mob.SendMessage($"a {x}");
}
else
{
    mob.SendMessage($"b {y}");
}
```

`RawInterpolatedStringHandler` is a `ref struct` and cannot appear in a conditional expression result type — the C# compiler unifies the ternary branches to `string`, and the call binds to the `string` / `ROS<char>` overload, allocating the message text per call.

### 2. Switch expression with interpolated arms

```csharp
// BAD — switch expression branches unify as `string`
mob.SendMessage(thing switch
{
    1 => $"a {x}",
    _ => $"b"
});
```
```csharp
// GOOD — switch statement, each arm calls the handler-aware API directly
switch (thing)
{
    case 1:
        mob.SendMessage($"a {x}");
        break;
    default:
        mob.SendMessage($"b");
        break;
}
```

Same root cause as the ternary case.

### 3. Pre-built local typed as `string`

```csharp
// BAD — `msg` is a `string`; ROS<char> overload picked, not the handler
var msg = $"foo {x}";
mob.SendMessage(msg);
```
```csharp
// GOOD — inline at the call site so the compiler sees the literal
mob.SendMessage($"foo {x}");
```

If the local is reused (multiple calls, multiple branches), keep the local — pre-building avoids re-interpolating per send. Inline only when the local is single-use.

### 4. `.ToString()` (or any string-returning method) inside a hole

```csharp
// BAD — .ToString() allocates a string before the handler copies the chars
mob.SendMessage($"You are now {accessLevel.ToString()}.");
mob.SendMessage($"Your guild is {td.String()}.");
```
```csharp
// GOOD — drop the .ToString() and let the handler format the value directly
mob.SendMessage($"You are now {accessLevel}.");
mob.SendMessage($"Your guild is {td}.");
```

The handler's `AppendFormatted<T>` calls `ISpanFormattable.TryFormat` on the value directly, with zero intermediate `string`. An explicit `.ToString()` defeats this. The same applies to `.String()` (TextDefinition), `.GetValue()`, `.AsHexString()`, and any other method that returns a freshly allocated `string`.

For values that don't implement `ISpanFormattable`, the handler falls back to `value.ToString()` internally — same allocation as the explicit call, but at least the call site is consistent.

For lowercase output, use the `:L` format specifier instead of `.ToString().ToLowerInvariant()` (see [RawInterpolatedStringHandler](#rawinterpolatedstringhandler)).

### 5. String concatenation inside a hole

```csharp
// BAD — `+` on strings allocates an intermediate string
mob.SendMessage($"Total: {a + b}");
mob.SendMessage($"Title: {string.Concat(prefix, name)}");
```
```csharp
// GOOD — multiple holes, each formatted directly into the buffer
mob.SendMessage($"Total: {a}{b}");
mob.SendMessage($"Title: {prefix}{name}");
```

Note: `int + int` inside a hole is arithmetic, not concatenation — that's fine. The anti-pattern is `string + string` or `string + value`.

### 6. `string.Format` feeding a handler-aware API

```csharp
// BAD — string.Format allocates a string the handler then re-buffers
mob.SendMessage(string.Format("You earned {0:N0} gold", amount));
```
```csharp
// GOOD — the handler formats `amount` directly into its buffer
mob.SendMessage($"You earned {amount:N0} gold");
```

### 7. LINQ-built strings inside a hole

```csharp
// BAD — Select/Aggregate/Join on strings allocates a chain of intermediates
mob.SendMessage($"Allies: {names.Aggregate((a, b) => $"{a}, {b}")}");
```
```csharp
// GOOD — build via ValueStringBuilder, pass the span
using var sb = new ValueStringBuilder(stackalloc char[256]);
sb.Append("Allies: ");
for (var i = 0; i < names.Count; i++)
{
    if (i > 0)
    {
        sb.Append(", ");
    }
    sb.Append(names[i]);
}
mob.SendMessage(sb.AsSpan());
```

For unbounded inputs, use `ValueStringBuilder.Create()` and call `mob.SendMessage(sb.ToString())` if the consumer needs a `string` (one allocation, vs LINQ's many).

### 8. Pre-built concat var

```csharp
// BAD — concatenation allocates, then the local picks the ROS overload
var s = obj.Name + " says hi";
mob.SendMessage(s);
```
```csharp
// GOOD — interpolation literal at the call site
mob.SendMessage($"{obj.Name} says hi");
```

### Why these matter

These patterns aren't bugs — they produce correct output. But they each leak a `string` per call, and message/gump/OPL APIs are called constantly during gameplay. The handler overload exists specifically to eliminate that allocation, but only when the call-site argument is a direct `$"..."` literal.

When in doubt, ask: "is the handler overload selected here?" — and if the argument is anything other than a top-level `$"..."` literal in the parameter slot, the answer is no.

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
