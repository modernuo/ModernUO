# ModernUO Serialization System

ModernUO uses a source generator-based serialization system that automatically generates `Serialize()` and `Deserialize()` methods from attribute-decorated fields and properties.

## Overview

The serialization system is provided by two NuGet packages:
- `ModernUO.Serialization.Annotations` - Defines attributes
- `ModernUO.Serialization.Generator` - C# source generator that produces serialization code

Source: https://github.com/modernuo/SerializationGenerator

## Quick Start

### Minimal Serializable Item
```csharp
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class SimpleItem : Item
{
    [Constructible]
    public SimpleItem() : base(0x1234)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "a simple item";
}
```

Key requirements:
1. `using ModernUO.Serialization;` for the attributes
2. `[SerializationGenerator(0)]` on the class
3. `partial` class declaration
4. `[Constructible]` on the parameterless constructor

### Item with Serialized Fields
```csharp
[SerializationGenerator(0)]
public partial class ChargedGem : Item
{
    [SerializableField(0)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    private TimerExecutionToken _glowTimer;  // NOT serialized

    [Constructible]
    public ChargedGem() : base(0x1EA7)
    {
        _charges = Utility.RandomMinMax(5, 15);
        Light = LightType.Circle150;
        Timer.StartTimer(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Glow, out _glowTimer);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.StartTimer(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Glow, out _glowTimer);
    }

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

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1060741, $"{_charges}");  // "charges: ~1_val~"
    }
}
```

---

## Attribute Reference

### [SerializationGenerator(version, encoded)]

**Target**: Class declaration
**Required**: Yes, for any serializable type

| Parameter | Type | Default | Description |
|---|---|---|---|
| `version` | `int` | Required | Current serialization version number |
| `encoded` | `bool` | `true` | How the version was written in old saves. Only needed when migrating from pre-codegen Serialize/Deserialize — pass `false` if old code used `reader.ReadInt()` |

The version number determines which `Deserialize` overload is called. When you add, remove, or reorder fields, increment the version.

For **new classes**, omit the `encoded` parameter:
```csharp
[SerializationGenerator(0)]  // New class — omit encoded
public partial class MyItem : Item { }
```

When **migrating from RunUO/pre-codegen** classes that used `reader.ReadInt()` for version, pass `false` and bump the version:
```csharp
[SerializationGenerator(3, false)]  // Old version was 2 → bump to 3; false because old saves used ReadInt()
public partial class MyItem : Item { }
```

See `dev-docs/runuo-migration-docs/02-serialization.md` for complete migration guidance.

### [SerializableField(index, setter, saveIf)]

**Target**: Private field (`_camelCase`)
**Generates**: Public `PascalCase` property with get/set

| Parameter | Type | Default | Description |
|---|---|---|---|
| `index` | `int` | Required | Serialization order (0-based) |
| `setter` | `string` | `null` (public) | `"private"` or `"internal"` to restrict setter |
| `saveIf` | `string` | `null` | Method name returning bool for conditional save |

```csharp
[SerializableField(0)]                              // Public property
private int _charges;

[SerializableField(1, setter: "private")]            // Private setter
private string _name;

[SerializableField(2, setter: "internal")]           // Internal setter
private DateTime _created;
```

The generated property for `_charges` would be:
```csharp
public int Charges
{
    get => _charges;
    set { _charges = value; this.MarkDirty(); }
}
```

### [SerializableProperty(index, useField)]

**Target**: Property with custom get/set logic
**Use when**: You need non-trivial getter/setter logic

| Parameter | Type | Default | Description |
|---|---|---|---|
| `index` | `int` | Required | Serialization order |
| `useField` | `string` | `null` | Explicit backing field name |

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

### [InvalidateProperties]

**Target**: `[SerializableField]`-decorated field
**Effect**: Calls `InvalidateProperties()` when the property setter is invoked, refreshing the client tooltip.

```csharp
[SerializableField(0)]
[InvalidateProperties]
private int _charges;
// Generated setter calls InvalidateProperties() automatically
```

### [SerializedCommandProperty(accessLevel)]

**Target**: `[SerializableField]`-decorated field
**Effect**: Exposes the generated property to the `[Props` gump for in-game editing.

```csharp
[SerializableField(0)]
[SerializedCommandProperty(AccessLevel.GameMaster)]
private int _charges;
// GMs can view/edit via [Props command
```

Overloads:
- `[SerializedCommandProperty(AccessLevel.GameMaster)]` - Same read/write level
- `[SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]` - Different read/write levels

### [EncodedInt]

**Target**: `int` field or property
**Effect**: Uses variable-length encoding. 1 byte for 0-127, 2 bytes for 128-16383, etc.

Best for fields that are usually small values (counts, IDs, indexes).

### [DeltaDateTime]

**Target**: `DateTime` field
**Effect**: Stores as offset from current time rather than absolute timestamp.

This ensures timers and expiration dates survive server restarts correctly.

```csharp
[DeltaDateTime]
[SerializableField(0)]
private DateTime _expireTime;
```

### [InternString]

**Target**: `string` field
**Effect**: Calls `string.Intern()` on deserialization to deduplicate identical strings in memory.

Best for frequently repeated strings (usernames, template names).

### [Tidy]

**Target**: Collection field (`List<T>`, `Dictionary<K,V>`, etc.)
**Effect**: Removes null and deleted entries from the collection after deserialization.

```csharp
[Tidy]
[SerializableField(0)]
private List<Mobile> _followers;
// After loading, any deleted/null mobiles are removed
```

### [CanBeNull]

**Target**: Any reference-type field
**Effect**: Allows the field to be null during deserialization without error.

```csharp
[CanBeNull]
[SerializableField(0)]
private Mobile _target;
```

### [AfterDeserialization(synchronous)]

**Target**: Parameterless private method
**Effect**: Called after fields are deserialized. The `synchronous` parameter controls execution timing.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `synchronous` | `bool` | `true` | `true` = runs immediately after this entity's deserialization. `false` = runs after ALL entities in the world are deserialized. |

**When to use `true` (default):**
- Restarting timers for this entity
- Setting up derived values from this entity's own fields
- Cleaning up this entity's own collections

**When to use `false` (deferred):**
- Logic that adds or deletes entities (e.g., `Delete()`)
- Logic that depends on other entities being fully loaded (cross-entity references)
- Logic that affects game state

```csharp
// Sync (default) — only touches own fields
[AfterDeserialization]
private void AfterDeserialization()
{
    Timer.StartTimer(TimeSpan.FromSeconds(5), CheckExpiry, out _timerToken);
}

// Deferred — calls Delete() which affects game state
[AfterDeserialization(false)]
private void AfterDeserialization()
{
    if (_expireTimer == null)
    {
        Delete();
    }
}
```

### [DeserializeTimerField(fieldIndex)]

**Target**: Method taking `TimeSpan` parameter
**Effect**: Custom deserialization for Timer fields. The timer is saved as remaining delay.

```csharp
[SerializableField(0, setter: "private")]
private Timer _decayTimer;

[DeserializeTimerField(0)]
private void DeserializeDecayTimer(TimeSpan delay)
{
    _decayTimer = Timer.DelayCall(delay, Delete);
    _decayTimer.Start();
}
```

### [SerializableFieldSaveFlag(fieldIndex)] / [SerializableFieldDefault(fieldIndex)]

**Conditional serialization** -- skip fields that have their default value.

```csharp
[EncodedInt]
[SerializableProperty(0)]
public int MaxItems
{
    get => _maxItems == -1 ? DefaultMaxItems : _maxItems;
    set { _maxItems = value; this.MarkDirty(); }
}

[SerializableFieldSaveFlag(0)]
private bool ShouldSerializeMaxItems() => _maxItems != -1;

[SerializableFieldDefault(0)]
private int MaxItemsDefaultValue() => -1;
```

### [TypeAlias(params string[] aliases)]

**Target**: Class declaration
**Effect**: Maps old type names to this class for deserialization of old saves.

```csharp
[TypeAlias("Server.Mobiles.Bear")]
[SerializationGenerator(0)]
public partial class BlackBear : BaseCreature { }
```

### [Constructible(accessLevel)]

**Target**: Constructor
**Effect**: Marks constructor as available for the `[add` command.

```csharp
[Constructible]                              // Any player can [add
public MyItem() : base(0x1234) { }

[Constructible(AccessLevel.Administrator)]   // Only admins can [add
public SpecialItem() : base(0x5678) { }
```

---

## Version Migration

### When to Increment Version

Increment the version number when you:
- Add a new serialized field
- Remove a serialized field
- Reorder fields (change indexes)
- Change a field's type

### Migration Schema Files

Located in `Projects/Server/Migrations/` and `Projects/UOContent/Migrations/`.
Format: `Namespace.TypeName.vN.json`

Example: `Server.Accounting.Account.v6.json`
```json
{
  "version": 6,
  "type": "Server.Accounting.Account",
  "properties": [
    {
      "name": "Username",
      "type": "string",
      "rule": "PrimitiveTypeMigrationRule",
      "ruleArguments": ["InternString"]
    },
    {
      "name": "Mobiles",
      "type": "Server.Mobile[]",
      "rule": "ArrayMigrationRule",
      "ruleArguments": ["Server.Mobile", "SerializableInterfaceMigrationRule"]
    }
  ]
}
```

### Migration Rule Types
| Rule | Description |
|---|---|
| `PrimitiveTypeMigrationRule` | Basic types: int, string, bool, DateTime, etc. |
| `EnumMigrationRule` | Enum values |
| `ListMigrationRule` | `List<T>` |
| `ArrayMigrationRule` | `T[]` |
| `DictionaryMigrationRule` | `Dictionary<K,V>` |
| `SerializableInterfaceMigrationRule` | Objects implementing ISerializable |
| `SerializationMethodSignatureMigrationRule` | Objects with custom Deserialize methods |

### MigrateFrom Pattern

When you bump the `[SerializationGenerator]` version number, you **must** add a `MigrateFrom` method to handle the transition from the previous version. The serialization generator creates a `VXContent` struct (where X is the previous version number) containing PascalCase properties matching the old serialized fields.

```csharp
// Version bumped from 0 to 1 (added _quality field)
[SerializationGenerator(1)]
public partial class MagicGem : Item
{
    [SerializableField(0)]
    private int _charges;

    [SerializableField(1)]  // New in v1
    private GemQuality _quality;
}

// In MagicGem.Migrations.cs:
public partial class MagicGem
{
    private void MigrateFrom(V0Content content)
    {
        _charges = content.Charges;
        // _quality defaults to GemQuality.Rough (default enum value)
    }
}
```

**Key rules:**
- The method signature is `private void MigrateFrom(VXContent content)` where X is the **previous** version
- `VXContent` is auto-generated with PascalCase properties matching the old fields
- New fields not present in the old version get their default values
- Organize migrations in a separate `.Migrations.cs` partial file for clarity

### Deserialize vs MigrateFrom — Know the Difference

> **WARNING**: Do NOT modify `Deserialize(IGenericReader reader, int version)` when bumping SerializationGenerator versions.

The two mechanisms serve different purposes:

| Method | When It Runs | Purpose |
|---|---|---|
| `Deserialize(IGenericReader reader, int version)` | Loading saves from **before** the class was converted to the SerializationGenerator | One-time legacy migration only |
| `MigrateFrom(VXContent content)` | Transitioning between SerializationGenerator versions | All post-codegen version bumps |

The `Deserialize` function exists **only** to handle deserialization of entities saved before they used `[SerializationGenerator]`. Once a class uses the generator, all future version transitions must use `MigrateFrom`.

---

## Extension Methods

From `Projects/Server/Serialization/ISerializableExtensions.cs`:

```csharp
// Mark entity as dirty (must be called in custom property setters)
entity.MarkDirty();

// Collection operations (auto-mark dirty)
entity.Add(list, value);
entity.Add(dict, key, value);
entity.Remove(list, value);
entity.Clear(list);

// Timer operations (auto-mark dirty)
entity.Stop(timer);
entity.Start(timer);
entity.Restart(timer, delay, interval);
entity.Stop(ref timer);  // Stops and nulls the reference
```

---

## Complete Example: Versioned Item

```csharp
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

public enum GemQuality
{
    Rough,
    Cut,
    Flawless
}

[SerializationGenerator(1)]  // Version 1 (added Quality in v1)
public partial class MagicGem : Item
{
    [SerializableField(0)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    [SerializableField(1)]  // Added in version 1
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private GemQuality _quality;

    private TimerExecutionToken _pulseTimer;

    [Constructible]
    public MagicGem() : base(0x1EA7)
    {
        _charges = Utility.RandomMinMax(5, 15);
        _quality = GemQuality.Rough;
        Weight = 1.0;
        Light = LightType.Circle150;
        StartPulse();
    }

    public override string DefaultName => "a magic gem";

    private void StartPulse()
    {
        Timer.StartTimer(
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(3),
            Pulse,
            out _pulseTimer
        );
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        StartPulse();
    }

    public override void OnAfterDelete()
    {
        _pulseTimer.Cancel();
        base.OnAfterDelete();
    }

    private void Pulse()
    {
        if (_charges <= 0)
        {
            _pulseTimer.Cancel();
            return;
        }

        Effects.SendLocationParticles(this, 0x376A, 9, 10, 5042);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1060741, $"{_charges}");  // charges: ~1_val~
        list.Add($"{"Quality: "}{_quality}");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001);  // Must be in backpack
            return;
        }

        if (_charges <= 0)
        {
            from.SendMessage("The gem is depleted.");
            return;
        }

        _charges--;
        InvalidateProperties();
        this.MarkDirty();
        from.SendMessage("The gem pulses with energy!");
    }
}
```

---

## Key File Locations

| File | Description |
|---|---|
| `Projects/Server/Serialization/ISerializableExtensions.cs` | MarkDirty(), collection helpers |
| `Projects/Server/Migrations/*.v*.json` | Server migration schemas |
| `Projects/UOContent/Migrations/*.v*.json` | Content migration schemas |
| `Projects/UOContent/Mobiles/Animals/Bears/BlackBear.cs` | Simple creature example |
| `Projects/UOContent/Items/Weapons/Ranged/BaseRanged.cs` | Fields + timer token |
| `Projects/UOContent/Items/Special/Solen Items/BagOfSending.cs` | Custom properties |
| `Projects/UOContent/Accounting/Account.cs` | Complex versioned type |
| `Projects/UOContent/Items/Aquarium/Aquarium.cs` | Timer deserialization |
| `Projects/Server/Items/Container.cs` | Conditional serialization |
