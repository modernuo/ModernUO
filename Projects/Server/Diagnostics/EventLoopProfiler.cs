/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EventLoopProfiler.cs                                            *
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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Server;

public enum LoopPhase
{
    MobileDeltas,
    ItemDeltas,
    TimerSlice,
    NetworkSlice,
    LoopTasks,
    WorldSnapshot,
}

/// <summary>
/// Event-loop time accounting, compiled out of normal builds. Build with
/// <c>-p:EventLoopProfiling=true</c> to enable; every hook is
/// <c>[Conditional("EVENT_LOOP_PROFILING")]</c>, so without the flag the call sites do not exist
/// in the IL and this class is dormant. See dev-docs/debugging-event-loop.md for how to read it.
/// </summary>
/// <remarks>
/// Each one-second sample decomposes wall time into work (per <see cref="LoopPhase"/>), sleep,
/// GC pause, and a stolen residual (wall - work - sleep): time the host ran something else.
/// Samples land in a ring buffer (~15 minutes) so a lag episode can be compared against the good
/// minutes on the same box, build, and world — the baseline RunUO's profiler never had.
/// </remarks>
public static class EventLoopProfiler
{
    public const int PhaseCount = 6;
    private const int RingSize = 900;
    private const long SampleIntervalMs = 1000;

    public struct Sample
    {
        public long WallStart;               // Core.TickCount at sample start
        public long WallMs;                  // sample length
        public long Iterations;
        public long Sleeps;
        public double SleepMs;               // total time blocked in WaitForCompletion
        public double SleepOvershootMaxMs;   // worst (elapsed - requested) this sample
        public long LateWakes;               // overshoot >= Timer.TickRate
        public long WheelLagMaxMs;           // worst wheel lateness observed at Slice entry
        public long WakesIssued;
        public long WakesElided;
        public double GcPauseMs;             // GC.GetTotalPauseDuration delta
        public int Gen0;
        public int Gen1;
        public int Gen2;
        public PhaseTimes Phases;

        // Work the phases did not account for and the loop did not spend sleeping: host
        // scheduling steals, and anything between the bracketed phases. GC pauses inside a
        // phase or sleep inflate those measurements instead, so GcPauseMs overlaps rather
        // than subtracts.
        public double StolenMs
        {
            get
            {
                var known = SleepMs + Phases.Total;
                return WallMs > known ? WallMs - known : 0;
            }
        }
    }

    [InlineArray(PhaseCount)]
    public struct PhaseTimes
    {
        private double _element0;

        public double Total
        {
            get
            {
                double total = 0;
                for (var i = 0; i < PhaseCount; i++)
                {
                    total += this[i];
                }

                return total;
            }
        }
    }

    private static readonly double _msPerTick = 1000.0 / Stopwatch.Frequency;

    private static Sample[] _ring;
    private static int _ringCount;
    private static int _ringHead;

    private static Sample _current;
    private static long _phaseStartTimestamp;
    private static long _sampleStartedAt;
    private static TimeSpan _lastGcPause;
    private static int _lastGen0;
    private static int _lastGen1;
    private static int _lastGen2;

    /// <summary>Number of samples recorded so far (capped at the ring size).</summary>
    public static int SampleCount => _ringCount;

    /// <summary>The sample currently being accumulated (not yet in the ring).</summary>
    public static Sample Current => _current;

    /// <summary>
    /// Copies the newest <paramref name="count"/> completed samples, oldest first.
    /// </summary>
    public static Sample[] History(int count = RingSize)
    {
        count = Math.Min(count, _ringCount);
        var result = new Sample[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = _ring[(_ringHead - count + i + RingSize) % RingSize];
        }

        return result;
    }

    [Conditional("EVENT_LOOP_PROFILING")]
    public static void IterationStart(long tickCount)
    {
        if (_ring == null)
        {
            _ring = new Sample[RingSize];
            _sampleStartedAt = tickCount;
            _current.WallStart = tickCount;
            _lastGcPause = GC.GetTotalPauseDuration();
            _lastGen0 = GC.CollectionCount(0);
            _lastGen1 = GC.CollectionCount(1);
            _lastGen2 = GC.CollectionCount(2);
        }

        _current.Iterations++;

        if (tickCount - _sampleStartedAt < SampleIntervalMs)
        {
            return;
        }

        _current.WallMs = tickCount - _sampleStartedAt;

        var pause = GC.GetTotalPauseDuration();
        _current.GcPauseMs = (pause - _lastGcPause).TotalMilliseconds;
        _lastGcPause = pause;

        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        _current.Gen0 = gen0 - _lastGen0;
        _current.Gen1 = gen1 - _lastGen1;
        _current.Gen2 = gen2 - _lastGen2;
        _lastGen0 = gen0;
        _lastGen1 = gen1;
        _lastGen2 = gen2;

        _ring[_ringHead] = _current;
        _ringHead = (_ringHead + 1) % RingSize;
        if (_ringCount < RingSize)
        {
            _ringCount++;
        }

        _sampleStartedAt = tickCount;
        _current = default;
        _current.WallStart = tickCount;
    }

    [Conditional("EVENT_LOOP_PROFILING")]
    public static void PhaseStart(LoopPhase phase) => _phaseStartTimestamp = Stopwatch.GetTimestamp();

    [Conditional("EVENT_LOOP_PROFILING")]
    public static void PhaseEnd(LoopPhase phase) =>
        _current.Phases[(int)phase] += (Stopwatch.GetTimestamp() - _phaseStartTimestamp) * _msPerTick;

    [Conditional("EVENT_LOOP_PROFILING")]
    public static void SleepEnd(int requestedMs, long elapsedMs)
    {
        _current.Sleeps++;
        _current.SleepMs += elapsedMs;

        var overshoot = elapsedMs - requestedMs;
        if (overshoot > _current.SleepOvershootMaxMs)
        {
            _current.SleepOvershootMaxMs = overshoot;
        }

        if (overshoot >= Timer.TickRate)
        {
            _current.LateWakes++;
        }
    }

    [Conditional("EVENT_LOOP_PROFILING")]
    public static void WheelSlice(long deltaSinceTurn)
    {
        var lag = deltaSinceTurn - Timer.TickRate;
        if (lag > _current.WheelLagMaxMs)
        {
            _current.WheelLagMaxMs = lag;
        }
    }

    // Cross-thread; approximate counts are fine for diagnosis, so no interlocked.
    [Conditional("EVENT_LOOP_PROFILING")]
    public static void WakeSignal(bool elided)
    {
        if (elided)
        {
            _current.WakesElided++;
        }
        else
        {
            _current.WakesIssued++;
        }
    }
}
