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
using Server.Movement;

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
        // Use client's expected movement delays for timer interval
        // This must match what DoMoveImpl uses for NextMove
        var isMounted = owner.Mobile.Mounted;
        var runDelay = isMounted ? Movement.Movement.RunMountDelay : Movement.Movement.RunFootDelay;
        var walkDelay = isMounted ? Movement.Movement.WalkMountDelay : Movement.Movement.WalkFootDelay;
        
        // Determine if should use running speed
        var shouldRun = owner.Mobile.Combatant != null || owner.Mobile.Warmode;
        
        // Controlled pets - check distance to master
        if (owner.Mobile is BaseCreature bc && bc.Controlled && bc.ControlMaster != null)
        {
            var order = bc.ControlOrder;
            
            // Stay/Stop - use slow idle interval (pet is stationary)
            if (order is OrderType.Stay or OrderType.Stop)
            {
                return 1000; // 1 second updates for idle pets
            }
            
            if (order is OrderType.Follow or OrderType.Guard or OrderType.Come)
            {
                // If within 1 tile and no combatant, use slow idle interval
                if (bc.Combatant == null && bc.InRange(bc.ControlMaster, 1))
                {
                    // Idle near master - use slower updates (1 second)
                    return 1000;
                }
                
                // Far from master - need to run
                if (!shouldRun)
                {
                    shouldRun = !bc.InRange(bc.ControlMaster, 2);
                }
            }
        }
        
        var interval = shouldRun ? runDelay : walkDelay;

        return Math.Max(interval, Core.AOS ? 100 : 200);
    }

    protected override void OnTick()
    {
        if (ShouldStop())
        {
            Stop();
            return;
        }

        Interval = TimeSpan.FromMilliseconds(GetBaseInterval(_owner));

        _owner.Mobile.OnThink();

        if (ShouldStop())
        {
            Stop();
            return;
        }

        HandleBardEffects();

        if (_owner.Mobile.Controlled ? !_owner.Obey() : !_owner.Think())
        {
            Stop();
            return;
        }

        HandleDetectHidden();
    }

    private bool ShouldStop()
    {
        if (_owner.Mobile.Deleted)
        {
            return true;
        }

        if (_owner.Mobile.Map != null && _owner.Mobile.Map != Map.Internal &&
            (!_owner.Mobile.PlayerRangeSensitive || _owner.Mobile.Map.GetSector(_owner.Mobile.Location).Active))
        {
            return false;
        }

        _owner.Deactivate();
        return true;
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
