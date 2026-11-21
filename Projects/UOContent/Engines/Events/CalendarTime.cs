using System;
using System.Runtime.CompilerServices;

namespace Server.Engines.Events;

/// <summary>
/// Wall-clock conversions shared by the scheduler and the recurrence patterns.
/// </summary>
public static class CalendarTime
{
    /// <summary>
    /// Converts <paramref name="value"/> to UTC. <see cref="DateTimeKind.Unspecified"/> is a wall-clock time in
    /// <paramref name="timeZone"/>; <see cref="DateTimeKind.Local"/> is the machine's local time.
    /// </summary>
    public static DateTime ToUtc(DateTime value, TimeZoneInfo timeZone) =>
        value.Kind switch
        {
            DateTimeKind.Utc   => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _                  => value.LocalToUtc(timeZone)
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToLocal(DateTime utc, TimeZoneInfo timeZone) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone), DateTimeKind.Unspecified);

    /// <summary>
    /// The UTC instant of <paramref name="time"/> on the local <paramref name="date"/>. A time skipped by a
    /// spring-forward transition moves forward by the gap; a time repeated by a fall-back transition resolves
    /// to its first pass.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime At(DateTime date, TimeOnly time, TimeZoneInfo timeZone) =>
        DateTime.SpecifyKind(date.Date + time.ToTimeSpan(), DateTimeKind.Unspecified).LocalToUtc(timeZone);
}
