# ModernUO Era & Expansion System

This document covers ModernUO's expansion system, era checks, and how to write era-conditional code.

## Overview

ModernUO supports all Ultima Online expansions from the original game through Endless Journey. The server's target expansion is configured at startup and affects gameplay mechanics, damage formulas, loot tables, skill systems, and more.

## Expansion Enum

Defined in `Projects/Server/ExpansionInfo.cs`:

```csharp
public enum Expansion
{
    None,    // 0 - Original UO (pre-T2A)
    T2A,     // 1 - The Second Age (October 1998)
    UOR,     // 2 - Renaissance (May 2000)
    UOTD,    // 3 - Third Dawn (March 2001)
    LBR,     // 4 - Lord Blackthorn's Revenge (February 2002)
    AOS,     // 5 - Age of Shadows (February 2003)
    SE,      // 6 - Samurai Empire (November 2004)
    ML,      // 7 - Mondain's Legacy (August 2005)
    SA,      // 8 - Stygian Abyss (September 2009)
    HS,      // 9 - High Seas (October 2010)
    TOL,     // 10 - Time of Legends (October 2015)
    EJ       // 11 - Endless Journey (March 2018)
}
```

## Era Check Properties

Each property returns `true` when `Core.Expansion >= that expansion`:

```csharp
Core.Expansion  // Exact expansion (Expansion enum value)
Core.T2A        // bool: >= The Second Age
Core.UOR        // bool: >= Renaissance
Core.UOTD       // bool: >= Third Dawn
Core.LBR        // bool: >= Blackthorn's Revenge
Core.AOS        // bool: >= Age of Shadows
Core.SE         // bool: >= Samurai Empire
Core.ML         // bool: >= Mondain's Legacy
Core.SA         // bool: >= Stygian Abyss
Core.HS         // bool: >= High Seas
Core.TOL        // bool: >= Time of Legends
Core.EJ         // bool: >= Endless Journey
```

## Major Era Boundaries

### Pre-AOS (None through LBR)
- Simple damage model: flat random damage
- Skill-based resistance
- No item properties system
- Simple loot tables

### AOS (Age of Shadows) -- The Big Divide
AOS fundamentally changed UO's combat and item systems:
- **Resistance system**: 5 damage types (Physical, Fire, Cold, Poison, Energy) each with resistances
- **Item properties**: Magic items with bonus properties (hit chance, damage increase, etc.)
- **New damage formula**: `GetNewAosDamage()` replaces flat random
- **Luck system**: Affects magic item quality from loot
- **Insurance**: Players can insure items against loss
- **Necromancy**: New spell school
- **Chivalry**: New spell school (Paladin)

### SE (Samurai Empire)
- **Bushido/Ninjitsu**: Two new skill/spell schools
- **Adjusted loot packs**: `SePoor`, `SeMeager`, `SeAverage`, etc.
- **Reduced hit delay**: `Core.SE ? 250 : Core.AOS ? 500 : 1000`

### ML (Mondain's Legacy)
- **Spellweaving**: New magic school
- **Adjusted skill requirements**: Different min skill values for spells
- **Container display**: Shows weight limits in tooltips
- **Stat gain changes**: Faster stat gain option

### SA (Stygian Abyss)
- **Gargoyle race**: New playable race
- **Mysticism/Throwing**: New skills
- **Extended mobile status**: Additional stats in status bar

### HS (High Seas)
- **Ship combat**: Naval warfare system
- **Extended status bar**: More stats visible

## Writing Era-Conditional Code

### Simple Value Selection
```csharp
// Ternary chain (most common pattern)
var delay = Core.SE ? 250 : Core.AOS ? 500 : 1000;
var statMax = Core.LBR ? 125 : 100;
```

### Logic Branching
```csharp
if (Core.AOS)
{
    // AOS+ damage formula
    damage = GetNewAosDamage(10, 1, 4, target);
}
else
{
    // Pre-AOS damage formula
    damage = Utility.Random(4, 4);
    if (CheckResisted(target))
    {
        damage *= 0.75;
        target.SendLocalizedMessage(501783);
    }
    damage *= GetDamageScalar(target);
}
```

### Display Branching
```csharp
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);

    if (Core.ML)
    {
        // ML+ shows full container info
        list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}");
    }
    else
    {
        // Pre-ML shows basic info
        list.Add(1050044, $"{TotalItems}\t{TotalWeight}");
    }
}
```

### Skill Requirements by Era
```csharp
// From MagerySpell.cs - spell difficulty varies by era
private static readonly double[] _requiredSkill = Core.ML
    ? new[] { -46.0, -32.0, -18.0, -4.0, 10.0, 24.0, 38.0, 52.0, 66.0, 80.0 }
    : new[] { -50.0, -30.0, 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0 };
```

### Loot by Era
LootPack properties auto-select era-appropriate variants:
```csharp
LootPack.Poor       // Selects: OldPoor / AosPoor / SePoor
LootPack.Meager     // Selects: OldMeager / AosMeager / SeMeager
LootPack.Average    // etc.
LootPack.Rich
LootPack.FilthyRich
LootPack.UltraRich
LootPack.SuperBoss
```

## Configuration

Expansion is set in `Distribution/Configuration/expansion.json`:
```json
{
  "expansion": "ML"
}
```

Full expansion metadata is in `Distribution/Data/expansions.json`, containing:
- Required client version
- Supported feature flags
- Character list flags
- Housing flags
- Mobile status version
- Map selection flags

## Best Practices

1. **Always ask the user** which expansion to target if not specified
2. **Test both branches** when writing era-conditional code
3. **Use `Core.XYZ` properties** (not `Core.Expansion >= Expansion.XYZ`)
4. **Chain ternaries** from newest to oldest for value selection
5. **Document era requirements** in comments when the logic is complex
6. **Use era-aware LootPack** properties instead of hardcoding specific era packs

## Key File References
- Expansion enum: `Projects/Server/ExpansionInfo.cs`
- Core properties: `Projects/Server/Core.cs`
- Expansion data: `Distribution/Data/expansions.json`
- Loot packs: `Projects/UOContent/Misc/LootPack.cs`
- Spell circles: `Projects/UOContent/Spells/Base/MagerySpell.cs`
- Skill check: `Projects/UOContent/Skills/SkillCheck.cs`
