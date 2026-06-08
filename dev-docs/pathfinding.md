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

**Disk cost (current, format v8):** the size-reduction roadmap (§ `.swb` size reduction) took
Trammel from ~565 MB to **17.9 MB**; the other facets are smaller, so all six together are on the
order of **tens of MB** — not the ~1.5–2 GB the uncompressed v2 format cost. Baking is now cheap
enough to **offer by prompt at first boot** (below) rather than being a heavy opt-in-only step.
Still don't *ship* prebaked `.swb` files (they're tile-data-version-specific and regenerate from
the client files anyway).

### First-boot pre-bake prompt

On the **first boot** (right after map selection) an interactive prompt offers to pre-bake the
`.swb` cache for the selected maps. The answer is stored in `modernuo.json` as
**`pathfinding.prebakeMaps`** (default **false**), so it is asked exactly once; headless/CI boots
(redirected input) skip the prompt and default to off — operators can set the flag directly.

When the flag is set, `PathCacheCommands.Initialize()` bakes, at startup, any map whose `.swb` is
missing or **stale** (its tile-data fingerprint no longer matches — e.g. after a client/map
update). A fresh cache is a no-op, so only the first boot (or a post-update boot) pays the
several-minutes cost. Wiring:

- The prompt runs in a dedicated `AssemblyHandler.Invoke("ConfigurePrompts")` startup phase —
  after assemblies load (so content can register prompts) but **before Serilog starts**, so the
  console prompt is not interleaved with the async console sink. Any class can participate by
  defining `public static void ConfigurePrompts()` and self-gating on first-boot state.
- The bake runs in the later `Invoke("Initialize")` phase (after the tile matrix + world load,
  which the bake walks).
- Staleness is decided by the `.swb` fingerprint, which `StepCacheFile.OpenForLazy` validates at
  open time (hash of `tiledata.mul` + the per-map `.mul`/`.uop` files — never the in-memory
  `TileData` tables, which the server patches at runtime). `Configure` opens a reader for every
  up-to-date file; the bake in `Initialize` then skips any map where `StepCache.HasLazyReader` is
  already true, so the fingerprint is computed once per boot, not twice.

## Configuration levers

| Lever | Where | Default | Effect |
|---|---|---|---|
| `pathfinding.enable` | `PathFollower.Configure` | `true` | Master switch for `PathFollower` pathfinding. Off → greedy/auto-turn only, no A* at all. |
| `bitmap_pathfinding_cache` feature flag (`ContentFeatureFlags.BitmapPathfindingCache`, `Server.Systems.FeatureFlags`) | `FeatureFlagManager` | `true` | Off → `BitmapAStar` routes straight to the slow path with **no cache probe and no warming memory**. ≈ old FastAStar at ~1×. |
| `pathfinding.maxResidentChunks` | `PathCacheCommands.Configure` | 8192 (~40 MB) | LRU cap on resident chunks = the warming-memory ceiling. Lower it (e.g. 512–1024 ≈ 2.5–5 MB) on small shards. |
| `pathfinding.maxSearchNodes` | `PathCacheCommands.Configure` → `BitmapAStarAlgorithm.MaxSearchNodes` | 1000 | A* per-Find node-expansion budget. See limits above; ~1000 is the sweet spot. |
| `pathfinding.prebakeMaps` | `PathCacheCommands` (first-boot prompt + `Initialize`) | `false` | When set, bakes any missing/stale `.swb` for the selected maps at startup (fingerprint-gated, so a fresh cache is a no-op). Set interactively by the first-boot prompt. |
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
| Cache on + baked `.swb` | + zero first-pathfind-after-boot latency | ~40 MB + index | **~tens of MB** (v8) |

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
2. **Predictive (lossless) Z residuals** — *shipped as format v6.* Kills the bulk of the 16
   base directional Z arrays in Full chunks. Each array is stored as a **masked residual**
   against the cell's **own `SourceZ`**: predict `mask bit ? SourceZ : 0` (the mask term matches
   the baker, which leaves non-walkable directional slots at `0`), residual `= Z − predict` via
   unchecked two's-complement (byte-exact for all inputs). A new u16 `ZArrayMask` flags which of
   the 16 arrays differ from their prediction; an array that matches everywhere (flat terrain —
   including partial-walkability coastlines with nonzero `SourceZ`) is **omitted entirely** and
   synthesized from `mask + SourceZ` at read. Reconstruct `Z = predict + residual` → byte-identical
   in-memory `StepChunk`, so **no runtime recompute and no risk of diverging from `MovementImpl`**.
   Self-prediction (not a neighbor) keeps chunk independence. The residual *subtraction* itself
   saves ~0 bytes (a residual is still 1 byte/cell); the win is the per-array elision, and leaving
   non-elided arrays in residual form makes #3 a pure codec add (no further transform/format bump).
   Swim-layer + strata trailers stay absolute, deferred to #3.
3. **Per-chunk block compression + index compaction.**
   - **#3a — per-chunk compression: *shipped as format v7.*** Each record is libdeflate-compressed
     independently (random access preserved; the reader inflates one chunk on read into a reused
     buffer) behind a `u32 UncompressedLen` frame; tiny Uniform records that do not shrink are
     stored raw (detected as on-disk payload length == UncompressedLen). Codec chosen by full-Trammel
     spike: **libdeflate VeryHigh** beat zstd L19/22 (17.4 MB) and tied managed Brotli q11 (16.4 MB)
     at **16.5 MB of compressed records**, with the *fastest* decompress (**1.83 µs/chunk**, vs zstd
     2.21) — and it is already the repo's packet codec (`LibDeflate.Bindings`), so no new dependency.
     zstd's large-window advantage doesn't apply at 256-cell record granularity. Compression runs at
     bake time only (offline); decompression is one-time per chunk (LRU-cached after). **Calibrated:
     v6 124.7 MB → v7 19.2 MB (−85%); −97% vs the original 565 MB.**
   - **#3b — index compaction: *shipped as format v8.*** The v7 file's residual was the
     uncompacted index (20 B/chunk × 114 K ≈ 2.3 MB). The trailer now stores only
     `{ u32 packedKey = (ChunkX << 16) | ChunkY, u32 recordLength }` per chunk (8 B), in record
     write order; the per-chunk file offset is dropped and reconstructed by cumulative `recordLength`
     from `HeaderSize` at open. No record reordering, no varint — a fixed-stride, low-risk change to
     the load-bearing index. **Calibrated: v7 19.2 MB → v8 17.9 MB.** (A varint/implicit-key scheme
     could shave the index toward ~0.34 MB for another ~0.6 MB, at the cost of variable-stride
     parsing and record reordering — not worth it on an already −97% file.)

**Do first:** a uniformity audit over a baked facet (how many chunks are fully uniform / have
all-zero Z residuals?) to size the #1/#2 win before writing any format code. Each technique is a
clean v3 format bump; `MinSupportedVersion` already silently rejects + overwrites older files.
Validate size **and** read-latency vs the corpus in the benchmark repo after each.

**Measured headroom (Trammel).** Two measurements, both 2026-06-06:

- *Pre-swim audit (v2 spike, indicative):* directional-Z is **95.2% zero-residual for WalkZ**,
  **38.1% for SwimZ** vs `SourceZ` (→ #2; swim wants its own predictor or leans on #3). The
  spike's combined size projection double-counted and is superseded by the calibration below.
- *Calibrated on the real v4/v5 format (`SaveToFile`-measured):* 114,688 chunks; **62.7% fully
  uniform** (swim-aware → #1 elides these), **8.9% carry a swim layer** and **1.8% strata**
  (both stay Full). **#1 alone: 592.2 MB → 231.9 MB (−61%), actual on-disk.** The residual is
  ~150 MB of non-uniform land Z-blocks (→ #2 predictive-Z) + ~81 MB of swim-layer trailers
  (→ #2/#3). Confirms build order **#1 → #2 → #3**, with #1 the dominant, lowest-risk,
  no-algorithm-change win (shipped as format v5).
- *Calibrated v6 (`BakeMap`-measured, full Trammel, `MaxResidentChunks` raised above 114,688):*
  **#2 predictive-Z: 231.9 MB → 124.7 MB (−46%, −107 MB; −78% vs the original 565 MB).** Of the
  42,775 Full chunks (71,913 are Uniform), **walk directional-Z arrays elide 47.7%** and **swim
  arrays elide 87.6%** (per-array, not per-cell — one slope cell keeps a 256-byte array, which is
  why the per-array rate trails the 95.2% per-cell zero-residual). Remaining v6 bytes are dominated
  by present walk-residual blocks (~46 MB, mostly ±1..3 + zeros → highly compressible), the
  per-Full-chunk mask/SourceZ base (~33 MB), and absolute swim-layer trailers (~26 MB) — all prime
  targets for #3 per-chunk compression.
- *Calibrated v7 (`BakeMap`-measured, full Trammel):* **#3a per-chunk libdeflate VeryHigh: 124.7 MB
  → 19.2 MB (−85%); −97% vs the original 565 MB.** Codec spike (per-chunk over the 122.4 MB of v6
  records): libdeflate VeryHigh **16.5 MB** vs Brotli q11 16.4 / zstd L19–22 17.4; decompress
  **1.83 µs/chunk** (libdeflate) vs 2.21 (zstd). The remaining ~2.3 MB is the uncompacted index
  (→ #3b).
- *Calibrated v8 (`BakeMap`-measured, full Trammel):* **#3b index compaction (20 → 8 B/chunk):
  19.2 MB → 17.9 MB.** Roadmap end-to-end: **565 MB → 17.9 MB (−96.8%)** across #1 uniform elision
  (v5) → #2 predictive-Z (v6) → #3a per-chunk libdeflate (v7) → #3b compact index (v8).
