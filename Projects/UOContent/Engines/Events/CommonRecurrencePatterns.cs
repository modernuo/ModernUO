namespace Server.Engines.Events;

using System;

public class HourlyRecurrencePattern : IRecurrencePattern
{
    public int IntervalHours { get; }

    public HourlyRecurrencePattern(int intervalHours = 1) => IntervalHours = Math.Max(1, intervalHours);

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);
        return new DateTime(local.Year, local.Month, local.Day, local.Hour, time.Minute, 0)
            .LocalToUtc(timeZone)
            .AddHours(IntervalHours);
    }
}

public class DailyRecurrencePattern : IRecurrencePattern
{
    public int IntervalDays { get; }

    public DailyRecurrencePattern(int intervalDays = 1) => IntervalDays = Math.Max(1, intervalDays);

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);
        return new DateTime(local.Year, local.Month, local.Day, time.Hour, time.Minute, 0)
            .LocalToUtc(timeZone)
            .AddDays(IntervalDays);
    }
}

public class WeeklyRecurrencePattern : IRecurrencePattern
{
    public int IntervalWeeks { get; }
    public AllowedDays AllowedDays { get; }
    public AllowedMonths AllowedMonths { get; }

    public WeeklyRecurrencePattern(int intervalWeeks = 1, AllowedMonths allowedMonths = AllowedMonths.All, AllowedDays allowedDays = AllowedDays.None)
    {
        IntervalWeeks = Math.Max(1, intervalWeeks);
        AllowedDays = allowedDays == AllowedDays.None ? AllowedDays.All : allowedDays;
        AllowedMonths = allowedMonths == AllowedMonths.None ? AllowedMonths.All : allowedMonths;
    }

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);
        var daysOfWeek = AllowedDays == AllowedDays.None ? local.DayOfWeek.ToDaysOfWeek() : AllowedDays;

        var weekStart = local.Date.AddDays(-(int)local.DayOfWeek);

        // No more days in this week, jump IntervalWeeks ahead
        for (var week = 0; week <= 52; week++)
        {
            var nextWeekStart = weekStart.AddDays(7 * IntervalWeeks * week);
            var weekEnd = nextWeekStart.AddDays(6);

            var startMonth = (AllowedMonths)(1 << (nextWeekStart.Month - 1));
            var endMonth = (AllowedMonths)(1 << (weekEnd.Month - 1));

            // Skip the entire week if the start and end months are not in the allowed months
            if ((AllowedMonths & (startMonth | endMonth)) == 0)
            {
                continue;
            }

            for (var i = 0; i < 7; i++)
            {
                var day = nextWeekStart.AddDays(i);

                var currentMonth = (AllowedMonths)(1 << (day.Month - 1));
                if ((AllowedMonths & currentMonth) == 0)
                {
                    continue;
                }

                var dayOfWeekFlag = (AllowedDays)(1 << (int)day.DayOfWeek);
                if ((daysOfWeek & dayOfWeekFlag) != 0)
                {
                    var candidate = new DateTime(day.Year, day.Month, day.Day, time.Hour, time.Minute, 0);
                    if (candidate > local && !timeZone.IsInvalidTime(candidate))
                    {
                        return candidate.LocalToUtc(timeZone);
                    }
                }
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

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);
        var year = local.Year;
        var month = local.Month;
        var day = DayOfMonth == -1 ? local.Day : DayOfMonth;

        for (var i = 0; i < 100; i++)
        {
            var nextMonth = month + IntervalMonths * i;
            var candidate = new DateTime(year, 1, 1)
                .AddMonths(nextMonth - 1)
                .AddDays(day - 1)
                .Add(time.ToTimeSpan());

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

    public DateTime GetNextOccurrence(DateTime afterUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);
        var year = local.Year;
        var month = local.Month;

        for (var i = 0; i < 100; i++)
        {
            var nextMonth = month + IntervalMonths * i;
            var candidateYearOffset = Math.DivRem(nextMonth - 1, 12, out var candidateMonthOffset);
            var candidateYear = year + candidateYearOffset;
            var candidateMonth = candidateMonthOffset + 1;

            DateTime candidate;
            if (Ordinal >= OrdinalDayOccurrence.First)
            {
                // Find the first day of the month
                var firstOfMonth = new DateTime(candidateYear, candidateMonth, 1)
                    .Add(time.ToTimeSpan());

                // Find the first occurrence of the desired day
                var daysOffset = ((int)DayOfWeek - (int)firstOfMonth.DayOfWeek + 7) % 7;
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
                var lastOfMonth = new DateTime(candidateYear, candidateMonth, daysInMonth)
                    .Add(time.ToTimeSpan());

                // Find the last occurrence of the desired day
                var daysOffset = (int)lastOfMonth.DayOfWeek - (int)DayOfWeek + 7;
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
