# Measuring the Event Loop

> **This branch vendors IORingGroup as source under `Projects/IORingGroup/` and is not for
> merging.** It exists so the scheduling design can be measured on real hardware before any
> IORingGroup version is published, since each published version would otherwise need its own
> measurement pass and a wrong guess would be stuck on the NuGet feed permanently. Clone and
> `dotnet build` — there is no package to restore. The sources are byte-identical to the upstream
> `perf/wake-and-timer` branch; the real PR will restore the `PackageReference` and delete that
> directory.

The game loop sleeps when it has nothing to do, and runs flat out when it does. This page covers the
one setting that controls it, the metric that tells you whether it is healthy, and how to measure
both on your own hardware.

## The lever

`server.eventLoopIdleWaitMs` is the longest the loop will block when every queue is empty. It wakes
on the next timer tick or the instant work arrives, whichever comes first, so this only caps how
long a *quiet* loop stays blocked.

Measured on an idle shard with a real world loaded (190,728 items, 33,158 mobiles):

| Setting | CPU | Skipped slots / 1875 | Peak lag |
|---|---|---|---|
| `0` | **98.5%** of one core | **0** | **0 ms** |
| `2` (default) | ~0.8% | 0–1 | 1–12 ms |
| `8` | 0.16% | ~1 | up to 23 ms |

**`0` never sleeps.** It is the way out of this entire apparatus: no sleeping, no wake signals, no
backoff. It buys something real — literally zero skipped slots and zero lag — for the price of a
core. A large shard on dedicated CPU that would rather spend the core than ever risk a late wake
should set it and stop reading here.

**`2` is the default** because it is where the trade stops being free. Below it, CPU rises for no
measurable latency gain. Above it, the wheel starts losing slots and the peak grows. On an idle
shard `2` loses about one slot in 7,500 — and so does pure spinning, because that floor is the
operating system, not the scheduler.

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

**Cycles per second is not a health signal at all.** It counts loop iterations, so once the loop
sleeps it is paced by `eventLoopIdleWaitMs` rather than by anything about your shard: roughly 400 at
the default, whether the world is empty or busy but keeping up. It is retained because existing
tooling reads it. Do not build alerts on it.

## Automatic backoff

Sleeping can only make latency worse in one way: the wait may return late, and a late return loses a
slot that a spinning loop would have caught. So the loop watches for exactly that.

If more than `server.skippedTickThreshold` slots (default 2) are lost in a second, across **two
consecutive** samples, idle sleeping suspends for five seconds and the loop spins instead. It logs:

```
Event loop is losing timer slots (N in 1000ms); idle sleeping suspended for 5000ms
```

Two samples rather than one because startup legitimately loses slots — the wheel is initialised
before the world loads, so the loop's first pass turns it once for every 8 ms that loading took, and
tiered JIT keeps things lumpy for a while after. Neither is a reason to abandon sleeping.

Under sustained load the backoff is inert, because the queues are never empty and the loop was not
sleeping anyway.

## Turn on reporting

```json
"server.loopStatsIntervalSeconds": "15"
```

Logs a line every 15 seconds (`0` disables it):

```
loop: cpu=1.0% cps=416 skippedTicks=0/1875 tickLagPeak=1ms sleeps=6208/6216 (99.9%) wakes=0 elided=0 backoffs=0 idleWait=2ms
```

- `cpu` — percent of one core since the previous line
- `skippedTicks` — slots lost / total turns. **This is the health number.**
- `sleeps` — iterations that actually blocked. Near zero under load means idle sleeping is dormant.
- `wakes` / `elided` — cross-thread posts that signalled the ring, and posts skipped because they
  came from the loop thread and could not have needed one.
- `backoffs` — times sleeping has been suspended, with `(SUSPENDED)` while it is.

In game, `[LoopStats` prints the same on demand and `[LoopStatsLog 15` starts logging without a
restart.

Both use `Environment.CpuUsage`, which reads the process' own accounting. **Do not** build a probe
that calls `Process.Threads` or `Process.GetProcesses` on a timer — that enumerates every process
and thread on the machine, and has been measured costing several percent of the main thread and
causing the very stalls it was added to diagnose.

## Will a busy shard regress?

**Constant networking generates no wakes at all.** Packet handling runs inline on the loop thread
(`NetState.Slice` → `HandleReceive` → `HandlePacket`), as do timers (`Timer.Slice` → `Turn` →
`OnTick`). Neither goes through `LoopContext.Post`, so traffic volume is not connected to wake
volume. Wakes come only from cross-thread work — async continuations and a handful of explicit posts
such as world-save boundaries. Posts originating *on* the loop thread are elided outright, which is
exact rather than heuristic: the loop is executing that call, so it cannot be blocked.

**Under sustained load the loop stops sleeping**, because the queues are never all empty at once.
At that point it runs exactly as it did before, and the only added per-iteration work is the idle
check — four `Count` comparisons.

To confirm on your own shard, at peak population:

1. Check `sleeps` — near 0 means idle sleeping is dormant and cannot be costing you anything.
2. Check `skippedTicks` — this is the regression signal, not CPU and not CPS.
3. Check `backoffs` — repeated backoffs mean the loop keeps deciding it is losing slots, which is
   worth reporting.
4. If you want certainty, set `eventLoopIdleWaitMs=0` and compare `skippedTicks`. Equal means the
   scheduler costs you nothing at that load.

## Measuring on your hardware

For a clean comparison, hold everything else constant: `"autosave.enabled": "False"`,
`"pathfinding.prebakeMaps": "False"`, same world, same population, back to back.

### Windows

```powershell
pwsh tools/Measure-EventLoop.ps1 -WarmupSeconds 45 -SampleSeconds 60
```

Boots the shard twice, samples `TotalProcessorTime` over the window, and prints both results.

### Linux and macOS

Start the server, wait for the world to finish loading, then sample the process:

```bash
pid=$(pgrep -f ModernUO)
t0=$(ps -o cputime= -p "$pid"); sleep 60; t1=$(ps -o cputime= -p "$pid")
echo "cpu before=$t0 after=$t1"
```

or watch it with `top -pid "$pid"` (macOS) / `top -p "$pid"` (Linux). Change the setting between
runs and restart.

macOS uses kqueue and Linux uses io_uring (falling back to epoll), so these are worth running on
their own rather than assuming the Windows result carries over — the wake path is different code on
each.

## If skipped slots are high regardless of setting

The loop is not your problem. In rough order:

1. **The host is not scheduling you.** Common on burstable/shared vCPU. Check steal time (`%st`).
2. **A custom system is blocking the loop** — file or network I/O, large scans, or `Process`
   enumeration inside a timer tick or command handler.
3. **Saves.** Re-enable `autosave.enabled` and see whether spikes line up with save intervals.
4. **You are undersized.** See [server-requirements.md](server-requirements.md).
