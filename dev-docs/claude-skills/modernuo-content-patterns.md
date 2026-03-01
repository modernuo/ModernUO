---
name: modernuo-content-patterns
description: >
  Trigger when creating new items, mobiles, creatures, spells, skills, loot tables, or any game content under Projects/UOContent/. This is the hub skill that connects to all other ModernUO skills.
---

# ModernUO Content Patterns (Hub Skill)

## When This Activates
- Creating new items, weapons, armor, clothing, containers
- Creating new creatures, NPCs, vendors
- Creating new spells
- Implementing skill handlers
- Adding loot tables
- Adding context menus
- Any new game content under `Projects/UOContent/`

## Key Rules

1. **Always ask target era** if the user hasn't specified (see `modernuo-era-expansion.md`)
2. **All serializable classes must be `partial`** with `[SerializationGenerator]`
3. **All Item/Mobile constructors need `[Constructible]`**
4. **Clean up timers and references in `OnDelete()`/`OnAfterDelete()`**
5. **No LINQ** in game logic -- use loops and `PooledRefList<T>`
6. **File placement** matters -- follow the directory conventions below

## New Item Template

```csharp
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MyItem : Item
{
    [Constructible]
    public MyItem() : base(0x1234)  // itemID from art
    {
        Weight = 1.0;
        // Stackable = true;  // if stackable
        // Amount = 1;         // if stackable
    }

    public override string DefaultName => "a my item";
    // OR: public override int LabelNumber => 1234567;  // cliloc number

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // Must be in backpack
            return;
        }

        // Item use logic
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        // list.Add(1060741, $"{_charges}");  // charges: ~1_val~
    }
}
```

## New Creature Template

```csharp
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class MyCreature : BaseCreature
{
    [Constructible]
    public MyCreature() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Body = 0;           // Body graphic ID
        BaseSoundID = 0;    // Base sound ID

        SetStr(100, 150);   // Strength min/max
        SetDex(80, 100);    // Dexterity min/max
        SetInt(30, 50);     // Intelligence min/max

        SetHits(80, 120);
        SetMana(0);

        SetDamage(8, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 30, 40);
        SetResistance(ResistanceType.Fire, 10, 20);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 15, 25);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.MagicResist, 30.0, 50.0);
        SetSkill(SkillName.Tactics, 50.0, 70.0);
        SetSkill(SkillName.Wrestling, 50.0, 70.0);

        Fame = 1000;
        Karma = -1000;  // Negative = evil, positive = good, 0 = neutral

        VirtualArmor = 30;
    }

    public override string CorpseName => "a creature corpse";
    public override string DefaultName => "a creature";

    // Optional overrides:
    // public override int Meat => 1;
    // public override int Hides => 8;
    // public override HideType HideType => HideType.Regular;
    // public override FoodType FavoriteFood => FoodType.Meat;
    // public override PackInstinct PackInstinct => PackInstinct.Canine;
    // public override bool CanRummageCorpses => true;
    // public override Poison PoisonImmune => Poison.Lesser;
    // public override Poison HitPoison => Poison.Regular;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Gems, 1);
        // PackItem(new SpecificItem());
        // PackGold(50, 100);
    }
}
```

### Tameable Creature Additions
```csharp
// In constructor:
Tamable = true;
ControlSlots = 1;      // 1-5, how many pet slots it uses
MinTameSkill = 35.1;   // Required Animal Taming skill
```

### AI Types
| AIType | Behavior |
|---|---|
| `AI_Melee` | Charges into melee combat |
| `AI_Mage` | Casts spells, keeps distance |
| `AI_Archer` | Uses ranged attacks |
| `AI_Animal` | Passive, flees or fights back |
| `AI_Predator` | Hunts other creatures |
| `AI_Healer` | Heals allies |
| `AI_Vendor` | NPC vendor behavior |
| `AI_Berserk` | Aggressive, attacks everything |
| `AI_Thief` | Steals from players |

### Fight Modes
| FightMode | Target Selection |
|---|---|
| `None` | Never attacks |
| `Aggressor` | Only attacks those who attack first |
| `Strongest` | Targets highest stats |
| `Weakest` | Targets lowest stats |
| `Closest` | Targets nearest entity |
| `Evil` | Attacks aggressors or negative-karma entities |

## New Spell Template

```csharp
using System;
using Server.Targeting;

namespace Server.Spells.First;

public class MySpell : MagerySpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Spell Name",      // Display name
        "In Vas Ort",       // Power words (mantra)
        212,                // Cast animation action
        9041,               // Cast sound
        Reagent.Bloodmoss,  // Required reagents
        Reagent.MandrakeRoot
    );

    public MySpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public void Target(Mobile m)
    {
        if (CheckHSequence(m))  // Harmful spell check
        {
            SpellHelper.Turn(Caster, m);

            double damage = GetNewAosDamage(10, 1, 4, m);

            SpellHelper.Damage(this, m, damage, 0, 100, 0, 0, 0);
            // Damage types: phys, fire, cold, poison, energy (must sum to 100)
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }
}
```

### Spell Circles (Magery)
| Circle | Mana Cost | Min Skill |
|---|---|---|
| First | 4 | -50.0 (Pre-ML) / -46.0 (ML+) |
| Second | 6 | -30.0 / -32.0 |
| Third | 9 | 0.0 / -18.0 |
| Fourth | 11 | 10.0 / -4.0 |
| Fifth | 14 | 20.0 / 10.0 |
| Sixth | 20 | 30.0 / 24.0 |
| Seventh | 40 | 40.0 / 38.0 |
| Eighth | 50 | 50.0 / 52.0 |

## Skill Implementation

```csharp
using Server.Targeting;

namespace Server.Skills;

public static class MySkillHandler
{
    public static void Initialize()
    {
        // Register handler delegate
        SkillInfo.Table[(int)SkillName.Alchemy].Callback = OnUse;
    }

    public static TimeSpan OnUse(Mobile from)
    {
        from.SendMessage("You begin working...");
        from.Target = new InternalTarget();
        return TimeSpan.FromSeconds(1.0);  // Delay before next use
    }

    private class InternalTarget : Target
    {
        public InternalTarget() : base(2, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item item)
            {
                // from.CheckSkill(SkillName.Alchemy, minSkill, maxSkill)
                if (from.CheckSkill(SkillName.Alchemy, 0.0, 100.0))
                {
                    from.SendMessage("Success!");
                }
                else
                {
                    from.SendMessage("You fail.");
                }
            }
        }
    }
}
```

## Loot Packs

Use predefined packs -- they auto-select era-appropriate loot:
```csharp
public override void GenerateLoot()
{
    AddLoot(LootPack.Poor);       // ~50 gold equivalent
    AddLoot(LootPack.Meager);     // ~100 gold equivalent
    AddLoot(LootPack.Average);    // ~250 gold equivalent
    AddLoot(LootPack.Rich);       // ~500 gold equivalent
    AddLoot(LootPack.FilthyRich); // ~1000 gold equivalent
    AddLoot(LootPack.UltraRich);  // ~2000 gold equivalent
    AddLoot(LootPack.SuperBoss);  // Boss-level loot

    AddLoot(LootPack.Gems, 2);    // 2 random gems
    AddLoot(LootPack.Potions);    // Random potion

    // Specific items
    PackItem(new Arrow(Utility.RandomMinMax(20, 40)));
    PackGold(100, 200);
}
```

## Context Menus

```csharp
public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
{
    base.GetContextMenuEntries(from, ref list);

    if (from.Alive && from.InRange(this, 2))
    {
        list.Add(new MyContextMenuEntry(this));
    }
}

private class MyContextMenuEntry : ContextMenuEntry
{
    private readonly Item _item;

    public MyContextMenuEntry(Item item) : base(6100)  // Cliloc number
    {
        _item = item;
    }

    public override void OnClick(Mobile from, IEntity target)
    {
        // Handle click
    }
}
```

## Two-Phase Deletion

```csharp
public override void OnDelete()
{
    _timerToken.Cancel();   // Cancel timers FIRST
    base.OnDelete();
}

public override void OnAfterDelete()
{
    _timer?.Stop();         // Stop Timer references
    _timer = null;
    _owner = null;          // Clear Mobile/Item references
    base.OnAfterDelete();
}
```

## File Placement

| Content Type | Directory |
|---|---|
| Items | `Projects/UOContent/Items/{Category}/` |
| Weapons | `Projects/UOContent/Items/Weapons/{Type}/` |
| Armor | `Projects/UOContent/Items/Armor/{Type}/` |
| Creatures | `Projects/UOContent/Mobiles/{Type}/` |
| Animals | `Projects/UOContent/Mobiles/Animals/{Species}/` |
| Monsters | `Projects/UOContent/Mobiles/Monsters/{Era}/` |
| Spells | `Projects/UOContent/Spells/{School}/` |
| Skills | `Projects/UOContent/Skills/` |
| Engines | `Projects/UOContent/Engines/{SystemName}/` |
| Gumps | `Projects/UOContent/Gumps/` |

## Real Examples
- Simple creature: `Projects/UOContent/Mobiles/Animals/Bears/BlackBear.cs`
- Boss creature: `Projects/UOContent/Mobiles/Special/Barracoon.cs`
- SE creature: `Projects/UOContent/Mobiles/Monsters/SE/RaiJu.cs`
- Spell: `Projects/UOContent/Spells/First/MagicArrow.cs`
- Skill check: `Projects/UOContent/Skills/SkillCheck.cs`
- Loot packs: `Projects/UOContent/Misc/LootPack.cs`

## See Also
- `dev-docs/claude-skills/modernuo-serialization.md` - Serialization details
- `dev-docs/claude-skills/modernuo-era-expansion.md` - Era-conditional code
- `dev-docs/claude-skills/modernuo-timers.md` - Timer patterns
- `dev-docs/claude-skills/modernuo-property-lists.md` - Item tooltips
- `dev-docs/claude-skills/modernuo-gump-system.md` - UI dialogs
- `dev-docs/claude-skills/modernuo-commands-targeting.md` - Commands and targeting
- `dev-docs/claude-skills/modernuo-events.md` - Event system
- `dev-docs/content-patterns.md` - Full content documentation
