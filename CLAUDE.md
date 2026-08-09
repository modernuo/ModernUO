# ModernUO

.NET 10 Ultima Online server emulator. Single-threaded game loop. All game logic runs on one thread.

- **Server engine**: `Projects/Server/` — do NOT modify without explicit request
- **Game content**: `Projects/UOContent/` — primary editing target
- **Build**: `dotnet build` from repo root

## Code Audit Rules

Apply these when writing or reviewing `.cs` files under `Projects/`.

1. **LINQ** — Tier 1 (zero-cost patterns) free on hot paths; Tier 2 (low overhead) OK on warm paths; Tier 3 (allocating) forbidden on hot paths → `dev-docs/code-standards.md`
2. **No `Console.WriteLine`** — use `LogFactory.GetLogger(typeof(MyClass))` → `logger.Information(...)` (requires `using Server.Logging;`)
3. **Threading policy** — game logic runs only on the main loop; **never** touch game state (`World`, mobiles, items, maps, timers) from a background thread. Heavy work that *needs* game state must be **chunked** across ticks, not threaded. Heavy work that does *not* need game state (large-file parse, external I/O) **must** run on a background thread **and must yield to world saves** (defer while `World.Saving`/`WorldState.PendingSave`). Publish results back to the loop as an immutable snapshot swapped via a single `volatile` reference — the only sanctioned `volatile`. No `lock`/`Mutex`/`ConcurrentDictionary` in game logic. Rule #10 covers how background work hands results back to the loop → `dev-docs/threading-model.md`
4. **No `World.Mobiles`/`World.Items` iteration** — use spatial queries: `map.GetMobilesInRange<T>()`, `map.GetItemsInRange<T>()`
5. **Clean up refs in `OnDelete()`/`OnAfterDelete()`** — null out `Item`/`Mobile` references
6. **Cancel timers in `OnDelete()`/`OnAfterDelete()`** — call `_token.Cancel()` or `_timer?.Stop()`
7. **`STArrayPool<T>.Shared`** not `ArrayPool<T>.Shared` — single-threaded optimized, no locks
8. **`PooledRefList<T>`** not `new List<T>()` on hot paths — zero GC pressure, stack-allocated ref struct
9. **Serialization** — class must be `partial`, constructor needs `[Constructible]`, `TimerExecutionToken` must NOT have `[SerializableField]`. New classes: use `[SerializationGenerator(version)]` (omit `encoded`). When bumping versions, add `MigrateFrom(VXContent)` (X = previous version). Never modify `Deserialize(reader, version)` for version bumps — that method is only for pre-codegen legacy saves. When migrating from pre-codegen Serialize/Deserialize: pass `false` if old code used `reader.ReadInt()`, bump version +1, and keep old logic as `private void Deserialize(IGenericReader reader, int version)` → `dev-docs/runuo-migration-docs/02-serialization.md`
10. **No `Task.Run`/`new Thread()` for game logic** (tandem with rule #3) — game logic is the single-threaded event loop. Backgrounding is allowed only for work that does not itself touch game state (external service calls, large-file parse). **Prove the need before adding a thread**: measure **on-loop** time, not wall-clock (frozen world is the cost, player latency is not), and gate on `Environment.ProcessorCount` — off-loading creates no CPU and buys nothing on 1–2 cores. New workers go in the vetted table in `dev-docs/threading-model.md` with their measurement. When such work must *feed* game logic: run the heavy/I/O part off-loop and `ConfigureAwait(false)` its awaits so a continuation never resumes on the loop and silently foregrounds heavy work; then hand the result back **explicitly** — publish an immutable snapshot swapped via a `volatile` reference (the loop reads it lock-free), or marshal the apply step with `Core.LoopContext.Post(() => …)`, re-validating in the continuation whatever may have changed while it ran. Never touch game state off-thread; never let the scheduler decide where the heavy work runs → `dev-docs/threading-model.md`
11. **Never assume era** — if code uses `Core.AOS`/`Core.SE`/etc., ask which expansion to target
12. **Naming** — `_camelCase` private fields, `PascalCase` properties/methods/classes; don't flag legacy `m_` but use `_` for new code
13. **No empty gumps** — every gump must produce visual elements. An empty gump leaks on client+server (no way to close it). Use static `DisplayTo()` to validate before constructing → `dev-docs/gump-system.md`
14. **PropertyList string literals must be holes** — `$"{"Map"}\t{value}"` not `$"Map\t{value}"`. The handler treats bare text as delimiters, `{}` holes as arguments. Only `\t` should be a bare literal → `dev-docs/property-lists.md`
15. **Braces required on all control flow** — `if`, `else`, `for`, `foreach`, `while`, `do`, `switch` must always have braces, even for single-line bodies → `dev-docs/code-standards.md`
16. **Prefer switch expressions and switch-when** — use switch expressions for value mapping and switch-when for pattern matching where they improve readability. Exception: skip if unreadable or cold path → `dev-docs/code-standards.md`
17. **No `System.Text.StringBuilder`** — use `ValueStringBuilder` with `stackalloc` (bounded output) or `ValueStringBuilder.Create()` (unbounded). Supports `$"..."` interpolation directly. Always use `using var` for disposal. Use `Reset()` instead of reassigning → `dev-docs/string-handling.md`
18. **Interpolation anti-patterns on handler-aware APIs** — `Send*`/`Say`/`Emote`/`PublicOverhead*`/`IPropertyList.Add`/gump `AddLabel`/`AddHtml`/`Html.Center`/`SpanWriter.Write*` all have `ref RawInterpolatedStringHandler` overloads that allocate zero strings, but only when the call-site argument is a `$"..."` literal directly. Avoid: ternaries with interpolated branches (`Send(c ? $"a" : $"b")`), switch expressions with interpolated arms, pre-built `var s = $"..."` locals (single-use), `.ToString()` / `.String()` / `string.Format` inside holes, string concat (`{a + b}`), LINQ string ops in holes. Use `:L` format spec for lowercase (`{rank:L}` not `rank.ToString().ToLowerInvariant()`) → `dev-docs/string-handling.md` § Interpolation Anti-Patterns
19. **No `InvalidateProperties()` from inside `GetProperties`** — every property a `GetProperties` override reads must be a pure read. `InvalidateProperties()` rebuilds the list in place (`Reset()` + rebuild), and `Reset()` returns the pooled interpolation buffer — which the compiler rents for the whole `$"..."` expression, so every hole is evaluated while it is live — and rewinds the packet cursor. A getter that invalidates therefore throws `ArgumentNullException` (parameter `"array"`) out of `GetProperties` from an unrelated-looking line, or silently corrupts the tooltip. The engine refuses and logs an error; `DEBUG` throws. Lazy recomputation in a getter is fine — the *notification* is not. Invalidate in the setter that changes the value, or defer with `Timer.DelayCall(InvalidateProperties)` → `dev-docs/property-lists.md` § Never Invalidate From Inside `GetProperties`
20. **Tick-count math must be wraparound-safe** — compare `Core.TickCount`/`GetTimestamp()` values only by subtraction (`a - b < 0`, never `a < b`), no zero/sign sentinels on tick fields, seed deadline fields from a real tick (never rely on the 0 default). Cloud hypervisors (GCP) pass through the host's never-resetting counter: ticks start enormous and can wrap negative. Linux affected in production; Windows not so far → `dev-docs/tick-counts.md`

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
| Server hardware requirements | `dev-docs/server-requirements.md` |
| Debugging event-loop performance (profiling build, decomposition, GC/RAM) | `dev-docs/debugging-event-loop.md` |
| Tick-count overflow rules (subtraction comparisons; GCP pass-through counters) | `dev-docs/tick-counts.md` |
| Server lifecycle & bootstrap phases (Configure/ConfigurePrompts/Initialize) | `dev-docs/server-lifecycle.md` |
| Platform prerequisites (ICU, tzdata, native libs per distro) | `dev-docs/platform-prerequisites.md` |
| Configuration system | `dev-docs/configuration.md` |
| Networking & packets | `dev-docs/networking-packets.md` |
| IP bans, blocklists & allowlists (incl. unblocking a player) | `dev-docs/ip-bans-and-allowlists.md` |
| Region system | `dev-docs/regions.md` |
| String handling & ValueStringBuilder | `dev-docs/string-handling.md` |
| RunUO migration (overview) | `dev-docs/runuo-migration-docs/00-overview.md` |
| RunUO migration (all docs) | `dev-docs/runuo-migration-docs/` |

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
| String building / formatting | `modernuo-string-handling` |
| Code review / audit | `modernuo-code-audit` |
| Any `.cs` file edit | `modernuo-code-audit` (always offer for code changes) |
| **RunUO Migration** | |
| Migrate any RunUO script | `migrate-from-runuo/migrate-foundation` (always), plus system-specific skills below |
| Migrate Item/Mobile/Creature | `migrate-from-runuo/migrate-foundation`, `migrate-from-runuo/migrate-serialization`, `migrate-from-runuo/migrate-items-mobiles` |
| Migrate serialization | `migrate-from-runuo/migrate-serialization` |
| Migrate timers | `migrate-from-runuo/migrate-timers` |
| Migrate gumps | `migrate-from-runuo/migrate-gumps` |
| Migrate packets | `migrate-from-runuo/migrate-packets` |
| Migrate property lists | `migrate-from-runuo/migrate-property-lists` |
| Migrate events/commands | `migrate-from-runuo/migrate-commands-events` |
| Migrate persistence (WorldSave) | `migrate-from-runuo/migrate-persistence` |
| Migrate multi-file system | `migrate-from-runuo/migrate-systems` |

To enable a skill — Claude Code loads `.claude/skills/<name>/SKILL.md`; a bare `.md` dropped
directly into `.claude/skills/` is **not** picked up, and newly installed skills appear in the
*next* session:

```sh
# Standard skills (modernuo-*)
mkdir -p .claude/skills/<name> && cp dev-docs/claude-skills/<name>.md .claude/skills/<name>/SKILL.md

# Migration skills — sources live in the migrate-from-runuo/ subfolder, but install under the
# bare skill name (the table's "migrate-from-runuo/<name>" is the source path, not the name):
mkdir -p .claude/skills/<name> && cp dev-docs/claude-skills/migrate-from-runuo/<name>.md .claude/skills/<name>/SKILL.md
```

Migration skills reference the deep docs in `dev-docs/runuo-migration-docs/` and point to existing ModernUO skills for best practices.

The `modernuo-code-audit` skill auto-triggers on `.cs` file edits and flags convention violations (warnings only, asks before fixing).
