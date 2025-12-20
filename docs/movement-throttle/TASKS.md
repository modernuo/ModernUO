# Movement Throttle System - Task List

## Overview

This document contains the execution task list for implementing the movement throttle system. Tasks are organized by phase and dependency order.

---

## Phase 1: Foundation & Data Structures

### Task 1.1: Add NetState Fields
**File**: `Projects/Server/Network/NetState/NetState.cs`
**Priority**: Critical
**Dependencies**: None

**Work**:
- [ ] Add `QueuedMovement` struct definition
- [ ] Add `_movementQueue` field (Queue<QueuedMovement>)
- [ ] Add `_nextMovementExecutionTime` field
- [ ] Add `_throttledUntil` field
- [ ] Add `_movementHardReject` field
- [ ] Add `_sustainedQueueDepth` field
- [ ] Add `_lastQueueDepthCheck` field
- [ ] Initialize queue in constructor (lazy or eager - decide)

**Acceptance**:
- [ ] Compiles successfully
- [ ] No breaking changes to existing code

---

### Task 1.2: Add Movement Reset Method
**File**: `Projects/Server/Network/NetState/NetState.cs`
**Priority**: Critical
**Dependencies**: Task 1.1

**Work**:
- [ ] Add `ResetMovementState()` method
- [ ] Clear queue
- [ ] Reset sequence to 0
- [ ] Reset `_nextMovementExecutionTime` to `Core.TickCount`
- [ ] Clear `_movementHardReject` flag

**Acceptance**:
- [ ] Method callable from other classes
- [ ] All state properly reset

---

### Task 1.3: Add Configuration Settings
**File**: `Projects/Server/Network/MovementThrottle.cs`
**Priority**: High
**Dependencies**: None

**Work**:
- [ ] Add `_softQueueLimit` setting (default: 6)
- [ ] Add `_hardQueueLimit` setting (default: 10)
- [ ] Add `_sustainedAbuseThreshold` setting (default: 10)
- [ ] Add `_enableStaffBroadcast` setting (default: true)
- [ ] Add `_staffBroadcastCooldown` setting (default: 5000)
- [ ] Load settings in `Configure()` method

**Acceptance**:
- [ ] Settings load from configuration
- [ ] Defaults work if config missing

---

### Task 1.4: Add Feature Flag
**File**: `Projects/UOContent/Mobiles/PlayerMobile.cs` or `ServerConfiguration`
**Priority**: High
**Dependencies**: None

**Work**:
- [ ] Add `UseNewMovementThrottle` property
- [ ] Default to `false` initially (opt-in)
- [ ] Make configurable via settings or per-player

**Acceptance**:
- [ ] Flag can be toggled
- [ ] Old and new code can coexist

---

## Phase 2: Core Logic Implementation

### Task 2.1: Implement Queue Processor
**File**: `Projects/Server/Network/MovementThrottle.cs`
**Priority**: Critical
**Dependencies**: Task 1.1, Task 1.2

**Work**:
- [ ] Create `ProcessMovementQueue(NetState ns)` method
- [ ] Check timing (`now >= _nextMovementExecutionTime`)
- [ ] Dequeue and validate sequence
- [ ] Call `Mobile.Move(direction)`
- [ ] Handle move success (timing update done in Move)
- [ ] Handle move failure (reject, clear queue, reset sequence)
- [ ] Call abuse tracking

**Acceptance**:
- [ ] Queue drains at correct rate
- [ ] Failures handled gracefully
- [ ] Unit tests pass

---

### Task 2.2: Implement Packet Parsing in Throttle
**File**: `Projects/Server/Network/MovementThrottle.cs`
**Priority**: Critical
**Dependencies**: Task 1.1

**Work**:
- [ ] Create `ParseMovementPacket(NetState ns)` method
- [ ] Read direction byte from recv buffer
- [ ] Read sequence byte from recv buffer
- [ ] Read key (unused but must advance)
- [ ] Return parsed values
- [ ] Advance recv reader to consume packet

**Acceptance**:
- [ ] Correctly parses packet format
- [ ] Packet consumed from buffer
- [ ] No buffer corruption

---

### Task 2.3: Rewrite Throttle Method
**File**: `Projects/Server/Network/MovementThrottle.cs`
**Priority**: Critical
**Dependencies**: Task 2.1, Task 2.2

**Work**:
- [ ] Check feature flag - use old or new logic
- [ ] Implement new throttle logic:
  - [ ] Early exits (deleted mobile, staff)
  - [ ] Check hard reject mode
  - [ ] Parse packet
  - [ ] Check queue capacity (hard limit)
  - [ ] Enqueue movement
  - [ ] Call `ProcessMovementQueue`
  - [ ] Set `_throttledUntil` if queue non-empty
  - [ ] Return throttle decision
- [ ] Keep old logic behind feature flag

**Acceptance**:
- [ ] New logic works when enabled
- [ ] Old logic preserved when disabled
- [ ] No requeue storm

---

### Task 2.4: Implement Abuse Tracking
**File**: `Projects/Server/Network/MovementThrottle.cs`
**Priority**: High
**Dependencies**: Task 1.1, Task 1.3

**Work**:
- [ ] Create `TrackQueueDepth(NetState ns)` method
- [ ] Check queue depth periodically (every 1 second)
- [ ] Increment `_sustainedQueueDepth` if >= soft limit
- [ ] Decay counter if queue healthy
- [ ] Log when threshold reached
- [ ] Broadcast to staff if enabled

**Acceptance**:
- [ ] Counter increments/decays correctly
- [ ] Logging triggers at threshold
- [ ] Staff broadcast works

---

### Task 2.5: Update Slice() for Throttled Processing
**File**: `Projects/Server/Network/NetState/NetState.cs`
**Priority**: Critical
**Dependencies**: Task 1.1, Task 2.1

**Work**:
- [ ] Modify throttled queue processing loop
- [ ] Check `_throttledUntil` before processing
- [ ] Skip and re-queue if not ready
- [ ] Call `ProcessMovementQueue` when ready
- [ ] Re-throttle if queue still has items

**Acceptance**:
- [ ] No unnecessary iterations
- [ ] Queue drains at correct rate
- [ ] Performance improved

---

## Phase 3: Integration Points

### Task 3.1: Update Mobile.Move()
**File**: `Projects/Server/Mobiles/Mobile.cs`
**Priority**: High
**Dependencies**: Task 1.1

**Work**:
- [ ] Change `_nextMovementTime` to `_nextMovementExecutionTime`
- [ ] Ensure timing update uses `+=` correctly
- [ ] Move sequence increment here (if not already)
- [ ] Verify MovementAck sent correctly

**Acceptance**:
- [ ] Timing accumulates correctly
- [ ] Acks sent with correct sequence

---

### Task 3.2: Update PlayerMobile.Move()
**File**: `Projects/UOContent/Mobiles/PlayerMobile.cs`
**Priority**: High
**Dependencies**: Task 3.1

**Work**:
- [ ] Ensure base.Move() called correctly
- [ ] No interference with new system

**Acceptance**:
- [ ] PlayerMobile moves work
- [ ] Resurrect gump handling preserved

---

### Task 3.3: Add Reset Calls for Sequence Events
**Files**: Various (paralysis, teleport, map change)
**Priority**: High
**Dependencies**: Task 1.2

**Work**:
- [ ] Identify all sequence reset points
- [ ] Add `ResetMovementState()` calls:
  - [ ] Paralysis applied
  - [ ] Teleportation (spell, item)
  - [ ] Map change
  - [ ] Server-forced movement
  - [ ] Client resync request
- [ ] Verify no missed reset points

**Acceptance**:
- [ ] All reset events trigger queue clear
- [ ] No orphaned queue items after reset

---

### Task 3.4: Update/Remove MovementReq Handler
**File**: `Projects/UOContent/Network/Packets/IncomingMovementPackets.cs`
**Priority**: Medium
**Dependencies**: Task 2.3

**Work**:
- [ ] Decide: minimal handler or remove registration
- [ ] If keeping: make it a no-op when new system active
- [ ] If removing: update registration in Configure()
- [ ] Ensure old path still works with feature flag off

**Acceptance**:
- [ ] No double-processing of packets
- [ ] Feature flag switches behavior correctly

---

## Phase 4: Testing Infrastructure

### Task 4.1: Create Test Helpers
**File**: `Projects/UOContent.Tests/Tests/Network/MovementThrottleTestHelpers.cs` (new)
**Priority**: High
**Dependencies**: Phase 2

**Work**:
- [ ] Create mock NetState factory
- [ ] Create mock Mobile factory
- [ ] Create time simulation helpers
- [ ] Create packet simulation helpers

**Acceptance**:
- [ ] Helpers usable in tests
- [ ] Time mockable

---

### Task 4.2: Create Movement Simulator
**File**: `Projects/UOContent.Tests/Tests/Network/MovementThrottleSimulator.cs` (new)
**Priority**: High
**Dependencies**: Task 4.1

**Work**:
- [ ] Create `MovementThrottleSimulator` class
- [ ] Properties: CurrentTick, NextMovementTime, Queue
- [ ] Method: `SimulatePacketArrival(dir, seq, mounted, running)`
- [ ] Method: `SimulateTick(count)` - advance time
- [ ] Method: `GetExecutedMovements()` - return results

**Acceptance**:
- [ ] Simulator mirrors real logic
- [ ] Can test scenarios without full server

---

### Task 4.3: Write Legitimate Player Tests
**File**: `Projects/UOContent.Tests/Tests/Network/MovementThrottleLegitTests.cs` (new)
**Priority**: Critical
**Dependencies**: Task 4.2

**Work**:
- [ ] Test: Normal walking
- [ ] Test: Normal running mounted
- [ ] Test: Burst of 4 packets
- [ ] Test: Consistently early packets (30ms)
- [ ] Test: Lag spike recovery
- [ ] Test: Direction change only
- [ ] Test: Mount/dismount mid-movement
- [ ] Test: Variable lag (jitter)

**Acceptance**:
- [ ] All tests pass
- [ ] No false positives

---

### Task 4.4: Write Edge Case Tests
**File**: `Projects/UOContent.Tests/Tests/Network/MovementThrottleEdgeCaseTests.cs` (new)
**Priority**: High
**Dependencies**: Task 4.2

**Work**:
- [ ] Test: Paralysis during movement
- [ ] Test: Teleportation during queue
- [ ] Test: Map change
- [ ] Test: Client disconnect mid-queue
- [ ] Test: Sequence wrap-around (255 → 1)

**Acceptance**:
- [ ] All tests pass
- [ ] No crashes or undefined behavior

---

### Task 4.5: Write Speed Hack Detection Tests
**File**: `Projects/UOContent.Tests/Tests/Network/MovementThrottleHackDetectionTests.cs` (new)
**Priority**: High
**Dependencies**: Task 4.2

**Work**:
- [ ] Test: 50% speed hack (sustained)
- [ ] Test: 25% speed hack (subtle)
- [ ] Test: Intermittent speed hack

**Acceptance**:
- [ ] Hacks detected within threshold
- [ ] Logging triggered correctly

---

### Task 4.6: Write Queue Management Tests
**File**: `Projects/UOContent.Tests/Tests/Network/MovementThrottleQueueTests.cs` (new)
**Priority**: High
**Dependencies**: Task 4.2

**Work**:
- [ ] Test: Queue overflow (hard limit)
- [ ] Test: Queue draining rate
- [ ] Test: Soft limit logging trigger

**Acceptance**:
- [ ] Limits enforced correctly
- [ ] Timing accurate

---

## Phase 5: Real-World Testing

### Task 5.1: Deploy to Test Server
**Priority**: High
**Dependencies**: Phase 4

**Work**:
- [ ] Enable feature flag on test server
- [ ] Staff testing with various scenarios
- [ ] Monitor logs for issues
- [ ] Document any problems found

**Acceptance**:
- [ ] No crashes
- [ ] Staff reports positive

---

### Task 5.2: A/B Testing Setup
**Priority**: Medium
**Dependencies**: Task 5.1

**Work**:
- [ ] Enable feature flag for subset of players
- [ ] Ensure logging distinguishes old vs new
- [ ] Monitor both groups

**Acceptance**:
- [ ] Can compare behaviors
- [ ] No player complaints

---

### Task 5.3: Full Rollout
**Priority**: Medium
**Dependencies**: Task 5.2

**Work**:
- [ ] Enable feature flag server-wide
- [ ] Monitor for 1-2 weeks
- [ ] Gather player feedback
- [ ] Tune thresholds if needed

**Acceptance**:
- [ ] Stable operation
- [ ] Speed hackers detected
- [ ] Legit players unaffected

---

## Phase 6: Cleanup

### Task 6.1: Remove Old Code
**Priority**: Low
**Dependencies**: Task 5.3 (after stable period)

**Work**:
- [ ] Remove feature flag checks
- [ ] Remove old throttle logic
- [ ] Clean up any dead code
- [ ] Update comments/documentation

**Acceptance**:
- [ ] Only new code remains
- [ ] Compiles clean

---

### Task 6.2: Documentation
**Priority**: Low
**Dependencies**: Task 6.1

**Work**:
- [ ] Update CLAUDE.md if needed
- [ ] Document configuration options
- [ ] Archive planning docs or merge into code comments

**Acceptance**:
- [ ] Future developers can understand system
- [ ] Config options documented

---

## Task Dependencies Graph

```
Phase 1 (Foundation)
├── 1.1 NetState Fields
├── 1.2 Reset Method ──────────┐
├── 1.3 Config Settings        │
└── 1.4 Feature Flag           │
                               │
Phase 2 (Core Logic)           │
├── 2.1 Queue Processor ◄──────┤
├── 2.2 Packet Parsing         │
├── 2.3 Throttle Method ◄──────┼─── 2.1, 2.2
├── 2.4 Abuse Tracking         │
└── 2.5 Slice() Update ◄───────┘

Phase 3 (Integration)
├── 3.1 Mobile.Move()
├── 3.2 PlayerMobile.Move()
├── 3.3 Reset Calls
└── 3.4 Handler Update

Phase 4 (Testing)
├── 4.1 Test Helpers
├── 4.2 Simulator
├── 4.3 Legit Tests
├── 4.4 Edge Case Tests
├── 4.5 Hack Detection Tests
└── 4.6 Queue Tests

Phase 5 (Real-World)
├── 5.1 Test Server
├── 5.2 A/B Testing
└── 5.3 Full Rollout

Phase 6 (Cleanup)
├── 6.1 Remove Old Code
└── 6.2 Documentation
```

---

## Estimated Effort

| Phase | Tasks | Complexity | Notes |
|-------|-------|------------|-------|
| 1 | 4 | Low | Data structures only |
| 2 | 5 | High | Core algorithm |
| 3 | 4 | Medium | Integration points |
| 4 | 6 | Medium | Test coverage |
| 5 | 3 | Variable | Real-world testing |
| 6 | 2 | Low | Cleanup |

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Breaks existing movement | Feature flag allows instant rollback |
| Performance regression | Benchmark before/after |
| False positive detection | Generous thresholds, logging before action |
| Missed reset events | Comprehensive edge case tests |
| Queue memory issues | Hard cap on queue size |
