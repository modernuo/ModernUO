# Property Lists (Tooltips) Migration

## Overview

RunUO uses `ObjectPropertyList` as both the interface and implementation for item/mobile tooltips. ModernUO introduces an `IPropertyList` interface and has a critical rule: string literals in interpolated strings must be wrapped as holes (`{"text"}` not `text`).

## RunUO Pattern

```csharp
public override void GetProperties(ObjectPropertyList list)
{
    base.GetProperties(list);
    list.Add(1060741, m_Charges.ToString());      // "charges: ~1_val~"
    list.Add(1060658, "Map\t" + m_MapDest);        // "~1_val~: ~2_val~"
    list.Add(1060637, "{0}\t{1}", m_Current, m_Max); // "~1_val~ / ~2_val~"
    list.Add("Custom text line");
}
```

## ModernUO Equivalent

```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);
    list.Add(1060741, $"{_charges}");              // "charges: ~1_val~"
    list.Add(1060658, $"{"Map"}\t{_mapDest}");     // "~1_val~: ~2_val~" — "Map" is a hole!
    list.Add(1060637, $"{_current}\t{_max}");      // "~1_val~ / ~2_val~"
    list.Add($"{"Custom text line"}");             // Raw string — still a hole
}
```

## Migration Mapping Table

| RunUO | ModernUO | Notes |
|---|---|---|
| `GetProperties(ObjectPropertyList list)` | `GetProperties(IPropertyList list)` | Interface instead of class |
| `list.Add(number, string.Format(...))` | `list.Add(number, $"...")` | Interpolated string |
| `list.Add(number, value.ToString())` | `list.Add(number, $"{value}")` | Interpolated |
| `list.Add(number, "text\t" + value)` | `list.Add(number, $"{"text"}\t{value}")` | Text must be hole |
| `list.Add(number, string.Format("{0}\t{1}", a, b))` | `list.Add(number, $"{a}\t{b}")` | Tab-separated args |
| `list.Add("raw string")` | `list.Add($"{"raw string"}")` | String literal as hole |
| `list.Add(number)` | `list.Add(number)` | Same — cliloc only |
| `list.Add(number, "#" + cliloc)` | `list.Add(number, $"{cliloc:#}")` | Cliloc as argument |

## The String Literal Rule (CRITICAL)

The `IPropertyList` interpolated string handler distinguishes between **literals** (text between `{}` holes) and **holes** (values inside `{}`). Literals are treated as delimiters (like `\t`). Holes are treated as cliloc arguments.

**Rule**: The only bare literal text should be `\t` (argument separator). All other text must be inside `{}` holes.

```csharp
// BAD — "Map" is a literal, treated as a delimiter
list.Add(1060658, $"Map\t{_mapDest}");

// GOOD — "Map" is a hole, treated as argument ~1_val~
list.Add(1060658, $"{"Map"}\t{_mapDest}");
```

Why this matters: The property list data is consumed beyond just the game client (e.g., web renderers). The system must distinguish arguments from delimiters to correctly format tooltips for all consumers.

### Cliloc Number as Argument

When a cliloc argument is itself a cliloc number to resolve:

```csharp
// BAD — string "#1060000" is not properly handled by all consumers
list.Add(1050039, $"{_amount}\t{"#1060000"}");

// GOOD — :#  format specifier marks it as a cliloc reference
list.Add(1050039, $"{_amount}\t{1060000:#}");
```

Or use the convenience methods:
```csharp
list.AddLocalized(clilocNumber);             // Single cliloc value
list.AddLocalized(1050039, clilocNumber);    // Cliloc with cliloc argument
```

## Step-by-Step Conversion

### Step 1: Change Method Signature
```csharp
// RunUO
public override void GetProperties(ObjectPropertyList list)
// ModernUO
public override void GetProperties(IPropertyList list)
```

### Step 2: Always Call Base First
```csharp
base.GetProperties(list); // Unchanged
```

### Step 3: Convert Each list.Add() Call

**Simple value:**
```csharp
// RunUO
list.Add(1060741, m_Charges.ToString());
// ModernUO
list.Add(1060741, $"{_charges}");
```

**Multiple tab-separated arguments:**
```csharp
// RunUO
list.Add(1060637, string.Format("{0}\t{1}", m_Current, m_Max));
// ModernUO
list.Add(1060637, $"{_current}\t{_max}");
```

**String literal arguments:**
```csharp
// RunUO
list.Add(1060658, "Coords\t" + m_Location.ToString());
// ModernUO
list.Add(1060658, $"{"Coords"}\t{_location}");
```

**Raw text:**
```csharp
// RunUO
list.Add("Soulbound");
// ModernUO
list.Add($"{"Soulbound"}");
```

### Step 4: Add [InvalidateProperties] to Serialized Fields
If the RunUO property setter called `InvalidateProperties()`, add the attribute:

```csharp
[SerializableField(0)]
[InvalidateProperties]  // Auto-refreshes tooltip
[SerializedCommandProperty(AccessLevel.GameMaster)]
private int _charges;
```

## Before/After Example

**RunUO:**
```csharp
public override void GetProperties(ObjectPropertyList list)
{
    base.GetProperties(list);
    list.Add(1060741, m_Charges.ToString());
    list.Add(1060658, "Map\t" + m_MapDest);
    list.Add(1060659, "Coords\t" + m_PointDest);
    list.Add(1060660, "Creatures\t" + (m_Creatures ? "Yes" : "No"));
    list.Add(1060661, "Range\t" + m_Range);

    if (m_Active)
        list.Add(1060742); // "active"
}
```

**ModernUO:**
```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);
    list.Add(1060741, $"{_charges}");
    list.Add(1060658, $"{"Map"}\t{_mapDest}");
    list.Add(1060659, $"{"Coords"}\t{_pointDest}");
    list.Add(1060660, $"{"Creatures"}\t{(Creatures ? "Yes" : "No")}");
    list.Add(1060661, $"{"Range"}\t{_range}");

    if (_active)
        list.Add(1060742);
}
```

## Edge Cases & Gotchas

### 1. Integer Overload vs String Interpolation
`list.Add(number, int)` exists and is different from `list.Add(number, $"{int}")`. The integer overload passes the raw int; the interpolation formats it as a string. Use whichever matches the cliloc expectation.

### 2. Era-Conditional Properties
Check era when properties differ between expansions:
```csharp
if (Core.ML)
    list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}");
else
    list.Add(1050044, $"{TotalItems}\t{TotalWeight}");
```

### 3. Don't Call InvalidateProperties() in Loops
It triggers hash computation and potential network sends. Batch changes first.

## See Also

- `dev-docs/property-lists.md` — Complete ModernUO property list reference
- `02-serialization.md` — [InvalidateProperties] on serialized fields
- `01-foundation-changes.md` — Foundation changes
