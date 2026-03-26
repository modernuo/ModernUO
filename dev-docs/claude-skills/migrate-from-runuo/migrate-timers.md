---
name: migrate-timers
description: >
  Trigger: when converting RunUO Timer subclasses, Timer.DelayCall patterns, or TimerPriority usage to ModernUO fire-and-forget timers.
  Covers: Timer subclass elimination, TimerExecutionToken, callback patterns, timer restoration.
---

# RunUO -> ModernUO Timer Migration

## When This Activates
- Converting nested `Timer` subclasses with `OnTick()`
- Converting `Timer.DelayCall()` patterns
- Removing `TimerPriority` usage
- Restoring timers after deserialization

## Conversion Steps
1. Move `OnTick()` logic to a method on the parent class
2. Replace `new InternalTimer(this).Start()` with `Timer.StartTimer(..., callback, out _token)`
3. Add `private TimerExecutionToken _token;` (NOT serialized)
4. Cancel in `OnAfterDelete()`: `_token.Cancel();`
5. Restore in `[AfterDeserialization]`: re-call `Timer.StartTimer(...)`
6. Delete the nested Timer class entirely
7. Remove all `TimerPriority` references

## Quick Mapping
| RunUO | ModernUO |
|---|---|
| `new Timer(delay).Start()` | `Timer.StartTimer(delay, callback)` |
| `new Timer(delay, interval).Start()` | `Timer.StartTimer(delay, interval, callback, out token)` |
| `timer.Stop()` | `token.Cancel()` |
| `Timer.DelayCall(delay, callback)` | `Timer.StartTimer(delay, callback)` |
| `Timer.DelayCall(delay, stateCallback, state)` | `Timer.DelayCall(delay, callback, state)` |
| `TimerPriority.XXX` | Remove -- timer wheel auto-schedules |
| Timer started in `Deserialize()` | `[AfterDeserialization]` method |

## Anti-Patterns
- Serializing `TimerExecutionToken` -- it's a struct tracking a pooled timer
- Starting timers in `Deserialize()` -- world isn't loaded yet, use `[AfterDeserialization]`
- Lambda closures on hot paths -- use `Timer.DelayCall` with state parameters instead

## See Also
- `dev-docs/runuo-migration-docs/03-timers.md` -- detailed migration reference
- `dev-docs/timers.md` -- complete ModernUO timer system
- `dev-docs/claude-skills/modernuo-timers.md` -- ModernUO timer skill
