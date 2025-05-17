using System;

namespace Server.Engines.Events;

public abstract class ScheduledEvent : BaseScheduledEvent
{
    public IRecurrencePattern Recurrence { get; }
    public TimeOnly Time { get; }
    public DateTime EndDate { get; }

    public ScheduledEvent(TimeOnly time, IRecurrencePattern recurrence = null)
        : this(time, DateTime.MaxValue, recurrence)
    {
    }

    public ScheduledEvent(
        TimeOnly time,
        DateTime endOn,
        IRecurrencePattern recurrence = null
    )
    {
        Time = time;
        Recurrence = recurrence;
        EndDate = endOn == DateTime.MaxValue || endOn.Kind == DateTimeKind.Utc ? endOn : endOn.LocalToUtc(TimeZone);
    }

    protected override DateTime GetNextOccurrence(DateTime after)
    {
        var next = Recurrence?.GetNextOccurrence(after, Time, TimeZone) ?? DateTime.MaxValue;
        return next >= EndDate ? DateTime.MaxValue : next;
    }
}
