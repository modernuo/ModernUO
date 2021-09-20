/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RealWorldTimer.cs                                               *
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
    public enum RealWorldTimerResolution
    {
        Minutes,
        Hours,
        Days
    }

    /**
     * Timer that ticks on every new minute, hour, day, week, month, and year. Execution is done against UTC.
     * For local system timezone or specific timezone requirements use LocalRealWorldTimer.
     *
     * Note: If a low resolution, such as RealWorldTimerResolution.Days is specified, the timer will never execute a tick
     * for a timeframe with more precision, such as every hour or minute. The resolution exists to minimize the amount of
     * times the timer has to execute internally.
     */
    public class RealWorldTimer : Timer
    {
        private readonly RealWorldTimerResolution _resolution;
        private readonly DayOfWeek _startOfWeek;

        public virtual DateTime Now
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Core.Now;
        }

        public RealWorldTimer(
            RealWorldTimerResolution res = RealWorldTimerResolution.Minutes,
            DayOfWeek startOfWeek = DayOfWeek.Sunday
        )
            : base(IntervalFromResolution(res), 0)
        {
            _resolution = res;
            _startOfWeek = startOfWeek;
        }

        protected override void OnTick()
        {
            Utility.DateToComponents(
                Now,
                out _,
                out var newMonth,
                out var newDay,
                out var dayOfWeek,
                out var newHour,
                out var newMinute,
                out var newSecond
            );

            if (_resolution == RealWorldTimerResolution.Minutes && newSecond == 0)
            {
                NewMinuteTick();
            }

            if (_resolution <= RealWorldTimerResolution.Hours && newMinute == 0)
            {
                NewHourTick();
            }

            if (_resolution <= RealWorldTimerResolution.Days)
            {
                if (newHour == 0)
                {
                    NewDayTick();
                }

                if (dayOfWeek == _startOfWeek)
                {
                    NewWeekTick();
                }

                if (newDay == 1)
                {
                    NewMonthTick();
                }

                if (newMonth == 1)
                {
                    NewYearTick();
                }
            }
        }

        public virtual void NewMinuteTick()
        {
        }

        public virtual void NewHourTick()
        {
        }

        public virtual void NewDayTick()
        {
        }

        public virtual void NewWeekTick()
        {
        }

        public virtual void NewMonthTick()
        {
        }

        public virtual void NewYearTick()
        {
        }

        // Create a resolution that is a little bit lower than the threshold we need so we don't lose exactly a second.
        private static TimeSpan IntervalFromResolution(RealWorldTimerResolution res) =>
            res switch
            {
                RealWorldTimerResolution.Minutes => TimeSpan.FromSeconds(1.0),
                RealWorldTimerResolution.Hours   => TimeSpan.FromMinutes(1.0),
                _                                => TimeSpan.FromHours(1.0)
            } - TimeSpan.FromMilliseconds(16.0);
    }
}
