/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TimeZoneHandler.cs                                              *
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

public static class TimeZoneHandler
{
    public static TimeZoneInfo SystemTimeZone { get; private set; }

    public static void Configure()
    {
        var tzId = ServerConfiguration.GetSetting("system.localTimeZone", TimeZoneInfo.Local.Id);
        SystemTimeZone = FindTimeZoneById(tzId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeZoneInfo FindTimeZoneById(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToSystemLocalTime(this DateTime date) =>
        TimeZoneInfo.ConvertTimeFromUtc(date, SystemTimeZone);
}
