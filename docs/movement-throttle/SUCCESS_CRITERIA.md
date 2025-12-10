# Movement Throttle System - Success Criteria

## Overview

This document defines the success criteria for the movement throttle system. These criteria will be used to validate the implementation through unit tests, simulation tests, and real-world testing.

---

## 1. Legitimate Player Scenarios

### 1.1 Normal Walking (Foot)

**Scenario**: Player walks on foot, packets arrive at ~400ms intervals.

**Input**:
- Movement packets at T=0, 400, 800, 1200, 1600ms
- Direction: consistent (e.g., North)
- Walking speed: 400ms

**Expected**:
- All movements execute successfully
- No throttling occurs
- Queue depth stays at 0-1
- No suspicious activity logged

**Success Criteria**:
- [ ] All 5 movements complete
- [ ] Player position advances 5 tiles
- [ ] `_sustainedQueueDepth` remains 0

---

### 1.2 Normal Running (Mounted)

**Scenario**: Player runs while mounted, packets arrive at ~100ms intervals.

**Input**:
- Movement packets at T=0, 100, 200, 300, 400ms
- Direction: consistent with Running flag
- Mounted speed: 100ms

**Expected**:
- All movements execute successfully
- Queue depth stays at 0-1

**Success Criteria**:
- [ ] All 5 movements complete
- [ ] Player position advances 5 tiles
- [ ] No suspicious activity logged

---

### 1.3 Burst of 4 Packets

**Scenario**: Client sends 4 packets rapidly (before any ack received).

**Input**:
- Movement packets at T=0, 5, 10, 15ms (burst)
- Then normal at T=100, 200, 300ms
- Running mounted speed: 100ms

**Expected**:
- First packet executes immediately
- Packets 2-4 queue up
- Queue drains at proper intervals (T=100, 200, 300)
- Subsequent packets also execute normally

**Success Criteria**:
- [ ] All movements eventually complete
- [ ] Queue never exceeds 4
- [ ] No packets rejected
- [ ] No suspicious activity logged

---

### 1.4 Consistently Early Packets (30ms)

**Scenario**: Player runs across map, every packet arrives 30ms early.

**Input**:
- 100 movement packets
- Running mounted speed: 100ms expected
- Actual arrival: every 70ms (30ms early)

**Expected**:
- Most packets queue briefly then execute
- Queue depth fluctuates but stays reasonable (1-4)
- No accumulated "debt" that causes rejection
- Player completes the journey

**Success Criteria**:
- [ ] All 100 movements complete
- [ ] Queue never exceeds soft limit (6)
- [ ] No packets rejected
- [ ] No suspicious activity logged

---

### 1.5 Lag Spike Recovery

**Scenario**: Player experiences 500ms lag spike, then packets burst.

**Input**:
- Packets at T=0, 100, 200 (normal)
- Gap until T=700 (500ms lag)
- Burst at T=700, 705, 710, 715, 720 (5 packets)
- Resume normal at T=800, 900

**Expected**:
- First 3 execute normally
- After lag, burst queues up
- Queue drains at proper rate
- No rejection despite burst

**Success Criteria**:
- [ ] All movements complete
- [ ] Queue peaks at ~5 during burst
- [ ] Queue drains within 500ms of burst
- [ ] No suspicious activity logged

---

### 1.6 Direction Change (Turn Only)

**Scenario**: Player turns without moving (TurnDelay = 0).

**Input**:
- Packet at T=0: Direction North (move)
- Packet at T=50: Direction East (turn only, same position)
- Packet at T=100: Direction East (move)

**Expected**:
- Turn executes immediately (TurnDelay = 0)
- Move at T=100 executes on schedule

**Success Criteria**:
- [ ] Turn at T=50 executes immediately
- [ ] Move at T=100 is not delayed by turn
- [ ] Player facing changes without position change

---

### 1.7 Mount/Dismount Mid-Movement

**Scenario**: Player dismounts while moving, speed changes.

**Input**:
- Packets at T=0, 100, 200 (mounted running, 100ms)
- Dismount event at T=250
- Packets at T=450, 650 (foot running, 200ms)

**Expected**:
- First 3 moves at mounted speed
- After dismount, timing adjusts to foot speed
- No queue buildup from speed change

**Success Criteria**:
- [ ] All movements complete
- [ ] Speed transition is smooth
- [ ] No rejection due to speed change

---

### 1.8 Variable Lag (Sometimes Early, Sometimes Late)

**Scenario**: Realistic network with jitter.

**Input**:
- Expected 100ms intervals
- Actual: 80, 120, 90, 110, 85, 130, 95, 105ms intervals
- Average is ~100ms

**Expected**:
- Some packets queue briefly
- Queue self-corrects over time
- No sustained queue buildup

**Success Criteria**:
- [ ] All movements complete
- [ ] Queue averages 0-2
- [ ] No suspicious activity logged

---

## 2. Edge Cases

### 2.1 Paralysis During Movement

**Scenario**: Player is paralyzed while movements are queued.

**Input**:
- 3 packets queued at T=0
- Paralysis applied at T=50
- More packets at T=100, 150

**Expected**:
- Queued movements clear on paralysis
- Subsequent packets rejected with MovementRej
- Sequence resets to 0
- No crash or undefined behavior

**Success Criteria**:
- [ ] Queue cleared on paralysis
- [ ] MovementRej sent for queued items
- [ ] Sequence reset to 0
- [ ] Post-paralysis packets handled correctly

---

### 2.2 Teleportation During Queue

**Scenario**: Player teleports (spell, recall) while movements queued.

**Input**:
- 3 packets queued at T=0
- Teleport at T=50
- Packets continue at T=100, 150

**Expected**:
- Queue cleared on teleport
- Sequence resets
- Post-teleport packets start fresh

**Success Criteria**:
- [ ] Queue cleared on teleport
- [ ] Sequence reset to 0
- [ ] New location used for subsequent moves

---

### 2.3 Map Change

**Scenario**: Player enters dungeon (map change) during movement.

**Input**:
- Movements queued on Map A
- Map change to Map B
- Movements continue

**Expected**:
- Queue cleared on map change
- Sequence resets
- New map context for subsequent moves

**Success Criteria**:
- [ ] Queue cleared on map change
- [ ] No movements execute on wrong map

---

### 2.4 Client Disconnect Mid-Queue

**Scenario**: Client disconnects while movements are queued.

**Input**:
- 3 packets queued
- Connection drops

**Expected**:
- Queue cleaned up
- No orphaned state
- No crashes

**Success Criteria**:
- [ ] Clean disconnect handling
- [ ] No memory leaks
- [ ] No null reference exceptions

---

### 2.5 Sequence Wrap-Around

**Scenario**: Sequence number wraps from 255 to 1.

**Input**:
- Sequence at 254
- 5 movement packets

**Expected**:
- Sequence: 254 → 255 → 1 → 2 → 3 (skip 0)
- All movements execute

**Success Criteria**:
- [ ] Wrap-around handled correctly
- [ ] No sequence validation errors

---

## 3. Speed Hack Detection

### 3.1 50% Speed Hack (Sustained)

**Scenario**: Hacker moves 50% faster than allowed.

**Input**:
- Running mounted (100ms allowed)
- Packets at 67ms intervals (sustained)
- 100 packets over ~6.7 seconds

**Expected**:
- Queue builds up over time
- Soft limit hit, logging starts
- `_sustainedQueueDepth` increments
- Staff alert after threshold
- Eventually hard limit hit if continued

**Success Criteria**:
- [ ] Suspicious activity logged
- [ ] `_sustainedQueueDepth` reaches threshold
- [ ] Staff broadcast sent (if enabled)
- [ ] Hard limit eventually triggers rejection

---

### 3.2 Moderate Speed Hack (25% Faster)

**Scenario**: Subtler hack, 25% faster.

**Input**:
- Running mounted (100ms allowed)
- Packets at 75ms intervals
- Sustained over time

**Expected**:
- Slower queue buildup than 50%
- Still eventually detected
- Logging occurs

**Success Criteria**:
- [ ] Eventually flagged as suspicious
- [ ] Takes longer than 50% hack to detect
- [ ] Logged for review

---

### 3.3 Intermittent Speed Hack

**Scenario**: Hacker alternates between normal and fast.

**Input**:
- 10 seconds normal speed
- 5 seconds 50% faster
- Repeat

**Expected**:
- Queue builds during hack periods
- Queue drains during normal periods
- `_sustainedQueueDepth` may not reach threshold
- Still logged if sustained queue observed

**Success Criteria**:
- [ ] Periods of high queue logged
- [ ] System doesn't crash
- [ ] Legitimate players not affected by shared detection

---

## 4. Queue Management

### 4.1 Queue Overflow (Hard Limit)

**Scenario**: Queue exceeds hard limit.

**Input**:
- 12 packets sent in rapid succession
- Queue limit: 10

**Expected**:
- First 10 queue successfully
- 11th and 12th rejected
- Queue cleared on overflow
- Sequence reset

**Success Criteria**:
- [ ] Hard limit enforced
- [ ] MovementRej sent
- [ ] Queue cleared
- [ ] Sequence reset to 0
- [ ] Client can resync and continue

---

### 4.2 Queue Draining Rate

**Scenario**: Verify queue drains at correct speed.

**Input**:
- 5 packets queued instantly at T=0
- Running mounted (100ms)

**Expected**:
- Executions at T=0, 100, 200, 300, 400
- Queue empty by T=400

**Success Criteria**:
- [ ] Timing matches ComputeMovementSpeed
- [ ] No faster or slower than expected

---

## 5. Performance

### 5.1 No Requeue Storm

**Scenario**: Throttled state doesn't cause excessive iterations.

**Input**:
- Packet throttled at T=0
- Next execution allowed at T=100
- Event loop runs at ~1000Hz

**Expected**:
- Old system: ~100 iterations checking the packet
- New system: 1 check at T=0, skip until T>=100, 1 check at T=100

**Success Criteria**:
- [ ] Throttle check count < 5 for single packet
- [ ] CPU usage doesn't spike on throttle

---

### 5.2 Memory Usage

**Scenario**: Queue doesn't grow unbounded.

**Input**:
- Various traffic patterns
- Long play sessions

**Expected**:
- Queue bounded by hard limit
- No memory leaks
- Garbage collection friendly

**Success Criteria**:
- [ ] Queue size never exceeds hard limit
- [ ] No memory growth over time
- [ ] Structs used (no heap allocation per movement)

---

## 6. Compatibility

### 6.1 Staff Movement

**Scenario**: Staff members should bypass throttle.

**Input**:
- Staff member moves
- AccessLevel > Player

**Expected**:
- No throttling applied
- Immediate movement

**Success Criteria**:
- [ ] Staff can move freely
- [ ] No queue for staff

---

### 6.2 NPC Movement

**Scenario**: NPCs don't use this system.

**Input**:
- NPC moves via AI

**Expected**:
- NPC movement unaffected
- No NetState for NPCs

**Success Criteria**:
- [ ] NPC movement works normally
- [ ] No null reference for missing NetState

---

## 7. Logging & Monitoring

### 7.1 Suspicious Activity Log Format

**Expected Log Entry**:
```
[Warning] Potential speed hack detected: CharacterName |
Queue Depth: 7 | Sustained Count: 15 |
Location: (1234, 5678, 10) Map: Felucca |
Account: accountname | IP: 192.168.1.1
```

**Success Criteria**:
- [ ] Log contains character name
- [ ] Log contains queue metrics
- [ ] Log contains location and map
- [ ] Log contains account/IP for investigation

---

### 7.2 Debug Logging (Optional)

**Scenario**: Detailed logging for debugging (disabled by default).

**Expected**:
- Per-packet timing logged
- Queue state changes logged
- Only when explicitly enabled

**Success Criteria**:
- [ ] Debug logging doesn't affect performance when disabled
- [ ] Sufficient detail when enabled

---

## Validation Checklist

### Unit Tests
- [ ] Queue enqueue/dequeue
- [ ] Soft/hard limit enforcement
- [ ] Sequence validation
- [ ] Timing calculations

### Simulation Tests
- [ ] All legitimate scenarios pass
- [ ] All edge cases handled
- [ ] Speed hack detection works

### Real-World Tests
- [ ] A/B test with feature flag
- [ ] No complaints from legitimate players
- [ ] Speed hackers detected/logged
- [ ] No server crashes
- [ ] Performance acceptable
