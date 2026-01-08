/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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
/// </summary>
public class DecayScheduler : Timer
{
    // Configuration
    private static int _maxItemsPerTick;
    private static TimeSpan _tickInterval;
    private static TimeSpan _bucketInterval;
    private static int _bucketCount;
    private static int _jitterMaxMilliseconds;

    // Timer wheel buckets (HashSets for O(1) add/remove)
    private static HashSet<Item>[] _buckets;
    private static int _currentBucketIndex;
    private static DateTime _lastBucketRotation;

    // Active processing queue for items due within current bucket window
    private static readonly PriorityQueue<Item, DateTime> _activeQueue = new();

    public static DecayScheduler Shared { get; private set; }

    public static void Configure()
    {
        _maxItemsPerTick = ServerConfiguration.GetOrUpdateSetting("decay.maxItemsPerTick", 250);
        _tickInterval = ServerConfiguration.GetOrUpdateSetting("decay.tickInterval", TimeSpan.FromMilliseconds(256));
        _bucketInterval = ServerConfiguration.GetOrUpdateSetting("decay.bucketInterval", TimeSpan.FromMinutes(5));
        _bucketCount = ServerConfiguration.GetOrUpdateSetting("decay.bucketCount", 13); // 12 buckets + 1 overflow
        _jitterMaxMilliseconds = ServerConfiguration.GetOrUpdateSetting("decay.jitterMaxMs", 25); // ±25ms jitter

        _buckets = new HashSet<Item>[_bucketCount];
        for (var i = 0; i < _bucketCount; i++)
        {
            _buckets[i] = [];
        }

        _lastBucketRotation = Core.Now;
        Shared = new DecayScheduler();
    }

    private DecayScheduler() : base(_tickInterval, _tickInterval)
    {
    }

    /// <summary>
    /// Checks if all decay tracking structures are empty.
    /// </summary>
    private static bool IsEmpty()
    {
        if (_activeQueue.Count > 0)
        {
            return false;
        }

        for (var i = 0; i < _bucketCount; i++)
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
        if (!Shared.Running)
        {
            Shared.Start();
        }

        var decayTime = item.ScheduledDecayTime;
        var timeUntilDecay = decayTime - Core.Now;

        // If already due or due very soon, add directly to active queue
        if (timeUntilDecay <= _bucketInterval)
        {
            _activeQueue.Enqueue(item, decayTime);
        }
        else
        {
            var bucketIndex = GetBucketIndex(timeUntilDecay);
            _buckets[bucketIndex].Add(item);
        }
    }

    /// <summary>
    /// Unregisters an item from decay tracking. Call when item is picked up, deleted, or otherwise ineligible.
    /// O(bucketCount) but each operation is O(1), so effectively O(1) constant time.
    /// </summary>
    public static void Unregister(Item item)
    {
        if (item == null)
        {
            return;
        }

        for (var i = 0; i < _bucketCount; i++)
        {
            if (_buckets[i].Remove(item))
            {
                return;
            }
        }

        _activeQueue.Remove(item, out _, out _);
    }

    /// <summary>
    /// Gets the bucket index for an item based on time until decay.
    /// </summary>
    private static int GetBucketIndex(TimeSpan timeUntilDecay)
    {
        // Bucket 0 is handled by _activeQueue, so buckets start at index 0 for _bucketInterval to 2*_bucketInterval
        var bucketOffset = (int)(timeUntilDecay.TotalMilliseconds / _bucketInterval.TotalMilliseconds) - 1;

        // Clamp to valid range (last bucket is overflow for items with very long decay times)
        return Math.Clamp(bucketOffset, 0, _bucketCount - 1);
    }

    protected override void OnTick()
    {
        var now = Core.Now;

        // Process items from active queue first (smaller queue = faster log(n) operations)
        ProcessActiveQueue(now);

        // Then check if it's time to rotate buckets (adds items for next tick)
        if (now - _lastBucketRotation >= _bucketInterval)
        {
            RotateBuckets(now);
        }

        // Stop timer if nothing left to track
        if (IsEmpty())
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
        _lastBucketRotation = now;

        // Get the next bucket to process
        var bucket = _buckets[_currentBucketIndex];

        // Move all items from this bucket into the active queue (with validation)
        foreach (var item in bucket)
        {
            if (item.Deleted || !item.CanDecay())
            {
                continue; // Lazy cleanup - item was picked up or deleted
            }

            var decayTime = item.ScheduledDecayTime;

            if (decayTime > now + _bucketInterval)
            {
                // Item was moved (SetLastMoved called) - re-bucket
                var newBucketIndex = GetBucketIndex(decayTime - now);
                var actualIndex = (newBucketIndex + _currentBucketIndex) % _bucketCount;

                if (actualIndex != _currentBucketIndex)
                {
                    _buckets[actualIndex].Add(item);
                    continue;
                }
            }

            // Due within next window - add to active queue
            _activeQueue.Enqueue(item, decayTime);
        }

        // Clear the processed bucket
        bucket.Clear();

        // Advance to next bucket
        _currentBucketIndex = (_currentBucketIndex + 1) % _bucketCount;
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

            _activeQueue.Dequeue();

            // Item was deleted
            if (item.Deleted)
            {
                continue;
            }

            // Check actual decay time (item may have moved)
            var currentDecayTime = item.ScheduledDecayTime;
            if (currentDecayTime > now)
            {
                // Item was moved - re-register with new time
                if (item.CanDecay())
                {
                    Register(item);
                }

                continue;
            }

            if (!item.CanDecay())
            {
                continue;
            }

            if (item.OnDecay())
            {
                item.Delete();
            }

            processed++;
        }
    }
}
