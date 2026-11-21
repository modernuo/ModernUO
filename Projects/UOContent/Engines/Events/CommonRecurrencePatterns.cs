namespace Server.Engines.Events;

using System;

/// <summary>
/// Every <see cref="IntervalHours"/> elapsed hours at the minute and second of the event time, counted from the
/// first such slot at or after the series start. Elapsed hours keep the cadence through DST: the repeated
/// fall-back hour runs twice and the skipped spring-forward hour does not run.
/// </summary>
public class HourlyRecurrencePattern : IRecurrencePattern
{
    public int IntervalHours { get; }

    public HourlyRecurrencePattern(int intervalHours = 1) => IntervalHours = Math.Max(1, intervalHours);

    public DateTime GetNextOccurrence(DateTime afterUtc, DateTime startUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var startLocal = CalendarTime.ToLocal(startUtc, timeZone);
        var slotLocal = startLocal.Date.AddHours(startLocal.Hour).AddTicks(time.Ticks % TimeSpan.TicksPerHour);

        // The local slot can resolve up to an hour early when the start sits in a repeated fall-back hour.
        var first = slotLocal.LocalToUtc(timeZone);
        while (first < startUtc)
        {
            first = first.AddHours(1);
        }

        if (afterUtc < first)
        {
            return first;
        }

        var interval = TimeSpan.TicksPerHour * IntervalHours;
        var steps = (afterUtc - first).Ticks / interval + 1;

        return first.Ticks + steps * interval > DateTime.MaxValue.Ticks
            ? DateTime.MaxValue
            : first.AddTicks(steps * interval);
    }
}

/// <summary>
/// Every <see cref="IntervalDays"/> calendar days at the event's local time, counted from the series start date.
/// </summary>
public class DailyRecurrencePattern : IRecurrencePattern
{
    public int IntervalDays { get; }

    public DailyRecurrencePattern(int intervalDays = 1) => IntervalDays = Math.Max(1, intervalDays);

    public DateTime GetNextOccurrence(DateTime afterUtc, DateTime startUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var startDate = CalendarTime.ToLocal(startUtc, timeZone).Date;

        // Start a day early: a run in a spring-forward gap lands an hour later, possibly past midnight.
        var fromDate = CalendarTime.ToLocal(afterUtc, timeZone).Date.AddDays(-1);
        var days = (fromDate - startDate).Days;
        var step = days <= 0 ? 0 : days / IntervalDays;

        // Each step advances at least a day, so the third candidate is past the after instant.
        for (var i = 0; i < 4; i++, step++)
        {
            var date = startDate.AddDays((double)step * IntervalDays);
            if (date.Year > 9998)
            {
                break;
            }

            var candidate = CalendarTime.At(date, time, timeZone);
            if (candidate >= startUtc && candidate > afterUtc)
            {
                return candidate;
            }
        }

        return DateTime.MaxValue;
    }
}

/// <summary>
/// Every <see cref="IntervalWeeks"/> weeks (Sunday to Saturday, counted from the series start week) on the
/// <see cref="AllowedDays"/> that fall in <see cref="AllowedMonths"/>. <see cref="AllowedDays.None"/> means the
/// weekday of the series start.
/// </summary>
public class WeeklyRecurrencePattern : IRecurrencePattern
{
    // Long enough for any cadence that can match the month filter at all.
    private const int SearchDays = 8 * 366;

    public int IntervalWeeks { get; }
    public AllowedDays AllowedDays { get; }
    public AllowedMonths AllowedMonths { get; }

    public WeeklyRecurrencePattern(int intervalWeeks = 1, AllowedMonths allowedMonths = AllowedMonths.All, AllowedDays allowedDays = AllowedDays.None)
    {
        IntervalWeeks = Math.Max(1, intervalWeeks);
        AllowedDays = allowedDays;
        AllowedMonths = allowedMonths == AllowedMonths.None ? AllowedMonths.All : allowedMonths;
    }

    public DateTime GetNextOccurrence(DateTime afterUtc, DateTime startUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var startDate = CalendarTime.ToLocal(startUtc, timeZone).Date;
        var daysOfWeek = AllowedDays == AllowedDays.None ? startDate.DayOfWeek.ToDaysOfWeek() : AllowedDays;
        var startWeek = startDate.AddDays(-(int)startDate.DayOfWeek);

        // Start a day early: a run in a spring-forward gap lands an hour later, possibly past midnight.
        var fromDate = CalendarTime.ToLocal(afterUtc, timeZone).Date.AddDays(-1);
        var weeks = (fromDate - startWeek).Days / 7;
        var step = weeks <= 0 ? 0 : weeks / IntervalWeeks;
        var maxSteps = SearchDays / (7 * IntervalWeeks) + 2;

        for (var i = 0; i < maxSteps; i++, step++)
        {
            var weekStart = startWeek.AddDays((double)step * 7 * IntervalWeeks);
            if (weekStart.Year > 9998)
            {
                break;
            }

            var weekEnd = weekStart.AddDays(6);
            if ((AllowedMonths & (ToMonth(weekStart) | ToMonth(weekEnd))) == 0)
            {
                continue;
            }

            for (var d = 0; d < 7; d++)
            {
                var day = weekStart.AddDays(d);
                if ((AllowedMonths & ToMonth(day)) == 0 || (daysOfWeek & day.DayOfWeek.ToDaysOfWeek()) == 0)
                {
                    continue;
                }

                var candidate = CalendarTime.At(day, time, timeZone);
                if (candidate >= startUtc && candidate > afterUtc)
                {
                    return candidate;
                }
            }
        }

        return DateTime.MaxValue;
    }

    private static AllowedMonths ToMonth(DateTime date) => (AllowedMonths)(1 << (date.Month - 1));
}


/// <summary>
/// A run on one day of every <see cref="IntervalMonths"/>th month, counted from the series start month.
/// </summary>
public abstract class MonthIntervalRecurrencePattern : IRecurrencePattern
{
    // Covers the rarest monthly match: a fifth weekday of February at a yearly interval.
    private const int MaxSteps = 100;

    public int IntervalMonths { get; }

    protected MonthIntervalRecurrencePattern(int intervalMonths) => IntervalMonths = Math.Max(1, intervalMonths);

    /// <summary>The day of the month to run on, or -1 to skip the month.</summary>
    protected abstract int GetDay(int year, int month, DateTime startLocal);

    public DateTime GetNextOccurrence(DateTime afterUtc, DateTime startUtc, TimeOnly time, TimeZoneInfo timeZone)
    {
        var start = CalendarTime.ToLocal(startUtc, timeZone);
        var startIndex = start.Year * 12 + start.Month - 1;

        // Start a day early: a run in a spring-forward gap lands an hour later, possibly past midnight.
        var from = CalendarTime.ToLocal(afterUtc, timeZone).Date.AddDays(-1);
        var months = from.Year * 12 + from.Month - 1 - startIndex;
        var step = months <= 0 ? 0 : months / IntervalMonths;

        for (var i = 0; i < MaxSteps; i++, step++)
        {
            var index = startIndex + (long)step * IntervalMonths;
            var year = (int)(index / 12);
            if (year > 9998)
            {
                break;
            }

            var month = (int)(index % 12) + 1;
            var day = GetDay(year, month, start);
            if (day < 1)
            {
                continue;
            }

            var candidate = CalendarTime.At(new DateTime(year, month, day), time, timeZone);
            if (candidate >= startUtc && candidate > afterUtc)
            {
                return candidate;
            }
        }

        return DateTime.MaxValue;
    }
}

/// <summary>
/// Every <see cref="MonthIntervalRecurrencePattern.IntervalMonths"/> months on <see cref="DayOfMonth"/>, or on
/// the series start day when it is -1. Months without that day are skipped.
/// </summary>
public class MonthlyRecurrencePattern : MonthIntervalRecurrencePattern
{
    public int DayOfMonth { get; }

    public MonthlyRecurrencePattern(int dayOfMonth = -1, int intervalMonths = 1) : base(intervalMonths) =>
        DayOfMonth = dayOfMonth != -1 ? Math.Clamp(dayOfMonth, 1, 31) : -1;

    protected override int GetDay(int year, int month, DateTime startLocal)
    {
        var day = DayOfMonth == -1 ? startLocal.Day : DayOfMonth;
        return day <= DateTime.DaysInMonth(year, month) ? day : -1;
    }
}

/// <summary>
/// Every <see cref="MonthIntervalRecurrencePattern.IntervalMonths"/> months on the <see cref="Ordinal"/>
/// <see cref="DayOfWeek"/>. Months without a fifth occurrence are skipped.
/// </summary>
public class MonthlyOrdinalRecurrencePattern : MonthIntervalRecurrencePattern
{
    public OrdinalDayOccurrence Ordinal { get; }
    public DayOfWeek DayOfWeek { get; }

    public MonthlyOrdinalRecurrencePattern(OrdinalDayOccurrence ordinal, DayOfWeek dayOfWeek, int intervalMonths = 1)
        : base(intervalMonths)
    {
        Ordinal = ordinal;
        DayOfWeek = dayOfWeek;
    }

    protected override int GetDay(int year, int month, DateTime startLocal)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);

        if (Ordinal == OrdinalDayOccurrence.Last)
        {
            var lastDayOfWeek = (int)new DateTime(year, month, daysInMonth).DayOfWeek;
            return daysInMonth - (lastDayOfWeek - (int)DayOfWeek + 7) % 7;
        }

        var firstDayOfWeek = (int)new DateTime(year, month, 1).DayOfWeek;
        var day = 1 + ((int)DayOfWeek - firstDayOfWeek + 7) % 7 + 7 * (int)Ordinal;
        return day <= daysInMonth ? day : -1;
    }
}
