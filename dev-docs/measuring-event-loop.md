# Measuring the Event Loop

> **This is the perennial `measure/event-loop` branch** — main plus the measurement harness, kept
> for future loop work and periodically rebased onto main. The idle-sleep design and the
> `EVENT_LOOP_PROFILING` diagnostics tier shipped in main (see
> `dev-docs/debugging-event-loop.md` for day-to-day diagnosis); this branch adds the A/B
> measurement scripts below (`tools/Measure-EventLoop.ps1`, `tools/measure-event-loop.sh`,
> `tools/HostLatencyProbe.cs`).
>
> When a future experiment needs to iterate on IORingGroup itself without burning published
> package versions, re-vendor it here: copy the ring branch's `IORingGroup/*.cs` byte-identical
> into `Projects/IORingGroup/`, give it a minimal csproj that inherits `Directory.Build.props`
> (needs `LangVersion` `Preview` and `NoWarn` `CS8981;CS0649` under `TreatWarningsAsErrors`),
> swap Server.csproj's `PackageReference` for a `ProjectReference`, and add the project to
> `ModernUO.slnx`. Revert = delete the directory and restore the `PackageReference`.

The game loop sleeps when it has nothing to do, and runs flat out when it does. This page covers the
one setting that controls it, the metric that tells you whether it is healthy, and how to measure
both on your own hardware.

## The lever

`server.eventLoopIdleWaitMs` is the longest the loop will block when every queue is empty. It wakes
on the next timer tick or the instant work arrives, whichever comes first, so this only caps how
long a *quiet* loop stays blocked.

Measured on an idle shard with a real world loaded (190,728 items, 33,158 mobiles):

| Setting | CPU | Skipped slots / 1875 | Peak lag (avg over windows) |
|---|---|---|---|
| `0` | **98.5%** of one core | **0** | **0 ms** |
| `1` | 0.81% | 0–1 | 7.8 ms |
| `2` (default) | ~0.8% | 0–1 | 11.2 ms |
| `4` | 0.57% | 0–1 | 7.0 ms |
| `8` | 0.16% | ~1 | 9.8 ms |

**The trade-off is not a smooth slope.** `0` is qualitatively different — never sleeping means
never waking late, so zero skipped slots and zero lag. Everything from `1` to `8` sits in the same
band: peaks of 7–11 ms with no trend, and roughly one lost slot in several thousand. The differences
across that range are measurement noise, not a dial. Pick `0` or pick a sleep value; the specific
sleep value barely matters for latency and matters a lot for CPU.

**`0` is the way out of this entire apparatus** — no sleeping, no wake signals, no backoff — for the
price of a core. A large shard on dedicated CPU that would rather spend the core than ever risk a
late wake should set it and stop reading here.

**`2` is the default** because it is the conservative end of that band while still costing almost
nothing. On an idle shard it loses about one slot in 7,500 — and so does pure spinning, because that
floor is the operating system, not the scheduler.

## The metric that matters: skipped slots

The timer wheel advances one slot per 8 ms of elapsed time. A **skipped slot** is one that came due
while the loop was elsewhere, counted as turns beyond the first in a single pass.

That distinction is the point. A wake can never land exactly on an 8 ms boundary, so the wheel is
always a fraction late and "lag" is always non-zero. Losing a *slot* is different: the wheel took a
step it should have taken earlier. Read as a rate it is directly meaningful — 125 slots per second
at an 8 ms tick, so a handful per minute is jitter and hundreds per minute is a server that cannot
keep up.

**Peak tick lag is reported but is a weak signal.** It is a single worst case over the whole window,
so one hiccup pins it and it reads identically whether the server stumbled once or is permanently
behind. Use it to size an outlier, not to judge health.

### Wheel lag is not network latency

These are separate paths, and conflating them is the easiest mistake to make here.

A player's action arrives as a packet. The receive completion is in the loop's wait set, so it wakes
the loop **immediately** — not after `eventLoopIdleWaitMs` elapses — and the packet is handled inline
by `NetState.Slice` → `HandleReceive` → `HandlePacket`. Sleeping longer does not add round-trip
latency, because a sleeping loop is woken by the thing it was waiting for.

Wheel lag only delays **timer-driven** logic: combat swings, spell timers, AI ticks, spawners. Those
are built around 100 ms to multi-second intervals, so 8–23 ms of wheel lag is well inside their
noise floor and is not something a player can perceive.

The one path that genuinely waits out the sleep is a **new connection**, because AcceptEx
completions are polled rather than signalled. That adds up to `eventLoopIdleWaitMs` to connection
setup — single-digit milliseconds on a handshake that takes far longer.

Round-trip latency under real player load has not been measured end to end here; the above is read
off the code paths, not off a wire capture.

**Cycles per second is not a health signal at all.** It counts loop iterations, so once the loop
sleeps it is paced by `eventLoopIdleWaitMs` rather than by anything about your shard: roughly 400 at
the default, whether the world is empty or busy but keeping up. It is retained because existing
tooling reads it. Do not build alerts on it.

### The first time any code path runs, it will probably miss

A freshly started shard misses the budget on paths it has never executed: the first login, the
first character creation, the first time a particular gump is opened. That is tier-0 JIT and
first-touch static initialisation, not the cost of the operation.

Profiling caught this directly. A 113 ms stall in a house-placement gump was two thirds
`InitClassSlow` and `GetGCStaticBaseSlow` — running static constructors — and only a third the gump
logic itself. Character creation on an M1 shows the same shape: a few missed deadlines the first
time, from a workload that is nowhere near 24 ms of actual work.

To tell them apart, run the same operation two or three times in one session. Cold-path cost
disappears on the second run; real cost does not. You do not need to reach tiered-compilation
maturity to see the difference, just the cold-to-warm transition.

### The 16 ms bar is stricter than the game needs

This measures **timer accuracy, not network latency**. A missed budget means whatever timers were
sitting in those wheel slots fired late — it does not mean a packet was delayed, since packets are
handled inline and wake a sleeping loop immediately.

UO systems run on 100 ms to multi-second cadences: combat swings, AI ticks, spawners, decay. A 24 ms
wheel stall is inside their noise floor, and invisible next to a typical 50 ms ping. The bar is set
where it is because it makes a sensitive detector that catches problems long before players would,
not because 16 ms is a threshold players can perceive.

So an occasional missed budget is not a defect. What matters is the **rate**, and whether the
misses coincide with sleeps that returned late. Sustained misses are worth chasing; a handful on a
cold path is the metric working.

### Self-inflicted stalls are expected, and are not the scheduler's fault

A world save can block the loop for a second or more on a large shard. A staff command like
advanced search is knowingly expensive. Both blow the 16 ms budget, sometimes badly — the wheel
then catches up by turning many times in one pass, because ModernUO does **not** drift timers and
should not start.

Those misses are real. What they are not is a reason to stop sleeping — and the backoff cannot be
tripped by them **by construction**: a sleep is bounded by the time to the next wheel turn, so a
correctly honoured sleep can never miss a deadline. The only thing the backoff watches is the
sleep itself returning late (`elapsed - requested >= tick rate`), which is measured around each
`WaitForCompletion` call and can only be caused by the host descheduling the process.

## Automatic backoff

If the host returns more than `server.lateWakeThreshold` waits late (default 1) per second, across
**two consecutive** samples, idle sleeping suspends with escalating duration (5s doubling to 2min)
and the loop spins instead. It logs:

```
This host returned a 2ms idle wait at least 8ms late N time(s) in the last second; idle sleeping suspended for 5000ms
```

Under sustained load the backoff is inert, because the queues are never empty and the loop was not
sleeping anyway. At the escalation ceiling it logs an error telling the operator to set
`server.eventLoopIdleWaitMs=0`.

## Turn on reporting

Build with the profiling flag and use the in-game command:

```
dotnet build -p:EventLoopProfiling=true
[LoopStats
```

`[LoopStats` prints the last minute's wall-time decomposition — sleep / GC / stolen percentages
and per-phase work with worst-second peaks — and writes the full ~15-minute per-second history to
a CSV for comparison. Normal builds compile all of this out (`[Conditional]` hooks), so there is
nothing to disable in production. See `dev-docs/debugging-event-loop.md` for how to read it.

**Do not** build a probe that calls `Process.Threads` or `Process.GetProcesses` on a timer — that
enumerates every process and thread on the machine, and has been measured costing several percent
of the main thread and causing the very stalls it was added to diagnose.

## Will a busy shard regress?

**Constant networking generates no wakes at all.** Packet handling runs inline on the loop thread
(`NetState.Slice` → `HandleReceive` → `HandlePacket`), as do timers (`Timer.Slice` → `Turn` →
`OnTick`). Neither goes through `LoopContext.Post`, so traffic volume is not connected to wake
volume. Wakes come only from cross-thread work — async continuations and a handful of explicit
posts such as world-save boundaries. Posts originating *on* the loop thread are elided outright,
which is exact rather than heuristic: the loop is executing that call, so it cannot be blocked.

**Under sustained load the loop stops sleeping**, because the queues are never all empty at once.
At that point it runs exactly as it did before, and the only added per-iteration work is the idle
check plus one timestamp read per (rare) sleep.

To confirm on your own shard, at peak population, with the profiling build:

1. Check sleeps/second in `[LoopStats` — near 0 means idle sleeping is dormant and cannot be
   costing you anything.
2. Check `lateWakes` and worst wheel lag — the regression signals, not CPU.
3. Repeated backoff warnings in the log mean the host keeps returning waits late, which is worth
   reporting.
4. If you want certainty, set `eventLoopIdleWaitMs=0` and compare wheel lag. Equal means the
   scheduler costs you nothing at that load.

## Measuring on your hardware

For a clean comparison, hold everything else constant: `"autosave.enabled": "False"`,
`"pathfinding.prebakeMaps": "False"`, same world, same population, back to back.

### Before you start, on Linux

Two native libraries are needed, and their absence looks alarming — the build succeeds and the
tests then fail with `DllNotFoundException`, which reads like a broken branch rather than a missing
package. The resolver wants the unversioned `.so`, so the `-dev` packages are the ones that matter:

```bash
sudo apt-get install -y libdeflate-dev libargon2-dev   # Debian/Ubuntu
```

Also **clone with full history**. A `--depth 1` clone fails the build in Nerdbank.GitVersioning,
which needs the commit history to compute a version, and the error points at MSBuild internals
rather than at the clone.

Verified in a clean container: with those two packages and a full clone, this branch builds and
passes 826 Server.Tests and 642 UOContent.Tests on Linux, matching Windows.

### Windows

```powershell
pwsh tools/Measure-EventLoop.ps1 -WarmupSeconds 45 -SampleSeconds 60
```

Boots the shard twice, samples `TotalProcessorTime` over the window, and prints both results.

### macOS and Linux

```bash
./tools/measure-event-loop.sh 45 60
```

Same procedure as the Windows script: boots the shard twice, samples process CPU over the window,
and prints the `loop:` lines from both runs so the CPU figures can be read against what they cost
in timer accuracy. Needs `python3`, which ships with the macOS developer tools.

To sample a shard that is already running instead:

```bash
pid=$(pgrep -f ModernUO)
ps -o time= -p "$pid"; sleep 60; ps -o time= -p "$pid"
```

or watch it with `top -pid "$pid"` (macOS) / `top -p "$pid"` (Linux).

**Run these rather than assuming the Windows result carries over.** The wake path is different code
on every backend — `EVFILT_USER` on kqueue, an eventfd on io_uring and epoll, an event object on
Windows — and so is the cost structure the loop sits on. Windows pays a syscall per iteration in the
accept scan that the others do not, and the timer-resolution problem that shapes the Windows
implementation has no equivalent elsewhere.

Before measuring on a platform for the first time, run the ring's own tests, which exercise the wake
contract natively:

```bash
dotnet test IORingGroup.Tests/IORingGroup.Tests.csproj
```

`WakeBeforeWaitIsNotLost` and `WakeFromAnotherThreadUnblocksWait` are the ones that matter. If the
platform's wake primitive is broken, those fail and the loop would otherwise appear merely
"slow to notice work" for no visible reason.

## If skipped slots are high regardless of setting

The loop is not your problem. In rough order:

1. **The host is not scheduling you.** Common on burstable/shared vCPU. Check steal time (`%st`).
2. **A custom system is blocking the loop** — file or network I/O, large scans, or `Process`
   enumeration inside a timer tick or command handler.
3. **Saves.** Re-enable `autosave.enabled` and see whether spikes line up with save intervals.
4. **You are undersized.** See [server-requirements.md](server-requirements.md).
