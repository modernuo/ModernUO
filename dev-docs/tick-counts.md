# Tick Counts: Overflow and Huge Starting Values

Rules for any code that compares `Core.TickCount` / `Core.GetTimestamp()` values. Getting this
wrong produces bugs that only appear on specific cloud hosts after long host uptimes — the worst
kind to reproduce.

## Why this matters (the Linux/cloud problem)

`Core.GetTimestamp()` is built on `Stopwatch.GetTimestamp()`, which on Linux reads the kernel's
monotonic clock — and on some hypervisors, notably **Google Cloud**, the VM receives a
**pass-through of the host's never-resetting counter**. The tick count is *not* zero when the
process starts and *not* zero when the operating system booted; it is however long the physical
host has been up, which can be months or years. We have been burned by this in production.

Consequences:

- Raw values are enormous from the first read. Arithmetic that would "never overflow in 292
  years" of process uptime can overflow immediately (`Core.GetTimestamp()`'s `UInt128`
  conversion path exists precisely because `raw * 1000` does not fit in 64 bits for large raws).
- Wrapped values can be **negative**. Nothing may assume a tick count is positive.
- **Windows is not affected** in our testing so far, which is exactly why this class of bug
  ships: it works on every dev machine and fails on a customer's GCP instance.

## The rules

1. **Compare by subtraction, never directly.** Subtraction of two ticks wraps correctly in two's
   complement; direct comparison does not.

   ```csharp
   // WRONG: fails when ticks wrap or start huge
   if (Core.TickCount < deadline)

   // RIGHT: wraparound-safe
   if (Core.TickCount - deadline < 0)
   ```

2. **Durations are always subtractions of two readings** (`elapsed = end - start`). Never derive
   a duration from a single absolute value.

3. **No zero or sign sentinels.** `if (_lastEventAt > 0)` as "has this happened yet" breaks when
   ticks are negative. Track "has happened" with a separate `bool` or an existing counter.

4. **Seed deadline fields from a real tick, not from field initialization.** A `long _deadline;`
   left at 0 compares wrong against a huge or negative tick. Initialize relative to the first
   observed timestamp (see the schedule-state seeding in `Core.Setup`).

5. **Store deadlines as `start + interval` only if every comparison follows rule 1.** The
   addition may wrap; the subtraction comparison handles it.

## Reviewing for it

Grep the diff for `TickCount <`, `TickCount >`, `GetTimestamp() <`, and comparisons against any
field whose name suggests a deadline (`*Until`, `*At`, `*Next*`). Each hit must be in subtraction
form. `DateTime`/`DateTimeOffset` comparisons are unaffected; this applies only to the monotonic
tick domain.
