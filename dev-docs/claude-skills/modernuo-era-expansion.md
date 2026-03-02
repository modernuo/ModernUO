---
name: modernuo-era-expansion
description: >
  Trigger when writing era-conditional code, using Core.AOS/SE/ML etc., or when user hasn't specified target era. Always ask which expansion to target if not specified.
---

# ModernUO Era & Expansion Support

## When This Activates
- Writing code that depends on game era (damage formulas, skill caps, mechanics)
- Using `Core.AOS`, `Core.SE`, `Core.ML`, etc.
- User asks for a feature without specifying era
- Implementing mechanics that changed across expansions

## CRITICAL RULE
**Never assume era.** If the user hasn't specified which expansion to target, ASK before writing era-dependent code.

## Expansion Enum

```csharp
public enum Expansion
{
    None,    // 0 - Pre-T2A
    T2A,     // 1 - The Second Age
    UOR,     // 2 - Renaissance
    UOTD,    // 3 - Third Dawn
    LBR,     // 4 - Blackthorn's Revenge
    AOS,     // 5 - Age of Shadows (major overhaul)
    SE,      // 6 - Samurai Empire
    ML,      // 7 - Mondain's Legacy
    SA,      // 8 - Stygian Abyss
    HS,      // 9 - High Seas
    TOL,     // 10 - Time of Legends
    EJ       // 11 - Endless Journey
}
```

## Era Check Properties

Each returns `true` when `Core.Expansion >= that era`:

```csharp
Core.T2A   // >= The Second Age
Core.UOR   // >= Renaissance
Core.UOTD  // >= Third Dawn
Core.LBR   // >= Blackthorn's Revenge
Core.AOS   // >= Age of Shadows
Core.SE    // >= Samurai Empire
Core.ML    // >= Mondain's Legacy
Core.SA    // >= Stygian Abyss
Core.HS    // >= High Seas
Core.TOL   // >= Time of Legends
Core.EJ    // >= Endless Journey
```

For exact expansion: `Core.Expansion == Expansion.AOS`

## Key Era Boundaries

### AOS (Age of Shadows) -- Most Significant Change
- Complete combat overhaul: resistance-based damage system
- Property-based item system (magic properties)
- New damage formula: `GetNewAosDamage()` vs flat `Utility.Random()`
- Luck system for loot
- Insurance system

### SE (Samurai Empire)
- Bushido / Ninjitsu skills
- Samurai/Ninja classes
- Loot pack adjustments

### ML (Mondain's Legacy)
- Spellweaving
- Adjusted skill gain chances
- Container weight display changes

## Pattern: Era-Conditional Code

```csharp
// Ternary for simple values
var delay = Core.SE ? 250 : Core.AOS ? 500 : 1000;

// If/else for logic branches
if (Core.AOS)
{
    damage = GetNewAosDamage(10, 1, 4, target);
}
else
{
    damage = Utility.Random(4, 4);
    if (CheckResisted(target))
        damage *= 0.75;
}

// Property display varies by era
if (Core.ML)
{
    list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}");
}
else
{
    list.Add(1050044, $"{TotalItems}\t{TotalWeight}");
}
```

## Pattern: Era-Dependent Loot

`LootPack` properties auto-select based on expansion:
```csharp
// These automatically pick the right era variant
LootPack.Poor       // OldPoor / AosPoor / SePoor
LootPack.Average    // OldAverage / AosAverage / SeAverage
LootPack.Rich       // OldRich / AosRich / SeRich
LootPack.FilthyRich // OldFilthyRich / AosFilthyRich / SeFilthyRich
LootPack.UltraRich  // OldUltraRich / AosUltraRich / SeUltraRich
```

## Anti-Patterns

- **Hardcoding mechanics for one era** without conditional checks
- **Assuming AOS** when the user might want pre-AOS
- **Using era-specific APIs** without checking (e.g., `GetNewAosDamage()` pre-AOS)

## Real Examples
- Era-conditional damage: `Projects/UOContent/Spells/First/MagicArrow.cs`
- Era-conditional properties: `Projects/Server/Items/Container.cs` (`GetProperties`)
- Era-conditional values: `Projects/UOContent/Items/Weapons/Ranged/BaseRanged.cs`
- Expansion enum: `Projects/Server/ExpansionInfo.cs`
- Stat config by era: `Projects/UOContent/Skills/SkillCheck.cs`

## See Also
- `dev-docs/era-expansion.md` - Complete expansion documentation
- `dev-docs/claude-skills/modernuo-content-patterns.md` - Content templates
- `dev-docs/claude-skills/modernuo-configuration.md` - Configuration system
