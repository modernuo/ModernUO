using System;

namespace Server.Engines.Events;

public record struct MonthDay : IComparable<MonthDay>
{
    public byte Month { get; }
    public byte Day { get; }

    public MonthDay(int year, int month, int day)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);

        if (day < 1 || day > daysInMonth)
        {
            throw new ArgumentOutOfRangeException(nameof(day), $"Day must be between 1 and {daysInMonth} for month {month}.");
        }

        Month = (byte)month;
        Day = (byte)day;
    }

    public int CompareTo(MonthDay other)
    {
        var monthComparison = Month.CompareTo(other.Month);
        return monthComparison != 0 ? monthComparison : Day.CompareTo(other.Day);
    }

    public static bool operator <(MonthDay left, MonthDay right) => left.CompareTo(right) < 0;

    public static bool operator >(MonthDay left, MonthDay right) => left.CompareTo(right) > 0;

    public static bool operator <=(MonthDay left, MonthDay right) => left.CompareTo(right) <= 0;

    public static bool operator >=(MonthDay left, MonthDay right) => left.CompareTo(right) >= 0;
}

public static class MonthDayExtensions
{
    public static bool IsBetween(this DateTime dateTime, MonthDay start, MonthDay end)
    {
        var checkMonth = dateTime.Month;
        var checkDay = dateTime.Day;

        var startMonth = start.Month;
        var startDay = start.Day;
        var endMonth = end.Month;
        var endDay = end.Day;

        var isAfterStart = checkMonth > startMonth || checkMonth == startMonth && checkDay >= startDay;
        var isBeforeEnd = checkMonth < endMonth || checkMonth == endMonth && checkDay <= endDay;

        if (startMonth < endMonth || startMonth == endMonth && startDay <= endDay)
        {
            return isAfterStart && isBeforeEnd;
        }

        // Complex case: spans year boundary (e.g., Nov 15 - Feb 15)
        return isAfterStart || isBeforeEnd;
    }
}
