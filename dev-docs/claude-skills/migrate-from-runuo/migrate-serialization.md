---
name: migrate-serialization
description: >
  Trigger: when converting RunUO Serialize/Deserialize methods, adding [SerializableField], converting [Constructable] to [Constructible], or migrating manual serialization code.
  Covers: source-generated serialization, field conversion, version handling, TypeAlias.
---

# RunUO -> ModernUO Serialization Migration

## When This Activates
- Converting `Serialize(GenericWriter)`/`Deserialize(GenericReader)` overrides
- Converting `[Constructable]` to `[Constructible]`
- Adding `[SerializableField]` attributes
- Handling save compatibility with `[TypeAlias]`

## Conversion Steps
1. Add `using ModernUO.Serialization;`
2. Add `[SerializationGenerator(0, false)]` to class (version 0, false for Item/Mobile)
3. Add `partial` to class declaration
4. Convert each serialized field: `private int m_X` -> `[SerializableField(N)] private int _x`
5. Add `[SerializedCommandProperty(AccessLevel.X)]` if RunUO had `[CommandProperty]`
6. Add `[InvalidateProperties]` if setter called `InvalidateProperties()`
7. DELETE the `Serial` constructor
8. DELETE `Serialize()` and `Deserialize()` overrides
9. Change `[Constructable]` to `[Constructible]`
10. Timer fields: leave unserialized, add `[AfterDeserialization]` method

## Quick Mapping
| RunUO | ModernUO |
|---|---|
| `public class Foo : Item` | `[SerializationGenerator(0, false)] public partial class Foo : Item` |
| `private int m_X` + manual Serialize | `[SerializableField(0)] private int _x` |
| `[CommandProperty(GM)]` on property | `[SerializedCommandProperty(GM)]` on field |
| `Foo(Serial serial) : base(serial)` | DELETE |
| `Serialize(GenericWriter)` | DELETE -- auto-generated |
| `Deserialize(GenericReader)` | DELETE -- auto-generated |
| Custom setter with InvalidateProperties() | `[InvalidateProperties]` attribute |
| Custom setter logic | `[SerializableProperty(N)]` with `this.MarkDirty()` |
| `reader.ReadMobile()` | `reader.ReadEntity<Mobile>()` |
| `reader.ReadItem()` | `reader.ReadEntity<Item>()` |

## Anti-Patterns
- Missing `partial` keyword -> build error
- Serializing `TimerExecutionToken` -> build error
- Missing `this.MarkDirty()` in `[SerializableProperty]` setter -> changes not saved
- Wrong field prefix (`m_` instead of `_`)

## See Also
- `dev-docs/runuo-migration-docs/02-serialization.md` -- detailed migration reference with before/after
- `dev-docs/serialization.md` -- complete ModernUO serialization system
- `dev-docs/claude-skills/modernuo-serialization.md` -- ModernUO serialization skill (patterns, attributes, examples)
