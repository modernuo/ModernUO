using System;

namespace Server.Engines.Events;

public abstract class ScheduledEvent
{
    public IRecurrencePattern Recurrence { get; }
    public TimeZoneInfo TimeZone { get; }
    public TimeOnly Time { get; }
    public DateTime EndDate { get; }
    public DateTime NextOccurrence { get; protected set; }
    public bool Cancelled { get; private set; }

    public ScheduledEvent(DateTime startOn, TimeZoneInfo timeZone = null)
        : this(startOn, startOn, TimeOnly.FromDateTime(startOn), null, timeZone)
    {
    }

    public ScheduledEvent(DateTime startAfter, TimeOnly time, IRecurrencePattern recurrence, TimeZoneInfo timeZone = null)
        : this(startAfter, DateTime.MaxValue, time, recurrence, timeZone)
    {
    }

    public ScheduledEvent(
        DateTime startAfter,
        DateTime endOn,
        TimeOnly time,
        IRecurrencePattern recurrence,
        TimeZoneInfo timeZone = null
    )
    {
        Time = time;
        Recurrence = recurrence;
        TimeZone = timeZone ?? TimeZoneInfo.Utc;

        var afterUtc = startAfter.Kind == DateTimeKind.Utc ? startAfter : startAfter.LocalToUtc(TimeZone);
        NextOccurrence = recurrence?.GetNextOccurrence(afterUtc, time, TimeZone) ?? afterUtc;
        EndDate = endOn == DateTime.MaxValue || endOn.Kind == DateTimeKind.Utc ? endOn : endOn.LocalToUtc(TimeZone);
    }

    public void Cancel() => Cancelled = true;

    public virtual DateTime Advance()
    {
        OnEvent();

        var next = Recurrence?.GetNextOccurrence(NextOccurrence, Time, TimeZone) ?? DateTime.MaxValue;
        if (next == DateTime.MaxValue || next > EndDate)
        {
            return DateTime.MaxValue;
        }

        return NextOccurrence = next;
    }

    public abstract void OnEvent();
}
