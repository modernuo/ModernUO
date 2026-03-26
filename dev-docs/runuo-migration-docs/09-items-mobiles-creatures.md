# Items, Mobiles & Creatures Migration

## Overview

Most RunUO migration work involves converting Item, Mobile, and BaseCreature subclasses. This doc combines all prior system changes (serialization, timers, property lists, naming) into complete step-by-step conversion guides for the most common content types.

## Item Migration Step-by-Step

### 1. Apply Foundation Changes
- File-scoped namespace
- `using ModernUO.Serialization;`
- Rename `m_` fields to `_camelCase`
- `[Constructable]` → `[Constructible]`
- Replace `Console.WriteLine` with logging
- Replace `DateTime.UtcNow` with `Core.Now`

### 2. Add Serialization Attributes
```csharp
[SerializationGenerator(N, false)]  // N = old version + 1; false if old Deserialize used ReadInt()
public partial class MyItem : Item  // Add partial
```

### 3. Convert Fields to [SerializableField]
```csharp
// RunUO
private int m_Charges;
[CommandProperty(AccessLevel.GameMaster)]
public int Charges { get { return m_Charges; } set { m_Charges = value; InvalidateProperties(); } }

// ModernUO
[SerializableField(0)]
[InvalidateProperties]
[SerializedCommandProperty(AccessLevel.GameMaster)]
private int _charges;
// Property auto-generated with InvalidateProperties
```

### 4. Delete Boilerplate
- Delete `public MyItem(Serial serial) : base(serial) { }`
- Delete `public override void Serialize(GenericWriter writer) { ... }`
- Delete `public override void Deserialize(GenericReader reader) { ... }`

### 5. Convert Timer Fields
```csharp
// RunUO
private InternalTimer m_Timer;
// + nested Timer class

// ModernUO
private TimerExecutionToken _timerToken;
// + direct Timer.StartTimer() calls
// + [AfterDeserialization] for timer restoration
// + OnAfterDelete() for timer cancellation
```

### 6. Convert GetProperties
```csharp
// RunUO
public override void GetProperties(ObjectPropertyList list)
{
    base.GetProperties(list);
    list.Add(1060741, m_Charges.ToString());
}

// ModernUO
public override void GetProperties(IPropertyList list)
{
    base.GetProperties(list);
    list.Add(1060741, $"{_charges}");
}
```

### 7. Convert Context Menus (if present)
```csharp
// RunUO
public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
{
    base.GetContextMenuEntries(from, list);
    list.Add(new MyEntry(this));
}

// ModernUO
public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
{
    base.GetContextMenuEntries(from, ref list);
    list.Add(new MyEntry(this));
}
```

## Complete Before/After: Item

**RunUO:**
```csharp
using System;
using Server;
using Server.Network;

namespace Server.Items
{
    public class MagicLantern : Item
    {
        private int m_Charges;
        private Mobile m_Owner;
        private InternalTimer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get { return m_Charges; }
            set { m_Charges = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        [Constructable]
        public MagicLantern() : base(0xA25)
        {
            m_Charges = Utility.RandomMinMax(5, 15);
            Weight = 2.0;
            Light = LightType.Circle300;
            Name = "a magic lantern";

            m_Timer = new InternalTimer(this);
            m_Timer.Start();
        }

        public MagicLantern(Serial serial) : base(serial) { }

        public override void OnDelete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            base.OnDelete();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060741, m_Charges.ToString());
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (m_Charges <= 0)
            {
                from.SendMessage("The lantern is depleted.");
                return;
            }

            m_Charges--;
            InvalidateProperties();
            from.SendMessage("The lantern flares brightly!");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write(m_Owner);
            writer.Write(m_Charges);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    m_Owner = reader.ReadMobile();
                    goto case 0;
                case 0:
                    m_Charges = reader.ReadInt();
                    break;
            }

            m_Timer = new InternalTimer(this);
            m_Timer.Start();
        }

        private void Glow()
        {
            if (m_Charges > 0)
                Effects.SendLocationParticles(this, 0x376A, 9, 10, 5042);
        }

        private class InternalTimer : Timer
        {
            private MagicLantern m_Lantern;

            public InternalTimer(MagicLantern lantern) : base(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3))
            {
                m_Lantern = lantern;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                m_Lantern.Glow();
            }
        }
    }
}
```

**ModernUO:**
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

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1060741, $"{_charges}");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001);
            return;
        }

        if (_charges <= 0)
        {
            from.SendMessage("The lantern is depleted.");
            return;
        }

        Charges--;
        from.SendMessage("The lantern flares brightly!");
    }

    private void Glow()
    {
        if (_charges > 0)
            Effects.SendLocationParticles(this, 0x376A, 9, 10, 5042);
    }
}
```

**What changed:**
- File-scoped namespace
- `partial class` + `[SerializationGenerator(0)]` (omit `encoded` parameter)
- `[Constructable]` → `[Constructible]`
- `m_Charges`/`m_Owner` → `_charges`/`_owner` with `[SerializableField]`
- Manual properties → auto-generated with `[SerializedCommandProperty]`
- `InvalidateProperties()` in setter → `[InvalidateProperties]` attribute
- `Name = "..."` → `DefaultName =>` property override
- Serial constructor deleted
- Serialize/Deserialize deleted
- Nested InternalTimer class → `Timer.StartTimer()` + `TimerExecutionToken`
- Timer in Deserialize → `[AfterDeserialization]`
- `OnDelete()` timer stop → `OnAfterDelete()` + `_token.Cancel()`
- `ObjectPropertyList` → `IPropertyList`
- `GetProperties` uses string interpolation with holes

## BaseCreature Migration

BaseCreature subclasses follow the same pattern as items but have additional considerations.

### Key Differences from Items
1. Constructor calls `base(AIType, FightMode)` instead of `base(itemID)`
2. Stats set with `SetStr()`, `SetDex()`, `SetInt()`, etc.
3. Damage/resistance types set explicitly
4. `GenerateLoot()` override for loot tables
5. Many property overrides (CorpseName, Meat, Hides, etc.)

### Before/After: Simple Creature

**RunUO:**
```csharp
namespace Server.Mobiles
{
    [CorpseName("a wolf corpse")]
    public class ForestWolf : BaseCreature
    {
        [Constructable]
        public ForestWolf() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a forest wolf";
            Body = 225;
            BaseSoundID = 0xE5;

            SetStr(80, 120);
            SetDex(90, 110);
            SetInt(20, 40);

            SetHits(60, 80);

            SetDamage(8, 14);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 35);

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

        public ForestWolf(Serial serial) : base(serial) { }

        public override int Meat { get { return 1; } }
        public override int Hides { get { return 6; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override PackInstinct PackInstinct { get { return PackInstinct.Canine; } }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
```

**ModernUO:**
```csharp
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class ForestWolf : BaseCreature
{
    [Constructible]
    public ForestWolf() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Body = 225;
        BaseSoundID = 0xE5;

        SetStr(80, 120);
        SetDex(90, 110);
        SetInt(20, 40);

        SetHits(60, 80);

        SetDamage(8, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 25, 35);

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
    public override FoodType FavoriteFood => FoodType.Meat;
    public override PackInstinct PackInstinct => PackInstinct.Canine;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}
```

**What changed:**
- `[CorpseName("...")]` attribute → `CorpseName` property override
- `Name = "..."` → `DefaultName` property override
- `BaseCreature(AI, Fight, 10, 1, 0.2, 0.4)` → `BaseCreature(AI, Fight)` (extra params have defaults)
- Expression-bodied property overrides
- Serialization boilerplate removed
- Serial constructor removed

## Key Creature Constructor Differences

```csharp
// RunUO — many parameters
public ForestWolf() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
// 10 = RangePerception, 1 = RangeFight, 0.2 = ActiveSpeed, 0.4 = PassiveSpeed

// ModernUO — simplified (defaults built in)
public ForestWolf() : base(AIType.AI_Melee, FightMode.Closest)
```

The extra parameters (RangePerception, RangeFight, ActiveSpeed, PassiveSpeed) have sensible defaults. Only specify them if they differ from defaults.

## Common Creature Attribute Changes

| RunUO | ModernUO |
|---|---|
| `[CorpseName("a corpse")]` attribute | `public override string CorpseName => "a corpse";` |
| `Name = "a creature"` in constructor | `public override string DefaultName => "a creature";` |
| `get { return value; }` | `=> value;` expression-bodied |

## Item Name Changes

```csharp
// RunUO
Name = "a magic gem";  // Set in constructor

// ModernUO — prefer property overrides
public override string DefaultName => "a magic gem";
// OR for cliloc:
public override int LabelNumber => 1234567;
```

## Equipment/Weapon/Armor Migration

Weapons and armor follow the same item pattern but inherit from specialized base classes:

```csharp
// ModernUO weapon example
[SerializationGenerator(0)]
public partial class MySpecialSword : BaseSword
{
    [Constructible]
    public MySpecialSword() : base(0x13FF) // Katana graphic
    {
        Weight = 6.0;
        Layer = Layer.TwoHanded;
    }

    public override string DefaultName => "a special sword";
    public override int AosStrengthReq => 25;
    public override int AosMinDamage => 11;
    public override int AosMaxDamage => 13;
    public override int AosSpeed => 44;
    public override float MlSpeed => 2.50f;
}
```

## Common Base Classes

| RunUO | ModernUO | Notes |
|---|---|---|
| `BaseWeapon` | `BaseWeapon` | Same, add `partial` |
| `BaseSword` / `BaseMace` / etc. | Same | Same, add `partial` |
| `BaseArmor` | `BaseArmor` | Same, add `partial` |
| `BaseClothing` | `BaseClothing` | Same, add `partial` |
| `BaseJewel` | `BaseJewel` | Same, add `partial` |
| `BaseContainer` | `BaseContainer` | Same, add `partial` |
| `Food` | `Food` | Same, add `partial` |
| `BasePotion` | `BasePotion` | Same, add `partial` |

## Edge Cases & Gotchas

### 1. [TypeAlias] for Save Compatibility
If a class changed namespace or name, use `[TypeAlias]`:
```csharp
[TypeAlias("Server.Items.OldName")]
[SerializationGenerator(0)]
public partial class NewName : Item { }
```

### 2. OnDoubleClick Validation
ModernUO patterns prefer:
```csharp
if (!IsChildOf(from.Backpack))
{
    from.SendLocalizedMessage(1042001);
    return;
}
```

### 3. CorpseName as Property Override
RunUO uses `[CorpseName]` attribute. ModernUO uses a property override instead.

### 4. SetMana(0) for Non-Casters
Always call `SetMana(0)` for creatures that shouldn't have mana.

### 5. Decrement via Generated Property
Use the generated property name (PascalCase) when decrementing to trigger dirty tracking:
```csharp
Charges--;  // Uses generated property — triggers MarkDirty + InvalidateProperties
// NOT: _charges--;  // Bypasses tracking
```

## See Also

- `dev-docs/content-patterns.md` — ModernUO content creation patterns
- `dev-docs/serialization.md` — Serialization system
- `02-serialization.md` — Serialization migration details
- `03-timers.md` — Timer migration
- `06-property-lists.md` — Property list migration
