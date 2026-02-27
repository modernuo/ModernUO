/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Network;

public class IPRateLimiter
{
    private static readonly ConcurrentQueue<IPStats> _statsPool = [];
    private const int MaxPoolSize = 32_768;

    private readonly SemaphoreSlim _cleanupSignal = new(0, 1);
    private readonly ConcurrentDictionary<IPAddress, IPStats> _ipAttempts;
    private readonly ConcurrentQueue<IPAddress> _cleanupQueue;
    private readonly CancellationTokenSource _cts;

    private readonly int _maxAttempts;
    private readonly long _timeWindow; // milliseconds
    private readonly long _initialBackoff; // milliseconds
    private readonly double _backoffMultiplier;
    private readonly long _maxBackoff; // milliseconds

    public IPRateLimiter(
        int maxAttempts, long timeWindow, long initialBackoff, double backoffMultiplier, long maxBackoff,
        CancellationToken token
    )
    {
        _ipAttempts = [];
        _cleanupQueue = [];
        _maxAttempts = maxAttempts;
        _timeWindow = timeWindow;
        _initialBackoff = initialBackoff;
        _backoffMultiplier = backoffMultiplier;
        _maxBackoff = maxBackoff;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        Task.Run(CleanupLoop, Core.ClosingTokenSource.Token);
    }

    public bool Verify(IPAddress ip, out int totalAttempts)
    {
        if (ip.IsPrivateNetwork())
        {
            totalAttempts = 0;
            return true;
        }

        var nowTicks = Core.TickCount;
        var ipStats = _ipAttempts.GetOrAdd(ip, _ => GetOrCreateIPStats());
        var added = ipStats.AttemptCount == 0;

        lock (ipStats)
        {
            if (nowTicks - ipStats.LastAttemptTicks > _timeWindow)
            {
                ipStats.AttemptCount = 1; // Reset
            }
            else
            {
                ipStats.AttemptCount++;
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

            if (added)
            {
                _cleanupQueue.Enqueue(ip);
                RunCleanup();
            }
        }

        return true;
    }

    private static IPStats GetOrCreateIPStats() => _statsPool.TryDequeue(out var stats) ? stats : new IPStats();

    private static void ReturnToPool(IPStats stats)
    {
        stats.Reset();

        if (_statsPool.Count < MaxPoolSize)
        {
            _statsPool.Enqueue(stats);
        }
    }

    private void RunCleanup()
    {
        if (_cleanupSignal.CurrentCount > 0)
        {
            return;
        }

        try
        {
            _cleanupSignal.Release();
            Task.Run(CleanupLoop, _cts.Token);
        }
        catch
        {
            // Do nothing
        }
    }

    private async ValueTask CleanupLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            await _cleanupSignal.WaitAsync(_cts.Token);

            var maxToProcess = Math.Min(_cleanupQueue.Count, 500);
            var nowTicks = Core.TickCount;

            for (var i = 0; i < maxToProcess; i++)
            {
                if (!_cts.IsCancellationRequested)
                {
                    break;
                }

                if (!_cleanupQueue.TryDequeue(out var ip) || !_ipAttempts.TryGetValue(ip, out var ipStats))
                {
                    continue;
                }

                lock (ipStats)
                {
                    if (nowTicks - ipStats.LastAttemptTicks < _timeWindow)
                    {
                        _cleanupQueue.Enqueue(ip);
                        continue;
                    }

                    if (_ipAttempts.TryRemove(ip, out _))
                    {
                        ReturnToPool(ipStats);
                    }
                }
            }

            if (!_cleanupQueue.IsEmpty)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
                try
                {
                    _cleanupSignal.Release();
                }
                catch
                {
                    // Do nothing
                }
            }
        }
    }

    private class IPStats
    {
        public int AttemptCount;
        public long LastAttemptTicks;
        public long BlockUntilTicks;

        public void Reset()
        {
            AttemptCount = 0;
            LastAttemptTicks = 0;
            BlockUntilTicks = 0;
        }
    }
}
