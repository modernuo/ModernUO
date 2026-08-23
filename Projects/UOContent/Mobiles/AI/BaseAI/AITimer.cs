/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AITimer.cs                                                      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System;

namespace Server.Mobiles;

/// <summary>
/// Drives an AI on two clocks: decisions at <see cref="BaseCreature.CurrentSpeed"/>, plus
/// move-only wakes at <see cref="BaseAI.NextMove"/> while a pursuit is live. Each tick
/// schedules the earlier of the two deadlines.
/// </summary>
public sealed class AITimer : Timer
{
    private readonly BaseAI _owner;
    private long _nextThink;
    private int _detectHiddenMinDelay;
    private int _detectHiddenMaxDelay;

    public AITimer(BaseAI owner) : base(TimeSpan.FromMilliseconds(Utility.Random(3000)),
        TimeSpan.FromSeconds(owner.Mobile.CurrentSpeed))
    {
        _owner = owner;
        _owner._nextDetectHidden = Core.TickCount;
        _nextThink = Core.TickCount;
    }

    public void Activate()
    {
        _nextThink = Core.TickCount;
        Interval = TimeSpan.FromSeconds(_owner.Mobile.CurrentSpeed);
        Start();
    }

    // A speed-up must not wait out a stale, longer think deadline.
    public void OnSpeedChanged()
    {
        var candidate = Core.TickCount + (long)(_owner.Mobile.CurrentSpeed * 1000);

        if (candidate - _nextThink < 0)
        {
            _nextThink = candidate;
        }

        Interval = TimeSpan.FromSeconds(_owner.Mobile.CurrentSpeed);
    }

    protected override void OnTick()
    {
        if (ShouldStop())
        {
            Stop();
            return;
        }

        if (Core.TickCount - _nextThink >= 0)
        {
            _owner.Mobile.OnThink();

            if (ShouldStop())
            {
                Stop();
                return;
            }

            HandleBardEffects();

            if (_owner.Mobile.Controlled ? _owner.Obey() : _owner.Think())
            {
                HandleDetectHidden();
            }

            // Cadence from the post-decision speed (decisions may flip active/passive).
            _nextThink = Core.TickCount + (long)(_owner.Mobile.CurrentSpeed * 1000);
        }
        else
        {
            _owner.ContinueMove();
        }

        ScheduleNext();
    }

    private void ScheduleNext()
    {
        var now = Core.TickCount;
        var delay = _nextThink - now;

        if (_owner.TryGetMoveWake(out var nextMove))
        {
            var moveDelay = nextMove - now;

            // Only a future budget is a wake — a blocked creature must not spin the timer.
            if (moveDelay > 0 && moveDelay < delay)
            {
                delay = moveDelay;
            }
        }

        // The wheel rounds up to its 8ms resolution; a non-positive delay becomes one turn.
        Interval = TimeSpan.FromMilliseconds(delay);
    }

    private bool ShouldStop()
    {
        if (_owner.Mobile.Deleted)
        {
            return true;
        }

        if (_owner.Mobile.Map == null || _owner.Mobile.Map == Map.Internal || _owner.Mobile.PlayerRangeSensitive &&
            !_owner.Mobile.Controlled && !_owner.Mobile.Map.GetSector(_owner.Mobile.Location).Active)
        {
            _owner.Deactivate();
            return true;
        }

        return false;
    }

    private void HandleBardEffects()
    {
        if (_owner.Mobile.BardPacified)
        {
            _owner.DoBardPacified();
        }
        else if (_owner.Mobile.BardProvoked)
        {
            _owner.DoBardProvoked();
        }
    }

    private void CacheDetectHiddenDelays()
    {
        var delay = Math.Min(30000 / _owner.Mobile.Int, 120);
        _detectHiddenMinDelay = delay * 900;  // 26s to 108s
        _detectHiddenMaxDelay = delay * 1100; // 32s to 132s
    }

    private void HandleDetectHidden()
    {
        if (!_owner.CanDetectHidden || Core.TickCount - _owner._nextDetectHidden < 0)
        {
            return;
        }

        _owner.DetectHidden();

        if (_detectHiddenMinDelay == 0 || _detectHiddenMaxDelay == 0)
        {
            CacheDetectHiddenDelays();
        }

        _owner._nextDetectHidden = Core.TickCount + Utility.RandomMinMax(_detectHiddenMinDelay, _detectHiddenMaxDelay);
    }
}
