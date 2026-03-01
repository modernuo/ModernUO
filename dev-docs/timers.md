# ModernUO Timer System

This document covers ModernUO's timer system: the timer wheel scheduler, delay calls, timer execution tokens, and patterns for timer usage in game content.

## Overview

ModernUO uses a 3-layer hierarchical timer wheel for scheduling delayed and recurring actions. The system is single-threaded and processes timers during each game loop tick.

> **Timer vs EventScheduler**: The timer wheel is for **game-tick** delays and repeats (8ms precision, sub-second to ~16 days). For **wall-clock/calendar** scheduling — daily resets, weekly events, holiday seasons — use `EventScheduler` instead (1-second granularity, timezone-aware, calendar recurrence patterns). See `dev-docs/event-scheduler.md`.

## Architecture

### Timer Wheel
3-layer wheel with 4096 slots per layer:

| Layer | Resolution | Range |
|---|---|---|
| 0 | 8ms | ~32.8 seconds |
| 1 | ~32.8s | ~22 minutes |
| 2 | ~22m | ~16 days |

- Tick rate: 8ms (minimum precision)
- All delays are rounded up to nearest 8ms boundary
- O(1) insert and remove operations

### Execution Flow
1. `Timer.Slice(tickCount)` called each game loop iteration
2. Wheel rotates to current slot
3. All timers in the slot are executed via `OnTick()`
4. Repeating timers are re-inserted at next interval
5. Finished timers call `OnDetach()` for cleanup

## API Reference

### Timer.StartTimer (Fire-and-Forget with Pooling)

Preferred for most use cases. Timers are automatically pooled for reuse.

```csharp
// Immediate execution
Timer.StartTimer(callback);

// Delayed execution
Timer.StartTimer(TimeSpan.FromSeconds(5), callback);

// Repeating
Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), callback);

// Repeating with count limit
Timer.StartTimer(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), 10, callback);

// Delayed start, then repeating
Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), callback);
```

### Timer.StartTimer with Token (Cancellable)

Use when you need to cancel the timer later.

```csharp
private TimerExecutionToken _token;

// Start with token
Timer.StartTimer(TimeSpan.FromSeconds(5), DoWork, out _token);

// Check state
if (_token.Running) { }
var remaining = _token.RemainingCount;  // int.MaxValue if infinite
var next = _token.Next;                  // DateTime of next tick
var index = _token.Index;                // Times ticked so far

// Cancel (safe to call multiple times)
_token.Cancel();
```

### Timer.DelayCall (Returns Timer Object)

Legacy-style API that returns the timer object directly.

```csharp
var timer = Timer.DelayCall(TimeSpan.FromSeconds(5), DoWork);
timer.Stop();  // Cancel

// With state parameters (avoids lambda allocation)
Timer.DelayCall(TimeSpan.FromSeconds(2), ProcessTarget, mobile, item);
Timer.DelayCall(TimeSpan.FromSeconds(1), DoWork, arg1, arg2, arg3);
// Supports up to 5 state parameters
```

### Timer.Pause (Awaitable)

For async/await patterns:

```csharp
await Timer.Pause(TimeSpan.FromMilliseconds(100));
await Timer.Pause(500);  // Milliseconds overload
```

Safe because `EventLoopContext` routes continuations to the main thread.

## TimerExecutionToken

Lightweight struct for tracking fire-and-forget timers:

```csharp
public struct TimerExecutionToken
{
    public bool Running { get; }         // Is timer still active?
    public int Index { get; }            // How many times OnTick fired
    public int RemainingCount { get; }   // Ticks remaining (int.MaxValue if infinite)
    public DateTime Next { get; }        // When next tick fires
    public void Cancel();                // Stop and return to pool
}
```

Key behaviors:
- `Cancel()` is safe to call multiple times
- Default value (`default(TimerExecutionToken)`) has `Running = false`
- NOT serializable -- restore in `[AfterDeserialization]`

## Patterns

### Pattern 1: Simple Delayed Action
```csharp
// Delete item after 10 seconds
Timer.StartTimer(TimeSpan.FromSeconds(10), Delete);
```

### Pattern 2: Cancellable Recurring Timer
```csharp
private TimerExecutionToken _checkTimer;

public MyItem() : base(0x1234)
{
    Timer.StartTimer(
        TimeSpan.FromSeconds(5),    // Initial delay
        TimeSpan.FromSeconds(5),    // Repeat interval
        CheckExpiry,                // Callback
        out _checkTimer             // Token for cancellation
    );
}

public override void OnAfterDelete()
{
    _checkTimer.Cancel();
    base.OnAfterDelete();
}

private void CheckExpiry()
{
    if (Core.Now >= _expireTime)
        Delete();
}
```

### Pattern 3: Timer Restoration After Deserialization
```csharp
[SerializationGenerator(0, false)]
public partial class TimedItem : Item
{
    private TimerExecutionToken _timer;  // NOT serialized

    [SerializableField(0)]
    [DeltaDateTime]
    private DateTime _expireTime;

    [Constructible]
    public TimedItem() : base(0x1234)
    {
        _expireTime = Core.Now + TimeSpan.FromHours(1);
        StartTimer();
    }

    private void StartTimer()
    {
        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), Check, out _timer);
    }

    [AfterDeserialization]
    private void AfterDeserialization() => StartTimer();

    public override void OnAfterDelete()
    {
        _timer.Cancel();
        base.OnAfterDelete();
    }
}
```

### Pattern 4: Serializable Timer Field
```csharp
[SerializableField(0, setter: "private")]
private Timer _decayTimer;

[DeserializeTimerField(0)]
private void DeserializeDecayTimer(TimeSpan delay)
{
    _decayTimer = Timer.DelayCall(delay, Delete);
    _decayTimer.Start();
}

public void BeginDecay(TimeSpan delay)
{
    _decayTimer?.Stop();
    _decayTimer = new InternalTimer(this, delay);
    _decayTimer.Start();
}

public override void OnAfterDelete()
{
    _decayTimer?.Stop();
    _decayTimer = null;
    base.OnAfterDelete();
}
```

### Pattern 5: Custom Timer Class
```csharp
private class DecayTimer : Timer
{
    private readonly Corpse _corpse;

    public DecayTimer(Corpse c, TimeSpan delay) : base(delay)
    {
        _corpse = c;
    }

    protected override void OnTick()
    {
        if (!_corpse.GetFlag(CorpseFlag.NoBones))
            _corpse.TurnToBones();
        else
            _corpse.Delete();
    }
}
```

### Pattern 6: State-Carrying Delay (No Lambda)
```csharp
// Instead of lambda (allocates closure):
Timer.StartTimer(TimeSpan.FromSeconds(2), () => ProcessTarget(from, target));

// Use state parameters (no allocation):
Timer.DelayCall(TimeSpan.FromSeconds(2), ProcessTarget, from, target);

private static void ProcessTarget(Mobile from, Mobile target)
{
    // Process...
}
```

## Timer Pool

Timers created via `Timer.StartTimer()` are pooled for reuse:
- Initial pool: 1024 timers (configurable: `timer.initialPoolCapacity`)
- Max pool: 16x initial (configurable: `timer.maxPoolCapacity`)
- Pool refills asynchronously when depleted
- `Timer.CheckTimerPool()` called each game loop to monitor

## ISerializableExtensions for Timers

```csharp
// Extension methods on ISerializable:
entity.Stop(timer);           // Stop + MarkDirty
entity.Start(timer);          // Start + MarkDirty
entity.Restart(timer, delay, interval);  // Stop + reconfigure + Start + MarkDirty
entity.Stop(ref timer);       // Stop + null + MarkDirty
```

## Common Mistakes

| Mistake | Problem | Fix |
|---|---|---|
| Serializing `TimerExecutionToken` | Build error / data corruption | Leave unserialized, use `[AfterDeserialization]` |
| Not cancelling on delete | Timer fires on deleted entity | Cancel in `OnDelete()`/`OnAfterDelete()` |
| Using `Thread.Sleep` | Blocks game loop | Use `await Timer.Pause()` |
| Creating timer in deserialization | Timer starts before world is ready | Use `[AfterDeserialization]` |
| Lambda in hot-path timer | Allocates closure every time | Use state parameters |

## Key File References
- Timer base class: `Projects/Server/Timer/Timer.cs`
- DelayCall + StartTimer: `Projects/Server/Timer/Timer.DelayCall.cs`
- Timer wheel: `Projects/Server/Timer/Timer.TimerWheel.cs`
- Pool management: `Projects/Server/Timer/Timer.Pool.cs`
- State timers: `Projects/Server/Timer/Timer.DelayStateCall.cs`
- Token: `Projects/Server/Timer/TimerExecutionToken.cs`
- Serializable extensions: `Projects/Server/Serialization/ISerializableExtensions.cs`
