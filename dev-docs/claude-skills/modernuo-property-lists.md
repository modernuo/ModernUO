---
name: modernuo-property-lists
description: >
  Trigger when implementing GetProperties(), working with IPropertyList/ObjectPropertyList, or customizing item tooltips.
---

# ModernUO Property Lists (Tooltips)

## When This Activates
- Implementing `GetProperties()` override
- Working with `IPropertyList` or `ObjectPropertyList`
- Customizing item or mobile tooltips
- Using `[InvalidateProperties]` attribute
- Adding cliloc-based text to items

## Key Rules

1. **Always call `base.GetProperties(list)` first** in overrides
2. **Use cliloc numbers** when possible (int IDs that map to localized strings)
3. **String interpolation** works with `IPropertyList` -- use `$"..."` syntax
4. **`[InvalidateProperties]`** on `[SerializableField]` auto-refreshes tooltip on change
5. **Call `InvalidateProperties()`** manually when non-serialized state changes tooltip

## IPropertyList Interface

```csharp
public interface IPropertyList
{
    void Add(int number);                    // Cliloc number only
    void Add(int number, string argument);   // Cliloc with ~1_val~ arg
    void Add(string text);                   // Raw string (uses internal cliloc)
    void Add(int number, int value);         // Cliloc with int arg
    void AddLocalized(int value);            // Cliloc number as value
    void AddLocalized(int number, int value); // Cliloc wrapper for cliloc

    // String interpolation overloads
    void Add(ref InterpolatedStringHandler handler);
    void Add(int number, ref InterpolatedStringHandler handler);
}
```

## Patterns

### Basic GetProperties Override
```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);  // ALWAYS call base first

    list.Add(1060741, $"{_charges}");          // "charges: ~1_val~"
    list.Add($"{"Quality: "}{_quality}");          // Raw string
    list.Add(1060637, $"{_uses}\t{_maxUses}"); // "~1_val~ / ~2_val~"
}
```

### Cliloc Arguments Format
Cliloc strings use `~1_val~`, `~2_val~`, etc. as placeholders. Arguments are tab-separated:

```csharp
// Cliloc 1060637 = "~1_val~ / ~2_val~"
list.Add(1060637, $"{current}\t{max}");

// Cliloc 1072241 = "Contents: ~1_ITEMS~/~2_MAXITEMS~ items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones"
list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}");

// Cliloc 1042971 = "~1_val~" (generic single argument)
list.Add(1042971, $"{"Custom text here"}");
```

### String Literals Must Be Holes (CRITICAL)

The interpolated string handler distinguishes **literals** (bare text between `{}` holes) from **holes** (values inside `{}`). Literals are delimiters. Holes are arguments. This matters because the property list system is also used for web rendering, which must tell arguments apart from delimiters.

**String constants must always be wrapped as holes: `{"..."}`**

```csharp
// BAD — "Chances" becomes a literal/delimiter, not an argument
list.Add(1060658, $"Chances\t{_charges}");

// GOOD — "Chances" is a hole → argument ~1_val~
list.Add(1060658, $"{"Chances"}\t{_charges}");
```

Real examples (`Teleporter.cs`):
```csharp
list.Add(1060658, $"{"Map"}\t{_mapDest}");       // "~1_val~: ~2_val~"
list.Add(1060659, $"{"Coords"}\t{_pointDest}");
list.Add(1060661, $"{"Range"}\t{_range}");
```

**Rule**: Only `\t` (argument separator) should be bare literal text. Everything else — including string constants — must be inside `{}` holes.

### Cliloc as Argument (Use `:#` Format Specifier)

When an argument is itself a cliloc number, use the `:#` format specifier — **not** a `"#number"` string:

```csharp
// BAD — "#1060000" is a string, web renderers will display it literally
list.Add(1050039, $"{m_Amount}\t{"#1060000"}");

// GOOD — :# tells the handler this is a cliloc number to resolve
list.Add(1050039, $"{m_Amount}\t{1060000:#}");
```

The `:#` format lets the handler (and other consumers like web renderers) know the value is a cliloc reference to resolve, not a raw number. Also available via `list.AddLocalized(number, clilocValue)`.

### Looking Up Cliloc Text

If you don't know what arguments a cliloc number expects, you can read the `cliloc.enu` binary file. Loading logic is in `Projects/Server/Localization/Localization.cs` → `LoadClilocs(string lang, string file)`. Ask the user where their `cliloc.enu` file is (typically in the UO client data directory).

### Auto-Refresh with [InvalidateProperties]
```csharp
[SerializableField(0)]
[InvalidateProperties]  // Auto-calls InvalidateProperties() when Charges changes
[SerializedCommandProperty(AccessLevel.GameMaster)]
private int _charges;
```

### Manual Refresh
```csharp
public void UseCharge()
{
    _charges--;
    InvalidateProperties();  // Manually trigger tooltip refresh
    this.MarkDirty();
}
```

### Conditional Properties
```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);

    if (_charges > 0)
        list.Add(1060741, $"{_charges}");

    if (_owner != null)
        list.Add($"{"Owned by: "}{_owner.Name}");

    if (Core.AOS)  // Era-conditional properties
        list.Add(1061170, $"{_imbueLevel}");  // "animal " ~1_val~
}
```

### Mobile Properties
```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);

    if (Core.AOS && Faction != null)
    {
        list.Add(1060776, $"{Rank.Title}\t{Faction.Definition.PropName}");
    }

    if (DisplayChampionTitle)
    {
        var titleLabel = ChampionTitleSystem.GetChampionTitleLabel(this);
        if (titleLabel > 0)
            list.Add(titleLabel);
    }
}
```

## Common Cliloc Numbers

| Number | Text | Usage |
|---|---|---|
| 1042971 | `~1_val~` | Generic single argument |
| 1060741 | `charges: ~1_val~` | Charge count |
| 1060637 | `~1_val~ / ~2_val~` | Current/max values |
| 1060658 | `~1_val~: ~2_val~` | Key: value pair |
| 1050044 | `~1_ITEMS~ items, ~2_WEIGHT~ stones` | Container contents |
| 1072241 | `Contents: ~1~/~2~ items, ~3~/~4~ stones` | ML container |
| 1060776 | `~1_val~, ~2_val~` | Two comma-separated values |
| 1061170 | `animal lore ~1_val~` | Taming info |
| 1053099 | `damage ~1_val~ - ~2_val~` | Damage range |

## ObjectPropertyList Internals

- Packet ID: 0xD6
- Hash-based change detection -- only sends if content actually changed
- `InvalidateProperties()` rebuilds the list and compares hash
- Uses `STArrayPool<char>` for string building (zero GC)
- Global toggle: `ObjectPropertyList.Enabled`

## Anti-Patterns

- **Forgetting `base.GetProperties(list)`**: Loses default name/weight display
- **Not using cliloc**: Raw strings don't get localized
- **Excessive rebuilds**: Don't call `InvalidateProperties()` in tight loops
- **Assuming tooltip support**: Check `ObjectPropertyList.Enabled` if needed

## Real Examples
- Item properties: `Projects/Server/Items/Item.cs` (AddNameProperties, GetProperties)
- Mobile properties: `Projects/UOContent/Mobiles/PlayerMobile.cs` (GetProperties)
- Container properties: `Projects/Server/Items/Container.cs` (era-conditional display)
- Interface: `Projects/Server/PropertyList/IPropertyList.cs`
- Implementation: `Projects/Server/PropertyList/ObjectPropertyList.cs`

## See Also
- `dev-docs/property-lists.md` - Complete property list documentation
- `dev-docs/claude-skills/modernuo-serialization.md` - [InvalidateProperties] on fields
- `dev-docs/claude-skills/modernuo-era-expansion.md` - Era-conditional properties
