# Migrating from RunUO to ModernUO — Overview

## Why Migration Is Needed

RunUO 2.7 targets .NET Framework (Windows-only, end-of-life). ModernUO targets .NET 10 (cross-platform, actively supported). Beyond the runtime, ModernUO has rewritten or replaced nearly every major subsystem for performance, safety, and maintainability.

## High-Level Architecture Differences

| Aspect | RunUO 2.7 | ModernUO |
|---|---|---|
| Runtime | .NET Framework 4.x (Windows) | .NET 10 (cross-platform) |
| Project structure | `Server/` + `Scripts/` (dynamic compilation) | `Projects/Server/` + `Projects/UOContent/` (compiled project) |
| Serialization | Manual `Serialize(GenericWriter)`/`Deserialize(GenericReader)` overrides | Source-generated via `[SerializationGenerator]` + `[SerializableField]` |
| Timers | `Timer` subclass instances with `Start()`/`Stop()` | Fire-and-forget `Timer.StartTimer()` + `TimerExecutionToken` |
| Packets | `Packet` class hierarchy with `PacketWriter` | Static `Create*Packet(Span<byte>)` methods + `SpanWriter` |
| Gumps | `Gump` with entry lists in constructor | `StaticGump<T>`/`DynamicGump` with builder pattern |
| Property lists | `ObjectPropertyList` directly | `IPropertyList` interface |
| Persistence | `EventSink.WorldSave`/`WorldLoad` with manual binary files | `GenericPersistence`/`GenericEntityPersistence<T>` |
| Events | `EventSink` with delegate types (`WorldSaveEventHandler`) | `EventSink` with `Action<T>` delegates |
| Configuration | XML/cfg files | JSON (`modernuo.json`, `JsonConfig`) |
| Commands | `CommandSystem.Register` | Same, but handler conventions updated |
| Naming | `m_` private fields | `_camelCase` private fields |
| Logging | `Console.WriteLine` | `LogFactory.GetLogger()` → `logger.Information()` |
| Threading | Locks, volatile, concurrent collections | Single-threaded game loop (forbidden) |
| Collections | `new List<T>()` everywhere | `PooledRefList<T>`, `STArrayPool<T>` |

## Approach to Migration

1. **Foundation changes first** — namespace style, naming, usings, logging, attributes
2. **Serialization** — the biggest change; convert `Serialize`/`Deserialize` to source-generated
3. **System-specific changes** — timers, gumps, packets, events, persistence
4. **Content fixes** — item/mobile/creature adjustments
5. **Testing** — compile, load world, verify in-game behavior

## How These Docs Are Organized

| Doc | Content |
|---|---|
| `01-foundation-changes.md` | Universal changes for ALL scripts |
| `02-serialization.md` | Source-generated serialization system |
| `03-timers.md` | Timer.StartTimer + TimerExecutionToken |
| `04-gumps.md` | StaticGump/DynamicGump builder pattern |
| `05-packets-networking.md` | SpanWriter/SpanReader packet system |
| `06-property-lists.md` | IPropertyList + string hole rule |
| `07-commands-events.md` | Command registration + EventSink changes |
| `08-persistence.md` | GenericPersistence system |
| `09-items-mobiles-creatures.md` | Content migration patterns |
| `10-systems-engines.md` | Multi-file system migration |
| `11-api-reference.md` | Comprehensive API mapping table |

## Key Terminology Changes

| RunUO Term | ModernUO Term |
|---|---|
| `Scripts/` directory | `Projects/UOContent/` directory |
| `[Constructable]` | `[Constructible]` |
| `GenericWriter` / `GenericReader` | `IGenericWriter` / `IGenericReader` |
| `PacketWriter` / `PacketReader` | `SpanWriter` / `SpanReader` |
| `ObjectPropertyList` (parameter) | `IPropertyList` (parameter) |
| `BinaryFileWriter` | `GenericPersistence` base class |
| `EventSink.WorldSave += new WorldSaveEventHandler(Save)` | `GenericPersistence` subclass |
| `m_FieldName` | `_fieldName` |

## See Also

- ModernUO wiki: [Migrating from RunUO](https://github.com/modernuo/ModernUO/wiki/4.-Migrating-From-RunUO)
- ModernUO dev-docs: `dev-docs/` (system-specific documentation)
- SerializationGenerator: https://github.com/modernuo/SerializationGenerator
