# Measuring the Event Loop

> **This branch vendors IORingGroup as source under `Projects/IORingGroup/` and is not for
> merging.** It exists so the scheduling design can be measured on real hardware before any
> IORingGroup version is published, since each published version would otherwise need its own
> measurement pass and a wrong guess would be stuck on the NuGet feed permanently. Clone and
> `dotnet build` — there is no package to restore. The sources are byte-identical to the upstream
> `perf/wake-and-timer` branch; the real PR will restore the `PackageReference` and delete that
> directory.

The game loop can be switched between two schedulers at runtime, so you can measure both on your
own hardware without rebuilding. If you are trying to work out whether ModernUO or your host is
responsible for lag, this is the first thing to run.

## The two schedulers

| `server.eventLoopIdleWaitMs` | Behaviour |
|---|---|
| `0` | **Legacy.** Only considers sleeping once every 100 iterations, and only when the next timer tick is at least 2 ms away. Spins through the loop body the rest of the time. |
| `2` (default) | **Idle sleeping.** Sleeps whenever every queue is empty, waking on the next timer tick or the instant work arrives, whichever comes first. |

Both are the same binary. Only the setting changes.

## What to measure

Two numbers matter, and they are not the same thing.

**Process CPU** is how much of a core the server burns. On a dedicated box a spinning loop is
merely wasteful; on a shared or burstable VPS it drains CPU credits and gets you throttled, which
is what most "random lag spikes on a cheap VPS" reports turn out to be.

**Tick lag** is how far behind schedule the timer wheel actually turned. This is the number that
corresponds to what players feel. A loop can spin at a million iterations per second and still be
lagging; a loop that sleeps most of the time can be perfectly healthy.

Do not use **cycles per second (CPS)** as a health signal. It counts loop iterations, so it is
dominated by the scheduler rather than by load — see [Reading the numbers](#reading-the-numbers).

## Turn on reporting

```json
"server.loopStatsIntervalSeconds": "15"
```

Logs a line every 15 seconds (`0` disables it):

```
loop: cpu=0.4% cps=422 tickLagPeak=4ms tickLagNow=1ms sleeps=6581/6588 (99.9%) wakes=0 elided=0 idleWait=2ms
```

`cpu` is percent of a single core since the previous line, `tickLagPeak` is the worst tick lag in
that window, and the window resets each time so an old spike does not pin the number.

`sleeps` is how many loop iterations actually blocked, `wakes` is how many cross-thread posts
signalled the ring, and `elided` is how many skipped the signal because they came from the loop
thread and therefore could not have needed one.

## Will a busy shard regress?

The scheduling change costs something only where it does something, and these counters show which.

**Under sustained load the loop stops sleeping.** `sleeps` approaches 0 because the queues are
never all empty at once — 500 players generate a continuous stream of pending sends. At that point
the loop is running exactly as it did before, and the only added per-iteration work is the idle
check, which is four `Count` comparisons.

**Constant networking generates no wakes at all.** Packet handling runs inline on the loop thread
(`NetState.Slice` → `HandleReceive` → `HandlePacket`), as do timers (`Timer.Slice` → `Turn` →
`OnTick`). Neither goes through `LoopContext.Post`, so neither can trigger a wake. Traffic volume
is simply not connected to wake volume.

Wakes come only from cross-thread work: async continuations resuming on the loop, and a handful of
explicit posts such as the start and end of a world save. Those are rare by construction. Posts
originating **on** the loop thread are elided outright — the loop is executing that call, so it
cannot be blocked, which makes the check exact rather than a heuristic.

So the shape to expect on a busy shard is `sleeps` near 0 and `wakes` low. To confirm on your own
shard:

1. Run at peak population with `server.loopStatsIntervalSeconds` enabled.
2. Check `sleeps` — if it is near 0, idle sleeping is dormant and cannot be costing you anything.
3. Check `wakes` — if it is high while `sleeps` is near 0, signals are being issued that nobody is
   waiting on. Report it; that is the case worth optimising and it is not expected.
4. Compare `tickLagPeak` and `cpu` against a run at `server.eventLoopIdleWaitMs=0`. Equal or better
   on both means no regression.

In game, `[LoopStats` prints the same values on demand and `[LoopStatsLog 15` starts periodic
logging without a restart.

Both use `Environment.CpuUsage`, which reads the process' own accounting. **Do not** build a probe
that calls `Process.Threads` or `Process.GetProcesses` on a timer — that enumerates every process
and thread on the machine, and has been measured costing several percent of the main thread and
causing the very stalls it was added to diagnose.

## Running an A/B

For a clean comparison, hold everything else constant:

- `"autosave.enabled": "False"` — a save would dominate the sample
- `"pathfinding.prebakeMaps": "False"` — a startup-only CPU burn
- Same world, same player count, same machine, back to back

Then run each scheduler for at least 60 seconds after a 45-second warmup, and compare.

### Windows

```powershell
pwsh tools/Measure-EventLoop.ps1 -WarmupSeconds 45 -SampleSeconds 60
```

Boots the shard twice, samples `TotalProcessorTime` over the window, and prints both results plus
the ratio.

### Linux and macOS

Start the server, wait for the world to finish loading, then sample the process:

```bash
# 60-second CPU sample of an already-running shard
pid=$(pgrep -f ModernUO)
t0=$(ps -o cputime= -p "$pid"); sleep 60; t1=$(ps -o cputime= -p "$pid")
echo "cpu before=$t0 after=$t1"
```

or watch it live with `top -pid "$pid"` (macOS) / `top -p "$pid"` (Linux). Change
`server.eventLoopIdleWaitMs` between runs and restart.

macOS uses the kqueue backend and Linux uses io_uring (falling back to epoll), so these are worth
running on their own rather than assuming the Windows result carries over — the wake path is
different code on each.

## Reading the numbers

Measured on a Windows desktop, real world of 190,728 items and 33,158 mobiles, no players
connected, saves off:

| | Legacy (`0`) | Idle sleeping (`2`) |
|---|---|---|
| CPU | 10.4% of one core | 1.0% of one core |
| CPS | ~1,100,000 | ~400 |
| Tick lag (peak per 15s) | 4–10 ms | 5–11 ms |

Three things to take from that:

**The CPU is not traded for latency.** Tick lag is unchanged. The loop was not doing useful work
with those cycles.

**CPS inverts, and is not a health metric.** It counts iterations, so the legacy loop reports a
number a thousand times larger while doing the same amount of real work. If you have dashboards
keyed on "CPS dropped, something is wrong", they will read backwards after this change. Watch tick
lag instead. CPS was left as-is rather than silently redefined, because third-party shards read it.

**Your ratio will differ, and slower hosts gain more.** The legacy loop reaches its sleep check
after 100 iterations; the slower each iteration, the longer it spins first. On the fast desktop
above that is 10% of a core. On a 3 vCPU VPS the same build was profiled at roughly 70% of a core
with a single player connected. If your host is cheap, expect the larger win.

## If tick lag is high in both modes

The loop is not your problem. Likely causes, in rough order:

1. **The host is not scheduling you.** Common on burstable/shared vCPU plans. Check steal time
   (`top`, `%st` on Linux) and whether your plan has CPU credits.
2. **A custom system is blocking the loop.** Anything doing file or network I/O, large scans, or
   `Process` enumeration inside a timer tick or command handler.
3. **Saves.** Re-enable `autosave.enabled` and see whether spikes line up with save intervals.
4. **You are undersized.** See [server-requirements.md](server-requirements.md).
