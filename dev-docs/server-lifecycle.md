# Server Lifecycle & Bootstrap Phases

How a ModernUO server starts, the reflection-discovered lifecycle hooks (`ConfigurePrompts`,
`Configure`, `Initialize`), the runtime `EventSink` events, and **which hook to use for what**.

The startup orchestration lives in `Projects/Server/Main.cs` (`Core` entry point). The named
phases are dispatched by `AssemblyHandler.Invoke("<Name>")`, which finds every
`public static void <Name>()` (parameterless) across `Core.Assembly` **and** all loaded
content assemblies and calls them — no registration required.

## Startup sequence (in order)

> Don't hardcode line numbers when reasoning about this — refer to the phase/method names; the
> ordering is what's stable.

1. **Console banner + setup** — direct synchronous `Console.*` writes (no logging yet).
2. **`ServerConfiguration.Load()`** — reads/creates `modernuo.json`. On **first boot** (file
   absent) it runs the engine's own interactive console prompts: data directories, listeners,
   server name, expansion + map selection. **Pre-Serilog** — nothing has logged yet, so the
   console is clean for prompts. (`Load(mocked: true)` skips all prompts; that's what tests use.)
3. **`AssemblyHandler.LoadAssemblies(...)`** — loads `UOContent.dll` (and friends) from
   `AssemblyDirectories` (default `./Assemblies`). Note: this depends on `AssemblyDirectories`,
   **not** `DataDirectories`, so it does not need the data-dir prompt to have run.
4. **`AssemblyHandler.Invoke("ConfigurePrompts")`** — first-boot interactive prompts contributed
   by *any* assembly (engine or content). Runs **after** assemblies load (so content can
   participate) but **before the first Serilog line** (so prompts aren't interleaved with the
   async console sink). Each handler self-gates on first-boot state.
5. **First `logger.Information(...)`** — Serilog goes live. From here on, log via the logger;
   the console sink is async, so anything you write with `Console.*` after this can interleave
   with log output.
6. **`VerifySerialization()`** → **`Timer.Init(...)`**.
7. **`AssemblyHandler.Invoke("Configure")`** — the main configuration phase. World is **not**
   loaded yet (no entities), but maps are registered.
8. **`TileMatrixLoader.LoadTileMatrix()`** → **`RegionJsonSerializer.LoadRegions()`**.
9. **`World.Load()`** — deserializes all items/mobiles; fires `EventSink.WorldLoad`.
10. **`AssemblyHandler.Invoke("Initialize")`** — post-world phase. World entities **and** the
    tile matrix are available.
11. **`NetState.Start()`** / **`PingServer.Start()`** → **`EventSink.InvokeServerStarted()`** →
    **`RunEventLoop()`** (the single-threaded game loop begins).

## The three reflection phases — which to use

| Phase | Runs | Use it for | Don't |
|---|---|---|---|
| **`ConfigurePrompts()`** | after assemblies load, **before logging** | one-time **first-boot interactive prompts**; persist the answer to `modernuo.json`; self-gate so it asks once; skip when input is redirected | log (Serilog isn't live — use `Console`); touch World/maps/tile data (not ready) |
| **`Configure()`** | post-logging, **pre-World** | command registration, reading settings (`GetOrUpdateSetting`), `EventSink` subscriptions, wiring systems | anything needing loaded **World entities** or the tile matrix |
| **`Initialize()`** | **post-World**, post-tile-matrix | work needing a loaded world / tile data: decoration/generation, validation, pre-baking caches | first-boot prompts (too late, and it would clobber logs) |

All three are `public static void <Name>()`, parameterless, discovered across every loaded
assembly. Within a phase, order is controlled by **`[CallPriority(n)]`** (lower runs first;
default `50`). **Same-priority order is unspecified**, so never rely on one class's `Configure`
running before another's at the same priority — use `EventSink`/explicit calls for ordering.

## Pre-Serilog vs post-Serilog — why `ConfigurePrompts` exists

Logging uses an **async** Serilog console sink (`Serilog.Sinks.Async` → `LogFactory`). Once the
first `logger.*` call fires (right after the `ConfigurePrompts` phase), log lines are pumped to
the console from a background thread and will **interleave** with anything written via
`Console.*`. Interactive prompts therefore have to run *before* that point. `ConfigurePrompts`
is the **only** reflection phase that runs pre-logging — that is its entire reason to exist.
Inside it: use `Console`, never the logger; and guard with `Console.IsInputRedirected` so
headless/CI boots don't block on `Console.ReadLine`.

## Runtime lifecycle events (`EventSink`)

Subscribe to these from `Configure`/`Initialize` (`EventSink.<Event> += handler`):

- **`ServerStarted`** — after world load and listeners are up, at loop start.
- **`WorldLoad`** / **`WorldSave`** — around persistence (see `WorldEvents`).
- **`Shutdown`** — during shutdown.

## Recipe: add a first-boot prompt

```csharp
public static void ConfigurePrompts()
{
    // Ask once, and only when a human is at the console. The answer persists in modernuo.json.
    if (ServerConfiguration.GetSetting("my.feature", (string)null) != null || Console.IsInputRedirected)
    {
        return;
    }

    Console.Write("Enable my feature? [y/N] ");
    var yes = Console.ReadLine()?.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) == true;
    ServerConfiguration.SetSetting("my.feature", yes);
}
```

If acting on the answer needs a loaded world / tile data, do that in `Initialize()` (read the
setting there), not in `ConfigurePrompts`.

### Canonical example — pathfinding pre-bake

`Projects/UOContent/Engines/Pathing/PathCacheCommands.cs` is the reference pairing:

- `ConfigurePrompts()` — first-boot `[y/N]`, stores `pathfinding.prebakeMaps`.
- `Initialize()` — when set, bakes any missing/stale `.swb` (needs the tile matrix, so it must
  be `Initialize`, not `Configure`).

## Testing note

Tests do **not** go through `Main`. The test fixtures (`Server.Tests`/`UOContent.Tests`
`TestServerInitializer`) call a curated subset of phase methods directly with
`ServerConfiguration.Load(mocked: true)`, so console prompts are skipped. Consequence: changes
to the **startup ordering in `Main.cs`** (including the prompt phases) are **not** covered by the
test suite and need first-boot runtime verification.

## Planned: unify the engine's first-boot prompts into `ConfigurePrompts`

Today the engine's own first-boot prompts (data dirs, listeners, server name, expansion + maps)
are inline in `ServerConfiguration.Load`, separate from the `ConfigurePrompts` mechanism. They
can be unified into the same phase so there's one prompt sequence/wiring:

- **Feasible because** assembly loading uses `AssemblyDirectories` (default `./Assemblies`), not
  `DataDirectories` — so assemblies can load *before* the data-dir prompt, letting all prompts
  move into the post-assembly `ConfigurePrompts` phase.
- **`UOClient.Load()`** (client-file discovery via `Core.FindDataFile`) needs `DataDirectories`,
  so it must move *with* the data-dir prompt into the unified phase.
- **`Core.Expansion`** is currently assigned during `Load`; under unification it'd be set during
  `ConfigurePrompts` — verify nothing between assembly-load and that point depends on it.

This is an engine-startup restructure the test suite can't cover (see Testing note), so it needs
first-boot runtime verification before merging.
