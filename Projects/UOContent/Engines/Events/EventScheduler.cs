using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Engines.Events;

public interface IRecurrencePattern
{
    /// <summary>
    /// Get the next occurrence of the event.
    /// <returns><c>DateTime</c> of the next occurence in UTC or DateTime.MaxValue</returns>
    /// </summary>
    DateTime GetNextOccurrence(DateTime afterUtc, TimeOnly time, TimeZoneInfo timeZone);
}

public enum OrdinalDayOccurrence { Last = -1, First, Second, Third, Fourth, Fifth }

public class EventScheduler : Timer
{
    private readonly PriorityQueue<BaseScheduledEvent, DateTime> _schedule = new();

    public static EventScheduler Shared { get; private set; }

    public static IRecurrencePattern Hourly => new HourlyRecurrencePattern();

    public static IRecurrencePattern Daily => new DailyRecurrencePattern();

    // Recur every week, on the same day/time as the first occurence
    public static IRecurrencePattern Weekly => new WeeklyRecurrencePattern();

    // Recur every two weeks, on the same day/time as the first occurence
    public static IRecurrencePattern Biweekly => new WeeklyRecurrencePattern(2);

    public static IRecurrencePattern Monthly => new MonthlyRecurrencePattern();

    public static IRecurrencePattern Yearly => new MonthlyRecurrencePattern(-1, 12);

    // For each of the days of the week
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
        Shared.ScheduleEvent(startOn, action, GetMonthlyRecurrence(startOn.Day), timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent YearlyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Shared.ScheduleEvent(startOn, action, GetMonthlyRecurrence(startOn.Day), timeZone);

    public static void Configure()
    {
        Shared ??= new EventScheduler();
        Shared.Start();
    }

    private EventScheduler() : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
    {
    }

    public ScheduledEvent ScheduleEvent(
        DateTime startOn,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    ) => ScheduleEvent(startOn, TimeOnly.FromDateTime(startOn), callback, recurrencePattern, timeZone);

    public ScheduledEvent ScheduleEvent(
        DateTime after,
        TimeOnly time,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    )
    {
        var scheduledEvent = new CallbackScheduledEvent(time, callback, recurrencePattern);
        scheduledEvent.Schedule(this, after, timeZone);
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

        while (_schedule.Count > 0)
        {
            var entry = _schedule.Peek();
            var cancelled = entry.Cancelled;
            if (!cancelled && entry.NextOccurrence > now)
            {
                break;
            }

            _schedule.Dequeue();

            if (cancelled)
            {
                continue;
            }

            entry.Advance(); // Advances the event and self queues if necessary
        }
    }
}
