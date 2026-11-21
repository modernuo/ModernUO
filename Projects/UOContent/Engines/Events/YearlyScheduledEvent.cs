using System;

namespace Server.Engines.Events;

/// <summary>
/// A recurring event that only runs between <see cref="YearlyStart"/> and <see cref="YearlyEnd"/> (inclusive,
/// local dates) each year. A window may wrap the new year, e.g. Nov 15 - Feb 15.
/// </summary>
public abstract class YearlyScheduledEvent : ScheduledEvent
{
    // A run must land in a window within this many years, e.g. a Feb 29 run in a February window.
    private const int MaxWindowSearches = 32;

    public MonthDay YearlyStart { get; }

    public MonthDay YearlyEnd { get; }

    protected YearlyScheduledEvent(
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        IRecurrencePattern recurrence
    ) : this(time, yearlyStart, yearlyEnd, DateTime.MaxValue, recurrence)
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

        // Each pass either lands in a window or jumps to a later window's start.
        for (var i = 0; i < MaxWindowSearches && next != DateTime.MaxValue; i++)
        {
            var local = CalendarTime.ToLocal(next, TimeZone);

            if (local.IsBetween(YearlyStart, YearlyEnd))
            {
                return next;
            }

            // Outside a wrapping window means between its end and this year's start; outside a non-wrapping
            // window means before this year's start or after its end.
            var year = YearlyStart > YearlyEnd || new MonthDay(local.Year, local.Month, local.Day) < YearlyStart
                ? local.Year
                : local.Year + 1;

            if (year > 9998)
            {
                break;
            }

            // A Feb 29 start rolls to Mar 1 in a common year.
            var windowStart = new DateTime(year, YearlyStart.Month, 1).AddDays(YearlyStart.Day - 1);
            next = base.GetNextOccurrence(CalendarTime.At(windowStart, TimeOnly.MinValue, TimeZone).AddTicks(-1));
        }

        return DateTime.MaxValue;
    }
}
