# Server Requirements

Hardware guidance for running a ModernUO shard. If you are diagnosing lag rather than choosing a
host, start with [measuring-event-loop.md](measuring-event-loop.md).

## Tiers

| Use | vCPU | RAM | Storage |
|---|---|---|---|
| Development / test | 2 **dedicated** | 2 GB | SSD |
| Small live shard (< 50 concurrent) | 4 dedicated | 4 GB | NVMe |
| Medium (50–200) | 4–8 | 8 GB | NVMe |
| Large (200+) | 8+, high clock | 16 GB+ | NVMe |

These are starting points. Save size drives RAM more than player count does, and single-thread
clock speed drives tick latency more than core count does. Both are explained below.

## Dedicated vCPU, not burstable

This matters more than any other line on this page.

Budget VPS plans sold as "2 vCPU" are frequently shared or burstable: you get a CPU credit balance
or a cgroup quota, and once it is exhausted the hypervisor throttles you. Throttling shows up in
game as periodic freezes that correlate with nothing in your logs, and it is the single most common
cause of "ModernUO is laggy on my $3/month VPS".

Symptoms worth checking before blaming the server:

- Steal time above ~1% (`top`, the `%st` column on Linux)
- Lag that disappears when you move to a larger plan with the same core count
- Tick lag spikes with no matching CPU spike in the process itself

## Cores

Game logic is **single-threaded**. Every mobile, item, timer, and packet handler runs on one
thread, so a shard's headroom is bounded by how fast one core is. Two fast cores beat four slow
ones.

Cores beyond the first are used by:

- **World saves.** `world.useMultithreadedSaves` (default on) spins up `ProcessorCount - 1`
  serialization workers plus one inline on the main thread. On a 2-core box that is one worker; on
  a 2-core box with a large world, consider setting it to `false` so saves do not contend with the
  loop.
- **The .NET runtime.** Tiered JIT compilation (heaviest in the first minutes after boot) and
  background GC.
- **Everything else on the machine**, including your OS and, on Windows, antivirus.

Since ModernUO 2026 the loop sleeps when idle, so an empty shard costs roughly 1% of a core rather
than spinning. That change disproportionately helps small hosts — see
[measuring-event-loop.md](measuring-event-loop.md) for the numbers.

## Memory

Three things dominate, and only one of them scales with players.

**World size.** A world of ~190,000 items and ~33,000 mobiles loads in about a second and is not
itself large. Items and mobiles are the cheap part.

**Saves.** Each serialization worker pre-allocates a heap sized to its share of the last save, at
roughly 1.25× total save size, and those buffers are retained afterwards. A 400 MB save therefore
implies about 500 MB of resident serialization heap on top of the live world. **This is the reason
1 GB hosts are not viable for a real shard**, even though an empty one boots fine.

**Map residency.** `TileMatrix` reads map blocks from disk on demand and caches them permanently —
there is no eviction. Memory climbs toward full-facet residency as players explore. Felucca's land
tiles alone are around 117 MB, and statics are larger.

Optional systems can add substantially more. The pathfinding prebake
(`pathfinding.prebakeMaps`) peaks above 1 GB of heap while baking. Budget for it or leave it off on
small hosts.

Network buffers are minor by comparison: 64 KB receive plus a configurable 256 KB send
(`network.sendBufferSize`) per connection, so 100 players is roughly 32 MB.

ModernUO runs **Workstation GC**, which is the right default for small hosts. Do not switch to
Server GC on a 2-core box.

## Storage

Saves are write-heavy bursts. Cheap network-attached storage with throttled IOPS will stall the
save path, and `World.WaitForWriteCompletion` blocks the loop at shutdown. Use local NVMe or SSD.

Budget disk for: the world save, plus archives and backups if `autoArchive` is enabled (retention
defaults keep 24 hourly, 30 daily, and 12 monthly copies), plus the pathfinding cache if enabled.

## Operating systems

See the README for the full supported list. Two things are worth calling out:

- **Windows Server 2012 R2 and 2016 are supported, but do not get idle sleeping.** Sleeping for a
  couple of milliseconds needs a high-resolution waitable timer, which requires Windows 10 1803 /
  Server 2019. Without one, the only tool left is the plain wait timeout, and that rounds up to the
  15.625 ms system timer resolution — a 2 ms request would block for eight times as long and put
  the timer wheel permanently behind.

  So on those versions the server detects the absence at startup, logs it, and spins instead. That
  is the same behaviour as setting `server.eventLoopIdleWaitMs` to 0: a full core at idle, and
  zero missed deadlines. Everything else, including the accept-path improvements, applies
  normally — the detection happens once at startup, not per iteration, so there is no ongoing cost
  to running an older version.
- **Linux kernel 6.1** or newer (Debian 12 and equivalents). io_uring is used where available, with
  automatic epoll fallback.

## Tuning for a small host

| Setting | Default | Why change it |
|---|---|---|
| `server.eventLoopIdleWaitMs` | `2` | `0` never sleeps: ~98% of one core, but zero skipped timer slots and zero lag. The choice for a large shard on dedicated CPU that would rather spend a core than risk a late wake. Above `2` the wheel starts losing slots. |
| `server.skippedTickThreshold` | `2` | Timer slots the loop may lose per second before it stops sleeping and spins instead. Raise on a jittery host; set very high to disable the backoff. |
| `world.useMultithreadedSaves` | `true` | Set `false` on 2-core hosts so saves do not contend with the game loop. |
| `pathfinding.prebakeMaps` | varies | Leave off on memory-constrained hosts; it peaks above 1 GB while baking. |
| `network.sendBufferSize` | 256 KB | Lower it if you are memory-bound with many connections. |
| `autoArchive.*` retention | 24h/30d/12m | Reduce if disk is tight. |

## Am I undersized?

Enable `server.loopStatsIntervalSeconds` and watch tick lag. Sustained tick lag well above the 8 ms
tick rate, especially when process CPU is *not* correspondingly high, means the host is not giving
you the CPU you asked for. [measuring-event-loop.md](measuring-event-loop.md) walks through
separating that from a genuine server-side problem.
