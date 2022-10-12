/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Server.Json;

namespace Server;

/**
     * TimeZoneHandler provides a cross platform compatible way to handle system timezones.
     *
     * By default the system timezone is used.
     * To manually configure the local timezone add the following setting to modernuo.json:
     * "system.localTimeZone": "Pacific Standard Time"
     * All valid timezones can be found in the Distribution/Data/timezones.json file.
     *
     * Notes for timezones.json:
     * By default, Windows will use the values in the "timezone" property.
     * By default, Unix/MacOS will use the values in the "region" property.
     * system.localTimeZone can be either timezone or a region.
     *
     * Example:
     * "system.localTimeZone": "Europe/Lisbon"
     */
public static class TimeZoneHandler
{
    private static readonly Dictionary<string, TimeZoneInfo> _timeZoneById = new();
    public static TimeZoneInfo SystemTimeZone { get; private set; }

    static TimeZoneHandler()
    {
        var timezones = JsonConfig.Deserialize<TimeZoneWithRegions[]>(Path.Combine(Core.BaseDirectory, "Data/timezones.json"));
        foreach (var tz in timezones)
        {
            // Get the timezone if we are on Windows
            var tzInfo = Core.IsWindows ? TimeZoneInfo.FindSystemTimeZoneById(tz.TimeZone) : null;

            foreach (var region in tz.Regions)
            {
                // Get the timezone by region if we are on linux
                tzInfo ??= TimeZoneInfo.FindSystemTimeZoneById(region);
                _timeZoneById[region] = tzInfo;
            }

            _timeZoneById[tz.TimeZone] = tzInfo;
        }
    }

    public static void Configure()
    {
        var tzId = ServerConfiguration.GetSetting("system.localTimeZone", TimeZoneInfo.Local.Id);
        SystemTimeZone = FindTimeZoneById(tzId);
    }

    /**
         * Cross platform version of TimeZoneInfo.FindSystemTimeZoneById
         */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeZoneInfo FindTimeZoneById(string id) => _timeZoneById[id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToSystemLocalTime(this DateTime date) =>
        TimeZoneInfo.ConvertTimeFromUtc(date, SystemTimeZone);
}
