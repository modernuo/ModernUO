---
name: modernuo-timers
description: >
  Trigger when creating delayed actions, recurring timers, or any time-based behavior. When using Timer.StartTimer, Timer.DelayCall, or TimerExecutionToken.
---

# ModernUO Timers & Scheduling

## When This Activates
- Creating delayed actions or recurring timers
- Working with `Timer`, `TimerExecutionToken`, `DelayCallTimer`
- Implementing decay, expiration, or periodic behavior
- Restoring timers after deserialization

## Key Rules

1. **Prefer `Timer.StartTimer` with token** for cancellable timers
2. **Prefer `Timer.StartTimer` without token** for fire-and-forget
3. **Never serialize `TimerExecutionToken`** -- restore in `[AfterDeserialization]`
4. **Always cancel timers in `OnDelete()`/`OnAfterDelete()`**
5. **8ms minimum precision** -- timer wheel uses 8ms tick rate
6. **Timers are NOT thread-safe** -- never Start/Stop timers or call `Timer.DelayCall`/`Timer.StartTimer` from any thread other than the game thread. This includes `Serialize()` which runs on background serialization threads during world saves.

## Preferred APIs (In Order)

### 1. Timer.StartTimer with Token (Cancellable Fire-and-Forget)
```csharp
private TimerExecutionToken _timerToken;

// One-shot
Timer.StartTimer(TimeSpan.FromSeconds(5), DoSomething, out _timerToken);

// Repeating
Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), CheckExpiry, out _timerToken);

// Repeating with count limit
Timer.StartTimer(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), 10, Tick, out _timerToken);

// Cancel
_timerToken.Cancel();  // Safe to call multiple times

// Check state
if (_timerToken.Running) { }
```

### 2. Timer.StartTimer without Token (Fire-and-Forget, No Cancel)
```csharp
Timer.StartTimer(Delete);                                    // Immediate
Timer.StartTimer(TimeSpan.FromSeconds(5), Delete);           // Delayed
Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Tick);  // Repeating
```

### 3. Timer.DelayCall (Returns Timer Object)
```csharp
var timer = Timer.DelayCall(TimeSpan.FromSeconds(5), DoSomething);
timer.Stop();  // Cancel later if needed
```

### 4. Timer.DelayCall with State (Avoids Closures)
```csharp
// Passes state without lambda allocation
Timer.DelayCall(TimeSpan.FromSeconds(2), ProcessTarget, from, target);
// Calls: ProcessTarget(Mobile from, Mobile target) after 2s

// Up to 5 parameters supported
Timer.DelayCall(TimeSpan.FromSeconds(1), DoWork, arg1, arg2, arg3);
```

### 5. Timer.Pause (Awaitable)
```csharp
await Timer.Pause(TimeSpan.FromMilliseconds(100));
await Timer.Pause(500);  // 500ms overload
```

## TimerExecutionToken Properties

```csharp
_timerToken.Running        // bool: is timer still active?
_timerToken.Index          // int: how many times OnTick has fired
_timerToken.RemainingCount // int: ticks remaining (int.MaxValue if infinite)
_timerToken.Next           // DateTime: when next tick fires
_timerToken.Cancel()       // Stop and return to pool (safe to call multiple times)
```

## Timer Wheel Architecture

3-layer hierarchical wheel with 4096 slots per layer:
- Layer 0: 8ms resolution, ~33 second range
- Layer 1: ~33s resolution, ~22 minute range
- Layer 2: ~22m resolution, ~16 day range

All delays rounded up to nearest 8ms boundary.

## Patterns

### Cleanup in Deletion
```csharp
public override void OnDelete()
{
    _timerToken.Cancel();    // TimerExecutionToken
    base.OnDelete();
}

public override void OnAfterDelete()
{
    _timer?.Stop();          // Timer reference
    _timer = null;
    base.OnAfterDelete();
}
```

### Timer Restoration After Deserialization
```csharp
[SerializationGenerator(0)]
public partial class DecayingItem : Item
{
    private TimerExecutionToken _decayTimer;  // NOT serialized

    [SerializableField(0)]
    [DeltaDateTime]
    private DateTime _expireTime;

    [Constructible]
    public DecayingItem() : base(0x1234)
    {
        _expireTime = Core.Now + TimeSpan.FromHours(1);
        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), CheckDecay, out _decayTimer);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), CheckDecay, out _decayTimer);
    }

    public override void OnAfterDelete()
    {
        _decayTimer.Cancel();
        base.OnAfterDelete();
    }

    private void CheckDecay()
    {
        if (Core.Now >= _expireTime)
            Delete();
    }
}
```

### [DeserializeTimerField] Pattern (for Timer fields)
```csharp
[SerializableField(0, setter: "private")]
private Timer _evaluateTimer;

[DeserializeTimerField(0)]
private void DeserializeEvaluateTimer(TimeSpan delay)
{
    _evaluateTimer = Timer.DelayCall(delay, EvaluationInterval, Evaluate);
}
```

### Custom Timer Class (When You Need Complex Logic)
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

// Usage:
_decayTimer = new DecayTimer(this, delay);
_decayTimer.Start();

// Cleanup:
_decayTimer?.Stop();
_decayTimer = null;
```

## Anti-Patterns

- **Serializing `TimerExecutionToken`**: It's a struct with internal Timer reference -- not serializable
- **Forgetting cleanup**: Timers keep running if not cancelled on deletion
- **Using `Thread.Sleep`**: Blocks the game loop. Use `Timer.StartTimer` or `await Timer.Pause` instead
- **Creating timers in constructors called during deserialization**: Use `[AfterDeserialization]` instead
- **Starting/stopping timers in `Serialize()`**: `Serialize()` runs on background threads during world saves -- timer APIs are game-thread-only and will corrupt state or crash

## Real Examples
- Token cleanup: `Projects/UOContent/Spells/Third/WallOfStone.cs`
- Repeating timer: `Projects/UOContent/Spells/Spellweaving/Items/TransientItem.cs`
- Custom timer class: `Projects/UOContent/Items/Misc/Corpses/Corpse.cs`
- State-carrying delay: Various files using `Timer.DelayCall<T1,T2>(delay, callback, arg1, arg2)`
- Timer deserialization: `Projects/UOContent/Items/Aquarium/Aquarium.cs`
- Timer pool config: `Projects/Server/Timer/Timer.Pool.cs`

## Timer Files
- `Projects/Server/Timer/Timer.cs` - Base class
- `Projects/Server/Timer/Timer.DelayCall.cs` - DelayCall + StartTimer
- `Projects/Server/Timer/Timer.TimerWheel.cs` - Scheduler
- `Projects/Server/Timer/Timer.Pool.cs` - Pool management
- `Projects/Server/Timer/Timer.DelayStateCall.cs` - Generic state timers
- `Projects/Server/Timer/TimerExecutionToken.cs` - Token struct

## See Also
- `dev-docs/timers.md` - Complete timer documentation
- `dev-docs/event-scheduler.md` - Wall-clock/calendar scheduling (EventScheduler) — use for daily resets, weekly events, holiday seasons instead of Timer
- `dev-docs/claude-skills/modernuo-event-scheduler.md` - EventScheduler skill for calendar-based events
- `dev-docs/claude-skills/modernuo-serialization.md` - Timer fields not serialized
- `dev-docs/claude-skills/modernuo-content-patterns.md` - Deletion patterns
- `dev-docs/claude-skills/modernuo-threading.md` - Single-threaded model
