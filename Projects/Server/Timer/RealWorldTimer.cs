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
using Server.Timezones;

namespace Server
{
    public enum RealWorldTimerResolution
    {
        Minutes,
        Hours,
        Days
    }

    /**
     * RealWorldTimer executes the Tick functions based on system timezone. Check notes in Server/TimeZones/TimeZoneHandler.cs
     * for more information on how to manually configure your system timezone.
     */
    public class RealWorldTimer : Timer
    {
        private static DateTime _lastConvertedTimeUtc;
        private static DateTime _lastConvertedTime;

        private readonly RealWorldTimerResolution _resolution;
        private readonly DayOfWeek _startOfWeek;

        public RealWorldTimer(RealWorldTimerResolution res, DayOfWeek startOfWeek = DayOfWeek.Sunday) : base(IntervalFromResolution(res), 0)
        {
            _resolution = res;
            _startOfWeek = startOfWeek;
        }

        protected override void OnTick()
        {
            var nowUtc = Core.Now;
            if (_lastConvertedTimeUtc != nowUtc)
            {
                _lastConvertedTime = TimeZoneInfo.ConvertTimeFromUtc(Core.Now, TimeZoneHandler.SystemTimeZone);
                _lastConvertedTimeUtc = nowUtc;
            }

            Utility.DateToComponents(
                _lastConvertedTime,
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
            } - TimeSpan.FromMilliseconds(16);
    }
}
