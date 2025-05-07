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
        EndDate = endOn == DateTime.MaxValue || endOn.Kind == DateTimeKind.Utc ? endOn : endOn.LocalToUtc(TimeZone);

        var afterUtc = startAfter.Kind == DateTimeKind.Utc ? startAfter : startAfter.LocalToUtc(TimeZone);

        var next = GetOccurrence(afterUtc);
        // For the first occurrence, we should set it to the startAfter date if we have no recurrence.
        NextOccurrence = next == DateTime.MaxValue ? afterUtc : next;
    }

    protected virtual DateTime GetOccurrence(DateTime after)
    {
        var next = Recurrence?.GetNextOccurrence(after, Time, TimeZone) ?? DateTime.MaxValue;
        return next >= EndDate ? DateTime.MaxValue : next;
    }

    public void Cancel() => Cancelled = true;

    public virtual DateTime Advance()
    {
        OnEvent();

        return NextOccurrence = GetOccurrence(NextOccurrence);
    }

    public abstract void OnEvent();
}
