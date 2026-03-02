# ModernUO Property Lists (Tooltips)

This document covers ModernUO's property list system for item and mobile tooltips, including the IPropertyList interface, ObjectPropertyList internals, and patterns for customizing tooltips.

## Overview

Property lists (also called Object Property Lists or OPL) are the tooltip popups that appear when a player hovers over items and mobiles. They display the item name, stats, charges, and other relevant information.

The system uses cliloc numbers (localized string IDs) with argument substitution to support multiple languages.

## IPropertyList Interface

Defined in `Projects/Server/PropertyList/IPropertyList.cs`:

```csharp
public interface IPropertyList
{
    void Add(int number);                    // Cliloc number only
    void Add(int number, string argument);   // Cliloc with string argument
    void Add(string text);                   // Raw string
    void Add(int number, int value);         // Cliloc with int argument
    void AddLocalized(int value);            // Cliloc number as argument value
    void AddLocalized(int number, int value); // Cliloc with localized argument

    // String interpolation support
    void Add(ref InterpolatedStringHandler handler);
    void Add(int number, ref InterpolatedStringHandler handler);
}
```

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
