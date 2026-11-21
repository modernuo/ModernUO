using System;

namespace Server.Engines.Events;

public abstract class ScheduledEvent : BaseScheduledEvent
{
    private readonly DateTime _endOn;

    public IRecurrencePattern Recurrence { get; }
    public TimeOnly Time { get; }

    /// <summary>The UTC time the series ends (exclusive). An unspecified-kind end is read in the event's time zone.</summary>
    public DateTime EndDate => _endOn == DateTime.MaxValue ? _endOn : CalendarTime.ToUtc(_endOn, TimeZone);

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
        _endOn = endOn;
    }

    // Without a recurrence the series is the single run at Start.
    protected override DateTime GetNextOccurrence(DateTime after)
    {
        var next = Recurrence?.GetNextOccurrence(after, Start, Time, TimeZone) ?? (Start > after ? Start : DateTime.MaxValue);
        return next >= EndDate ? DateTime.MaxValue : next;
    }
}
