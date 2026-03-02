# ModernUO Threading Model

This document covers ModernUO's single-threaded game loop architecture, the EventLoopContext synchronization context, memory pooling, and rules for safe concurrent code.

## Core Principle: Single-Threaded Game Logic

All game logic in ModernUO runs on a single thread. There are no exceptions for code under `Projects/UOContent/`.

This means:
- No locks, mutexes, or synchronization primitives needed
- No concurrent collections needed
- No volatile fields needed
- No race conditions possible in game code
- `await` is safe because continuations route through EventLoopContext

## Game Loop

The game loop in `Projects/Server/Main.cs` runs continuously:

```csharp
public static void RunEventLoop()
{
    while (!Closing)
    {
        _tickCount = GetTimestamp();
        _now = DateTime.UtcNow;

        Mobile.ProcessDeltaQueue();    // Send mobile state changes to clients
        Item.ProcessDeltaQueue();      // Send item state changes to clients
        Timer.Slice(_tickCount);       // Execute due timers
        NetState.Slice();              // Process network I/O
        LoopContext.ExecuteTasks();     // Run async continuations
        Timer.CheckTimerPool();        // Refill timer pool if needed

        // World save handling
        if (_performSnapshot)
        {
            World.Snapshot(_snapshotPath);
            _performSnapshot = false;
        }
    }
}
```

Each iteration:
1. Updates timestamp
2. Sends pending mobile/item updates to clients
3. Fires due timers
4. Processes incoming network packets
5. Runs async continuations (from `await`)
6. Checks timer pool health
7. Handles world save snapshots if requested

## EventLoopContext

`EventLoopContext` implements `SynchronizationContext` to ensure all `await` continuations run on the game thread.

Defined in `Projects/Server/EventLoopTasks.cs`:

```csharp
public sealed class EventLoopContext : SynchronizationContext
{
    public enum Priority { Normal, High }

    private readonly ConcurrentQueue<Action> _queue;
    private readonly ConcurrentQueue<Action> _priorityQueue;
    private readonly Thread _mainThread;
    private readonly int _maxPerFrame;  // Default: 128

    // Post: queues action for next ExecuteTasks() call
    public void Post(Action d, Priority priority = Priority.Normal);

    // SynchronizationContext.Post: used by await
    public override void Post(SendOrPostCallback d, object state);

    // Send: immediate if on main thread, blocks if on other thread
    public override void Send(SendOrPostCallback d, object state);

    // Called once per game loop tick
    public void ExecuteTasks();
}
```

### How await Works

```csharp
// Safe in game code:
await Timer.Pause(TimeSpan.FromMilliseconds(100));
// After the pause, execution continues on the game thread
```

Flow:
1. `await` captures `EventLoopContext` as the current `SynchronizationContext`
2. When the awaited task completes, the continuation is posted to `_queue`
3. `LoopContext.ExecuteTasks()` runs the continuation on the main thread
4. Game state is safely accessible

### Task Limits
- Maximum 128 tasks per frame by default (configurable)
- High-priority tasks (`_priorityQueue`) are always processed first
- Normal tasks are processed up to the per-frame limit

## Forbidden Patterns

### In Game Code (Projects/UOContent/)

| Pattern | Problem | Alternative |
|---|---|---|
| `Task.Run(...)` | Runs on thread pool, races with game state | `Timer.StartTimer()` |
| `new Thread(...)` | Manual thread, races with game state | `Timer.StartTimer()` |
| `ThreadPool.QueueUserWorkItem(...)` | Thread pool, same issue | `Timer.StartTimer()` |
| `lock(obj) { ... }` | Unnecessary overhead, no contention | Remove lock |
| `Monitor.Enter(obj)` | Same as lock | Remove |
| `volatile int _field` | Memory barriers not needed | Plain field |
| `ConcurrentDictionary<K,V>` | Lock-free overhead, unnecessary | `Dictionary<K,V>` |
| `ConcurrentQueue<T>` | Same | `Queue<T>` or `List<T>` |
| `ConcurrentBag<T>` | Same | `List<T>` |
| `Interlocked.Increment(...)` | Atomic operations unnecessary | `_field++` |
| `Mutex` / `Semaphore` | OS-level sync, unnecessary | Remove |
| `ReaderWriterLockSlim` | Lock overhead, unnecessary | Remove |
| `Thread.Sleep(ms)` | Blocks entire game loop | `await Timer.Pause(ms)` |

### Exceptions: Server Infrastructure

These files in `Projects/Server/` MAY use threading because they handle I/O outside the game loop:

- `Main.cs` -- Event loop setup, thread configuration
- `World/World.cs` -- World save disk I/O (serialization on main thread, writes on background)
- `Network/` -- Network I/O processing
- `Timer/Timer.Pool.cs` -- Async pool refill
- `EventLoopTasks.cs` -- The synchronization context itself

## Memory Pooling

### STArrayPool<T>

Single-threaded array pool optimized for game code (no locks):

```csharp
// Defined in Projects/Server/Buffers/STArrayPool.cs
public class STArrayPool<T> : ArrayPool<T>
{
    public static new STArrayPool<T> Shared { get; }

    public override T[] Rent(int minimumLength);
    public override void Return(T[]? array, bool clearArray = false);
}
```

Usage:
```csharp
var buffer = STArrayPool<byte>.Shared.Rent(1024);
try
{
    // Use buffer (may be larger than requested)
}
finally
{
    STArrayPool<byte>.Shared.Return(buffer);
}
```

Architecture:
- 27 buckets covering sizes 16 to 1GB+
- Per-bucket cache (1 array) + stack storage (32 arrays)
- Trim callbacks on Gen2 GC to reduce memory pressure
- Formula: bucket index = `Log2(size - 1 | 15) - 3`

**Use `STArrayPool<T>.Shared`** in game code, **not** `ArrayPool<T>.Shared` (which uses locks).

### PooledRefList<T>

Stack-allocated list using pooled arrays:

```csharp
// Defined in Projects/Server/Collections/PooledRefList.cs
public ref struct PooledRefList<T>
{
    public static PooledRefList<T> Create(int capacity = 32, bool mt = false);
    public static PooledRefList<T> CreateMT(int capacity = 32);  // Multi-threaded

    public void Add(T item);
    public bool Remove(T item);
    public void Clear();
    public int Count { get; }
    public T this[int index] { get; set; }
    public void Dispose();  // Returns array to pool
}
```

Usage:
```csharp
using var list = PooledRefList<Mobile>.Create();
list.Add(mobile);
// list is stack-allocated, zero GC pressure
// Dispose() returns backing array to STArrayPool
```

Key properties:
- `ref struct` -- stack-allocated, cannot escape to heap
- Uses `STArrayPool<T>` by default, `ArrayPool<T>.Shared` with `CreateMT()`
- Auto-grows when capacity exceeded
- Must be disposed (use `using` pattern)

## World Save Threading

World saves involve both threads:

1. **`World.Save()`** -- Called on main thread, queues preserialize to thread pool
2. **`Preserialize()`** -- Thread pool: allocates serialization heaps, wakes workers
3. **`Snapshot()`** -- Main thread: serializes all game state (safe access), blocks game loop briefly
4. **`WriteFiles()`** -- Thread pool: writes serialized data to disk (no game state access)

```
Main Thread:    Save() → ... → Snapshot() → ... → continue loop
Thread Pool:    Preserialize() → ... → WriteFiles()
```

The main thread blocks during `Snapshot()` to ensure consistent state, then the disk I/O happens asynchronously.

## Best Practices

1. **Never use concurrency primitives in game code** -- they add overhead for no benefit
2. **Use `STArrayPool<T>.Shared`** instead of `ArrayPool<T>.Shared`
3. **Use `PooledRefList<T>`** instead of `new List<T>()` in hot paths
4. **Use `await Timer.Pause()`** instead of `Thread.Sleep()`
5. **Use `Timer.StartTimer()`** instead of `Task.Run()` for delayed work
6. **Trust single-threaded invariants** -- no need to protect shared state

## Key File References

| File | Description |
|---|---|
| `Projects/Server/Main.cs` | Game loop (RunEventLoop) |
| `Projects/Server/EventLoopTasks.cs` | EventLoopContext |
| `Projects/Server/Buffers/STArrayPool.cs` | Single-threaded array pool |
| `Projects/Server/Collections/PooledRefList.cs` | Pooled ref list |
| `Projects/Server/World/World.cs` | World save system |
