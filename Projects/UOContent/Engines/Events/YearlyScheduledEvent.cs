using System;

namespace Server.Engines.Events;

public abstract class YearlyScheduledEvent : ScheduledEvent
{
    public MonthDay YearlyStart { get; }

    public MonthDay YearlyEnd { get; }

    protected YearlyScheduledEvent(
        DateTime startAfter,
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        IRecurrencePattern recurrence,
        TimeZoneInfo timeZone = null
    ) : this(startAfter, DateTime.MaxValue, time, yearlyStart, yearlyEnd, recurrence, timeZone)
    {
    }

    protected YearlyScheduledEvent(
        DateTime startAfter,
        DateTime endOn,
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        IRecurrencePattern recurrence,
        TimeZoneInfo timeZone = null
    ) : base(startAfter, endOn, time, recurrence, timeZone)
    {
        YearlyStart = yearlyStart;
        YearlyEnd = yearlyEnd;
    }

    protected override DateTime GetOccurrence(DateTime after)
    {
        var next = base.GetOccurrence(after);

        if (next == DateTime.MaxValue)
        {
            return DateTime.MaxValue;
        }

        var localNext = TimeZoneInfo.ConvertTimeFromUtc(next, TimeZone);

        if (localNext.IsBetween(YearlyStart, YearlyEnd))
        {
            return next;
        }

        int yearToUse;

        // If we're after the end of this year's range but before the start of next year's range
        if (YearlyStart > YearlyEnd && localNext.Month > YearlyEnd.Month)
        {
            // We're in the same calendar year, targeting this year's start
            yearToUse = localNext.Year;
        }
        else
        {
            // Either we're in a non-spanning range, or we're in the early part of next year
            // In either case, we need to advance to the next year's start
            yearToUse = localNext.Year + 1;
        }

        var nextYearStartUtc = new DateTime(yearToUse, YearlyStart.Month, YearlyStart.Day).LocalToUtc(TimeZone);

        return Recurrence!.GetNextOccurrence(nextYearStartUtc, Time, TimeZone);
    }
}
