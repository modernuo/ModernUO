# Serialization Migration

## Overview

This is the most impactful migration change. RunUO uses manual `Serialize(GenericWriter)`/`Deserialize(GenericReader)` overrides. ModernUO uses a source generator that automatically produces serialization code from attribute-decorated fields.

## RunUO Pattern

```csharp
using Server;

namespace Server.Items
{
    public class ChargedGem : Item
    {
        private int m_Charges;
        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges { get { return m_Charges; } set { m_Charges = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get { return m_Owner; } set { m_Owner = value; } }

        [Constructable]
        public ChargedGem() : base(0x1EA7)
        {
            m_Charges = 10;
            Weight = 1.0;
        }

        public ChargedGem(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            // Version 1
            writer.Write(m_Owner);

            // Version 0
            writer.Write(m_Charges);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                {
                    m_Owner = reader.ReadMobile();
                    goto case 0;
                }
                case 0:
                {
                    m_Charges = reader.ReadInt();
                    break;
                }
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060741, m_Charges.ToString()); // charges: ~1_val~
        }
    }
}
```

## ModernUO Equivalent

```csharp
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(2, false)]  // Old version was 1 → bump to 2; false because old saves used ReadInt()
public partial class ChargedGem : Item
{
    [SerializableField(0)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [Constructible]
    public ChargedGem() : base(0x1EA7)
    {
        _charges = 10;
        Weight = 1.0;
    }

    // Handles loading saves from BEFORE the SerializationGenerator conversion
    private void Deserialize(IGenericReader reader, int version)
    {
        switch (version)
        {
            case 1:
            {
                _owner = reader.ReadEntity<Mobile>();
                goto case 0;
            }
            case 0:
            {
                _charges = reader.ReadInt();
                break;
            }
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1060741, $"{_charges}"); // charges: ~1_val~
    }
}
```

## Migration Mapping Table

| RunUO | ModernUO | Notes |
|---|---|---|
| `public class Foo : Item` | `public partial class Foo : Item` | Must add `partial` |
| `[Constructable]` | `[Constructible]` | Spelling change |
| `Foo(Serial serial) : base(serial)` | DELETE | Generated automatically |
| `Serialize(GenericWriter writer)` | DELETE | Generated from `[SerializableField]` attributes |
| `Deserialize(GenericReader reader)` | DELETE | Generated from attributes |
| `writer.Write((int)version)` | `[SerializationGenerator(version, false)]` | Version in attribute |
| `private int m_Charges` | `[SerializableField(0)] private int _charges` | Attribute + rename |
| `[CommandProperty(AccessLevel.GM)]` on property | `[SerializedCommandProperty(AccessLevel.GM)]` on field | Moves to field |
| `InvalidateProperties()` in setter | `[InvalidateProperties]` on field | Attribute replaces manual call |
| `GenericWriter` | `IGenericWriter` | Interface now |
| `GenericReader` | `IGenericReader` | Interface now |
| `reader.ReadInt()` | `reader.ReadInt()` | Same for manual cases |
| `reader.ReadMobile()` | `reader.ReadEntity<Mobile>()` | Generic method |
| `reader.ReadItem()` | `reader.ReadEntity<Item>()` | Generic method |
| `writer.Write((int)0)` version | `[SerializationGenerator(N, false)]` | Version bumped +1; `false` because old saves used `ReadInt()` |

## Step-by-Step Conversion

### Step 1: Add Required Using
```csharp
using ModernUO.Serialization;
```

### Step 2: Add Class Attributes and `partial`

Read the old `Serialize()` to find the version number it writes. Bump it by 1 for the `[SerializationGenerator]` first parameter. If the old `Deserialize()` used `reader.ReadInt()` (not `ReadEncodedInt()`), pass `false` as the second parameter.

```csharp
// Change:
public class MyItem : Item
// To (old Serialize wrote version 0, old Deserialize used ReadInt()):
[SerializationGenerator(1, false)]
public partial class MyItem : Item
```

### Step 3: Delete Serial Constructor
Remove `public MyItem(Serial serial) : base(serial) { }` entirely.

### Step 4: Convert Fields
For each field that was serialized in `Serialize()`:

```csharp
// RunUO
private int m_Charges;
[CommandProperty(AccessLevel.GameMaster)]
public int Charges { get { return m_Charges; } set { m_Charges = value; } }

// ModernUO
[SerializableField(0)]  // Index = serialization order
[SerializedCommandProperty(AccessLevel.GameMaster)]
private int _charges;
// Property is auto-generated: public int Charges { get; set; }
```

Add `[InvalidateProperties]` if the RunUO setter called `InvalidateProperties()`:
```csharp
[SerializableField(0)]
[InvalidateProperties]
[SerializedCommandProperty(AccessLevel.GameMaster)]
private int _charges;
```

### Step 5: Migrate Serialize and Deserialize Methods

Delete the `Serialize()` override entirely — the source generator creates it.

For `Deserialize()`: if there are existing saves to support, convert it to `private void Deserialize(IGenericReader reader, int version)` (remove the `override`, change the signature). This method handles loading saves from before the SerializationGenerator conversion. Remove the `base.Deserialize(reader)` call and the version reading line — the generator handles those.

```csharp
// Old RunUO:
public override void Deserialize(GenericReader reader)
{
    base.Deserialize(reader);
    int version = reader.ReadInt();
    m_Charges = reader.ReadInt();
}

// Converted — keeps backward compat with old saves:
private void Deserialize(IGenericReader reader, int version)
{
    _charges = reader.ReadInt();
}
```

If there are no existing saves to worry about (fresh world), you can delete `Deserialize()` entirely.

### Step 6: Change [Constructable] to [Constructible]
```csharp
[Constructible]
public MyItem() : base(0x1234) { }
```

### Step 7: Handle Timer Fields
`TimerExecutionToken` MUST NOT have `[SerializableField]`. Restore timers in `[AfterDeserialization]`:

```csharp
private TimerExecutionToken _timerToken; // NOT serialized

[AfterDeserialization]
private void AfterDeserialization()
{
    Timer.StartTimer(TimeSpan.FromSeconds(5), CheckExpiry, out _timerToken);
}

public override void OnAfterDelete()
{
    _timerToken.Cancel();
    base.OnAfterDelete();
}
```

### Step 8: Handle Custom Property Logic
If a property has non-trivial getter/setter logic, use `[SerializableProperty]` instead:

```csharp
[SerializableProperty(0)]
[CommandProperty(AccessLevel.GameMaster)]
public int MaxItems
{
    get => _maxItems == -1 ? DefaultMaxItems : _maxItems;
    set
    {
        _maxItems = value;
        InvalidateProperties();
        this.MarkDirty();  // REQUIRED in custom setters
    }
}
```

### Step 9: Update GetProperties
Change `ObjectPropertyList` to `IPropertyList`:
```csharp
// RunUO
public override void GetProperties(ObjectPropertyList list)

// ModernUO
public override void GetProperties(IPropertyList list)
```

## Before/After Examples

### Simple Item (No Custom Fields)

**RunUO:**
```csharp
namespace Server.Items
{
    public class SimpleGem : Item
    {
        [Constructable]
        public SimpleGem() : base(0x1EA7)
        {
            Weight = 1.0;
        }

        public SimpleGem(Serial serial) : base(serial) { }

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

namespace Server.Items;

[SerializationGenerator(1, false)]  // Old version was 0 → bump to 1; false because old saves used ReadInt()
public partial class SimpleGem : Item
{
    [Constructible]
    public SimpleGem() : base(0x1EA7)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "a simple gem";

    // Handles loading saves from BEFORE the SerializationGenerator conversion
    private void Deserialize(IGenericReader reader, int version)
    {
        // Version 0 had no custom fields — nothing to read
    }
}
```

### Versioned Item

**RunUO (version 2 — added Owner in v1, Quality in v2):**
```csharp
namespace Server.Items
{
    public class MagicGem : Item
    {
        private int m_Charges;
        private Mobile m_Owner;
        private GemQuality m_Quality;

        [Constructable]
        public MagicGem() : base(0x1EA7)
        {
            m_Charges = 10;
            m_Quality = GemQuality.Rough;
        }

        public MagicGem(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2);
            writer.Write((int)m_Quality);
            writer.Write(m_Owner);
            writer.Write(m_Charges);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    m_Quality = (GemQuality)reader.ReadInt();
                    goto case 1;
                case 1:
                    m_Owner = reader.ReadMobile();
                    goto case 0;
                case 0:
                    m_Charges = reader.ReadInt();
                    break;
            }
        }
    }
}
```

**ModernUO:**
```csharp
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(3, false)]  // Old version was 2 → bump to 3; false because old saves used ReadInt()
public partial class MagicGem : Item
{
    [SerializableField(0)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(2)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private GemQuality _quality;

    [Constructible]
    public MagicGem() : base(0x1EA7)
    {
        _charges = 10;
        _quality = GemQuality.Rough;
    }

    // Handles loading saves from BEFORE the SerializationGenerator conversion
    private void Deserialize(IGenericReader reader, int version)
    {
        switch (version)
        {
            case 2:
            {
                _quality = (GemQuality)reader.ReadInt();
                goto case 1;
            }
            case 1:
            {
                _owner = reader.ReadEntity<Mobile>();
                goto case 0;
            }
            case 0:
            {
                _charges = reader.ReadInt();
                break;
            }
        }
    }
}
```

**Important**: When migrating RunUO code:
- Read the old `Serialize()` to find the version it writes, then bump it by 1 for the `[SerializationGenerator]` first parameter
- Pass `false` as the second parameter if the old `Deserialize()` used `reader.ReadInt()` (not `ReadEncodedInt()`) — this tells the generator how old saves encoded the version number
- Keep the old deserialization logic as `private void Deserialize(IGenericReader reader, int version)` to handle loading pre-codegen saves
- This `Deserialize` method is called automatically for old saves; the generator handles new saves

## Edge Cases & Gotchas

### 1. Save Compatibility
ModernUO's serialization format is completely different from RunUO's. You CANNOT load RunUO saves directly into ModernUO with source-generated serialization. Options:
- Use `[TypeAlias("Old.Namespace.ClassName")]` to map old type names
- Start with a fresh world
- Write a one-time migration tool

### 2. MarkDirty() in Custom Setters
If you use `[SerializableProperty]` with a custom setter, you MUST call `this.MarkDirty()`:
```csharp
set
{
    _value = value;
    this.MarkDirty();  // Required!
}
```
Without this, changes won't be saved.

### 3. Field Ordering
The `[SerializableField(N)]` index determines serialization order. Choose a logical order and don't change it after the first save — or increment the version.

### 4. Conditional Serialization
Use `[SerializableFieldSaveFlag]` and `[SerializableFieldDefault]` to skip default values:
```csharp
[SerializableFieldSaveFlag(0)]
private bool ShouldSerializeMaxItems() => _maxItems != -1;

[SerializableFieldDefault(0)]
private int MaxItemsDefaultValue() => -1;
```

### 5. Collection Fields
Use `[Tidy]` to auto-clean null/deleted entries on deserialization:
```csharp
[Tidy]
[SerializableField(0)]
private List<Mobile> _followers;
```

### 6. DateTime Fields
Use `[DeltaDateTime]` to survive server restarts:
```csharp
[DeltaDateTime]
[SerializableField(0)]
private DateTime _expireTime;
```

### 7. Keeping Manual Serialization (Rare)
Some edge cases still need manual serialization. If a type has complex conditional logic that can't be expressed with attributes, you can implement `ISerializable` manually. But this is rare — try attributes first.

## See Also

- `dev-docs/serialization.md` — Complete ModernUO serialization reference
- `01-foundation-changes.md` — Foundation changes to apply first
- `03-timers.md` — Timer migration (often coupled with serialization)
