using System;
using System.Runtime.CompilerServices;
using Server.Logging;

namespace Server.Engines.Events;

public abstract class BaseScheduledEvent
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BaseScheduledEvent));

    /// <summary>
    /// How late a run may fire. The scheduler polls every second and a world save freezes the loop for up to
    /// ~10s on large shards; saves run on the 5-minute wall-clock grid, so they land on the same boundaries as
    /// events. A run later than this was missed, and is skipped like a calendar skips it.
    /// </summary>
    public static readonly TimeSpan MissedOccurrenceGrace = TimeSpan.FromMinutes(1);

    public TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Utc;

    /// <summary>The UTC start of the series. Recurrence cadences are counted from it.</summary>
    public DateTime Start { get; private set; }

    public DateTime NextOccurrence { get; private set; } = DateTime.MaxValue;
    public bool Cancelled { get; private set; }
    public EventScheduler Scheduler { get; private set; }

    // Bumped by Schedule and Cancel so a run in flight knows it was superseded.
    internal int Version { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Schedule(DateTime startOn, TimeZoneInfo timeZone = null) =>
        Schedule(EventScheduler.Shared, startOn, timeZone);

    /// <summary>
    /// Starts the series at <paramref name="startOn"/>. A start in the past joins the series at its first run
    /// after now; runs before now are not fired.
    /// </summary>
    /// <param name="startOn">
    /// A UTC time, a machine-local time, or (<see cref="DateTimeKind.Unspecified"/>) a wall-clock time in
    /// <paramref name="timeZone"/>.
    /// </param>
    public void Schedule(EventScheduler scheduler, DateTime startOn, TimeZoneInfo timeZone = null)
    {
        Cancel();
        Cancelled = false;
        Scheduler = scheduler;
        TimeZone = timeZone ?? TimeZoneInfo.Utc;
        Start = CalendarTime.ToUtc(startOn, TimeZone);

        var now = Core.Now;
        var from = Start > now ? Start : now;
        SetNext(from.AddTicks(-1));
    }

    /// <summary>
    /// Gets the first run strictly after <paramref name="after"/> (UTC), or <see cref="DateTime.MaxValue"/>.
    /// </summary>
    protected abstract DateTime GetNextOccurrence(DateTime after);

    public void Cancel()
    {
        Cancelled = true;
        Version++;
        NextOccurrence = DateTime.MaxValue;
        Scheduler?.UnscheduleEvent(this);
    }

    /// <summary>Fires the due run unless it was missed, then queues the first run after now.</summary>
    public virtual void Advance()
    {
        var now = Core.Now;
        var version = Version;

        if (now - NextOccurrence <= MissedOccurrenceGrace)
        {
            try
            {
                OnEvent();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "OnEvent failed to execute.");
            }

            // OnEvent cancelled or rescheduled this event.
            if (version != Version)
            {
                return;
            }
        }
        else
        {
            logger.Warning(
                "Skipped {Event} run due {Due} (now {Now}).",
                GetType().Name,
                NextOccurrence,
                now
            );
        }

        SetNext(now);
    }

    private void SetNext(DateTime after)
    {
        var next = GetNextOccurrence(after);

        // A pattern that does not move forward would fire again in the same tick, forever.
        if (next <= after)
        {
            logger.Error(
                "{Event} recurrence returned {Next}, not after {After}; the event is stopped.",
                GetType().Name,
                next,
                after
            );
            next = DateTime.MaxValue;
        }

        NextOccurrence = next;

        if (next != DateTime.MaxValue)
        {
            Scheduler.ScheduleEvent(this);
        }
    }

    public abstract void OnEvent();
}
