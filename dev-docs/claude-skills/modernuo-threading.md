---
name: modernuo-threading
description: >
  Trigger when discussing async patterns, world saves, game loop, or reviewing code for threading issues. When using await, Task, or any concurrency-related code in game logic.
---

# ModernUO Threading & Event Loop

## When This Activates
- Reviewing code for threading issues
- Discussing async/await patterns
- Working with world saves
- Any mention of `Task.Run`, `Thread`, `lock`, `ConcurrentDictionary`
- Understanding the game loop

## CRITICAL RULE: Single-Threaded Game Logic

ModernUO uses a **single-threaded game loop**. All game logic runs on one thread. There are NO exceptions for game code.

## Forbidden in Game Code

```csharp
// ALL of these are WRONG in Projects/UOContent/ code:
Task.Run(() => ProcessItems());           // Background thread
new Thread(BackgroundWork).Start();       // Manual thread
ThreadPool.QueueUserWorkItem(Work);       // Thread pool
lock (_syncObj) { ... }                   // Locking
Monitor.Enter(obj);                       // Monitor
volatile int _counter;                    // Volatile
ConcurrentDictionary<int, Item> _items;   // Concurrent collections
ConcurrentQueue<T> _queue;               // Concurrent collections
Interlocked.Increment(ref _count);        // Atomics
Mutex mutex;                              // OS mutex
Semaphore sem;                            // Semaphore
ReaderWriterLockSlim rwl;                 // RW lock
```

**Why**: The game loop is single-threaded. Concurrency primitives add overhead for no benefit, and background threads would cause data races with game state.

## Why await Is Safe

`EventLoopContext` implements `SynchronizationContext` and routes all `await` continuations back to the main thread:

```csharp
// This is SAFE in game code:
await Timer.Pause(TimeSpan.FromMilliseconds(100));
// Continuation runs on the game thread, not a thread pool thread
```

The flow:
1. `await` captures `EventLoopContext` as the synchronization context
2. When the awaited task completes, the continuation is posted to `EventLoopContext._queue`
3. `LoopContext.ExecuteTasks()` runs those continuations on the main thread during the next game loop tick

## Game Loop Structure

```csharp
// Simplified from Projects/Server/Main.cs
while (!Closing)
{
    _tickCount = GetTimestamp();
    _now = DateTime.UtcNow;

    Mobile.ProcessDeltaQueue();    // Send mobile state changes to clients
    Item.ProcessDeltaQueue();      // Send item state changes to clients
    Timer.Slice(_tickCount);       // Execute due timers
    NetState.Slice();              // Process network I/O
    LoopContext.ExecuteTasks();     // Run async continuations (Timer.Pause, etc.)
    Timer.CheckTimerPool();        // Refill timer pool if needed
}
```

## EventLoopContext Details

```csharp
public sealed class EventLoopContext : SynchronizationContext
{
    private readonly ConcurrentQueue<Action> _queue;          // Normal tasks
    private readonly ConcurrentQueue<Action> _priorityQueue;  // High priority
    private readonly int _maxPerFrame;                         // Default: 128

    // Posts run on next ExecuteTasks() call
    public void Post(Action d, Priority priority = Priority.Normal);

    // Send blocks if called from another thread, immediate if on game thread
    public override void Send(SendOrPostCallback d, object state);

    // Called once per game loop tick
    public void ExecuteTasks();
}
```

## Memory: STArrayPool vs ArrayPool

In game code, use `STArrayPool<T>.Shared` (single-threaded, no locks):
```csharp
// GOOD - no locking overhead
var buffer = STArrayPool<byte>.Shared.Rent(1024);
try { /* use buffer */ }
finally { STArrayPool<byte>.Shared.Return(buffer); }
```

`ArrayPool<T>.Shared` uses locks for thread safety -- unnecessary overhead in single-threaded context.

## Memory: PooledRefList

```csharp
// Stack-allocated list using pooled arrays
using var list = PooledRefList<Mobile>.Create();
list.Add(mobile);
// Automatically returns array to pool on Dispose

// For multi-threaded contexts (rare):
using var list = PooledRefList<Mobile>.CreateMT();
```

## World Saves

World saves use **parallel serialization threads**. This is critical to understand:

1. **Preserialize**: Allocates heaps and wakes serialization thread workers (background)
2. **Snapshot**: Main thread calls `Persistence.SerializeAll()` which pushes entities into `SerializationThreadWorker` queues (round-robin). Workers call `Serialize(writer)` on **their own background threads** in parallel.
3. **Write snapshot**: Disk I/O on background threads after serialization completes

```csharp
// From World.cs -- save flow:
World.Save();
→ Preserialize() on thread pool (allocate heaps, wake serialization workers)
→ Snapshot() on main thread (queues entities to workers, workers serialize in parallel)
  → SerializationThreadWorker.Execute() calls e.Serialize(writer) on background thread
→ PauseSerializationThreads() (wait for workers to finish)
→ WriteSnapshot() on thread pool (disk I/O only)
```

### Serialize() runs on background threads
Because `SerializationThreadWorker` calls `Serialize()` on its own thread, **`Serialize()` must be pure**:
- **NO** creating/destroying Items or Mobiles
- **NO** starting/stopping timers (not thread-safe)
- **NO** sending packets or modifying NetState
- **NO** mutating shared game state
- **ONLY** read fields and write to `IGenericWriter`

See `modernuo-serialization.md` for full purity rules.

## Exceptions: Server Infrastructure

These files MAY use threading (they're server infrastructure, not game logic):
- `Projects/Server/Main.cs` - Event loop, thread setup
- `Projects/Server/World/World.cs` - World save I/O
- `Projects/Server/Network/` - Network I/O
- `Projects/Server/Timer/Timer.Pool.cs` - Pool refill

## Anti-Patterns

| Pattern | Problem | Solution |
|---|---|---|
| `Task.Run(...)` | Runs on thread pool, races with game state | Use `Timer.StartTimer()` |
| `new Thread(...)` | Same as above | Use `Timer.StartTimer()` |
| `lock(obj)` | Unnecessary overhead, no contention exists | Remove lock, use plain code |
| `ConcurrentDictionary` | Lock-free but still overhead | Use `Dictionary<K,V>` |
| `volatile` | Memory barriers not needed on single thread | Use plain field |
| `Thread.Sleep()` | Blocks entire game loop | Use `await Timer.Pause()` |
| `ArrayPool<T>.Shared` | Uses locks | Use `STArrayPool<T>.Shared` |

## Real Examples
- Game loop: `Projects/Server/Main.cs` (RunEventLoop)
- EventLoopContext: `Projects/Server/EventLoopTasks.cs`
- STArrayPool: `Projects/Server/Buffers/STArrayPool.cs`
- PooledRefList: `Projects/Server/Collections/PooledRefList.cs`
- World save: `Projects/Server/World/World.cs`

## See Also
- `dev-docs/threading-model.md` - Complete threading documentation
- `dev-docs/claude-skills/modernuo-code-audit.md` - Threading audit rules
- `dev-docs/claude-skills/modernuo-timers.md` - Timer-based scheduling
