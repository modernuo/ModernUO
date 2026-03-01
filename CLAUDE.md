# ModernUO

.NET 10 Ultima Online server emulator. Single-threaded game loop. All game logic runs on one thread.

- **Server engine**: `Projects/Server/` — do NOT modify without explicit request
- **Game content**: `Projects/UOContent/` — primary editing target
- **Build**: `dotnet build` from repo root

## Code Audit Rules

Apply these when writing or reviewing `.cs` files under `Projects/`.

1. **LINQ** — Tier 1 (zero-cost patterns) free on hot paths; Tier 2 (low overhead) OK on warm paths; Tier 3 (allocating) forbidden on hot paths → `dev-docs/code-standards.md`
2. **No `Console.WriteLine`** — use `LogFactory.GetLogger(typeof(MyClass))` → `logger.Information(...)` (requires `using Server.Logging;`)
3. **No concurrency primitives** — no `lock`, `volatile`, `ConcurrentDictionary`, `Mutex`, etc. Server is single-threaded.
4. **No `World.Mobiles`/`World.Items` iteration** — use spatial queries: `map.GetMobilesInRange<T>()`, `map.GetItemsInRange<T>()`
5. **Clean up refs in `OnDelete()`/`OnAfterDelete()`** — null out `Item`/`Mobile` references
6. **Cancel timers in `OnDelete()`/`OnAfterDelete()`** — call `_token.Cancel()` or `_timer?.Stop()`
7. **`STArrayPool<T>.Shared`** not `ArrayPool<T>.Shared` — single-threaded optimized, no locks
8. **`PooledRefList<T>`** not `new List<T>()` on hot paths — zero GC pressure, stack-allocated ref struct
9. **Serialization** — class must be `partial`, constructor needs `[Constructible]`, `TimerExecutionToken` must NOT have `[SerializableField]`
10. **No `Task.Run`/`new Thread()`** in game code — game logic is single-threaded event loop
11. **Never assume era** — if code uses `Core.AOS`/`Core.SE`/etc., ask which expansion to target
12. **Naming** — `_camelCase` private fields, `PascalCase` properties/methods/classes; don't flag legacy `m_` but use `_` for new code
13. **No empty gumps** — every gump must produce visual elements. An empty gump leaks on client+server (no way to close it). Use static `DisplayTo()` to validate before constructing → `dev-docs/gump-system.md`
14. **PropertyList string literals must be holes** — `$"{"Map"}\t{value}"` not `$"Map\t{value}"`. The handler treats bare text as delimiters, `{}` holes as arguments. Only `\t` should be a bare literal → `dev-docs/property-lists.md`

## Dev-Docs Reference

| Topic | File |
|---|---|
| Code standards & LINQ tiers | `dev-docs/code-standards.md` |
| Serialization system | `dev-docs/serialization.md` |
| Content patterns (Items, Mobiles, Creatures) | `dev-docs/content-patterns.md` |
| Era & expansion handling | `dev-docs/era-expansion.md` |
| Timer system | `dev-docs/timers.md` |
| Event scheduler (wall-clock/calendar) | `dev-docs/event-scheduler.md` |
| Object property lists (tooltips) | `dev-docs/property-lists.md` |
| Gump (UI dialog) system | `dev-docs/gump-system.md` |
| Commands & targeting | `dev-docs/commands-targeting.md` |
| Event system | `dev-docs/events.md` |
| Threading model | `dev-docs/threading-model.md` |
| Configuration system | `dev-docs/configuration.md` |
| Networking & packets | `dev-docs/networking-packets.md` |
| Region system | `dev-docs/regions.md` |

## Claude Skills (Opt-In)

Detailed Claude Code skills live in `dev-docs/claude-skills/`. They are **not auto-loaded** — they must be copied to `.claude/skills/` to activate.

**When to offer**: If the user is building complex content (new items, creatures, spells, gumps, quests, packets, serialization work, etc.), ask:

> I have detailed Claude Code skills for this kind of work. Want me to enable them?
> I'll copy the relevant files from `dev-docs/claude-skills/` to `.claude/skills/`.

Then copy only the relevant skill files based on the task:

| Task | Skills to enable |
|---|---|
| New Item or Mobile | `modernuo-content-patterns`, `modernuo-serialization`, `modernuo-property-lists` |
| Creature / spawn | `modernuo-content-patterns`, `modernuo-serialization`, `modernuo-timers` |
| Spell or ability | `modernuo-content-patterns`, `modernuo-serialization`, `modernuo-timers`, `modernuo-era-expansion` |
| Gump / UI dialog | `modernuo-gump-system`, `modernuo-commands-targeting` |
| Quest or event system | `modernuo-events`, `modernuo-content-patterns`, `modernuo-configuration` |
| Scheduled / seasonal / holiday events | `modernuo-event-scheduler`, `modernuo-timers` |
| Custom regions / dynamic areas | `modernuo-regions`, `modernuo-content-patterns` |
| Packet / networking | `modernuo-networking`, `modernuo-threading` |
| Commands | `modernuo-commands-targeting` |
| Timer work | `modernuo-timers`, `modernuo-serialization` |
| Config system | `modernuo-configuration` |
| Era-conditional code | `modernuo-era-expansion` |
| Code review / audit | `modernuo-code-audit` |
| Any `.cs` file edit | `modernuo-code-audit` (always offer for code changes) |

To enable a skill: `cp dev-docs/claude-skills/<name>.md .claude/skills/`

The `modernuo-code-audit` skill auto-triggers on `.cs` file edits and flags convention violations (warnings only, asks before fixing).
