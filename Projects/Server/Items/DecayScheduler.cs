/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DecayScheduler.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;

namespace Server.Items;

/// <summary>
/// Timer wheel-based decay scheduler with O(1) registration/unregistration.
/// Uses coarse-grained HashSet buckets for time intervals, with a PriorityQueue
/// for fine-grained processing of items due within the current window.
/// <para>
/// An item that becomes decay eligible must be passed to <see cref="Item.UpdateDecayRegistration" />
/// once its parent, map and location are final. The scheduler drops items that stop being eligible,
/// but nothing enrolls one that becomes eligible while untracked.
/// </para>
/// <para>
/// <see cref="Item.Decays" /> and <see cref="Item.DecayTime" /> overrides must be stable rather than
/// recomputed per read: <see cref="Item.ScheduledDecayTime" /> is read repeatedly, and a value that
/// changes between reads re-buckets the item every tick and decays it at the wrong time.
/// </para>
/// </summary>
public class DecayScheduler : Timer
{
    // Item.DecaySlot values; any non-negative value is a bucket index.
    internal const sbyte SlotNone = -1;
    internal const sbyte SlotOverflow = -2;
    internal const sbyte SlotActive = -3;

    private const int BucketCount = 12;
    private static int _maxItemsPerTick;
    private static TimeSpan _tickInterval;
    private static TimeSpan _bucketInterval;
    private static int _jitterMaxMilliseconds;

    // Timer wheel buckets (HashSets for O(1) add/remove)
    private static HashSet<Item>[] _buckets;
    private static int _currentBucketIndex;
    private static DateTime _nextBucketRotation;

    // Overflow bucket (separate from regular rotation, for items with decay > total bucket span)
    private static HashSet<Item> _overflowBucket;
    private static TimeSpan _totalBucketSpan;
    private static DateTime _nextOverflowCheck;

    // Active processing queue for items due within current bucket window
    private static readonly PriorityQueue<Item, DateTime> _activeQueue = new();

    public static DecayScheduler Shared { get; private set; }

    public static void Configure()
    {
        _maxItemsPerTick = ServerConfiguration.GetSetting("decay.maxItemsPerTick", 250);
        _tickInterval = ServerConfiguration.GetSetting("decay.tickInterval", TimeSpan.FromMilliseconds(256));
        _bucketInterval = ServerConfiguration.GetSetting("decay.bucketInterval", TimeSpan.FromMinutes(5));
        _jitterMaxMilliseconds = ServerConfiguration.GetSetting("decay.jitterMaxMs", 25); // ±25ms jitter

        _buckets = new HashSet<Item>[BucketCount];
        for (var i = 0; i < BucketCount; i++)
        {
            _buckets[i] = [];
        }

        // Overflow bucket is separate from the timer wheel
        _overflowBucket = [];
        _totalBucketSpan = TimeSpan.FromTicks(_bucketInterval.Ticks * BucketCount);

        var now = Core.Now;
        _nextBucketRotation = now + _bucketInterval;
        _nextOverflowCheck = now + _totalBucketSpan;

        Shared = new DecayScheduler();
    }

    private DecayScheduler() : base(_tickInterval, _tickInterval)
    {
    }

    /// <summary>
    /// Test hook: drops all tracked items and re-bases the rotation schedule on
    /// <see cref="Core.Now" />, so deadlines left by a test that moved the clock do not suppress
    /// rotations in the next one.
    /// </summary>
    internal static void ResetForTests()
    {
        Shared?.Stop();

        _activeQueue.Clear();
        _overflowBucket.Clear();

        for (var i = 0; i < BucketCount; i++)
        {
            _buckets[i].Clear();
        }

        _currentBucketIndex = 0;

        var now = Core.Now;
        _nextBucketRotation = now + _bucketInterval;
        _nextOverflowCheck = now + _totalBucketSpan;
    }

    /// <summary>
    /// Diagnostics: true if the item is currently tracked in any bucket, the overflow
    /// bucket, or the active queue.
    /// </summary>
    internal static bool IsRegistered(Item item)
    {
        if (item == null)
        {
            return false;
        }

        if (_overflowBucket.Contains(item))
        {
            return true;
        }

        for (var i = 0; i < BucketCount; i++)
        {
            if (_buckets[i].Contains(item))
            {
                return true;
            }
        }

        foreach (var (queued, _) in _activeQueue.UnorderedItems)
        {
            if (queued == item)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if all decay tracking structures are empty.
    /// </summary>
    private static bool IsEmpty()
    {
        if (_activeQueue.Count > 0 || _overflowBucket.Count > 0)
        {
            return false;
        }

        for (var i = 0; i < BucketCount; i++)
        {
            if (_buckets[i].Count > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Registers an item for decay tracking. Call after item becomes decay-eligible.
    /// </summary>
    public static void Register(Item item)
    {
        if (item?.Deleted != false || !item.CanDecay())
        {
            return;
        }

        // Start timer if not running
        Shared.Start();

        var decayTime = item.ScheduledDecayTime;
        var timeUntilDecay = decayTime - Core.Now;

        // If already due or due very soon, add directly to active queue
        if (timeUntilDecay <= _bucketInterval)
        {
            _activeQueue.Enqueue(item, decayTime);
            item.DecaySlot = SlotActive;
        }
        // If beyond the total bucket span, add to overflow bucket
        else if (timeUntilDecay > _totalBucketSpan)
        {
            _overflowBucket.Add(item);
            item.DecaySlot = SlotOverflow;
        }
        else
        {
            // Calculate bucket index relative to current position
            var bucketOffset = GetBucketOffset(timeUntilDecay);
            var absoluteIndex = (_currentBucketIndex + bucketOffset) % BucketCount;
            _buckets[absoluteIndex].Add(item);
            item.DecaySlot = (sbyte)absoluteIndex;
        }
    }

    /// <summary>
    /// Unregisters an item from decay tracking. Call when item is picked up, deleted, or otherwise ineligible.
    /// </summary>
    public static void Unregister(Item item)
    {
        if (item == null || item.DecaySlot == SlotNone)
        {
            return;
        }

        var slot = item.DecaySlot;
        item.DecaySlot = SlotNone;

        if (slot == SlotOverflow)
        {
            _overflowBucket.Remove(item);
        }
        else if (slot == SlotActive)
        {
            _activeQueue.Remove(item, out _, out _);
        }
        else
        {
            _buckets[slot].Remove(item);
        }
    }

    /// <summary>
    /// Gets the bucket offset for an item based on time until decay.
    /// Returns a value from 0 to BucketCount - 1, representing how many buckets ahead of current.
    /// </summary>
    private static int GetBucketOffset(TimeSpan timeUntilDecay)
    {
        // Items due within _bucketInterval go to active queue, so offset 0 means _bucketInterval to 2*_bucketInterval
        var bucketOffset = (int)(timeUntilDecay.Ticks / _bucketInterval.Ticks) - 1;

        // Clamp to valid range within regular buckets
        return Math.Clamp(bucketOffset, 0, BucketCount - 1);
    }

    /// <summary>
    /// Advances the wheel for <paramref name="now" />. Returns true when nothing is left to track.
    /// </summary>
    internal static bool ProcessTick(DateTime now)
    {
        // Process items from active queue first (smaller queue = faster log(n) operations)
        ProcessActiveQueue(now);

        // Then check if it's time to rotate buckets (adds items for next tick)
        if (now >= _nextBucketRotation)
        {
            RotateBuckets(now);
        }

        // Check overflow bucket on its own schedule (when items might enter the regular window)
        if (now >= _nextOverflowCheck)
        {
            ProcessOverflow(now);
        }

        return IsEmpty();
    }

    protected override void OnTick()
    {
        // Stop timer if nothing left to track
        if (ProcessTick(Core.Now))
        {
            Stop();
            return;
        }

        // Apply jitter for next tick to prevent synchronization with other systems
        if (_jitterMaxMilliseconds > 0)
        {
            var jitter = Utility.Random(-_jitterMaxMilliseconds, _jitterMaxMilliseconds * 2 + 1);
            Interval = _tickInterval + TimeSpan.FromMilliseconds(jitter);
        }
    }

    /// <summary>
    /// Rotates the timer wheel, moving the next bucket's contents into the active queue.
    /// </summary>
    private static void RotateBuckets(DateTime now)
    {
        _nextBucketRotation = now + _bucketInterval;

        // Get the next bucket to process
        var bucket = _buckets[_currentBucketIndex];

        // Move all items from this bucket into the active queue (with validation)
        foreach (var item in bucket)
        {
            if (item.Deleted || !item.CanDecay())
            {
                item.DecaySlot = SlotNone; // Lazy cleanup - item was picked up or deleted
                continue;
            }

            var decayTime = item.ScheduledDecayTime;
            var timeUntilDecay = decayTime - now;

            if (timeUntilDecay > _bucketInterval)
            {
                // Item was moved (SetLastMoved called) - re-bucket or move to overflow
                if (timeUntilDecay > _totalBucketSpan)
                {
                    // Extended beyond total span - move to overflow
                    _overflowBucket.Add(item);
                    item.DecaySlot = SlotOverflow;
                }
                else
                {
                    // Re-bucket within regular buckets
                    var bucketOffset = GetBucketOffset(timeUntilDecay);
                    var actualIndex = (_currentBucketIndex + bucketOffset) % BucketCount;

                    if (actualIndex != _currentBucketIndex)
                    {
                        _buckets[actualIndex].Add(item);
                        item.DecaySlot = (sbyte)actualIndex;
                    }
                    else
                    {
                        // Still maps to current bucket - add to active queue
                        _activeQueue.Enqueue(item, decayTime);
                        item.DecaySlot = SlotActive;
                    }
                }
                continue;
            }

            // Due within next window - add to active queue
            _activeQueue.Enqueue(item, decayTime);
            item.DecaySlot = SlotActive;
        }

        // Clear the processed bucket
        bucket.Clear();

        // Advance to next bucket
        _currentBucketIndex = (_currentBucketIndex + 1) % BucketCount;
    }

    /// <summary>
    /// Processes the overflow bucket, moving items that are now within the regular bucket window.
    /// </summary>
    private static void ProcessOverflow(DateTime now)
    {
        _nextOverflowCheck = now + _totalBucketSpan;

        // Move items that are now within the regular bucket span
        _overflowBucket.RemoveWhere(item =>
        {
            if (item.Deleted || !item.CanDecay())
            {
                item.DecaySlot = SlotNone;
                return true; // Remove invalid items
            }

            var decayTime = item.ScheduledDecayTime;
            var timeUntilDecay = decayTime - now;

            if (timeUntilDecay > _totalBucketSpan)
            {
                return false; // Keep in overflow
            }

            // Now within regular bucket range - move to appropriate bucket
            if (timeUntilDecay <= _bucketInterval)
            {
                _activeQueue.Enqueue(item, decayTime);
                item.DecaySlot = SlotActive;
            }
            else
            {
                var bucketOffset = GetBucketOffset(timeUntilDecay);
                var actualIndex = (_currentBucketIndex + bucketOffset) % BucketCount;
                _buckets[actualIndex].Add(item);
                item.DecaySlot = (sbyte)actualIndex;
            }

            return true; // Remove from overflow
        });
    }

    /// <summary>
    /// Processes items from the active queue that are due for decay.
    /// </summary>
    private static void ProcessActiveQueue(DateTime now)
    {
        var processed = 0;

        while (_activeQueue.TryPeek(out var item, out var scheduledTime) && processed < _maxItemsPerTick)
        {
            // Not yet due - stop processing
            if (scheduledTime > now)
            {
                break;
            }

            processed++;
            _activeQueue.Dequeue();
            item.DecaySlot = SlotNone;

            if (item.Deleted || !item.CanDecay())
            {
                continue;
            }

            if (item.ScheduledDecayTime > now)
            {
                Register(item);
            }
            else if (item.OnDecay())
            {
                item.Delete();
            }
            else
            {
                // Refused by the region. Restart the clock rather than dropping the item, which has
                // already left the queue; re-registering as-is would spin on a due time in the past.
                item.SetLastMoved();
            }
        }
    }
}
