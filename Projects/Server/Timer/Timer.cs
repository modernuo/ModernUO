/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Timer.cs                                                        *
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
using Server.Logging;

namespace Server;

public partial class Timer
{
    protected internal static readonly ILogger logger = LogFactory.GetLogger(typeof(Timer));

    public static void Configure()
    {
        ConfigureTimerPool();
    }

    // We need to know what ring/slot we are in so we can be removed if we are "head" of the link list.
    private int _ring;
    private int _slot;
    private long _remaining;
    private Timer _nextTimer;
    private Timer _prevTimer;
    private TimeSpan _delay;
    private TimeSpan _interval;

    public Timer(TimeSpan delay) => Init(delay, TimeSpan.Zero, 1);

    public Timer(TimeSpan interval, int count) => Init(interval, interval, count);

    public Timer(TimeSpan delay, TimeSpan interval, int count = 0) => Init(delay, interval, count);

    protected void Init(TimeSpan delay, TimeSpan interval, int count)
    {
        Delay = delay;
        Next = DateTime.MinValue;
        Interval = interval;
        Count = count;
        Running = false;
        Index = 0;
        _nextTimer = null;
        _prevTimer = null;
        _ring = -1;
        _slot = -1;
    }

    protected int Version { get; set; } // Used to determine if a timer was altered and we should abandon it.

    public DateTime Next { get; private set; }

    public TimeSpan Delay
    {
        get => _delay;
        set => _delay = TimeSpan.FromMilliseconds(RoundTicksToNextPowerOfTwo((long)value.TotalMilliseconds));
    }

    public TimeSpan Interval
    {
        get => _interval;
        set => _interval = TimeSpan.FromMilliseconds(RoundTicksToNextPowerOfTwo((long)value.TotalMilliseconds));
    }

    public int Index { get; private set; }
    public int Count { get; private set; }
    public int RemainingCount => Count == 0 ? int.MaxValue : Count - Index;
    public bool Running { get; private set; }

    public override string ToString() => GetType().FullName;

    public Timer Start()
    {
        if (World.WorldState is WorldState.Saving)
        {
            logger.Error(
                $"Attempted to start timer {{Timer}} ({{HashCode}}) while world is {{State}}{Environment.NewLine}{{StackTrace}}",
                GetType(),
                GetHashCode(),
                World.WorldState,
                new StackTrace()
            );
        }

#if THREADGUARD
        if (Thread.CurrentThread != Core.Thread)
        {
            logger.Error(
                $"Attempted to start timer {{Timer}} ({{HashCode}}) from an invalid thread!{Environment.NewLine}{{StackTrace}}",
                GetType(),
                GetHashCode(),
                new StackTrace()
            );
        }
#endif

        if (Running)
        {
            return this;
        }

        Index = 0;
        Running = true;
        AddTimer(this, (long)Delay.TotalMilliseconds);

        return this;
    }

    public void Stop()
    {
        if (World.WorldState is WorldState.Saving)
        {
            logger.Error(
                $"Attempted to stop timer {{Timer}} ({{HashCode}}) while world is {{State}}{Environment.NewLine}{{StackTrace}}",
                GetType(),
                GetHashCode(),
                World.WorldState,
                new StackTrace()
            );
        }

#if THREADGUARD
        if (Thread.CurrentThread != Core.Thread)
        {
            logger.Error(
                $"Attempted to stop timer {{Timer}} ({{HashCode}}) from an invalid thread!{Environment.NewLine}{{StackTrace}}",
                GetType(),
                GetHashCode(),
                new StackTrace()
            );
        }
#endif

        if (!Running)
        {
            return;
        }

        InternalStop();

        Detach();
        OnDetach();

        Version++;
    }

    private void InternalStop()
    {
        Running = false;

        // We are the head on the timer ring
        if (_rings[_ring][_slot] == this)
        {
            _rings[_ring][_slot] = _nextTimer;
        }

        // We are the head on the executing ring
        if (_executingRings[_ring] == this)
        {
            _executingRings[_ring] = _nextTimer;
        }
    }

    protected virtual void OnTick()
    {
    }

    private void Attach(Timer timer)
    {
#if DEBUG_TIMERS
        if (_nextTimer != null)
        {
            logger.Error(
                "{Timer} ({HashCode}) attached with a next timer already set!",
                this,
                GetHashCode()
            );
        }
#endif
        _nextTimer = timer;

        if (timer != null)
        {
#if DEBUG_TIMERS
            if (timer._prevTimer != null)
            {
                logger.Error(
                    "{Timer} ({HashCode}) attached from with a previous timer already set!",
                    timer,
                    timer.GetHashCode()
                );
            }
#endif
            timer._prevTimer = this;
        }
    }

    private void Detach()
    {
        if (_prevTimer != null)
        {
            _prevTimer._nextTimer = _nextTimer;
        }

        if (_nextTimer != null)
        {
            _nextTimer._prevTimer = _prevTimer;
        }

        _nextTimer = null;
        _prevTimer = null;
    }

    internal virtual void OnDetach()
    {
        if (Running)
        {
            logger.Error(
                $"{{Timer}} detached while still running!{Environment.NewLine}{{StackTrace}}",
                this,
                new StackTrace()
            );
            return;
        }

        _ring = -1;
        _slot = -1;
    }
}
