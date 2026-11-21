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

        // A spanning range resumes later this year; a non-spanning one only if its start is still ahead.
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
