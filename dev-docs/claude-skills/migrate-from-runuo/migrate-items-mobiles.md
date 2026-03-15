---
name: migrate-items-mobiles
description: >
  Trigger: when converting RunUO Item, Mobile, or BaseCreature subclasses to ModernUO. Most common migration task.
  Covers: complete item/creature conversion combining serialization, timers, properties, naming.
---

# RunUO -> ModernUO Item/Mobile/Creature Migration

## When This Activates
- Converting any `Item` subclass from RunUO
- Converting any `Mobile`/`BaseCreature` subclass
- This is the most common migration task -- combines all other systems

## Item Conversion Checklist
1. [ ] Foundation: file-scoped namespace, `using ModernUO.Serialization;`
2. [ ] Class: add `[SerializationGenerator(N, false)]` (N = old version + 1, `false` if old `Deserialize` used `ReadInt()`), add `partial`
3. [ ] Fields: `m_X` -> `[SerializableField(N)] _x` with `[SerializedCommandProperty]`
4. [ ] Add `[InvalidateProperties]` where RunUO setter called `InvalidateProperties()`
5. [ ] Delete: Serial constructor, Serialize, Deserialize
6. [ ] `[Constructable]` -> `[Constructible]`
7. [ ] `Name = "text"` -> `public override string DefaultName => "text";`
8. [ ] Timers: nested class -> `Timer.StartTimer()` + `TimerExecutionToken` + `[AfterDeserialization]` + `OnAfterDelete`
9. [ ] Properties: `GetProperties(ObjectPropertyList)` -> `GetProperties(IPropertyList)`, apply string hole rule
10. [ ] Context menus: `List<ContextMenuEntry>` -> `ref PooledRefList<ContextMenuEntry>`

## Creature-Specific Changes
- `[CorpseName("...")]` attribute -> `public override string CorpseName => "...";`
- `BaseCreature(AI, Fight, 10, 1, 0.2, 0.4)` -> `BaseCreature(AI, Fight)` (extra params default)
- `Name = "text"` -> `public override string DefaultName => "text";`
- Expression-bodied overrides: `public override int Meat { get { return 1; } }` -> `public override int Meat => 1;`

## Anti-Patterns
- Using `_field--` instead of `Property--` (bypasses MarkDirty tracking)
- Forgetting `[AfterDeserialization]` for timer restoration
- Forgetting `OnAfterDelete()` for timer cancellation

## See Also
- `dev-docs/runuo-migration-docs/09-items-mobiles-creatures.md` -- detailed migration with before/after examples
- `dev-docs/content-patterns.md` -- ModernUO content templates
- `dev-docs/claude-skills/modernuo-content-patterns.md` -- ModernUO content skill
- `dev-docs/claude-skills/modernuo-serialization.md` -- serialization patterns
- `dev-docs/claude-skills/modernuo-timers.md` -- timer patterns
