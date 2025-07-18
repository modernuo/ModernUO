/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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

internal sealed class AITimer : Timer
{
    private readonly BaseAI _owner;
    private int _detectHiddenMinDelay;
    private int _detectHiddenMaxDelay;

    public AITimer(BaseAI owner) : base(TimeSpan.FromMilliseconds(Utility.Random(3000)),
        TimeSpan.FromMilliseconds(GetBaseInterval(owner)))
    {
        _owner = owner;
        _owner._nextDetectHidden = Core.TickCount;
    }

    private static double GetBaseInterval(BaseAI owner)
    {
        double interval;

        if (owner._mobile.Controlled && owner._mobile.ControlOrder == OrderType.Follow
                                     && owner._mobile.Combatant != owner._mobile.ControlMaster)
        {
            interval = owner._mobile.CurrentSpeed * 400;
        }
        else if (owner._mobile.CurrentSpeed <= 0.4)
        {
            interval = owner._mobile.CurrentSpeed * 1000;
        }
        else
        {
            interval = owner._mobile.CurrentSpeed * 3000;
        }

        return Math.Max(interval, 200);
    }

    protected override void OnTick()
    {
        if (ShouldStop())
        {
            Stop();
            return;
        }

        Interval = TimeSpan.FromMilliseconds(GetBaseInterval(_owner));

        _owner._mobile.OnThink();

        if (ShouldStop())
        {
            Stop();
            return;
        }

        HandleBardEffects();

        if (_owner._mobile.Controlled ? !_owner.Obey() : !_owner.Think())
        {
            Stop();
            return;
        }

        HandleDetectHidden();
    }

    private bool ShouldStop()
    {
        if (_owner._mobile.Deleted)
        {
            return true;
        }

        if (_owner._mobile.Map != null && _owner._mobile.Map != Map.Internal &&
            (!_owner._mobile.PlayerRangeSensitive || _owner._mobile.Map.GetSector(_owner._mobile.Location).Active))
        {
            return false;
        }

        _owner.Deactivate();
        return true;
    }

    private void HandleBardEffects()
    {
        if (_owner._mobile.BardPacified)
        {
            _owner.DoBardPacified();
        }
        else if (_owner._mobile.BardProvoked)
        {
            _owner.DoBardProvoked();
        }
    }

    private void CacheDetectHiddenDelays()
    {
        var delay = Math.Min(30000 / _owner._mobile.Int, 120);
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

        _owner._nextDetectHidden = Core.TickCount +
                                   Utility.RandomMinMax(_detectHiddenMinDelay, _detectHiddenMaxDelay);
    }
}
