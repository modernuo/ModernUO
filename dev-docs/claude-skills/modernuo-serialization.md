---
name: modernuo-serialization
description: >
  Trigger when creating or modifying classes inheriting Item, Mobile, BaseCreature, or any type with [SerializationGenerator]. When adding serialized fields. When discussing migration or version bumps.
---

# ModernUO Serialization System

## When This Activates
- Creating/modifying classes that inherit `Item`, `Mobile`, `BaseCreature`, or any serializable type
- Adding `[SerializableField]` or `[SerializableProperty]` attributes
- Bumping serialization versions
- Working with migration schemas
- Discussing save/load behavior

## Key Rules

1. **Always use `partial` class** when applying `[SerializationGenerator]`
2. **Always add `[Constructible]`** on parameterless constructors for Items/Mobiles
3. **Never serialize `TimerExecutionToken`** -- restore timers in `[AfterDeserialization]`
4. **Call `this.MarkDirty()`** in custom property setters that modify serialized state
5. **Use `using ModernUO.Serialization;`** for serialization attributes
6. **Field order matters** -- `[SerializableField(N)]` index determines serialization order
7. **Increment version** when adding, removing, or reordering fields

## Core Attributes

### [SerializationGenerator(version, encoded)]
Applied to class. Generates Serialize/Deserialize methods.
- `version`: Current serialization version (0+)
- `encoded`: Omit for new classes. When migrating from pre-codegen Serialize/Deserialize, pass `false` if old code used `reader.ReadInt()` (not `ReadEncodedInt()`)

```csharp
// New class — omit encoded
[SerializationGenerator(0)]
public partial class MyItem : Item { }

// Migration from pre-codegen — old version was 2, used ReadInt()
[SerializationGenerator(3, false)]
public partial class MigratedItem : Item { }
```

### [SerializableField(index, setter, saveIf)]
Applied to `_camelCase` private fields. Generates `PascalCase` property.
- `index`: Serialization order (0+)
- `setter`: Access level -- `"private"`, `"internal"`, or omit for public
- `saveIf`: Condition method name for conditional serialization

```csharp
[SerializableField(0)]
[SerializedCommandProperty(AccessLevel.GameMaster)]
private int _charges;
// Generates: public int Charges { get; set; }
```

### [SerializableProperty(index, useField)]
Applied to properties with custom get/set logic.
- `index`: Serialization order
- `useField`: Backing field name if auto-detection fails

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
        this.MarkDirty();
    }
}
```

### [InvalidateProperties]
On serialized fields -- auto-calls `InvalidateProperties()` when field changes (refreshes client tooltip).

```csharp
[SerializableField(0)]
[InvalidateProperties]
[SerializedCommandProperty(AccessLevel.GameMaster)]
private bool _balanced;
```

### [SerializedCommandProperty(accessLevel)]
Exposes field to `[Props` gump for in-game editing.

### [EncodedInt]
Variable-length int encoding (saves space for small values).

### [DeltaDateTime]
Stores DateTime as offset from current time (handles server restarts).

### [InternString]
Interns strings to reduce memory for repeated values.

### [Tidy]
Auto-removes null/deleted entries from collections after deserialization.

### [CanBeNull]
Marks field as nullable during deserialization.

### [AfterDeserialization(synchronous)]
Method called after fields are deserialized. The `synchronous` parameter controls timing:
- `true` (default): runs immediately after this entity's deserialization
- `false`: runs after ALL entities in the world are deserialized

Use `true` (default) for: restarting timers, setting up derived values from own fields.
Use `false` for: logic that calls `Delete()`, depends on other entities, or affects game state.

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
Custom timer deserialization. Timer is saved as remaining TimeSpan.

```csharp
[SerializableField(0, setter: "private")]
private Timer _evaluateTimer;

[DeserializeTimerField(0)]
private void DeserializeEvaluateTimer(TimeSpan delay)
{
    _evaluateTimer = Timer.DelayCall(delay, EvaluationInterval, Evaluate);
}
```

### [SerializableFieldSaveFlag(fieldIndex)] / [SerializableFieldDefault(fieldIndex)]
Conditional serialization -- skip fields with default values.

```csharp
[SerializableFieldSaveFlag(0)]
private bool ShouldSerializeMaxItems() => _maxItems != -1;

[SerializableFieldDefault(0)]
private int MaxItemsDefaultValue() => -1;
```

### [TypeAlias(aliases)]
Maps old type names for backward-compatible deserialization.

```csharp
[TypeAlias("Server.Mobiles.Bear")]
[SerializationGenerator(0)]
public partial class BlackBear : BaseCreature { }
```

## Patterns

### Minimal Item (Version 0, No Custom Fields)
```csharp
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class MyItem : Item
{
    [Constructible]
    public MyItem() : base(0x1234)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "a my item";
}
```

### Item with Fields
```csharp
[SerializationGenerator(0)]
public partial class ChargedItem : Item
{
    [SerializableField(0)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    private TimerExecutionToken _timerToken;  // NOT serialized

    [Constructible]
    public ChargedItem() : base(0x1234) => _charges = 10;

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
}
```

### Item with Custom Properties
```csharp
[SerializationGenerator(2)]
public partial class BagOfSending : Item
{
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public BagOfSendingHue BagOfSendingHue
    {
        get => _bagOfSendingHue;
        set
        {
            _bagOfSendingHue = value;
            Hue = value switch
            {
                BagOfSendingHue.Yellow => 0x8A5,
                BagOfSendingHue.Blue => 0x8AD,
                BagOfSendingHue.Red => 0x89B,
                _ => Hue
            };
            this.MarkDirty();
        }
    }
}
```

## Custom Serialize/Deserialize (Purity Rules)

When writing custom `Serialize(IGenericWriter)` or `Deserialize(IGenericReader)` methods (e.g. for `GenericPersistence` subclasses), the following rules apply:

### Serialize() MUST remain pure
`Serialize()` is called from **background serialization threads** during world saves (see `SerializationThreadWorker`). Multiple entities are serialized in parallel across threads. This means `Serialize()` must NOT:

- **Create or destroy Items/Mobiles** -- mutates shared world state
- **Move, equip, or unequip Items/Mobiles** -- mutates shared world state
- **Start or stop timers** (`Timer.StartTimer`, `Timer.DelayCall`, `_token.Cancel()`) -- timers are NOT thread-safe
- **Send packets or modify NetState** -- networking is game-thread-only
- **Access or modify other entities' mutable state** -- data race
- **Call `Delete()`** on anything -- triggers deletion cascades on wrong thread

`Serialize()` should ONLY read fields and write them to the `IGenericWriter`. Treat it as a read-only snapshot.

```csharp
// CORRECT -- pure reads and writes only
public override void Serialize(IGenericWriter writer)
{
    writer.WriteEncodedInt(0); // version
    writer.WriteEncodedInt(_records.Count);
    foreach (var (key, value) in _records)
    {
        writer.Write(key);
        writer.Write(value);
    }
}

// WRONG -- side effects in Serialize
public override void Serialize(IGenericWriter writer)
{
    CleanupExpiredEntries();     // BAD: mutates state
    Timer.StartTimer(Recheck);  // BAD: not thread-safe
    writer.Write(_data);
}
```

### Deserialize() runs on the game thread
`Deserialize()` runs during world load on the main thread, so it CAN create entities and start timers. However, prefer `[AfterDeserialization]` for timer setup to keep deserialization clean.

## MigrateFrom Pattern

When bumping the `[SerializationGenerator]` version, you **must** add a `MigrateFrom` method:

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

- Signature: `private void MigrateFrom(VXContent content)` where X is the **previous** version
- `VXContent` is auto-generated with PascalCase properties matching the old fields
- New fields not in the old version get their default values
- Use `.Migrations.cs` partial files for organization

## Anti-Patterns

- **Missing `partial`**: `[SerializationGenerator]` requires `partial class`
- **Serializing timers**: `TimerExecutionToken` cannot be serialized
- **Side effects in `Serialize()`**: Serialize runs on background threads -- must be pure (no creating/destroying entities, no timer start/stop, no packets)
- **Missing `MarkDirty()`**: Custom property setters must call `this.MarkDirty()`
- **Wrong field prefix**: Use `_camelCase`, not `m_camelCase` for new fields
- **Forgetting `[Constructible]`**: Items/Mobiles need this for `[add` command
- **Modifying `Deserialize(reader, version)` for version bumps**: `Deserialize` exists ONLY for pre-codegen legacy saves. Use `MigrateFrom(VXContent)` for all post-codegen version transitions.

## Real Examples
- Simple creature: `Projects/UOContent/Mobiles/Animals/Bears/BlackBear.cs`
- Serialized fields + timer: `Projects/UOContent/Items/Weapons/Ranged/BaseRanged.cs`
- Custom properties: `Projects/UOContent/Items/Special/Solen Items/BagOfSending.cs`
- Complex with AfterDeserialization: `Projects/UOContent/Accounting/Account.cs`
- Timer deserialization: `Projects/UOContent/Items/Aquarium/Aquarium.cs`
- Tidy + DeltaDateTime: `Projects/UOContent/Engines/CannedEvil/ChampionSpawn.cs`
- Conditional serialization: `Projects/Server/Items/Container.cs`

## Version Migration
Migration schemas are JSON files in `Projects/Server/Migrations/` and `Projects/UOContent/Migrations/`:
- Format: `TypeName.vN.json`
- Generated automatically by the serialization generator
- Used for reading old save formats

External reference: https://github.com/modernuo/SerializationGenerator

## See Also
- `dev-docs/serialization.md` - Complete serialization documentation
- `dev-docs/claude-skills/modernuo-timers.md` - Timer token patterns
- `dev-docs/claude-skills/modernuo-content-patterns.md` - Item/Mobile templates
- `dev-docs/claude-skills/modernuo-property-lists.md` - [InvalidateProperties] usage
