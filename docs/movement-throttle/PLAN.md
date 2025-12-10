# Movement Throttle System - Detailed Plan

## Overview

This document describes the redesigned movement throttle system for ModernUO. The goal is to prevent speed hacking while being lenient toward legitimate players who experience lag, packet bursts, or early packet delivery.

## Problem Statement

### Current Issues

1. **Requeue Storm**: When a packet is throttled, it stays in the recv buffer and gets re-checked every event loop tick (potentially thousands of times) until enough time passes.

2. **Lack of Game Context**: The throttle runs before packet parsing, so it has no access to:
   - `ComputeMovementSpeed()` (mounted vs foot, running vs walking)
   - Direction change vs actual movement (TurnDelay = 0)
   - Paralysis or other movement-blocking states

3. **Debt Accumulation**: Strict timing causes legitimate players to accumulate "debt" when sending packets consistently 30ms early across long distances.

### UO Client Behavior (Constraints)

1. **Cannot change protocol** - must work with existing packet format (0x02)
2. **Client sends up to 4 packets without waiting for ack/rej**
3. **Client sends packets up to 30ms early** - variable, not compensated
4. **Sequence resets** occur on: paralysis, map change, server-forced movement, teleportation, full resync
5. **Direction changes** use TurnDelay (typically 0ms) - no actual tile movement
6. **Movement speeds**:
   - Running mounted: 100ms
   - Walking mounted / Running foot: 200ms
   - Walking foot: 400ms

## Architecture

### High-Level Design

```
┌─────────────────┐     ┌─────────────────┐     ┌──────────────────┐
│    Throttle     │────▶│ Movement Queue  │────▶│  Mobile.Move()   │
│ (packet intake) │     │  (on NetState)  │     │ (full context)   │
└─────────────────┘     └─────────────────┘     └──────────────────┘
        │                       │                        │
        │                       │                        ▼
        │                       │              ┌──────────────────┐
        │                       │              │   Update State   │
        │◀──────────────────────┴──────────────│   & Hint Back    │
        │                                      └──────────────────┘
        ▼
┌─────────────────┐
│  Queue Drainer  │ (triggered via throttle mechanism)
│  (in Slice())   │
└─────────────────┘
```

### Component Details

#### 1. Movement Queue (NetState)

**Location**: `Projects/Server/Network/NetState/NetState.cs`

```csharp
// New fields on NetState
internal struct QueuedMovement
{
    public Direction Direction;
    public int Sequence;
    public long QueuedAt;  // For diagnostics/logging
}

internal Queue<QueuedMovement> _movementQueue;      // The queue itself
internal long _nextMovementExecutionTime;            // When next move can execute
internal long _throttledUntil;                       // Don't recheck until this time
internal bool _movementHardReject;                   // Signal to reject all incoming
internal int _sustainedQueueDepth;                   // Rolling tracker for abuse detection
internal long _lastQueueDepthCheck;                  // Time of last depth check
```

**Queue Limits**:
- **Soft limit: 6 queued** - Start logging, increment sustained counter
- **Hard limit: 10 queued** - Reject incoming, clear queue, reset sequence

**Rationale**:
- Legitimate burst: 4-5 packets
- With early sending and lag variance: could temporarily hit 6
- Sustained 6+ indicates likely speed hack
- 10 is generous hard cap that should never be hit legitimately

#### 2. Throttle (Packet Intake)

**Location**: `Projects/Server/Network/MovementThrottle.cs`

**Responsibilities**:
1. Parse the incoming packet (direction, sequence)
2. Check for hard reject mode
3. Add movement to queue
4. Attempt to process queue
5. Decide whether to throttle (for later reprocessing)

**Key Change**: Always consume the packet. No more requeue storm.

```csharp
public static bool Throttle(int packetId, NetState ns)
{
    // 1. Early exits (deleted mobile, staff, etc.)

    // 2. Check hard reject mode
    if (ns._movementHardReject)
    {
        ConsumePacket(ns);
        SendRejectAndReset(ns);
        return false; // Packet consumed
    }

    // 3. Parse packet from recv buffer
    var (direction, sequence) = ParseMovementPacket(ns);

    // 4. Check queue capacity
    if (ns._movementQueue.Count >= HardLimit)
    {
        LogQueueOverflow(ns);
        ClearQueueAndReject(ns, sequence);
        return false; // Packet consumed
    }

    // 5. Queue the movement
    ns._movementQueue.Enqueue(new QueuedMovement
    {
        Direction = direction,
        Sequence = sequence,
        QueuedAt = Core.TickCount
    });

    // 6. Try to process queue
    ProcessMovementQueue(ns);

    // 7. If queue still has items, schedule reprocessing
    if (ns._movementQueue.Count > 0)
    {
        ns._throttledUntil = ns._nextMovementExecutionTime;
        return true; // Throttled, but packet already consumed
    }

    return false; // All processed
}
```

#### 3. Queue Processor

**Location**: New method in `MovementThrottle.cs` or separate file

**Responsibilities**:
1. Check if execution time has arrived
2. Dequeue movement
3. Validate sequence (handle resets)
4. Call `Mobile.Move(direction)`
5. Handle success/failure
6. Update timing state
7. Track queue depth for abuse detection

```csharp
public static void ProcessMovementQueue(NetState ns)
{
    var mobile = ns.Mobile;
    if (mobile?.Deleted != false)
    {
        ns._movementQueue.Clear();
        return;
    }

    var now = Core.TickCount;

    while (ns._movementQueue.Count > 0)
    {
        // Check if it's time to execute
        if (now < ns._nextMovementExecutionTime)
        {
            break; // Not yet, leave in queue
        }

        var movement = ns._movementQueue.Dequeue();

        // Validate sequence
        if (ns.Sequence == 0 && movement.Sequence != 0)
        {
            // Sequence was reset, reject this and clear queue
            ns.SendMovementRej(movement.Sequence, mobile);
            ns._movementQueue.Clear();
            ns.Sequence = 0;
            return;
        }

        // Execute the move
        if (!mobile.Move(movement.Direction))
        {
            // Move failed (paralyzed, blocked, etc.)
            ns.SendMovementRej(movement.Sequence, mobile);
            ns._movementQueue.Clear();
            ns.Sequence = 0;
            return;
        }

        // Success - sequence updated in Mobile.Move()
        // _nextMovementExecutionTime updated in Mobile.Move()

        now = Core.TickCount; // Refresh for next iteration
    }

    // Track queue depth for abuse detection
    TrackQueueDepth(ns);
}
```

#### 4. Mobile.Move() Modifications

**Location**: `Projects/Server/Mobiles/Mobile.cs`

**Changes**:
- Continue to update `_nextMovementExecutionTime += ComputeMovementSpeed(d)`
- Send MovementAck on success
- No longer directly called from packet handler (called from queue processor)

```csharp
public virtual bool Move(Direction d)
{
    // ... existing validation ...

    if (m_NetState != null)
    {
        // Update next allowed movement time
        m_NetState._nextMovementExecutionTime += ComputeMovementSpeed(d);

        // Send ack
        m_NetState.SendMovementAck(m_NetState.Sequence, this);

        // Update sequence
        var seq = m_NetState.Sequence + 1;
        if (seq == 256) seq = 1;
        m_NetState.Sequence = seq;
    }

    // ... rest of move logic ...
}
```

#### 5. Slice() Integration (Queue Draining)

**Location**: `Projects/Server/Network/NetState/NetState.cs` - `Slice()` method

**Changes**: Modify throttled queue processing to use `_throttledUntil`

```csharp
public static void Slice()
{
    // ... existing code ...

    while (_throttled.Count > 0)
    {
        var ns = _throttled.Dequeue();

        // Check if it's time to process this NetState
        if (Core.TickCount < ns._throttledUntil)
        {
            // Not ready yet, re-queue without processing
            _throttledPending.Enqueue(ns);
            continue;
        }

        ns._isThrottled = false;

        if (ns.Running && ns._movementQueue?.Count > 0)
        {
            // Process pending movements
            MovementThrottle.ProcessMovementQueue(ns);

            // If still has items, re-throttle
            if (ns._movementQueue.Count > 0)
            {
                ns._throttledUntil = ns._nextMovementExecutionTime;
                ns._isThrottled = true;
                _throttledPending.Enqueue(ns);
            }
        }
    }

    // ... rest of existing code ...
}
```

#### 6. Abuse Detection & Logging

**Location**: `MovementThrottle.cs`

**Approach**: Track sustained queue depth over time

```csharp
private static void TrackQueueDepth(NetState ns)
{
    var now = Core.TickCount;

    // Check every second
    if (now - ns._lastQueueDepthCheck < 1000)
        return;

    ns._lastQueueDepthCheck = now;

    if (ns._movementQueue.Count >= SoftLimit)
    {
        ns._sustainedQueueDepth++;

        if (ns._sustainedQueueDepth >= SustainedThreshold)
        {
            LogSuspiciousActivity(ns);
            // Optionally broadcast to staff
        }
    }
    else
    {
        // Queue is healthy, decay the counter
        ns._sustainedQueueDepth = Math.Max(0, ns._sustainedQueueDepth - 1);
    }
}
```

### Sequence Handling

**Reset Events** (clear queue, reset sequence):
- Paralysis applied
- Teleportation
- Map change
- Server-forced movement
- Client-requested full resync

**Implementation**: Add calls to a new method at these events:

```csharp
// On NetState or as extension
public void ResetMovementState()
{
    _movementQueue?.Clear();
    Sequence = 0;
    _nextMovementExecutionTime = Core.TickCount;
    _movementHardReject = false;
}
```

### Packet Handler Changes

**Location**: `Projects/UOContent/Network/Packets/IncomingMovementPackets.cs`

The `MovementReq` handler becomes minimal or removed since throttle handles everything:

```csharp
public static void MovementReq(NetState state, SpanReader reader)
{
    // Movement is now handled entirely by the throttle + queue system
    // This handler is kept for protocol compliance but does nothing
    // The throttle has already:
    // 1. Parsed the packet
    // 2. Queued the movement
    // 3. Attempted execution
}
```

Or we remove the handler registration and handle everything in throttle.

## Configuration

**Location**: `ServerConfiguration` settings

```json
{
    "movementThrottle": {
        "softQueueLimit": 6,
        "hardQueueLimit": 10,
        "sustainedAbuseThreshold": 10,
        "suspiciousLogCooldown": 60000,
        "enableStaffBroadcast": true,
        "staffBroadcastCooldown": 5000
    }
}
```

## Feature Flag for A/B Testing

```csharp
// On PlayerMobile
[CommandProperty(AccessLevel.GameMaster)]
public bool UseNewMovementThrottle { get; set; } = true;

// Or via ServerConfiguration
public static bool UseNewMovementThrottle { get; private set; }
```

This allows real-world testing of old vs new system on specific players or server-wide.

## Migration Path

1. **Phase 1**: Implement new system behind feature flag (default off)
2. **Phase 2**: Enable for staff/testers, gather feedback
3. **Phase 3**: Enable for subset of players, monitor logs
4. **Phase 4**: Enable server-wide, remove old code
5. **Phase 5**: Remove feature flag

## Rollback Plan

If issues are discovered:
1. Disable feature flag
2. Old throttle code remains functional
3. No data migration needed (state is transient)
