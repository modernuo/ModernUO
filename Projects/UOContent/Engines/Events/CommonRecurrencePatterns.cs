namespace Server.Engines.Events;

using System;

public class HourlyRecurrencePattern : IRecurrencePattern
{
    public int IntervalHours { get; }

    public HourlyRecurrencePattern(int intervalHours = 1) => IntervalHours = Math.Max(1, intervalHours);

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeZoneInfo timeZone) => afterUtc.AddHours(IntervalHours);
}

public class DailyRecurrencePattern : IRecurrencePattern
{
    public int IntervalDays { get; }

    public DailyRecurrencePattern(int intervalDays = 1) => IntervalDays = Math.Max(1, intervalDays);

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeZoneInfo timeZone) => afterUtc.AddDays(IntervalDays);
}

public class WeeklyRecurrencePattern : IRecurrencePattern
{
    public int IntervalWeeks { get; }
    public DaysOfWeek DaysOfWeek { get; }

    public WeeklyRecurrencePattern(int intervalWeeks = 1, DaysOfWeek daysOfWeek = DaysOfWeek.None)
    {
        IntervalWeeks = Math.Max(1, intervalWeeks);
        DaysOfWeek = daysOfWeek;
    }

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);
        var weekStart = local.Date.AddDays(1);
        var daysOfWeek = DaysOfWeek;

        // Set the day of the week to whatever day it is now
        if (daysOfWeek == DaysOfWeek.None)
        {
            daysOfWeek = (DaysOfWeek)(1 << (int)local.DayOfWeek);
        }

        // Example:
        // Recurrence is Monday, Wednesday, Friday - and today is Wednesday
        // weekStart will be Thursday, and then we check every day for 7 days to find the next occurrence match.
        for (int i = 0; i < 7; i++)
        {
            var candidate = weekStart.AddDays(i);
            var candidateDay = (DaysOfWeek)(1 << (int)candidate.DayOfWeek);

            if ((daysOfWeek & candidateDay) != 0 && candidate > local && !timeZone.IsInvalidTime(candidate))
            {
                return candidate.LocalToUtc(timeZone);
            }
        }

        return DateTime.MaxValue;
    }
}

public class MonthlyRecurrencePattern : IRecurrencePattern
{
    public int DayOfMonth { get; }
    public int IntervalMonths { get; }

    public MonthlyRecurrencePattern(int dayOfMonth = -1, int intervalMonths = 1)
    {
        DayOfMonth = dayOfMonth != -1 ? Math.Clamp(dayOfMonth, 1, 31) : -1;
        IntervalMonths = Math.Max(1, intervalMonths);
    }

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);
        var year = local.Year;
        var month = local.Month;
        var time = local.TimeOfDay;
        var day = DayOfMonth == -1 ? local.Day : DayOfMonth;

        for (int i = 0; i < 100; i++)
        {
            var nextMonth = month + IntervalMonths * i;
            var candidate = new DateTime(year, 1, 1)
                .AddMonths(nextMonth - 1)
                .AddDays(day - 1)
                .Add(time);

            // Some months may not have that day of the month, if not, we skip to the next interval
            if (candidate > local && candidate.Day == day && !timeZone.IsInvalidTime(candidate))
            {
                return candidate.LocalToUtc(timeZone);
            }
        }

        return DateTime.MaxValue;
    }
}

public class MonthlyOrdinalRecurrencePattern : IRecurrencePattern
{
    public int IntervalMonths { get; }
    public OrdinalDayOccurrence Ordinal { get; }
    public DayOfWeek DayOfWeek { get; }

    public MonthlyOrdinalRecurrencePattern(OrdinalDayOccurrence ordinal, DayOfWeek dayOfWeek, int intervalMonths = 1)
    {
        IntervalMonths = Math.Max(1, intervalMonths);
        Ordinal = ordinal;
        DayOfWeek = dayOfWeek;
    }

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);
        var year = local.Year;
        var month = local.Month;

        for (int i = 0; i < 100; i++)
        {
            var nextMonth = month + IntervalMonths * i;
            var candidateYearOffset = Math.DivRem(nextMonth - 1, 12, out var candidateMonthOffset);
            var candidateYear = year + candidateYearOffset;
            var candidateMonth = candidateMonthOffset + 1;

            DateTime candidate;
            if (Ordinal >= OrdinalDayOccurrence.First)
            {
                // Find the first day of the month
                var firstOfMonth = new DateTime(candidateYear, candidateMonth, 1, local.Hour, local.Minute, local.Second);

                // Find the first occurrence of the desired day
                int daysOffset = ((int)DayOfWeek - (int)firstOfMonth.DayOfWeek + 7) % 7;
                if (daysOffset > 7)
                {
                    daysOffset -= 7;
                }
                candidate = firstOfMonth.AddDays(daysOffset + 7 * (int)Ordinal);

                // If candidate is not in the same month, skip
                if (candidate.Month != candidateMonth)
                {
                    continue;
                }
            }
            else
            {
                // Find the last day of the month
                var daysInMonth = DateTime.DaysInMonth(candidateYear, candidateMonth);
                var lastOfMonth = new DateTime(candidateYear, candidateMonth, daysInMonth, local.Hour, local.Minute, local.Second);

                // Find the last occurrence of the desired day
                int daysOffset = (int)lastOfMonth.DayOfWeek - (int)DayOfWeek + 7;
                if (daysOffset >= 7)
                {
                    daysOffset -= 7;
                }
                candidate = lastOfMonth.AddDays(-daysOffset);
            }

            if (candidate > local && !timeZone.IsInvalidTime(candidate))
            {
                return candidate.LocalToUtc(timeZone);
            }
        }

        return DateTime.MaxValue;
    }
}
