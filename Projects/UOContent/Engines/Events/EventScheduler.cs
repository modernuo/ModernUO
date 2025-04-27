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
    DateTime GetNextOccurrence(DateTime afterUtc, TimeZoneInfo timeZone);
}

public enum OrdinalDayOccurrence { Last = -1, First, Second, Third, Fourth, Fifth }

[Flags]
public enum DaysOfWeek : byte
{
    None = 0,
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,
    EveryDay = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

public abstract class ScheduledEvent
{
    private static Serial _nextSerial = (Serial)1;

    // Tie breaker for sorted set
    public Serial Serial { get; }
    public IRecurrencePattern Recurrence { get; }
    public TimeZoneInfo TimeZone { get; }
    public DateTime EndDate { get; }
    public DateTime NextOccurrence { get; private set; }

    public ScheduledEvent(DateTime startOn, TimeZoneInfo timeZone = null)
        : this(startOn, startOn, null, timeZone)
    {
    }

    public ScheduledEvent(DateTime afterUtc, IRecurrencePattern recurrence, TimeZoneInfo timeZone = null)
        : this(afterUtc, DateTime.MaxValue, recurrence, timeZone)
    {
    }

    public ScheduledEvent(
        DateTime afterUtc,
        DateTime endDateUtc,
        IRecurrencePattern recurrence,
        TimeZoneInfo timeZone = null
    )
    {
        Serial = _nextSerial++;
        Recurrence = recurrence;
        TimeZone = timeZone ?? TimeZoneInfo.Utc;
        NextOccurrence = recurrence?.GetNextOccurrence(afterUtc, TimeZone) ?? afterUtc;
        EndDate = endDateUtc;
    }

    public bool Advance()
    {
        OnEvent();

        var next = Recurrence?.GetNextOccurrence(NextOccurrence, TimeZone) ?? DateTime.MaxValue;
        if (next == DateTime.MaxValue || next > EndDate)
        {
            return false;
        }

        NextOccurrence = next;
        return true;
    }

    public abstract void OnEvent();
}

public class EventScheduler : Timer
{
    private readonly SortedSet<ScheduledEvent> _schedule = new(ScheduledEventComparer.Default);

    public static EventScheduler Instance { get; private set; }

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
        Instance.ScheduleEvent(startOn, action, Hourly, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent DailyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Instance.ScheduleEvent(startOn, action, Daily, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent WeeklyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Instance.ScheduleEvent(startOn, action, Weekly, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent BiweeklyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Instance.ScheduleEvent(startOn, action, Biweekly, timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent MonthlyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Instance.ScheduleEvent(startOn, action, GetMonthlyRecurrence(startOn.Day), timeZone);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledEvent YearlyAt(DateTime startOn, Action action, TimeZoneInfo timeZone = null) =>
        Instance.ScheduleEvent(startOn, action, GetMonthlyRecurrence(startOn.Day), timeZone);

    public static void Configure()
    {
        Instance ??= new EventScheduler();
        Instance.Start();
    }

    private EventScheduler() : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
    {
    }

    public ScheduledEvent ScheduleEvent(
        DateTime afterUtc,
        Action callback,
        IRecurrencePattern recurrencePattern = null,
        TimeZoneInfo timeZone = null
    )
    {
        var scheduledEvent = new CallbackScheduledEvent(afterUtc, callback, recurrencePattern, timeZone);
        ScheduleEvent(scheduledEvent);
        return scheduledEvent;
    }

    public void ScheduleEvent(ScheduledEvent e) => _schedule.Add(e);

    public void StopEvent(ScheduledEvent entry) => _schedule.Remove(entry);

    protected override void OnTick()
    {
        var now = Core.Now;

        while (_schedule.Count > 0)
        {
            var entry = _schedule.Min!;
            if (entry.NextOccurrence > now)
            {
                break;
            }

            _schedule.Remove(entry);

            if (entry.Advance() && entry.NextOccurrence < DateTime.MaxValue)
            {
                _schedule.Add(entry);
            }
        }
    }

    private sealed class ScheduledEventComparer : IComparer<ScheduledEvent>
    {
        public static readonly ScheduledEventComparer Default = new();

        public int Compare(ScheduledEvent x, ScheduledEvent y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            var next = x.NextOccurrence.CompareTo(y.NextOccurrence);
            return next != 0 ? next : x.Serial.CompareTo(y.Serial);
        }
    }
}
