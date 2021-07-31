/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
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

using System.Runtime.CompilerServices;

namespace Server
{
    public struct TimerExecutionToken
    {
        private Timer.DelayCallTimer _timer;

        internal TimerExecutionToken(Timer.DelayCallTimer timer) => _timer = timer;

        public bool CanCancel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _timer?.Running == true;
        }

        public int RemainingCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _timer?.RemainingCount ?? 0;
        }

        public void Cancel()
        {
            _timer?.Stop();
            _timer?.Return();
            _timer = null;

            this = default;
        }
    }
}
