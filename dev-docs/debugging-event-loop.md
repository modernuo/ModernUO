# Debugging Event Loop Performance

How to diagnose "the server feels slow" — written for both humans and AI assistants. Follow the
funnel in order; most incidents resolve before the last step. Do not start with dotnet-trace.

## The model

Every second of the main thread's wall time goes to exactly one of four places:

1. **Work** — the loop's phases: mobile deltas, item deltas, timer callbacks (`Timer.Slice`),
   network processing (`NetState.Slice`), posted tasks (`LoopContext`), world snapshots
   (`WorldSnapshot` — the on-loop portion of a save).
2. **Sleep** — idle blocking in `NetState.WaitForCompletion`, bounded by the next timer tick and
   `server.eventLoopIdleWaitMs`.
3. **GC pauses** — land inside whichever phase (or sleep) was running.
4. **Stolen** — the host ran something else: hypervisor scheduling, noisy neighbors, CPU credit
   throttling.

A sleep is bounded by the time to the next wheel turn, so **a correctly honoured sleep can never
cost a deadline**. The only way sleeping harms the game is the wait *returning late* — that is
stolen time, and the server measures it directly on every sleep.

## Step 0 — Read what production already tells you

No build changes needed. Three signals exist, all actionable:

| Signal | Meaning | Action |
|---|---|---|
| Startup error: *host cannot honour short waits* | No high-resolution timer and `timeBeginPeriod` failed. Very old or unusual Windows. | Nothing is wrong with the server; it spins and uses a full core. Upgrade the OS or accept the core. |
| Warning: *host returned a Nms idle wait late … for the Nth time running* | The OS did not reschedule the process promptly after a 1–2ms wait, through several escalating backoffs. Shared/burstable vCPU signature. | Move to dedicated CPU, or set `server.eventLoopIdleWaitMs=0` to spin permanently. This is a **host** problem — no amount of server-side change fixes it. |
| Error: *keeps returning idle waits late and sleeping has backed off N times* | The escalation hit its 120s ceiling. The host is not going to recover. | As above, but stop waiting for it to settle. Logged once per degradation, re-armed after a clean minute. |
| Admin gump → Performance → *Event Loop* | `Healthy` / `Sleep suspended (host)` / `Spinning (configured)` / `Spinning - host cannot honor short waits` | Same as above; the last verdict is the startup error's state, not a config choice. |

The first two backoffs of any episode log at **Debug**, not Warning: a single suspension is
recoverable and not something an operator can act on. Raise the log level if you are chasing a
marginal host and want to see them. Late wakes that coincide with a gen1-or-higher GC are not
counted at all — the GC deliberately collects during idle sleeps, so its pauses land there by
design and are not the host's fault.

If none of these fired and the shard still feels laggy, the cause is work, GC, or something a
boot-time signal cannot see. Continue.

## Step 1 — Flip the profiling build

```
dotnet build -p:EventLoopProfiling=true
```

This compiles in `EventLoopProfiler` (Server) and the `[LoopStats` command (UOContent). Without
the flag every hook call site is removed by the compiler (`[Conditional]`), so there is nothing to
"turn off" in normal builds and no cost to leave the hooks in the code. The profiling build's own
overhead is a handful of timestamp reads per iteration — small enough to run for days while
hunting an intermittent problem.

**Capture a baseline first.** Run `[LoopStats` while the shard feels *fine* and keep the CSV. The
profiler also keeps ~15 minutes of history in memory, so if the problem is episodic you can wait
for an episode and the good minutes on either side are already recorded. Numbers without a
baseline are how RunUO's profiler became useless — always compare bad minutes to good minutes on
the same box, build, and world.

## Step 2 — Read the decomposition

`[LoopStats` prints the last minute and writes the full history CSV (one row per second). Match
the shape against these signatures:

| Signature | Diagnosis | Next step |
|---|---|---|
| One phase consistently hot (e.g. `TimerSlice` 40%/s) | Deep processing in that subsystem | Step 3 — find the culprit in that phase |
| All phases near zero, `stolen` high, `lateWakes` > 0 | Host is stealing CPU | Host problem; see step 0 actions |
| `gcPauseMs` high, gen2 counts rising | GC pressure — something is allocating heavily | Step 3 on the allocating phase, or dotnet-counters for alloc rate |
| Iterations ≫ sleeps while shard is idle | The loop is not sleeping: a queue never drains or a wake storm | Check `IsIdle` inputs; a stuck signal in the ring is the historical example |
| Sleeps ≈ iterations, each sleep ~0ms | Spurious wake storm | Ring backend issue; count `wakesIssued` vs actual cross-thread posts |
| Everything normal, complaint persists | Not the event loop | Look at the network path, client, or DB/save timing |

**Wheel lag vs player lag:** `wheelLagMaxMs` is how late timer callbacks fired. Receives are
handled the moment they arrive (they wake the loop), so player-felt lag with a clean wheel points
away from the loop entirely.

## Step 3 — Find the culprit inside a hot phase

Add a temporary culprit hook rather than reaching for a tracer. The pattern: same
`[Conditional("EVENT_LOOP_PROFILING")]` attribute, own file or the profiler file, record only the
worst offender per second (identity + duration), never a per-event log. Examples:

- `TimerSlice` hot → time each timer callback, keep the max and its `timer.ToString()`.
- `NetworkSlice` hot → time packet handlers by packet id, keep the max.
- GC pressure → `dotnet-counters monitor --counters System.Runtime` for alloc rate first; it is
  cheap and often names the culprit generation without a trace.

Keep the hook after the hunt if it earns its cost in the profiling build; delete it otherwise.

## Step 4 — dotnet-trace, last and targeted

Only when a hot phase resists the culprit hook. Know the costs: EventPipe visibly slows the
process (worst exactly when things are already bad) and adds artifacts to the trace — on small
vCPU hosts the tracer's own threads appear as hotspots and Rider/PerfView hotspot views can
mislead. Mitigate by being narrow:

- Trace the specific minutes the decomposition flagged, not "a while".
- `dotnet-trace collect --profile cpu-sampling --duration 00:00:30` is usually enough.
- Compare against a trace of a good minute (same rule as step 1: no baseline, no conclusions).

## The RAM / GC misconception (read before declaring a leak)

ModernUO allocates very little, and the GC collects opportunistically — mostly during idle sleeps
and world saves. Under a spinning loop (`eventLoopIdleWaitMs=0`, or the pre-2026 default) the GC
may find **no** natural pause point: memory climbs to a large fraction of physical RAM, a forced
collection eventually drops part of it, and fragmentation keeps the baseline permanently above
where it started. Task manager shows alarming numbers; the in-game numbers do not. **Performance
is unaffected — this is lazy collection working as designed, not a leak.** Idle sleeping largely
removes the effect because every sleep is a natural GC opportunity. Before investigating "a leak":
check `gen0/1/2` and `gcPauseMs` in the decomposition, and compare working set *after a world
save*, which forces the collection the spin loop never allowed.

## Rules of thumb

- Never trade always-on profiling for the numbers. Production carries one timestamp per sleep and
  nothing else; everything heavier lives behind the build flag or on the `measure/event-loop`
  branch (full harness, A/B scripts, vendored ring experiments).
- One decomposition chart beats a thousand log lines. Resist adding warnings the reader cannot
  act on; the three production signals are deliberate.
- When filing or reporting: attach the baseline CSV and the episode CSV. Relative statements
  ("TimerSlice went from 4% to 61% during the episode") are the useful form.
