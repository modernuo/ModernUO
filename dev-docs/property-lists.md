# ModernUO Property Lists (Tooltips)

This document covers ModernUO's property list system for item and mobile tooltips, including the IPropertyList interface, ObjectPropertyList internals, and patterns for customizing tooltips.

## Overview

Property lists (also called Object Property Lists or OPL) are the tooltip popups that appear when a player hovers over items and mobiles. They display the item name, stats, charges, and other relevant information.

The system uses cliloc numbers (localized string IDs) with argument substitution to support multiple languages.

## IPropertyList Interface

Defined in `Projects/Server/PropertyList/IPropertyList.cs`:

```csharp
public interface IPropertyList : ISelfInterpolatedStringHandler
{
    void Reset();
    void Terminate();

    void Add(int number);                     // Cliloc number only
    void Add(int number, string argument);    // Cliloc with string argument
    void Add(ReadOnlySpan<char> argument);    // Raw text, no string allocation
    void Add(int number, ReadOnlySpan<char> argument); // Cliloc with span argument
    void AddChunked(ReadOnlySpan<char> text); // Newline-joined text split across properties
    OplTextBlock TextBlock();                 // Builder that flushes via AddChunked on dispose
    void Add(int number, int value);          // Cliloc with int argument
    void AddLocalized(int value);             // Cliloc number as argument value
    void AddLocalized(int number, int value); // Cliloc with localized argument

    // String interpolation support
    void Add(ref InterpolatedStringHandler handler);
    void Add(int number, ref InterpolatedStringHandler handler);
}
```

> There is no `Add(string)` overload. Pass raw text as a span (`Add(text.AsSpan())`) or, ideally,
> as an interpolated `$"..."` literal so the handler formats straight into the pooled buffer.

## GetProperties Override

Override `GetProperties()` to add custom tooltip lines:

```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);  // ALWAYS call base first

    // Add cliloc with value
    list.Add(1060741, $"{_charges}");          // "charges: ~1_val~"

    // Add raw string
    list.Add($"{"Quality: "}{_quality}");

    // Add cliloc with multiple tab-separated arguments
    list.Add(1060637, $"{_current}\t{_max}");  // "~1_val~ / ~2_val~"

    // Add cliloc number only (no arguments)
    list.Add(1049644);                          // "Crafted by a Grandmaster"

    // Add int argument
    list.Add(1060741, _charges);               // "charges: ~1_val~"
}
```

## Cliloc Argument Format

Cliloc strings contain placeholders like `~1_val~`, `~2_val~`, etc. Multiple arguments are separated by tab characters (`\t`):

```csharp
// Cliloc 1060637 = "~1_val~ / ~2_val~"
list.Add(1060637, $"{current}\t{max}");

// Cliloc 1072241 = "Contents: ~1_ITEMS~/~2_MAXITEMS~ items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones"
list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}");
```

### String Literals Must Be Holes (CRITICAL)

The `IPropertyList` interpolated string handler distinguishes between **literals** (text between `{}` holes) and **holes** (values inside `{}`). Literals are treated as delimiters (like `\t`). Holes are treated as arguments. The property list system is used beyond just the game client — for example, web rendering — which must be able to tell arguments apart from delimiters.

**This means string constants must always be wrapped as holes using `{"..."}` syntax:**

```csharp
// BAD — "Chances" becomes a literal/delimiter, not an argument
list.Add(1060658, $"Chances\t{_charges}");

// GOOD — "Chances" is a hole, so it's treated as argument ~1_val~
list.Add(1060658, $"{"Chances"}\t{_charges}");
```

Real examples from the codebase (`Teleporter.cs`):
```csharp
// Cliloc 1060658 = "~1_val~: ~2_val~"
list.Add(1060658, $"{"Map"}\t{_mapDest}");
list.Add(1060659, $"{"Coords"}\t{_pointDest}");
list.Add(1060660, $"{"Creatures"}\t{(Creatures ? "Yes" : "No")}");
list.Add(1060661, $"{"Range"}\t{_range}");
```

The compiler generates different calls for each:
- `$"Map\t{value}"` → `AppendLiteral("Map\t")` then `AppendFormatted(value)` — "Map\t" is a delimiter, only `value` is an argument
- `$"{"Map"}\t{value}"` → `AppendFormatted("Map")` then `AppendLiteral("\t")` then `AppendFormatted(value)` — both "Map" and `value` are arguments, `\t` is the delimiter

**Rule of thumb**: The only text that should appear as bare literals in the interpolated string is `\t` (the argument separator). Everything else — including string constants — must be inside `{}` holes.

### No `.ToString()` Inside Holes

`IPropertyList`'s interpolated string handler formats values directly into a pooled buffer via `ISpanFormattable.TryFormat` — no intermediate `string` allocation per hole. An explicit `.ToString()` defeats this:

```csharp
// BAD — .ToString() allocates a string, then the handler copies its chars
list.Add(1060658, $"{"Charges"}\t{_charges.ToString()}");

// GOOD — the handler formats _charges directly with no intermediate string
list.Add(1060658, $"{"Charges"}\t{_charges}");
```

Same applies to `.String()` on `TextDefinition`, `.GetValue()`, and any method that returns a freshly allocated `string` — drop the call and let the handler format the underlying value directly.

The full list of interpolation anti-patterns (ternaries, switch expressions, pre-built locals, `string.Format`, concat, LINQ in holes) applies equally to `IPropertyList.Add($"...")`. See [`dev-docs/string-handling.md`](string-handling.md#interpolation-anti-patterns) for the full reference.

### Cliloc as Argument (Use `:#` Format Specifier)

When a cliloc argument is itself another cliloc number (i.e., the argument should resolve to localized text), use the `:#` format specifier on the integer — **not** a `"#number"` string:

```csharp
// BAD — "#1060000" is a string, not a cliloc reference.
// Other systems (web rendering) will render it as the literal text "#1060000"
list.Add(1050039, $"{m_Amount}\t{"#1060000"}");

// GOOD — 1060000:# tells the handler this argument is a cliloc number to resolve
list.Add(1050039, $"{m_Amount}\t{1060000:#}");
```

The `:#` format specifier is a hint that the value is a cliloc number. The handler calls `AppendFormatted(int value, string? format)` which, when `format == "#"`, resolves the cliloc and appends the localized text. Other consumers of the property list data (like web renderers) can see the `:#` format and know to look up the cliloc text rather than displaying a raw number.

This also works with the `AddLocalized` convenience methods:
```csharp
// These use :# internally
list.AddLocalized(clilocNumber);            // Single cliloc value as argument
list.AddLocalized(1050039, clilocNumber);   // Cliloc with cliloc argument
```

### Looking Up Cliloc Text

If you don't know what text a cliloc number maps to (and therefore what arguments it expects), you can read the `cliloc.enu` binary file. The loading logic is in `Projects/Server/Localization/Localization.cs`, method `LoadClilocs(string lang, string file)`:

- File format: 6-byte header, then repeating entries of `int number` + `byte flag` + `ushort length` + UTF-8 text
- Placeholders in the text look like `~1_val~`, `~2_AMOUNT~`, etc.
- Ask the user where their `cliloc.enu` file is located (typically in the UO client data directory)

## Multi-Line Free Text: `AddChunked` and `OplTextBlock`

Some tooltips need a block of **free-form text** (not cliloc lookups) whose length is variable and
potentially large — e.g. a consolidated dump of every AOS attribute on an item, or staged
identification text that grows as the item is identified. Emitting that as a single property is
dangerous:

> **The legacy 2D client copies each OPL property's text into a fixed ~512-char (1024-byte) buffer.**
> A single property longer than that smashes an adjacent world object's vtable on the client heap and
> crashes the client. `ObjectPropertyList.MaxArgumentLength = 504` is the safe per-property cap (a
> multiple of 8, comfortably under the empirically confirmed ~510-char ceiling).

Two APIs handle this safely. Both split the text at `\n` boundaries into as many OPL properties as
needed, so no single property ever exceeds `MaxArgumentLength`. Each chunk is emitted through the
cycling **passthrough clilocs** (`1042971`, `1070722`, `1114057`, `1114778`, `1114779` — each
localized to a single `~1_val~`/`~1_NOTHING~` argument), which render the raw string verbatim.

### `AddChunked` — the interface primitive

`AddChunked(ReadOnlySpan<char>)` is on `IPropertyList`, so it works from any `GetProperties(IPropertyList list)`
override. Give it `\n`-joined text; it breaks **only** at newlines:

```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);

    // _description may be hundreds of chars and contains embedded '\n's.
    list.AddChunked(_description);
}
```

Splitting only at `\n` means each line stays intact across the chunk boundary — a chunk is flushed at
the last newline that keeps it under the cap. (A single line longer than `MaxArgumentLength` is the
one case that still gets clamped; the engine logs a warning naming the offending entity and cliloc.)

### `OplTextBlock` — the ergonomic builder

`OplTextBlock` is a `ref struct` builder that accumulates `\n`-joined lines and calls `AddChunked` for
you on dispose. Obtain it from `IPropertyList.TextBlock()` (so it works in any `GetProperties` override)
and always scope it with `using` so the flush happens:

```csharp
using var block = list.TextBlock();

// Zero-alloc interpolated overload — formats directly into the pooled buffer:
block.Add($"Luck Bonus: +{luck}%");
block.Add($"Damage: {min} - {max}");

// Plain text as a span (no string allocation):
block.Add("Cannot be repaired".AsSpan());
```

Behavior worth knowing:

- **Lines join with `\n`**; the trailing separator is stripped before the single `AddChunked` flush on `Dispose()`.
- **Empty lines are skipped** (`block.Add(ReadOnlySpan<char>.Empty)` is a no-op).
- **No lines added → nothing is emitted** (no empty property, no wasted passthrough cliloc).
- The `Add($"...")` overload is a dedicated `[InterpolatedStringHandler]`, so interpolation allocates no
  intermediate strings — the same anti-patterns as `IPropertyList.Add($"...")` apply (no `.ToString()`
  in holes, no ternaries/`string.Format`/concat — see [`string-handling.md`](string-handling.md#interpolation-anti-patterns)).
- It is a `ref struct` tied to the single-threaded OPL build pass — never store it, capture it in a
  closure, or use it across an `await`.

### When to use which

| Situation | Use |
|---|---|
| You already have a `\n`-joined string (e.g. a serialized description) | `list.AddChunked(text)` |
| You're building several free-text lines conditionally | `using var block = list.TextBlock();` then `block.Add(...)` |
| The content is a single short cliloc-backed property | Plain `list.Add(number, $"...")` — chunking is unnecessary |

Real-world pattern (consolidated attribute lines, condensed from UOEvolution's `BaseWeapon`):

```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);

    using var block = list.TextBlock();

    if (_attrs.HitLowerParryCap > 0)
    {
        block.Add($"Lower Parry Cap {_attrs.HitLowerParryCap}%");
    }

    if (_attrs.ParryBonusDamage > 0)
    {
        block.Add($"Parry Damage {_attrs.ParryBonusDamage}%");
    }
    // ...any number of optional lines; the block flushes safely on dispose.
}
```

## Common Cliloc Numbers

| Number | Text | Usage |
|---|---|---|
| 1042971 | `~1_val~` | Generic single value |
| 1060741 | `charges: ~1_val~` | Charge count |
| 1060637 | `~1_val~ / ~2_val~` | Current/max values |
| 1060658 | `~1_val~: ~2_val~` | Key: value pair |
| 1050044 | `~1_ITEMS~ items, ~2_WEIGHT~ stones` | Container contents (pre-ML) |
| 1072241 | `Contents: ~1~/~2~ items, ~3~/~4~ stones` | Container contents (ML+) |
| 1060776 | `~1_val~, ~2_val~` | Two comma-separated values |
| 1053099 | `damage ~1_val~ - ~2_val~` | Damage range |
| 1061170 | `animal lore ~1_val~` | Taming info |
| 1049644 | `Crafted by a Grandmaster` | Crafting quality |
| 1042001 | `That must be in your pack...` | Backpack requirement message |
| 1011036 | `OK` | OK button text |
| 1011012 | `CANCEL` | Cancel button text |
| 1060635 | `Warning` | Warning header |

## Auto-Refresh with [InvalidateProperties]

When using `[SerializableField]`, adding `[InvalidateProperties]` automatically calls `InvalidateProperties()` whenever the generated property setter is invoked:

```csharp
[SerializableField(0)]
[InvalidateProperties]  // Auto-refreshes tooltip when Charges changes
[SerializedCommandProperty(AccessLevel.GameMaster)]
private int _charges;
```

The generated setter becomes:
```csharp
public int Charges
{
    get => _charges;
    set
    {
        _charges = value;
        InvalidateProperties();  // Added by [InvalidateProperties]
        this.MarkDirty();
    }
}
```

## Manual Refresh

Call `InvalidateProperties()` when non-serialized state changes affect the tooltip:

```csharp
public void UseCharge()
{
    _charges--;
    InvalidateProperties();  // Force tooltip rebuild
    this.MarkDirty();
}
```

## ObjectPropertyList Internals

Defined in `Projects/Server/PropertyList/ObjectPropertyList.cs`:

- **Packet ID**: 0xD6
- **Hash-based updates**: Each property list has a hash. When `InvalidateProperties()` is called, the list is rebuilt and compared. Only if the hash changed is the new list sent to clients.
- **String building**: Uses `STArrayPool<char>` for zero-GC string construction
- **Global toggle**: `ObjectPropertyList.Enabled` can disable the entire system
- **Lazy initialization**: Property lists are built on first access
- **Per-property cap**: `MaxArgumentLength` (504) bounds each property's text so the legacy 2D client's fixed tooltip buffer can't overflow. `AddChunked`/`OplTextBlock` keep multi-line content under it; anything that slips through is clamped with a logged warning

### Update Flow
1. `InvalidateProperties()` is called
2. If map is valid and world isn't loading:
   a. Save old hash
   b. Reset and rebuild property list via `GetProperties()`
   c. Compare new hash with old hash
   d. If changed, queue delta update to clients via `Delta(ItemDelta.Properties)`

## Era-Conditional Properties

```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);

    if (Core.ML)
    {
        if (ParentsContain<BankBox>())
            list.Add(1073841, $"{TotalItems}\t{MaxItems}\t{TotalWeight}");
        else
            list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}");
    }
    else
    {
        list.Add(1050044, $"{TotalItems}\t{TotalWeight}");
    }
}
```

## Mobile Properties

Mobiles can also have property lists:

```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);

    if (Core.AOS && Faction != null)
    {
        list.Add(1060776, $"{Rank.Title}\t{Faction.Definition.PropName}");
    }

    if (_guildTitle != null)
        list.Add($"[{_guildTitle}]");
}
```

## Item Built-in Property Methods

`Item` provides several helper methods called during property list building:

```csharp
public virtual void AddNameProperty(IPropertyList list)     // Item name
public virtual void AddLootTypeProperty(IPropertyList list) // Blessed/Cursed/etc.
public virtual void AddResistanceProperties(IPropertyList list) // Resistance values
public virtual void AddWeightProperty(IPropertyList list)   // Weight display
public virtual void AddQuestItemProperty(IPropertyList list) // Quest item marker
public virtual void AddSecureProperty(IPropertyList list)    // Secure container marker
```

## Best Practices

1. **Always call `base.GetProperties(list)` first** -- it adds the item name and standard properties
2. **Use cliloc numbers** over raw strings when possible for localization support
3. **Use string interpolation** with `$""` for clean argument formatting
4. **Use tab (`\t`) to separate** multiple arguments in a single cliloc
5. **Don't call `InvalidateProperties()` in tight loops** -- it triggers hash computation and potential network sends
6. **Use `[InvalidateProperties]`** on serialized fields to automate refresh
7. **Check era** when properties differ between expansions

## Key File References

| File | Description |
|---|---|
| `Projects/Server/PropertyList/IPropertyList.cs` | Interface definition |
| `Projects/Server/PropertyList/ObjectPropertyList.cs` | Implementation |
| `Projects/Server/PropertyList/IObjectPropertyListEntity.cs` | Entity interface |
| `Projects/Server/Items/Item.cs` | Item.GetProperties, InvalidateProperties |
| `Projects/UOContent/Mobiles/PlayerMobile.cs` | Mobile property example |
| `Projects/Server/Items/Container.cs` | Era-conditional properties |
