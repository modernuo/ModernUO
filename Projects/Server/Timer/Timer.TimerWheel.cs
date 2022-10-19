/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Timer.TimerWheel.cs                                             *
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
using System.IO;
using System.Linq;

namespace Server;

public partial class Timer
{
#if DEBUG_TIMERS
    private const int _chainExecutionThreshold = 512;
#endif
    private const int _ringSizePowerOf2 = 12;
    private const int _ringSize = 1 << _ringSizePowerOf2; // 4096
    private const int _ringLayers = 3;
    private const int _tickRatePowerOf2 = 3;
    private const int _tickRate = 1 << _tickRatePowerOf2; // 8ms

    private static Timer[][] _rings = new Timer[_ringLayers][];
    private static int[] _ringIndexes = new int[_ringLayers];
    private static Timer[] _executingRings = new Timer[_ringLayers];

    private static long _lastTickTurned = -1;

    public static void Init(long tickCount)
    {
        _lastTickTurned = tickCount;

        for (int i = 0; i < _rings.Length; i++)
        {
            _rings[i] = new Timer[_ringSize];
            _ringIndexes[i] = 0;
        }
    }

    public static void Slice(long tickCount)
    {
        var deltaSinceTurn = tickCount - _lastTickTurned;
        while (deltaSinceTurn >= _tickRate)
        {
            deltaSinceTurn -= _tickRate;
            _lastTickTurned += _tickRate;
            Turn();
        }
    }

    private static void Turn()
    {
        var turnNextWheel = false;

        // Detach the chain from the timer wheel. This allows adding timers to the same slot during execution.
        for (var i = 0; i < _ringLayers; i++)
        {
            if (i == 0 || turnNextWheel)
            {
                var ringIndex = ++_ringIndexes[i];
                turnNextWheel = ringIndex >= _ringSize;

                if (turnNextWheel)
                {
                    ringIndex = _ringIndexes[i] = 0;
                }

                _executingRings[i] = _rings[i][ringIndex];
                _rings[i][ringIndex] = null;
            }
            else
            {
                _executingRings[i] = null;
            }
        }

        for (var i = 0; i < _ringLayers; i++)
        {
#if DEBUG_TIMERS
            var executionCount = 0;
#endif
            while (_executingRings[i] != null)
            {
#if DEBUG_TIMERS
                executionCount++;
#endif

                var timer = _executingRings[i];

                // Set the executing timer to the next in the link list because we will be detaching.
                _executingRings[i] = timer._nextTimer;

                timer.Detach();

                // Check to see if it's running just in case it was stopped by another timer
                if (timer.Running)
                {
                    if (i > 0 && timer._remaining > 0)
                    {
                        // Promote
                        AddTimer(timer, timer._remaining);
                    }
                    else
                    {
                        Execute(timer);
                    }
                }

                if (!timer.Running)
                {
                    timer.OnDetach();
                }
            }
#if DEBUG_TIMERS
            if (executionCount > _chainExecutionThreshold)
            {
                logger.Warning(
                    "Timer threshold of {Threshold} met. Executed {Count} timers sequentially.",
                    _chainExecutionThreshold,
                    executionCount
                );
            }
#endif
        }
    }

    private static void Execute(Timer timer)
    {
        var finished = timer.Count != 0 && ++timer.Index >= timer.Count;

        var version = timer.Version;

        var prof = timer.GetProfile();
        prof?.Start();
        timer.OnTick();
        prof?.Finish();

        // If the timer has not been stopped, and it has not been altered (restarted, returned etc)
        if (timer.Running && timer.Version == version)
        {
            if (finished)
            {
                timer.Stop();
            }
            else
            {
                timer.Delay = timer.Interval;
                timer.Next = Core.Now + timer.Interval;
                AddTimer(timer, (long)timer.Delay.TotalMilliseconds);
            }
        }
    }

    private static void AddTimer(Timer timer, long delay)
    {
#if DEBUG_TIMERS
        var originalDelay = delay;
#endif
        delay = Math.Max(0, delay);

        var resolutionPowerOf2 = _tickRatePowerOf2;
        for (var i = 0; i < _ringLayers; i++)
        {
            var resolution = 1L << resolutionPowerOf2;
            var nextResolutionPowerOf2 = resolutionPowerOf2 + _ringSizePowerOf2;
            var max = 1L << nextResolutionPowerOf2;
            if (delay < max)
            {
                var remaining = delay & (resolution - 1);
                var slot = (delay >> resolutionPowerOf2) + _ringIndexes[i] + (remaining > 0 ? 1 : 0);

                // Round up if we have a delay of 0
                if (delay == 0)
                {
                    slot++;
                    remaining = 0;
                }

                if (slot >= _ringSize)
                {
                    slot -= _ringSize;
                }

                timer.Attach(_rings[i][slot]);
                timer._remaining = remaining;
                timer._ring = i;
                timer._slot = (int)slot;

                _rings[i][slot] = timer;

                return;
            }

            // The remaining amount until we turn this ring
            delay -= resolution * (_ringSize - _ringIndexes[i]);
            resolutionPowerOf2 = nextResolutionPowerOf2;
        }

        // TODO: Handle timers > 17yrs
#if DEBUG_TIMERS
        logger.Error("Timer is more than max duration. ({Duration})", originalDelay);
#endif
    }

    public static void DumpInfo(TextWriter tw)
    {
        tw.WriteLine($"Date: {Core.Now.ToLocalTime()}\n");
        tw.WriteLine($"Pool - Count: {_poolCount}; Capacity {_poolCapacity}\n");

        var total = 0.0;
        var hash = new Dictionary<string, int>();

        for (var i = 0; i < _ringLayers; i++)
        {
            for (var j = 0; j < _ringSize; j++)
            {
                var t = _rings[i][j];
                if (t == null)
                {
                    continue;
                }

                while (t != null)
                {
                    var name = t.ToString();

                    hash.TryGetValue(name, out var count);
                    hash[name] = count + 1;

                    total++;

                    t = t._nextTimer;
                }
            }
        }

        tw.WriteLine("Timers:");

        foreach (var (name, count) in hash.OrderByDescending(o => o.Value))
        {
            var percent = count / total;
            var line = $"{count:#,0} ({percent:P1})";
            // 6 - 15 / 8 = 1
            var tabs = new string('\t', line.Length < 12 ? 2 : 1);
            tw.WriteLine($"{line}{tabs}{name}");
        }

#if DEBUG_TIMERS
        tw.WriteLine($"{Environment.NewLine}Stack Traces:");
        foreach (var kvp in DelayCallTimer._stackTraces)
        {
            tw.WriteLine(kvp.Value);
            tw.WriteLine();
        }
#endif

        tw.WriteLine();
        tw.WriteLine();
    }

    public static void ClearAllTimers(long tickCount)
    {
        _lastTickTurned = tickCount;

        foreach (var t in _rings)
        {
            if (t == null)
            {
                continue;
            }

            for (var i = 0; i < _ringSize; i++)
            {
                var node = t[i];
                Timer next;

                do
                {
                    next = node?._nextTimer;
                    node?.Stop();
                } while (next != null);
            }
        }
    }
}
