---
name: migrate-commands-events
description: >
  Trigger: when converting RunUO command registration, EventSink handlers, or event delegate patterns.
  Covers: event name changes, delegate removal, Configure vs Initialize.
---

# RunUO -> ModernUO Commands & Events Migration

## When This Activates
- Converting `EventSink` subscriptions
- Converting event handler signatures
- Moving from `Initialize()` to `Configure()`
- Converting WorldSave/WorldLoad events to GenericPersistence

## Conversion Steps
1. Change `Initialize()` to `Configure()` for event registration
2. Remove delegate constructors: `new LoginEventHandler(OnLogin)` -> `OnLogin`
3. Rename events: `Login` -> `Connected`, `Logout` -> `Disconnected`
4. Update signatures: `OnLogin(LoginEventArgs e)` -> `OnConnected(Mobile m)`
5. WorldSave persistence -> convert to `GenericPersistence` (see migrate-persistence skill)

## Event Mapping
| RunUO | ModernUO |
|---|---|
| `EventSink.Login` | `EventSink.Connected` (Action<Mobile>) |
| `EventSink.Logout` | `EventSink.Disconnected` (Action<Mobile>) |
| `EventSink.WorldSave` | `EventSink.WorldSave` (Action -- no args) |
| `EventSink.Crashed` | `EventSink.ServerCrashed` |
| `new XXXEventHandler(method)` | `method` (direct reference) |

## Commands (Minimal Changes)
Commands use the same `CommandSystem.Register()` API. Main change: use `Configure()` for registration.

## See Also
- `dev-docs/runuo-migration-docs/07-commands-events.md` -- detailed migration reference
- `dev-docs/events.md` -- complete ModernUO event system
- `dev-docs/claude-skills/modernuo-events.md` -- ModernUO events skill
- `dev-docs/claude-skills/modernuo-commands-targeting.md` -- ModernUO commands skill
