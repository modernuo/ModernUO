# Pathfinding architecture, configuration, and tuning levers

How creatures navigate in ModernUO: the algorithm, the static-walkability cache, the AI
integration, the configuration knobs, and what to tell shard owners on different hardware.
Written so a future contributor (human or AI) can reason about the system without re-deriving
it from the code.

## The stack (top to bottom)

```
BaseAI movement  (AIMovement.cs: MoveTo / WalkMobileRange → ApproachTarget)
        │  builds/keeps a
        ▼
PathFollower     (Engines/Pathing/PathFollower.cs)  — owns a MovementPath, walks it, repaths
        │  constructs a
        ▼
MovementPath     (Engines/Pathing/MovementPath.cs)  — thin wrapper; calls the algorithm once
        │  calls
        ▼
BitmapAStarAlgorithm.Find   (Engines/Pathing/BitmapAStarAlgorithm.cs)  — windowed A*
        │  per cell expansion asks
        ▼
StepCache        (Engines/Pathing/Cache/)  — static per-chunk walkability bitmaps (+ optional .swb)
        │  on cache miss / non-default walker, falls through to
        ▼
MovementImpl.CheckMovement   (the per-direction "slow path")
```

There is **one** pathfinding algorithm now: `BitmapAStarAlgorithm`. The old `FastAStarAlgorithm`
was removed. Its behavior survives as the **slow path** inside BitmapAStar
(`GetSuccessorsSlowPath` → `CheckMovement`), which is what runs on a cache miss or when the
cache is disabled (see Levers). So "no cache" ≈ "old FastAStar", not a missing capability.

## AI integration: `BaseAI.ApproachTarget`

All creature goal-seeking funnels through one primitive (`AIMovement.cs`). `MoveTo` (combat
chase, AOS follow) and `WalkMobileRange` → `MoveTowardsOrAwayFrom` (pet come/follow, most
combat AIs) both delegate to `ApproachTarget(target, run, range)`.

Per think-tick decision:

1. **Greedy fast path** — if no path is active and the target is in LOS, take one direct step.
   It only counts as progress when the move *fully succeeds* (`MoveResult.Success`) **and**
   actually gets closer. A blocked step or an auto-turn *sidestep* (`SuccessAutoTurn`) that
   didn't reduce distance does **not** count — it falls through to the planner. (This is the
   fix for the old "pace back and forth at a concave obstacle" bug: a sidestep used to be
   mistaken for progress and discard the path.)
2. **Planner** — commit to a persistent `PathFollower` (kept across ticks, never discarded by a
   greedy step) and follow it around the obstacle.
3. **Give-up / idle** — a best-distance stall counter (`ApproachGiveUpTicks = 40`,
   `AIMovement.cs`) idles a creature that can't lower its closest-ever distance to a
   **stationary** goal, so it stops shuffling on a genuinely unreachable target. A **moving**
   goal (active chase) resets the baseline every tick and never gives up.

Open terrain stays on the greedy fast path and never builds a `PathFollower` — pathfinding only
engages when greedy movement stalls.

## The algorithm: windowed A* with hard limits

`BitmapAStarAlgorithm` (`Find`) is a **bounded local** pathfinder, not a global one. Key
constants:

| Constant | Value | Meaning |
|---|---|---|
| `AreaSize` | 38 | Search is a 38×38 box **centered on the midpoint of start+goal**. Every path cell must lie within ~19 tiles of that midpoint. |
| `MaxSearchNodes` | 1000 (settable) | Max node expansions per `Find` before bailing — a per-pathfind CPU bound on the single game thread. |
| `PlaneCount`/`PlaneHeight`/`PlaneOffset` | 13 / 20 / 128 | Z handling: 13 planes × 20 height, offset 128 → multi-Z (stairs) representable when it fits the window. |

**Consequences (these are by-design limits, not bugs):**

- The window is sized by the **straight-line** start↔goal span, *not* the route. When start and
  goal are close but separated by a big obstacle (around a building, or one floor up via remote
  stairs), the real detour can fall outside the 38-box and is unfindable at any budget.
  Verified example: pet at `(1443,1568,30)` → owner upstairs `(1444,1566,50)` Trammel returns
  `null` at every budget (300→3000). That class needs higher-level handling (waypoints, or pets
  teleporting to master when they can't path — classic UO behavior), not a bigger A*.
- `MaxSearchNodes` only bounds **long / failed** searches; successful open paths terminate on
  goal-found and never approach it. Sizing data (Release, BDN, `MaxSearchNodesBenchmarks`):
  open path ~2 µs at every budget; a ~33-step indoor detour needs ≥ ~500 expansions (NULL
  below); an unreachable search's cost rises then plateaus when it exhausts the window
  (~1500–1700 nodes). **1000 is near-optimal**: above the ~500 needed for indoor detours, below
  the window-exhaustion ceiling, so a failed search at 1000 costs ~79 µs (Release) and bails
  before the ~185 µs full-exhaustion cost. Raising it past ~1500 buys nothing.

Internal A* working buffers (`_nodes` ~18,772 entries, `_nodeStates`, `_path`, `_successors`,
`_openQueue`) are `static` singletons reused every call — **zero allocation**. The only
intentional per-call allocation is the returned `Direction[]` path (handed off to the
`PathFollower` and read across ticks until repath/arrival).

## The StepCache (static walkability)

`StepCache` stores, per map chunk, a precomputed bitmap of which of the 8 directions are
walkable from each cell plus the destination Z — so a default walker's cell expansion is one
`TryGetMask` lookup instead of 8 `CheckMovement` calls. Cells the cache can't model (multi-Z
fallthroughs, non-default walkers: fliers, non-GM players, swim/door/clip capabilities) fall
through to the slow path for that one cell.

- **Warming is on-demand and second-touch gated.** A chunk is built when a *second* distinct
  pathfind touches it (avoids building chunks a one-off search will never reuse). Until built,
  touches fall through to the slow path.
- **Memory is bounded, not unbounded.** Resident chunks are LRU-capped at
  `pathfinding.maxResidentChunks` (default 8192 ≈ ~40 MB). It ramps to the cap and *plateaus*;
  it does not grow forever.
- **Steady-state win:** once warm, BitmapAStar is **2–5× faster than the old FastAStar** at zero
  allocation (BDN `PathfindBenchmarks`, `LazyWarm`/`WarmNoFile` providers).
- **Cold cost:** with the cache *on* but never warm (every search a first-touch), it is ~1.15–
  1.25× *slower* than FastAStar (the per-cell probe overhead without payoff). This is the
  transient first-pathfind-per-region case, not steady state.

### `.swb` baked files (optional)

`[PathBake` / `[PathCacheSave` write a `<mapId>.swb` per map; `[PathCacheLoad` (and startup
auto-load) open them as **lazy** backing stores — only the header + chunk-offset index is read
up front (~16 B/chunk); individual chunks are fetched on demand and remain LRU-capped, so
**RAM stays bounded by `maxResidentChunks` regardless of file size.** The only thing a baked
file buys is **zero first-pathfind-after-boot latency** for a region (chunks reload from file
instead of being rebuilt by the runtime baker).

**Disk cost is large — measured, not the stale ~25 MB some older notes claim:** Trammel
(`1.swb`) is **~565 MB**. Felucca is comparable; all six facets together are on the order of
**~1.5–2 GB**. Baking is therefore a heavy, opt-in operation for serious shards with disk to
spare — **do not ship `.swb` files, and do not bake by default.** (If that footprint seems
wrong for what it stores, the file format is worth auditing separately — it is far above what
the format's design notes projected.)

## Configuration levers

| Lever | Where | Default | Effect |
|---|---|---|---|
| `pathfinding.enable` | `PathFollower.Configure` | `true` | Master switch for `PathFollower` pathfinding. Off → greedy/auto-turn only, no A* at all. |
| `bitmap_pathfinding_cache` feature flag (`ContentFeatureFlags.BitmapPathfindingCache`, `Server.Systems.FeatureFlags`) | `FeatureFlagManager` | `true` | Off → `BitmapAStar` routes straight to the slow path with **no cache probe and no warming memory**. ≈ old FastAStar at ~1×. |
| `pathfinding.maxResidentChunks` | `PathCacheCommands.Configure` | 8192 (~40 MB) | LRU cap on resident chunks = the warming-memory ceiling. Lower it (e.g. 512–1024 ≈ 2.5–5 MB) on small shards. |
| `pathfinding.maxSearchNodes` | `PathCacheCommands.Configure` → `BitmapAStarAlgorithm.MaxSearchNodes` | 1000 | A* per-Find node-expansion budget. See limits above; ~1000 is the sweet spot. |
| `PathFollower` `RepathDelay` | `PathFollower.cs` (const) | 2 s | Throttle: a moving goal re-`Find`s at most ~once per 2 s; a stationary reachable goal is pathed once and reused until arrival. Not a setting (compile-time). |

### Default configuration (recommended)

Cache **on**, warm-on-demand, **no `.swb`**. Most shards get the 2–5× steady-state win for
free, with warming memory plateau-capped at ~40 MB. `.swb` baking stays opt-in for large shards
that care about first-pathfind-after-boot latency and can spend ~1.5–2 GB of disk.

### Small / crappy-hardware shards — the spectrum

| Config | Perf | Memory | Disk |
|---|---|---|---|
| `bitmap_pathfinding_cache = false` | ≈1× (old FastAStar) | **0** cache RAM | 0 |
| Cache on, `maxResidentChunks` low (~512) | ~2–5× on hot regions | ~few MB | 0 |
| Cache on, default cap (8192) | 2–5× warm | ~40 MB plateau | 0 |
| Cache on + baked `.swb` | + zero first-pathfind-after-boot latency | ~40 MB + index | **~1.5–2 GB** |

The key point for RAM-starved boxes: **disabling the cache is not a regression** — it's the old
FastAStar behavior at ~1× with zero warming memory (the slow path does the same per-cell work,
and with the flag off there's no probe overhead). The `CacheOffBenchmarks` in the benchmark repo
exists to prove this (ratio ≈ 1.0 vs the vendored FastAStar baseline).

## Diagnostics & tooling

- **`[PathCacheStats`** — resident-chunk count + hit/miss/eviction telemetry. Watch
  `evictions(lruCap)` on a live shard to see if the working set exceeds the cap.
- **`[PathCacheClear`** — drop residents + zero counters.
- **`[PathBake [mapId]` / `[PathCacheSave` / `[PathCacheLoad`** — produce / persist / lazy-open
  `.swb` files (see disk cost above).
- **`[PathRecord on|off|flush`** — capture every `Find` as JSONL (replay / benchmark corpus).
- **`MapDump`** (`ModernUO-Tools/TerrainAnalyzer/`, see its `MapDump.md`) — offline UO
  client-file inspector (`dump`/`scan`/`floors` modes) for diagnosing whether a route exists and
  whether it fits the 38-tile window. Read raw geometry, then confirm with a real `Find` in a
  test.
- **Benchmarks** (`ModernUO-Benchmarks/Benchmarks/PathfindInGame/`, BenchmarkDotNet, Release):
  `PathfindBenchmarks` (BitmapAStar vs FastAStar × 4 cache providers × corpus),
  `MaxSearchNodesBenchmarks` (budget sweep), `CacheOffBenchmarks` (flag-off ≈ FastAStar). Use
  these to validate any change to the defaults above against the recorded corpus.

## Testing notes

- Pathfinding tests live in `Projects/UOContent.Tests/Tests/Engines/Pathing/` and
  `.../Mobiles/AI/ApproachTargetTests.cs`, in the `Sequential Pathfinding Tests` collection
  (the A* statics are not reentrant). They run against the live Trammel `TileMatrix`.
- Run in **Debug or Release** — both work since the `PathfindingTestFixture` data-copy was fixed
  to use a project-relative path (`UOContent.Tests.csproj`); historically Release threw an NRE
  because `Data/skills.json` wasn't copied to the Release bin.

## Future work

### Cache / bake (deferred follow-ups)

- **Background-thread bake.** Build chunks off the game thread so even *promoted* chunks don't
  pay the ~700 µs build cost on the main thread. Rule 10 (no `Task.Run`/`new Thread()` in game
  code) applies — the bake itself is a pure data transform, but the main-thread synchronization
  on chunk-state transitions (resident map insert, LRU bookkeeping, generation gate) has to be
  threaded through carefully. Needs its own design; defer.
- **Long-traverse BDN scenario.** A multi-`Find` benchmark simulating ~50 pet repaths across
  chunk transitions, to exercise the gate under sustained cross-chunk movement. Requires
  restructuring the bench harness (it's currently one `Find` per scenario); the existing
  corpus + `Cold` provider already covers the single-Find gate, so this is lower priority.
- **Swim `SourceZ` bake.** The corpus sea-serpent scenario shows ~56 B allocation on warm paths
  because the cache's `SourceZ` is computed under default-walker rules, so swim creatures fall
  through to the slow path. Baking swim-aware source Z (or a swim stratum) would let them hit
  the cache. Independent of the size-reduction work below.

### `.swb` size reduction (the ~565 MB → tens of MB roadmap)

The v2 format stores every 16×16 chunk as a flat ~5,393-byte record, uncompressed, with no
uniform-region elision — so Trammel's ~114,688 chunks × 5.4 KB ≈ ~565 MB, and ocean / Green
Acres / void cost the same as dense dungeon. Three **independent, separately-shippable** wins,
all of which preserve seekable random reads (compress at chunk/sector granularity, never
whole-file) and bounded RAM (only touched chunks materialize, LRU-capped):

1. **Uniform-chunk elision** (biggest ratio, lowest risk). A chunk whose 256 cells share one
   mask + Z is stored as a few bytes (flag + mask + Z) instead of 5,393. Pairs with a two-level
   **sector index** so a uniform super-sector (open ocean) collapses to a single entry — UO's
   16-tile sectors nest cleanly inside.
2. **Predictive (lossless) Z residuals** (kills the 76% that is the 16 directional Z arrays).
   Predict each `WalkZ_dir`/`SwimZ_dir` from the cell's **own `SourceZ`** and store only the
   residual: 0 on flat ground (→ compresses to nothing → effectively just the walk *bit*),
   ±1..3 on slopes/stairs, larger or strata for bridges/multi-Z. Reconstruct
   `WalkZ_dir = SourceZ + residual` at load → byte-identical in-memory `StepChunk`, so **no
   runtime recompute and no risk of diverging from `MovementImpl`** (the bugs the cache exists
   to avoid). Predict from *self* (not a neighbor) to keep chunk independence.
3. **Per-chunk block compression + index compaction.** zstd/deflate each chunk record
   independently (index already carries a per-chunk `length`; reader decompresses one chunk on
   read). At 256-cell granularity the **index overhead** then dominates (20 B/chunk ×
   114 K ≈ 2.3 MB), so compact it in the same pass: chunks in fixed sweep order → implicit keys,
   delta-encoded offsets, lengths derivable → ~2–4 B/chunk.

**Do first:** a uniformity audit over a baked facet (how many chunks are fully uniform / have
all-zero Z residuals?) to size the #1/#2 win before writing any format code. Each technique is a
clean v3 format bump; `MinSupportedVersion` already silently rejects + overwrites older files.
Validate size **and** read-latency vs the corpus in the benchmark repo after each.
