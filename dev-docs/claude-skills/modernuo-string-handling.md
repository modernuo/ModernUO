# ModernUO String Handling Skill

## When This Skill Applies
- Any code that builds strings dynamically (concatenation, formatting, interpolation)
- Converting `System.Text.StringBuilder` to `ValueStringBuilder`
- Packet string construction
- Gump/message text building

## Core Rule
**Never use `System.Text.StringBuilder`**. Use `Server.Text.ValueStringBuilder` everywhere.

## Quick Reference

### Construction
```csharp
// Bounded output (preferred): zero heap allocation
using var sb = new ValueStringBuilder(stackalloc char[128]);

// Unbounded output: rents from STArrayPool
using var sb = ValueStringBuilder.Create(256);
using var sb = ValueStringBuilder.Create(); // default 64 chars
```

### String Interpolation
Works with stackalloc — writes directly into the builder's buffer:
```csharp
using var sb = new ValueStringBuilder(stackalloc char[64]);
sb.Append($"Player {name} has {kills} kills");
```

### Reuse via Reset
Use `Reset()` instead of creating a new builder:
```csharp
using var sb = new ValueStringBuilder(stackalloc char[128]);
foreach (var item in items)
{
    sb.Reset();
    sb.Append($"{item.Name}: {item.Value}");
    Process(sb.ToString());
}
```

### Reading Results
- `sb.ToString()` — when you need a string (allocates)
- `sb.AsSpan()` — when consumer accepts `ReadOnlySpan<char>` (zero-alloc)

## Common Mistakes

### 1. Using StringBuilder
```csharp
// BAD
var sb = new StringBuilder();
sb.Append(name);
return sb.ToString();

// GOOD
using var sb = new ValueStringBuilder(stackalloc char[64]);
sb.Append(name);
return sb.ToString();
```

### 2. Forgetting `using var`
```csharp
// BAD: pooled array may leak if Grow() happened
var sb = ValueStringBuilder.Create();
return sb.ToString(); // never disposed!

// GOOD
using var sb = ValueStringBuilder.Create();
return sb.ToString();
```

### 3. Chaining Append calls
```csharp
// BAD: VSB Append returns void, not this
sb.Append("a").Append("b");

// GOOD
sb.Append("a");
sb.Append("b");

// BETTER: use interpolation
sb.Append($"a{value}b");
```

### 4. Reassigning a using variable
```csharp
// BAD: can't reassign using var
using var sb = ValueStringBuilder.Create();
sb = ValueStringBuilder.Create(); // CS1656!

// GOOD: use Reset()
using var sb = ValueStringBuilder.Create();
sb.Reset();
```

### 5. `using var` with `ref` extension methods
```csharp
// BAD: CS1657 — using var can't be passed by ref
using var sb = new ValueStringBuilder(stackalloc char[64]);
sb.AppendSpaceWithArticle(text, articleAn); // takes ref VSB

// GOOD: manual Dispose
var sb = new ValueStringBuilder(stackalloc char[64]);
sb.AppendSpaceWithArticle(text, articleAn);
var result = sb.ToString();
sb.Dispose();
```

### 6. No AppendFormat — use `$"..."` interpolation
```csharp
// BAD: AppendFormat doesn't exist on VSB (no object[] params equivalent)
sb.AppendFormat("{0:N0} points, {1:N0} kills", score, kills);

// GOOD: use interpolation with format specifiers (zero boxing, zero intermediate strings)
sb.Append($"{score:N0} points, {kills:N0} kills");
```

## Interpolation Anti-Patterns (handler-aware APIs)

Many APIs accept `ref RawInterpolatedStringHandler` (messages on `Mobile`/`Item`, `IPropertyList.Add`, `SpanWriter.WriteAscii`/`WriteLatin1`, gump `AddLabel`/`AddHtml`, `Html.Center`/`Color`/`Right`, etc.). The handler overload renders the interpolation directly into a pooled buffer with zero `string` allocation — but **only when the call-site argument is a `$"..."` literal directly in position**. These patterns silently defeat that selection. Flag any of them in messaging/gump/OPL code.

| Pattern | Why bad | Fix |
|---|---|---|
| `Send(cond ? $"a" : $"b")` | Ternary unifies branches as `string` | `if/else` with two calls |
| `Send(thing switch { 1 => $"a", _ => $"b" })` | Switch expr unifies as `string` | `switch` statement, call per arm |
| `var s = $"foo {x}"; Send(s);` | Local typed `string`; ROS overload picked | Inline at call site |
| `Send($"x {value.ToString()}")` | `.ToString()` allocates a `string` per call | Drop `.ToString()` — handler formats directly |
| `Send($"x {td.String()}")` | `TextDefinition.String` allocates | Drop `.String()` (or pass `td` directly if API supports it) |
| `Send($"x {a + b}")` | `string + string` allocates | Multiple holes: `Send($"x {a}{b}")` |
| `Send(string.Format("x {0}", v))` | Format allocates | `Send($"x {v}")` |
| `Send($"x {items.Aggregate(...)}")` | LINQ string ops allocate | `ValueStringBuilder` + pass span |

For lowercase output, use the `:L` format specifier instead of `value.ToString().ToLowerInvariant()`:

```csharp
mob.SendMessage($"You earned a {rank:L} trophy!");          // "gold" not "Gold"
```

`:L` is recognized by `RawInterpolatedStringHandler.AppendFormatted<T>(T, string?)` and the `(ROS<char>, int, string?)` overload. Case-sensitive — use uppercase `:L`.

## Capacity Sizing Guide

| Content | Recommended |
|---|---|
| Version strings, coordinates | `stackalloc char[32-48]` |
| Player names, short messages | `stackalloc char[64]` |
| Item descriptions, titles | `stackalloc char[128]` |
| Paragraph text, HTML snippets | `stackalloc char[256]` |
| Large HTML, gump content | `Create(512)` or `Create()` |
| Unbounded (logs, file paths) | `Create()` |

## Related Docs
- `dev-docs/string-handling.md` — full reference (incl. interpolation anti-patterns + `:L` spec)
- `dev-docs/code-standards.md` — memory management rules
- `dev-docs/property-lists.md` — IPropertyList string interpolation (different handler)
- `dev-docs/networking-packets.md` — player-facing message APIs and their handler overloads
