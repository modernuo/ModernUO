/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Timer.Pause.cs                                                  *
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

namespace Server
{
    public partial class Timer
    {
        public class DelayTaskTimer : Timer, INotifyCompletion
        {
            private Action _continuation;
            private bool _complete;

            internal DelayTaskTimer(TimeSpan delay) : base(delay) => Start();

            protected override void OnTick()
            {
                _complete = true;
                _continuation?.Invoke();
            }

            public DelayTaskTimer GetAwaiter() => this;

            public bool IsCompleted => _complete;

            public void OnCompleted(Action continuation) => _continuation = continuation;

            public void GetResult()
            {
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayTaskTimer Pause(TimeSpan ms) => new(ms);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayTaskTimer Pause(int ms) => Pause(TimeSpan.FromMilliseconds(ms));
    }
}
