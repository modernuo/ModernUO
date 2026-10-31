/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IPRateLimiter.cs                                                *
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
using System.Net;
using System.Runtime.InteropServices;

namespace Server.Network;

/// <summary>
/// Per-address connection rate limiter. Called from the accept path on the game loop; not thread-safe.
/// </summary>
public class IPRateLimiter
{
    private const long CleanupInterval = 60_000; // milliseconds
    private const int MaxCleanupPerCall = 1024;

    private readonly Dictionary<IPAddress, IPStats> _ipAttempts = [];

    // Holds exactly the keys of _ipAttempts, oldest first, so a bounded sweep resumes where it stopped
    private readonly Queue<IPAddress> _cleanupQueue = [];

    private readonly int _maxAttempts;
    private readonly long _timeWindow; // milliseconds
    private readonly long _initialBackoff; // milliseconds
    private readonly double _backoffMultiplier;
    private readonly long _maxBackoff; // milliseconds

    private long _nextCleanupTicks;
    private int _cleanupRemaining;

    public IPRateLimiter(int maxAttempts, long timeWindow, long initialBackoff, double backoffMultiplier, long maxBackoff)
    {
        _maxAttempts = maxAttempts;
        _timeWindow = timeWindow;
        _initialBackoff = initialBackoff;
        _backoffMultiplier = backoffMultiplier;
        _maxBackoff = maxBackoff;
        _nextCleanupTicks = Core.TickCount + CleanupInterval;
    }

    internal int Count => _ipAttempts.Count;

    public bool Verify(IPAddress ip, out int totalAttempts)
    {
        if (ip.IsPrivateNetwork())
        {
            totalAttempts = 0;
            return true;
        }

        var nowTicks = Core.TickCount;

        if (_cleanupRemaining > 0 || nowTicks - _nextCleanupTicks >= 0)
        {
            Cleanup(nowTicks);
        }

        ref var ipStats = ref CollectionsMarshal.GetValueRefOrAddDefault(_ipAttempts, ip, out var exists);

        if (!exists || nowTicks - ipStats.LastAttemptTicks > _timeWindow)
        {
            ipStats.AttemptCount = 1;
        }
        else
        {
            ipStats.AttemptCount++;
        }

        if (!exists)
        {
            // Ticks can start negative, so a zero default would read as a block far in the future
            ipStats.BlockUntilTicks = nowTicks;
            _cleanupQueue.Enqueue(ip);
        }

        totalAttempts = ipStats.AttemptCount;
        ipStats.LastAttemptTicks = nowTicks;

        if (ipStats.BlockUntilTicks - nowTicks > 0)
        {
            return false;
        }

        if (ipStats.AttemptCount > _maxAttempts)
        {
            var backoffTime = Math.Min(
                (long)(_initialBackoff * Math.Pow(_backoffMultiplier, ipStats.AttemptCount - _maxAttempts)),
                _maxBackoff
            );

            ipStats.BlockUntilTicks = nowTicks + backoffTime;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks at most <see cref="MaxCleanupPerCall"/> entries. A rotation that does not finish continues on the next call.
    /// </summary>
    internal void Cleanup(long nowTicks)
    {
        if (_cleanupRemaining <= 0)
        {
            _cleanupRemaining = _cleanupQueue.Count;
        }

        var toProcess = Math.Min(_cleanupRemaining, MaxCleanupPerCall);
        _cleanupRemaining -= toProcess;

        for (var i = 0; i < toProcess; i++)
        {
            var ip = _cleanupQueue.Dequeue();
            ref var ipStats = ref CollectionsMarshal.GetValueRefOrNullRef(_ipAttempts, ip);

            // A blocked address outlives its attempt window; evicting it early would lift the block
            if (nowTicks - ipStats.LastAttemptTicks > _timeWindow && nowTicks - ipStats.BlockUntilTicks >= 0)
            {
                _ipAttempts.Remove(ip);
            }
            else
            {
                _cleanupQueue.Enqueue(ip);
            }
        }

        if (_cleanupRemaining == 0)
        {
            _nextCleanupTicks = nowTicks + CleanupInterval;
        }
    }

    private struct IPStats
    {
        public int AttemptCount;
        public long LastAttemptTicks;
        public long BlockUntilTicks;
    }
}
