# Timer Migration

## Overview

RunUO uses `Timer` subclass instances that you construct, start, and stop. ModernUO replaces most of this with fire-and-forget `Timer.StartTimer()` calls and lightweight `TimerExecutionToken` structs for cancellation. The `TimerPriority` enum is removed — ModernUO's timer wheel handles scheduling automatically with 8ms precision.

## RunUO Pattern

```csharp
// RunUO — Timer subclass pattern
public class MyItem : Item
{
    private InternalTimer m_Timer;

    [Constructable]
    public MyItem() : base(0x1234)
    {
        m_Timer = new InternalTimer(this);
        m_Timer.Start();
    }

    public MyItem(Serial serial) : base(serial) { }

    public override void OnDelete()
    {
        if (m_Timer != null)
            m_Timer.Stop();
    }

    public override void Serialize(GenericWriter writer)
    {
        base.Serialize(writer);
        writer.Write((int)0);
    }

    public override void Deserialize(GenericReader reader)
    {
        base.Deserialize(reader);
        int version = reader.ReadInt();

        m_Timer = new InternalTimer(this);
        m_Timer.Start();
    }

    private void DoWork()
    {
        // Timer callback logic
    }

    private class InternalTimer : Timer
    {
        private MyItem m_Item;

        public InternalTimer(MyItem item) : base(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))
        {
            m_Item = item;
            Priority = TimerPriority.OneSecond;
        }

        protected override void OnTick()
        {
            m_Item.DoWork();
        }
    }
}
```

### RunUO Timer.DelayCall
```csharp
// One-shot delay
Timer.DelayCall(TimeSpan.FromSeconds(5), new TimerCallback(DoWork));
Timer.DelayCall(TimeSpan.FromSeconds(5), new TimerStateCallback(DoWork), target);

// Repeating
Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1), new TimerCallback(DoWork));
```

## ModernUO Equivalent

```csharp
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class MyItem : Item
{
    private TimerExecutionToken _timerToken;

    [Constructible]
    public MyItem() : base(0x1234)
    {
        StartTimer();
    }

    private void StartTimer()
    {
        Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), DoWork, out _timerToken);
    }

    [AfterDeserialization]
    private void AfterDeserialization() => StartTimer();

    public override void OnAfterDelete()
    {
        _timerToken.Cancel();
        base.OnAfterDelete();
    }

    private void DoWork()
    {
        // Timer callback logic
    }
}
```

## Migration Mapping Table

| RunUO | ModernUO | Notes |
|---|---|---|
| `new InternalTimer().Start()` | `Timer.StartTimer(..., callback, out token)` | Fire-and-forget |
| `Timer` subclass with `OnTick()` | Static callback method | No class needed |
| `timer.Stop()` | `_token.Cancel()` | Lightweight struct |
| `timer.Running` | `_token.Running` | Same concept |
| `TimerPriority.OneSecond` | (removed) | Timer wheel handles scheduling |
| `TimerPriority.FiveSeconds` | (removed) | Timer wheel handles scheduling |
| `Timer.DelayCall(delay, callback)` | `Timer.StartTimer(delay, callback)` | Similar API |
| `Timer.DelayCall(delay, callback, state)` | `Timer.DelayCall(delay, callback, state)` | State-carrying version still exists |
| `new TimerCallback(Method)` | `Method` | Direct method reference |
| `new TimerStateCallback(Method)` | Use state-carrying overload | `Timer.DelayCall(delay, Method, arg1, arg2)` |
| Timer started in `Deserialize()` | `[AfterDeserialization]` method | Never start timers in deserialization |
| `m_Timer != null` check | `_token.Running` check | Token is a value type, always valid |

## Step-by-Step Conversion

### Step 1: Identify the Timer Pattern
Look for:
- Nested `Timer` subclass with `OnTick()` override
- `Timer.DelayCall()` calls
- `TimerPriority` usage

### Step 2: Extract the Callback
Move the `OnTick()` logic to a regular method on the parent class:

```csharp
// RunUO — nested class
private class InternalTimer : Timer
{
    private MyItem m_Item;
    public InternalTimer(MyItem item) : base(TimeSpan.FromSeconds(5)) { m_Item = item; }
    protected override void OnTick() { m_Item.DoWork(); }
}

// ModernUO — just the method
private void DoWork()
{
    // Same logic, directly on the item
}
```

### Step 3: Replace Construction with Timer.StartTimer
```csharp
// RunUO
m_Timer = new InternalTimer(this);
m_Timer.Start();

// ModernUO (one-shot)
Timer.StartTimer(TimeSpan.FromSeconds(5), DoWork);

// ModernUO (repeating, need cancellation)
Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), DoWork, out _timerToken);

// ModernUO (repeating with count limit)
Timer.StartTimer(TimeSpan.Zero, TimeSpan.FromSeconds(1), 10, DoWork, out _timerToken);
```

### Step 4: Add TimerExecutionToken Field (if cancellable)
```csharp
private TimerExecutionToken _timerToken; // NOT serialized — no [SerializableField]
```

### Step 5: Cancel in OnAfterDelete
```csharp
public override void OnAfterDelete()
{
    _timerToken.Cancel(); // Safe to call multiple times
    base.OnAfterDelete();
}
```

### Step 6: Restore in [AfterDeserialization]
```csharp
[AfterDeserialization]
private void AfterDeserialization()
{
    Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), DoWork, out _timerToken);
}
```

### Step 7: Delete the Nested Timer Class
Remove the entire `private class InternalTimer : Timer { ... }` block.

### Step 8: Remove TimerPriority
Delete any `Priority = TimerPriority.xxx` lines. The timer wheel handles scheduling.

## Before/After Examples

### Simple One-Shot Timer

**RunUO:**
```csharp
Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerCallback(Delete));
```

**ModernUO:**
```csharp
Timer.StartTimer(TimeSpan.FromSeconds(10), Delete);
```

### Repeating Timer with State

**RunUO:**
```csharp
private class HealTimer : Timer
{
    private Mobile m_Target;

    public HealTimer(Mobile target) : base(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2))
    {
        m_Target = target;
        Priority = TimerPriority.TwoFiftyMS;
    }

    protected override void OnTick()
    {
        if (m_Target.Alive && m_Target.Hits < m_Target.HitsMax)
            m_Target.Hits += 5;
        else
            Stop();
    }
}
```

**ModernUO:**
```csharp
private TimerExecutionToken _healTimer;

private void StartHeal(Mobile target)
{
    Timer.StartTimer(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), () => HealTick(target), out _healTimer);
}

private void HealTick(Mobile target)
{
    if (target.Alive && target.Hits < target.HitsMax)
        target.Hits += 5;
    else
        _healTimer.Cancel();
}
```

Or for zero-allocation, use the state-carrying `Timer.DelayCall`:
```csharp
Timer.DelayCall(TimeSpan.FromSeconds(2), HealTick, target);
```

### Timer.DelayCall with State

**RunUO:**
```csharp
Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(ProcessTarget), target);

private static void ProcessTarget(object state)
{
    Mobile target = (Mobile)state;
    // ...
}
```

**ModernUO:**
```csharp
Timer.DelayCall(TimeSpan.FromSeconds(2), ProcessTarget, target);

private static void ProcessTarget(Mobile target)
{
    // Type-safe — no casting needed
}
```

ModernUO supports up to 5 typed state parameters:
```csharp
Timer.DelayCall(TimeSpan.FromSeconds(2), ProcessTarget, mobile, item);
Timer.DelayCall(TimeSpan.FromSeconds(2), DoWork, arg1, arg2, arg3);
```

## Edge Cases & Gotchas

### 1. TimerExecutionToken Is NOT Serializable
Never add `[SerializableField]` to a `TimerExecutionToken`. It's a struct that tracks a pooled timer — it can't survive serialization. Always restore timers in `[AfterDeserialization]`.

### 2. Don't Start Timers in Deserialization
In RunUO, timers are commonly started in `Deserialize()`. In ModernUO, use `[AfterDeserialization]` — this runs after the world is fully loaded.

### 3. Cancel() Is Always Safe
`_token.Cancel()` can be called on a default token, a stopped token, or an already-cancelled token. No null checks needed.

### 4. Timer.DelayCall Still Exists
`Timer.DelayCall()` is still available and returns a `Timer` object. Use it when you need the `Timer` reference (e.g., for `[DeserializeTimerField]`) or state-carrying overloads.

### 5. Custom Timer Classes Are Still Possible
For complex timer logic (e.g., `Corpse.DecayTimer`), you can still subclass `Timer` with `OnTick()`. But prefer the fire-and-forget pattern for simple cases.

### 6. Avoid Lambda on Hot Paths
Lambdas allocate closures. For hot-path timers, use state-carrying `Timer.DelayCall` or direct method references:
```csharp
// Allocates closure:
Timer.StartTimer(TimeSpan.FromSeconds(2), () => ProcessTarget(from, target));

// No allocation:
Timer.DelayCall(TimeSpan.FromSeconds(2), ProcessTarget, from, target);
```

## See Also

- `dev-docs/timers.md` — Complete ModernUO timer reference
- `02-serialization.md` — Serialization (timer fields, [AfterDeserialization])
- `01-foundation-changes.md` — Foundation changes
