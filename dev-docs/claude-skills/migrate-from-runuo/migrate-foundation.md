---
name: migrate-foundation
description: >
  Trigger: when migrating ANY RunUO code to ModernUO. Always load this skill first.
  Covers: namespace changes, naming conventions, attribute renames, logging, threading, performance.
---

# RunUO -> ModernUO Foundation Migration

## When This Activates
- Converting ANY RunUO 2.7 script to ModernUO
- Always apply these changes FIRST before system-specific migration

## Universal Changes Checklist
1. File-scoped namespace: `namespace X { ... }` -> `namespace X;`
2. `using ModernUO.Serialization;` -- add for any serializable type
3. Rename fields: `m_FieldName` -> `_fieldName`
4. `[Constructable]` -> `[Constructible]`
5. `Console.WriteLine` -> `LogFactory.GetLogger(typeof(X))` -> `logger.Information(...)`
6. `DateTime.UtcNow` -> `Core.Now`
7. `World.Mobiles`/`World.Items` iteration -> spatial queries (`map.GetMobilesInRange<T>()`)
8. Remove `lock`, `volatile`, `ConcurrentDictionary`, `Mutex` -- server is single-threaded
9. Remove `Task.Run`, `new Thread` -- use `Timer.StartTimer()` instead
10. `ArrayPool<T>.Shared` -> `STArrayPool<T>.Shared`
11. `new List<T>()` on hot paths -> `PooledRefList<T>.Create()`
12. Modernize property syntax: `{ get { return x; } }` -> `{ get => x; }`
13. Delete `MyType(Serial serial) : base(serial)` constructor -- auto-generated
14. `Name = "text"` in constructor -> `public override string DefaultName => "text";`

## Quick Reference
| RunUO | ModernUO |
|---|---|
| `[Constructable]` | `[Constructible]` |
| `m_Field` | `_field` |
| `Console.WriteLine(...)` | `logger.Information(...)` |
| `DateTime.UtcNow` | `Core.Now` |
| `MyItem(Serial serial) : base(serial)` | DELETE |
| `lock (_obj) { }` | Remove entirely |
| `ConcurrentDictionary` | `Dictionary` |
| `ArrayPool<T>.Shared` | `STArrayPool<T>.Shared` |

## Anti-Patterns
- Don't rename existing `m_` fields in code you're not otherwise migrating
- Don't add threading constructs -- everything is single-threaded
- Don't use allocating LINQ (`.ToList()`, `.GroupBy()`, etc.) on hot paths

## See Also
- `dev-docs/runuo-migration-docs/01-foundation-changes.md` -- complete foundation changes reference
- `dev-docs/code-standards.md` -- ModernUO coding standards and LINQ tiers
- `dev-docs/threading-model.md` -- Why single-threaded, what's allowed
