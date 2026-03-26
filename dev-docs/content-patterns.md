# ModernUO Content Creation Patterns

This document covers the patterns and templates for creating game content in ModernUO: items, creatures, spells, skills, loot, context menus, and file organization.

## Table of Contents
1. [New Item](#new-item)
2. [New Creature](#new-creature)
3. [New Spell](#new-spell)
4. [Skill Implementation](#skill-implementation)
5. [Loot System](#loot-system)
6. [Context Menus](#context-menus)
7. [Entity Lifecycle](#entity-lifecycle)
8. [File Organization](#file-organization)

---

## New Item

### Minimal Item
```csharp
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class SimpleItem : Item
{
    [Constructible]
    public SimpleItem() : base(0x1234)  // itemID from UO art
    {
        Weight = 1.0;
    }

    public override string DefaultName => "a simple item";
    // OR: public override int LabelNumber => 1234567;  // cliloc number
}
```

### Item with Properties and Behavior
```csharp
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class MagicLantern : Item
{
    [SerializableField(0)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    private TimerExecutionToken _glowTimer;

    [Constructible]
    public MagicLantern() : base(0xA25)
    {
        _charges = Utility.RandomMinMax(5, 15);
        Weight = 2.0;
        Light = LightType.Circle300;
        StartGlow();
    }

    public override string DefaultName => "a magic lantern";

    private void StartGlow()
    {
        Timer.StartTimer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), Glow, out _glowTimer);
    }

    [AfterDeserialization]
    private void AfterDeserialization() => StartGlow();

    public override void OnAfterDelete()
    {
        _glowTimer.Cancel();
        base.OnAfterDelete();
    }

    private void Glow()
    {
        if (_charges > 0)
            Effects.SendLocationParticles(this, 0x376A, 9, 10, 5042);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001);  // Must be in your backpack
            return;
        }

        if (_charges <= 0)
        {
            from.SendMessage("The lantern is depleted.");
            return;
        }

        Charges--;
        from.SendMessage("The lantern flares brightly!");
        from.FixedParticles(0x376A, 9, 32, 5042, EffectLayer.Waist);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1060741, $"{_charges}");  // "charges: ~1_val~"
    }
}
```

### Common Item Base Classes
| Base Class | Use For |
|---|---|
| `Item` | Generic items |
| `BaseWeapon` | Melee weapons |
| `BaseRanged` | Ranged weapons (bows, crossbows) |
| `BaseArmor` | Armor pieces |
| `BaseShield` | Shields |
| `BaseClothing` | Wearable clothing |
| `BaseJewel` | Rings, bracelets, necklaces |
| `BaseContainer` | Containers (bags, boxes) |
| `BasePotion` | Potions |
| `BaseReagent` | Spell reagents |
| `Food` | Edible items |
| `SpellScroll` | Spell scrolls |

### Key Item Properties
```csharp
Weight = 1.0;              // Item weight in stones
Stackable = true;          // Can stack with same type
Amount = 1;                // Stack amount
Movable = true;            // Can be picked up
Visible = true;            // Visible to players
Hue = 0;                   // Color hue (0 = default)
Light = LightType.Circle300; // Light emission
LootType = LootType.Regular; // Regular, Newbied, Blessed, Cursed
Layer = Layer.OneHanded;   // Equipment layer
```

---

## New Creature

### Basic Creature
```csharp
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class ForestWolf : BaseCreature
{
    [Constructible]
    public ForestWolf() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Body = 225;          // Wolf body graphic
        BaseSoundID = 0xE5;  // Base sound ID

        SetStr(80, 120);
        SetDex(90, 110);
        SetInt(20, 40);

        SetHits(60, 80);
        SetMana(0);

        SetDamage(8, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 25, 35);
        SetResistance(ResistanceType.Fire, 5, 10);
        SetResistance(ResistanceType.Cold, 15, 25);
        SetResistance(ResistanceType.Poison, 10, 15);
        SetResistance(ResistanceType.Energy, 5, 10);

        SetSkill(SkillName.MagicResist, 30.0, 50.0);
        SetSkill(SkillName.Tactics, 50.0, 70.0);
        SetSkill(SkillName.Wrestling, 50.0, 70.0);

        Fame = 600;
        Karma = 0;

        VirtualArmor = 28;

        Tamable = true;
        ControlSlots = 1;
        MinTameSkill = 50.1;
    }

    public override string CorpseName => "a wolf corpse";
    public override string DefaultName => "a forest wolf";
    public override int Meat => 1;
    public override int Hides => 6;
    public override HideType HideType => HideType.Regular;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override PackInstinct PackInstinct => PackInstinct.Canine;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}
```

### AI Types
| AIType | Use For |
|---|---|
| `AI_Melee` | Warriors, melee fighters |
| `AI_Mage` | Spellcasters |
| `AI_Archer` | Ranged attackers |
| `AI_Animal` | Passive animals (flee/fight back) |
| `AI_Predator` | Hunting animals |
| `AI_Healer` | Healing NPCs |
| `AI_Vendor` | Shop NPCs |
| `AI_Berserk` | Mindless aggressors |
| `AI_Thief` | Pickpockets |

### Fight Modes
| FightMode | Behavior |
|---|---|
| `None` | Never attacks |
| `Aggressor` | Only retaliates |
| `Strongest` | Targets highest-stat enemy |
| `Weakest` | Targets lowest-stat enemy |
| `Closest` | Targets nearest enemy |
| `Evil` | Attacks aggressors or evil-karma targets |

### Creature Stats Guide
| Creature Level | Str | Dex | Int | Hits | Damage | Fame |
|---|---|---|---|---|---|---|
| Weak | 30-60 | 30-50 | 10-20 | 20-40 | 2-6 | 100-300 |
| Average | 80-120 | 60-90 | 20-40 | 60-100 | 6-14 | 500-1500 |
| Strong | 150-250 | 80-120 | 50-100 | 120-200 | 12-22 | 2000-5000 |
| Elite | 300-500 | 100-150 | 100-200 | 250-500 | 18-30 | 5000-15000 |
| Boss | 500-1000 | 150-250 | 200-400 | 500-2000 | 25-40 | 15000+ |

### Optional Creature Overrides
```csharp
public override Poison PoisonImmune => Poison.Regular;   // Poison immunity
public override Poison HitPoison => Poison.Lesser;       // Melee poison
public override double HitPoisonChance => 0.2;           // 20% poison chance
public override bool CanRummageCorpses => true;           // Loots corpses
public override bool BardImmune => true;                  // Cannot be provoked/peaced
public override bool Unprovokable => true;                // Cannot be provoked
public override bool CanFly => true;                      // Can fly
public override int TreasureMapLevel => 3;               // Drops treasure map
public override double WeaponAbilityChance => 0.4;        // Weapon ability chance
```

---

## New Spell

### Targeted Damage Spell (Magery)
```csharp
using System;
using Server.Targeting;

namespace Server.Spells.Third;

public class FireballSpellCustom : MagerySpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Fireball",             // Name
        "Vas Flam",             // Mantra
        212,                    // Cast animation
        9041,                   // Cast sound
        Reagent.BlackPearl      // Reagents (comma-separated)
    );

    public FireballSpellCustom(Mobile caster, Item scroll = null) : base(caster, scroll, _info) { }

    public override SpellCircle Circle => SpellCircle.Third;
    public override bool DelayedDamage => true;

    public void Target(Mobile m)
    {
        if (CheckHSequence(m))  // Harmful spell sequence check
        {
            var source = Caster;
            SpellHelper.Turn(source, m);
            SpellHelper.CheckReflect((int)Circle, ref source, ref m);

            double damage;
            if (Core.AOS)
            {
                damage = GetNewAosDamage(19, 1, 5, m);
            }
            else
            {
                damage = Utility.Random(10, 7);
                if (CheckResisted(m))
                {
                    damage *= 0.75;
                    m.SendLocalizedMessage(501783);  // You resist
                }
                damage *= GetDamageScalar(m);
            }

            source.MovingParticles(m, 0x36D4, 7, 0, false, true, 9502, 4019, 0x160);
            source.PlaySound(0x15E);

            // Damage types must sum to 100
            SpellHelper.Damage(this, m, damage, 0, 100, 0, 0, 0);
            //                                    phys fire cold pois energy
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }
}
```

### Spell Helper Methods
```csharp
SpellHelper.Turn(caster, target);           // Face target
SpellHelper.CheckReflect(circle, ref source, ref target);  // Magic reflect
SpellHelper.Damage(spell, target, damage, phys, fire, cold, poison, energy);
SpellHelper.AddStatCurse(caster, target, stat);
SpellHelper.AddStatBonus(caster, target, stat);
SpellHelper.CanRevealCaster(spell);

CheckHSequence(target);    // Harmful spell checks (LOS, range, criminal)
CheckBSequence(target);    // Beneficial spell checks
CheckResisted(target);     // Resistance check
GetNewAosDamage(bonus, dice, sides, target);  // AOS damage formula
GetDamageScalar(target);   // Pre-AOS damage multiplier
```

### Spell Circles (Magery)
| Circle | Mana | Base Delay |
|---|---|---|
| First | 4 | 0.25s + circle |
| Second | 6 | 0.50s + circle |
| Third | 9 | 0.75s + circle |
| Fourth | 11 | 1.00s + circle |
| Fifth | 14 | 1.25s + circle |
| Sixth | 20 | 1.50s + circle |
| Seventh | 40 | 1.75s + circle |
| Eighth | 50 | 2.00s + circle |

---

## Skill Implementation

### Registering a Skill Handler
```csharp
namespace Server.SkillHandlers;

public static class MySkillHandler
{
    public static void Initialize()
    {
        SkillInfo.Table[(int)SkillName.Tracking].Callback = OnUse;
    }

    public static TimeSpan OnUse(Mobile from)
    {
        from.SendMessage("You begin tracking...");
        from.Target = new TrackingTarget();
        return TimeSpan.FromSeconds(10.0);  // Cooldown
    }
}
```

### Skill Check
```csharp
// Difficulty-based check (with skill gain chance)
if (from.CheckSkill(SkillName.Mining, 0.0, 100.0))
{
    // Success
}

// Direct chance check
if (from.CheckSkill(SkillName.Hiding, minSkill: 25.0, maxSkill: 75.0))
{
    // Success
}
```

### SkillName Enum (58 skills)
Key skills: `Alchemy`, `Anatomy`, `AnimalLore`, `AnimalTaming`, `Archery`, `ArmsLore`, `Begging`, `Blacksmith`, `Bushido`, `Camping`, `Carpentry`, `Cartography`, `Chivalry`, `Cooking`, `DetectHidden`, `Discordance`, `EvalInt`, `Fencing`, `Fishing`, `Fletching`, `Focus`, `Forensics`, `Healing`, `Herding`, `Hiding`, `Inscribe`, `ItemID`, `Lockpicking`, `Lumberjacking`, `Macing`, `Magery`, `MagicResist`, `Meditation`, `Mining`, `Musicianship`, `Necromancy`, `Ninjitsu`, `Parry`, `Peacemaking`, `Poisoning`, `Provocation`, `RemoveTrap`, `Snooping`, `Spellweaving`, `SpiritSpeak`, `Stealing`, `Stealth`, `Swords`, `Tactics`, `Tailoring`, `TasteID`, `Tinkering`, `Tracking`, `Veterinary`, `Wrestling`

---

## Loot System

### Using Predefined Packs
```csharp
public override void GenerateLoot()
{
    AddLoot(LootPack.Poor);        // ~50g equivalent
    AddLoot(LootPack.Meager);      // ~100g equivalent
    AddLoot(LootPack.Average);     // ~250g equivalent
    AddLoot(LootPack.Rich);        // ~500g equivalent
    AddLoot(LootPack.FilthyRich);  // ~1000g equivalent
    AddLoot(LootPack.UltraRich);   // ~2000g equivalent
    AddLoot(LootPack.SuperBoss);   // Boss-level loot

    // Auxiliary packs
    AddLoot(LootPack.Gems, 2);     // 2 random gems
    AddLoot(LootPack.Potions);     // Random potion
    AddLoot(LootPack.LowScrolls);  // Low circle scroll
    AddLoot(LootPack.MedScrolls);  // Med circle scroll
    AddLoot(LootPack.HighScrolls); // High circle scroll
}
```

Packs auto-select era-appropriate loot (Pre-AOS, AOS, SE variants).

### Specific Items
```csharp
PackItem(new Arrow(Utility.RandomMinMax(20, 40)));
PackGold(100, 200);
PackItem(new Bandage(Utility.RandomMinMax(5, 10)));
```

---

## Context Menus

```csharp
public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
{
    base.GetContextMenuEntries(from, ref list);

    if (from.Alive && from.InRange(this, 2))
    {
        list.Add(new RepairEntry(this));
    }
}

private class RepairEntry : ContextMenuEntry
{
    private readonly Item _item;

    public RepairEntry(Item item) : base(6100)  // Cliloc number
    {
        _item = item;
        Enabled = item is { Deleted: false };
    }

    public override void OnClick(Mobile from, IEntity target)
    {
        if (_item.Deleted || !from.InRange(_item, 2))
            return;

        from.SendMessage("You repair the item.");
    }
}
```

---

## Entity Lifecycle

### Two-Phase Deletion
```csharp
// Phase 1: Pre-removal cleanup
public override void OnDelete()
{
    _timerToken.Cancel();       // Cancel managed timers
    // Remove from tracking systems
    base.OnDelete();
}

// Phase 2: Post-removal cleanup
public override void OnAfterDelete()
{
    _timer?.Stop();             // Stop Timer references
    _timer = null;
    _owner = null;              // Null Item/Mobile refs
    base.OnAfterDelete();
}
```

### OnDoubleClick Validation
```csharp
public override void OnDoubleClick(Mobile from)
{
    if (!IsChildOf(from.Backpack))
    {
        from.SendLocalizedMessage(1042001);  // Must be in backpack
        return;
    }

    if (!from.InRange(GetWorldLocation(), 2))
    {
        from.SendLocalizedMessage(500446);  // Too far away
        return;
    }

    // Item logic here
}
```

---

## File Organization

```
Projects/UOContent/
├── Items/
│   ├── Weapons/Swords/       # Swords
│   ├── Weapons/Maces/        # Maces
│   ├── Weapons/Ranged/       # Bows, crossbows
│   ├── Armor/Plate/          # Plate armor
│   ├── Armor/Chain/          # Chain armor
│   ├── Armor/Leather/        # Leather armor
│   ├── Clothing/             # Wearable clothing
│   ├── Containers/           # Bags, boxes
│   ├── Misc/                 # General items
│   ├── Special/              # Unique/quest items
│   └── Resources/            # Crafting materials
├── Mobiles/
│   ├── Animals/Bears/        # Bears (BlackBear, GrizzlyBear)
│   ├── Animals/Birds/        # Birds
│   ├── Monsters/AOS/         # AOS-era monsters
│   ├── Monsters/SE/          # SE-era monsters
│   ├── Monsters/ML/          # ML-era monsters
│   ├── Special/              # Champions, bosses
│   ├── Vendors/              # NPC vendors
│   └── Townfolk/             # NPCs
├── Spells/
│   ├── Base/                 # Spell base classes
│   ├── First/ - Eighth/     # Magery circles
│   ├── Necromancy/           # Necromancer spells
│   ├── Chivalry/             # Paladin spells
│   ├── Bushido/              # Samurai abilities
│   ├── Ninjitsu/             # Ninja abilities
│   └── Spellweaving/         # Spellweaving
├── Skills/                   # Skill handlers
├── Gumps/                    # UI dialogs
│   └── Base/                 # Gump base classes
├── Engines/                  # Complex systems
│   ├── Craft/                # Crafting system
│   ├── CannedEvil/           # Champion spawns
│   ├── Factions/             # Faction system
│   └── Quests/               # Quest system
└── Misc/                     # Miscellaneous
    └── LootPack.cs           # Loot tables
```

### Naming Rules
- File name = class name
- One primary class per file
- Group related items in subdirectories
- Era-specific content goes in era-named subdirectories
