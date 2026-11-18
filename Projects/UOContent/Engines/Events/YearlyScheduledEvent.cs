using System;

namespace Server.Engines.Events;

public abstract class YearlyScheduledEvent : ScheduledEvent
{
    public MonthDay YearlyStart { get; }

    public MonthDay YearlyEnd { get; }

    protected YearlyScheduledEvent(
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        IRecurrencePattern recurrence
    ) : this( time, yearlyStart, yearlyEnd, DateTime.MaxValue, recurrence)
    {
    }

    protected YearlyScheduledEvent(
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        DateTime endOn,
        IRecurrencePattern recurrence
    ) : base(time, endOn, recurrence)
    {
        YearlyStart = yearlyStart;
        YearlyEnd = yearlyEnd;
    }

    protected override DateTime GetNextOccurrence(DateTime after)
    {
        var next = base.GetNextOccurrence(after);

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

        // Outside the range, either between the end and the start of a spanning range, or before the start of
        // this year's non-spanning range: target this year's start. Otherwise we're past this year's range.
        if (YearlyStart > YearlyEnd || new MonthDay(localNext.Year, localNext.Month, localNext.Day) < YearlyStart)
        {
            yearToUse = localNext.Year;
        }
        else
        {
            yearToUse = localNext.Year + 1;
        }

        var nextYearStartUtc = new DateTime(yearToUse, YearlyStart.Month, YearlyStart.Day).LocalToUtc(TimeZone);

        return Recurrence!.GetNextOccurrence(nextYearStartUtc, Time, TimeZone);
    }
}
