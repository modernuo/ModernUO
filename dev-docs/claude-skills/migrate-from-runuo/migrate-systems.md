---
name: migrate-systems
description: >
  Trigger: when converting multi-file RunUO engines or systems (crafting, spawners, economy, quests) to ModernUO.
  Covers: system mapping, conversion order, file organization, cross-reference handling.
---

# RunUO -> ModernUO Multi-File System Migration

## When This Activates
- Converting RunUO systems with multiple interdependent files
- Converting custom engines (crafting, spawners, economy, quests)
- Organizing RunUO `Scripts/Custom/` code into ModernUO structure

## Conversion Order
1. **Data types / enums** -- Just naming and namespace changes
2. **Persistence classes** -- `EventSink.WorldSave` -> `GenericPersistence`
3. **Core entities (Items/Mobiles)** -- Full serialization migration
4. **Gumps** -- Convert to `DynamicGump`/`StaticGump`
5. **Commands** -- Usually minimal changes
6. **Packets** -- Convert to `SpanWriter` if custom packets exist
7. **Entry point** -- Update Configure/Initialize registration

## File Organization
| RunUO | ModernUO |
|---|---|
| `Scripts/Custom/MySystem/` | `Projects/UOContent/Engines/MySystem/` or `Projects/UOContent/Systems/MySystem/` |
| `Scripts/Items/X.cs` | `Projects/UOContent/Items/{Category}/X.cs` |
| `Scripts/Mobiles/X.cs` | `Projects/UOContent/Mobiles/{Category}/X.cs` |
| `Scripts/Gumps/X.cs` | `Projects/UOContent/Gumps/X.cs` |

## Configuration Migration
| RunUO | ModernUO |
|---|---|
| XML config files | `ServerConfiguration.GetOrUpdateSetting()` or `JsonConfig` |
| Custom .cfg parsing | `ServerConfiguration` for simple, `JsonConfig` for complex |

## Testing
After converting: `dotnet build`, fix errors, test [add for items, verify gumps, check persistence across save/restart.

## See Also
- `dev-docs/runuo-migration-docs/10-systems-engines.md` -- detailed system migration patterns
- `dev-docs/configuration.md` -- ModernUO configuration system
- `dev-docs/claude-skills/modernuo-configuration.md` -- ModernUO configuration skill
- All other migrate-* skills for system-specific guidance
