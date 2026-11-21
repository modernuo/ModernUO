using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Collections;

namespace Server.Engines.Events;

public interface IRecurrencePattern
{
    /// <summary>
    /// Gets the first run strictly after <paramref name="afterUtc"/> and at or after <paramref name="startUtc"/>,
    /// on the cadence counted from <paramref name="startUtc"/>.
    /// </summary>
    /// <returns>The run in UTC, or <see cref="DateTime.MaxValue"/> when there is none.</returns>
    DateTime GetNextOccurrence(DateTime afterUtc, DateTime startUtc, TimeOnly time, TimeZoneInfo timeZone);
}

public enum OrdinalDayOccurrence { Last = -1, First, Second, Third, Fourth, Fifth }

public class EventScheduler : Timer
{
    private readonly PriorityQueue<BaseScheduledEvent, DateTime> _schedule = new();

    public static EventScheduler Shared { get; private set; }

    public static IRecurrencePattern Hourly { get; } = new HourlyRecurrencePattern();

    public static IRecurrencePattern Daily { get; } = new DailyRecurrencePattern();

    // Recur every week on the weekday of the series start
    public static IRecurrencePattern Weekly { get; } = new WeeklyRecurrencePattern();

    // Recur every two weeks on the weekday of the series start
    public static IRecurrencePattern Biweekly { get; } = new WeeklyRecurrencePattern(2);

    // Recur every month on the day of the series start
    public static IRecurrencePattern Monthly { get; } = new MonthlyRecurrencePattern();

    // Recur every year on the month and day of the series start
    public static IRecurrencePattern Yearly { get; } = new MonthlyRecurrencePattern(-1, 12);

    // One shared pattern per day of the month
    private static readonly Dictionary<int, MonthlyRecurrencePattern> _monthlyRecurrenceByDay = [];

    public static IRecurrencePattern GetMonthlyRecurrence(int dayOfMonth)
    {
        ref var pattern = ref CollectionsMarshal.GetValueRefOrAddDefault(_monthlyRecurrenceByDay, dayOfMonth, out var exists);
        if (!exists)
        {
            pattern = new MonthlyRecurrencePattern(dayOfMonth);
        }

        return pattern;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent HourlyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Shared.ScheduleEvent(startOn, action, Hourly, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent DailyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Shared.ScheduleEvent(startOn, action, Daily, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent WeeklyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Shared.ScheduleEvent(startOn, action, Weekly, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent BiweeklyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Shared.ScheduleEvent(startOn, action, Biweekly, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent MonthlyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Shared.ScheduleEvent(startOn, action, Monthly, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent YearlyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Shared.ScheduleEvent(startOn, action, Yearly, timeZone);

    public static void Configure()
    {
        Shared ??= new EventScheduler();
        Shared.Start();
    }

    private EventScheduler() : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
    {
    }

    /// <summary>
    /// Schedules <paramref name="callback"/> at <paramref name="startOn"/>, recurring at its local time of day.
    /// </summary>
    public ScheduledEvent ScheduleEvent(
        DateTime startOn,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    )
    {
        timeZone ??= TimeZoneInfo.Utc;
        var localStart = CalendarTime.ToLocal(CalendarTime.ToUtc(startOn, timeZone), timeZone);
        return ScheduleEvent(startOn, TimeOnly.FromDateTime(localStart), callback, recurrencePattern, timeZone);
    }

    public ScheduledEvent ScheduleEvent(
        DateTime startOn,
        TimeOnly time,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    )
    {
        var scheduledEvent = new CallbackScheduledEvent(time, callback, recurrencePattern);
        scheduledEvent.Schedule(this, startOn, timeZone);
        return scheduledEvent;
    }

    public void ScheduleEvent(BaseScheduledEvent entry)
    {
        if (entry != null && entry.NextOccurrence < DateTime.MaxValue)
        {
            _schedule.Enqueue(entry, entry.NextOccurrence);
        }
    }

    public void UnscheduleEvent(BaseScheduledEvent entry)
    {
        if (entry != null)
        {
            _schedule.Remove(entry, out _, out _);
        }
    }

    protected override void OnTick()
    {
        var now = Core.Now;

        if (!_schedule.TryPeek(out _, out var first) || first > now)
        {
            return;
        }

        // Only runs due when the tick began fire, so a callback that schedules a run for now waits a tick
        // instead of spinning this loop.
        using var due = PooledRefQueue<(BaseScheduledEvent Entry, int Version)>.Create();
        while (_schedule.TryPeek(out var entry, out var at) && at <= now)
        {
            _schedule.Dequeue();
            due.Enqueue((entry, entry.Version));
        }

        while (due.TryDequeue(out var item))
        {
            // An earlier callback in this tick may have cancelled or rescheduled this event.
            if (!item.Entry.Cancelled && item.Entry.Version == item.Version)
            {
                item.Entry.Advance();
            }
        }
    }
}
