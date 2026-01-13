using System;
using System.Runtime.CompilerServices;
using Server.Logging;

namespace Server.Engines.Events;

public abstract class BaseScheduledEvent
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BaseScheduledEvent));

    public TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Utc;
    public DateTime NextOccurrence { get; private set; } = DateTime.MaxValue;
    public bool Cancelled { get; private set; }
    public EventScheduler Scheduler { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Schedule(DateTime startAfter, TimeZoneInfo timeZone = null) =>
        Schedule(EventScheduler.Shared, startAfter, timeZone);

    public void Schedule(EventScheduler scheduler, DateTime startAfter, TimeZoneInfo timeZone = null)
    {
        Cancel();
        Scheduler = scheduler;
        TimeZone = timeZone ?? TimeZoneInfo.Utc;
        Schedule(startAfter, timeZone, true);
    }

    private void Schedule(DateTime startAfter, TimeZoneInfo timeZone, bool isFirst)
    {
        Cancelled = false;
        var afterUtc = startAfter.Kind == DateTimeKind.Utc ? startAfter : startAfter.LocalToUtc(timeZone);

        var next = GetNextOccurrence(afterUtc);

        // For the first occurrence, we should set it to the startAfter date if we have no recurrence.
        NextOccurrence = next == DateTime.MaxValue && isFirst ? afterUtc : next;

        if (NextOccurrence != DateTime.MaxValue)
        {
            Scheduler.ScheduleEvent(this);
        }
    }

    protected abstract DateTime GetNextOccurrence(DateTime after);

    public void Cancel()
    {
        Cancelled = true;
        Scheduler?.UnscheduleEvent(this);
    }

    public virtual void Advance()
    {
        try
        {
            OnEvent();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "OnEvent failed to execute.");
        }

        Schedule(NextOccurrence, TimeZone, false);
    }

    public abstract void OnEvent();
}
