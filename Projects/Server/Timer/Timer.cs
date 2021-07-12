/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
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
using Server.Diagnostics;

namespace Server
{
    public partial class Timer
    {
        // We need to know what ring/slot we are in so we can be removed if we are "head" of the link list.
        private int _ring;
        private int _slot;

        private long _remaining;
        private Timer _nextTimer;
        private Timer _prevTimer;

        public Timer(TimeSpan delay) : this(delay, TimeSpan.Zero, 1)
        {
        }

        public Timer(TimeSpan delay, TimeSpan interval, int count = 0)
        {
            Delay = delay;
            Interval = interval;
            Count = count;
            _nextTimer = null;
            _prevTimer = null;
            Next = Core.Now + Delay;

            var prof = GetProfile();

            if (prof != null)
            {
                prof.Created++;
            }
        }

        public DateTime Next { get; private set; }
        public TimeSpan Delay { get; set; }
        public TimeSpan Interval { get; set; }
        public int Index { get; private set; }
        public int Count { get; }
        public bool Running { get; private set; }

        public TimerProfile GetProfile() => !Core.Profiling ? null : TimerProfile.Acquire(ToString() ?? "null");

        public override string ToString() => GetType().FullName;

        public Timer Start()
        {
            if (Running)
            {
                return this;
            }

            Running = true;
            AddTimer(this, (long)Delay.TotalMilliseconds);

            var prof = GetProfile();

            if (prof != null)
            {
                prof.Started++;
            }

            return this;
        }

        public Timer Stop()
        {
            if (!Running)
            {
                return this;
            }

            Running = false;
            RemoveTimer(this);

            var prof = GetProfile();

            if (prof != null)
            {
                prof.Stopped++;
            }

            return this;
        }

        protected virtual void OnTick()
        {
        }
    }
}
