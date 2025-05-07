using System;

namespace Server.Engines.Events;

public abstract class YearlyScheduledEvent : ScheduledEvent
{
    public MonthDay YearlyStart { get; }

    public MonthDay YearlyEnd { get; }

    public DateTime NextYearStart { get; private set; }

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

        var year = startAfter.Year;
        NextYearStart = new DateTime(
            year,
            yearlyStart.Month,
            yearlyStart.Day,
            startAfter.Hour,
            startAfter.Minute,
            startAfter.Second
        );

        if (NextYearStart < startAfter)
        {
            NextYearStart = NextYearStart.AddYears(1);
        }
    }

    public override DateTime Advance()
    {
        OnEvent();

        var next = Recurrence?.GetNextOccurrence(NextOccurrence, Time, TimeZone) ?? DateTime.MaxValue;

        if (next == DateTime.MaxValue || next > EndDate)
        {
            return DateTime.MaxValue;
        }

        if (!next.IsBetween(YearlyStart, YearlyEnd))
        {
            NextYearStart = NextYearStart.AddYears(1);
            next = Recurrence!.GetNextOccurrence(NextYearStart, Time, TimeZone);
        }

        return NextOccurrence = next;
    }
}
