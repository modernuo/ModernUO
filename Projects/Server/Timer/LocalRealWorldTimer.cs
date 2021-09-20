/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LocalRealWorldTimer.cs                                          *
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
    /**
     * LocalRealWorldTimer executes the Tick functions based on the specified time zone.
     * See Server/TimeZones/TimeZoneHandler.cs for more information on how to manually configure your system timezone
     * or specify a custom timezone.
     *
     * Note: This timer is subject to daylight savings and will not "skip" the extra time.
     */
    public class LocalRealWorldTimer : RealWorldTimer
    {
        private readonly TimeZoneInfo _timeZone;
        private DateTime _lastConvertedTimeUtc;
        private DateTime _lastConvertedTime;

        public override DateTime Now
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetLocal();
        }

        private DateTime GetLocal()
        {
            var nowUtc = Core.Now;
            if (_lastConvertedTimeUtc != nowUtc)
            {
                _lastConvertedTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _timeZone);
                _lastConvertedTimeUtc = nowUtc;
            }

            return _lastConvertedTime;
        }

        public LocalRealWorldTimer(
            RealWorldTimerResolution res = RealWorldTimerResolution.Minutes,
            DayOfWeek startOfWeek = DayOfWeek.Sunday
        ) : this(
            TimeZoneHandler.SystemTimeZone,
            res,
            startOfWeek
        )
        {
        }

        public LocalRealWorldTimer(
            TimeZoneInfo timeZone,
            RealWorldTimerResolution res = RealWorldTimerResolution.Minutes,
            DayOfWeek startOfWeek = DayOfWeek.Sunday
        ) : base(
            res,
            startOfWeek
        ) => _timeZone = timeZone;
    }
}
