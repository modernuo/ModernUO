/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TimerExecutionToken.cs                                          *
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
using System.Runtime.CompilerServices;

namespace Server;

public struct TimerExecutionToken
{
    private Timer.DelayCallTimer _timer;

    internal TimerExecutionToken(Timer.DelayCallTimer timer) => _timer = timer;

    public bool Running
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _timer?.Running == true;
    }

    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _timer?.Index ?? 0;
    }

    public int RemainingCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _timer?.RemainingCount ?? 0;
    }

    public DateTime Next
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _timer?.Next ?? DateTime.MinValue;
    }

    public void Cancel()
    {
        if (_timer != null)
        {
            _timer._returnOnDetach = true;
            _timer?.Stop();
            _timer = null;
        }

        this = default;
    }
}
