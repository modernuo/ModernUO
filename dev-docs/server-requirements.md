# Server Requirements

Hardware guidance for running a ModernUO shard.

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
than spinning. That change disproportionately helps small hosts.

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

- **Windows Server 2012 R2 and 2016 sleep via a raised timer resolution.** Sleeping for a couple
  of milliseconds prefers a high-resolution waitable timer, which requires Windows 10 1803 /
  Server 2019. On older versions the ring falls back to `timeBeginPeriod(1)`, which raises the
  system timer resolution to 1 ms so the plain wait timeout is accurate enough. The trade-off is a
  higher interrupt rate (system-wide on those versions) — an acceptable price on a dedicated game
  server, and the reason the high-resolution timer is preferred where it exists.

  Only if *both* mechanisms fail does the server detect it at startup, log it, and spin instead —
  the same behaviour as setting `server.eventLoopIdleWaitMs` to 0: a full core at idle, and zero
  missed deadlines. A host that claims short waits but cannot deliver them is caught at runtime by
  the adaptive backoff.
- **Linux kernel 6.1** or newer (Debian 12 and equivalents). io_uring is used where available, with
  automatic epoll fallback.

## Tuning for a small host

| Setting | Default | Why change it |
|---|---|---|
| `server.eventLoopIdleWaitMs` | `2` | `0` never sleeps: ~98% of one core, but zero skipped timer slots and zero lag. The choice for a large shard on dedicated CPU that would rather spend a core than risk a late wake. Above `2` the wheel starts losing slots. |
| `server.lateWakeThreshold` | `1` | Floor for the backoff: idle waits the host may return a full tick late, per second, before the rate test below applies at all. Raise on a jittery host; set very high to disable the backoff. |
| `server.lateWakePercent` | `10` | Share of a second's idle waits that must come back late before idle sleeping backs off. An idle loop sleeps hundreds of times a second, so a bare count cannot tell a few tail outliers from a host that never schedules the process — a genuinely bad host misses *most* of its waits. `0` leaves `lateWakeThreshold` in sole charge. |
| `world.useMultithreadedSaves` | `true` | Set `false` on 2-core hosts so saves do not contend with the game loop. |
| `pathfinding.prebakeMaps` | varies | Leave off on memory-constrained hosts; it peaks above 1 GB while baking. |
| `network.sendBufferSize` | 256 KB | Lower it if you are memory-bound with many connections. |
| `autoArchive.*` retention | 24h/30d/12m | Reduce if disk is tight. |

## Am I undersized?

Watch the log. The server warns when the host returns idle waits late and suspends idle sleeping,
and says so at startup if the host cannot honour short waits at all. Those warnings mean the host
is not scheduling the process promptly — typical of burstable or shared vCPU plans — and no
server-side change fixes that. For anything deeper, see
[debugging-event-loop.md](debugging-event-loop.md).
