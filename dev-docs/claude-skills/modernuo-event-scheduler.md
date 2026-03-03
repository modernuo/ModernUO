---
name: modernuo-event-scheduler
description: >
  Trigger when creating holiday events, seasonal content, scheduled maintenance, daily/weekly resets, or any wall-clock/calendar-based scheduling. When using EventScheduler, ScheduledEvent, YearlyScheduledEvent, or IRecurrencePattern.
---

# ModernUO EventScheduler (Wall-Clock / Calendar Scheduling)

## When This Activates
- Creating holiday or seasonal events (Halloween, Christmas, etc.)
- Scheduling daily/weekly/monthly resets or activities
- Any event that must fire at a real-world time or date
- Working with `EventScheduler`, `ScheduledEvent`, `YearlyScheduledEvent`, `CallbackScheduledEvent`
- Working with `IRecurrencePattern`, `AllowedDays`, `AllowedMonths`, `MonthDay`

## Key Rules

1. **EventScheduler for wall-clock/calendar, Timer for game-tick delays** — if the event is "at 9 AM every Monday," use EventScheduler; if it's "5 seconds from now," use Timer
2. **Always specify timezone** for local-time events — omitting defaults to UTC
3. **Prefer `CallbackScheduledEvent` (via static methods)** for simple recurring actions
4. **Use `YearlyScheduledEvent`** for seasonal windows (e.g., Oct 15 - Nov 1 each year)
5. **Cancel events on cleanup** — call `Cancel()` when disabling or shutting down
6. **1-second granularity** — EventScheduler ticks every second, not suitable for sub-second precision

## Timer vs EventScheduler Decision

| Need | Use |
|---|---|
| "Every 5 seconds" | `Timer.StartTimer` |
| "At 6:00 AM daily" | `EventScheduler.DailyAt` |
| "Delete after 10 seconds" | `Timer.StartTimer` |
| "Every Monday at noon" | `EventScheduler.WeeklyAt` |
| "Combat tick every 250ms" | `Timer.StartTimer` |
| "Oct 15 - Nov 1 each year" | `YearlyScheduledEvent` |
| "First Tuesday of each month" | `MonthlyOrdinalRecurrencePattern` |

## Quick Patterns

### Daily Event at a Specific Time
```csharp
var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
var startOn = new DateTime(2025, 1, 1, 6, 0, 0); // 6:00 AM
EventScheduler.DailyAt(startOn, ResetDailyQuests, eastern);
```

### Weekly Event
```csharp
var startOn = new DateTime(2025, 1, 6, 18, 0, 0); // Monday 6:00 PM
EventScheduler.WeeklyAt(startOn, StartWeeklyTournament, eastern);
```

### Monthly Event on a Specific Day
```csharp
var startOn = new DateTime(2025, 1, 15, 12, 0, 0); // 15th at noon
EventScheduler.MonthlyAt(startOn, MonthlyRewards, eastern);
```

### Yearly Seasonal Event (with Window)
```csharp
var halloween = new YearlyCallbackScheduledEvent(
    new TimeOnly(0, 0),
    new MonthDay(2025, 10, 15),   // Start: Oct 15
    new MonthDay(2025, 11, 1),    // End: Nov 1
    SpawnHalloweenContent,
    EventScheduler.Daily
);
halloween.Schedule(DateTime.UtcNow, eastern);
```

### Filtered Weekly (Specific Days/Months)
```csharp
var pattern = new WeeklyRecurrencePattern(
    intervalWeeks: 1,
    allowedMonths: AllowedMonths.June | AllowedMonths.July | AllowedMonths.August,
    allowedDays: AllowedDays.Friday | AllowedDays.Saturday
);
var evt = new CallbackScheduledEvent(new TimeOnly(20, 0), SummerWeekendEvent, pattern);
evt.Schedule(DateTime.UtcNow, eastern);
```

### Ordinal Monthly (e.g., "Second Tuesday")
```csharp
var pattern = new MonthlyOrdinalRecurrencePattern(
    OrdinalDayOccurrence.Second,
    DayOfWeek.Tuesday
);
var evt = new CallbackScheduledEvent(new TimeOnly(12, 0), MonthlyMeeting, pattern);
evt.Schedule(DateTime.UtcNow, eastern);
```

## Custom Event Class Template

```csharp
using System;
using Server.Engines.Events;

public class MyScheduledEvent : ScheduledEvent
{
    public MyScheduledEvent(TimeOnly time, IRecurrencePattern recurrence)
        : base(time, recurrence)
    {
    }

    public override void OnEvent()
    {
        // Your event logic here
    }
}

// Schedule it:
var evt = new MyScheduledEvent(new TimeOnly(9, 0), EventScheduler.Daily);
evt.Schedule(DateTime.UtcNow, timeZone);

// Cancel it:
evt.Cancel();
```

### Custom Yearly Seasonal Event Template

```csharp
public class MySeasonalEvent : YearlyScheduledEvent
{
    protected MySeasonalEvent(
        TimeOnly time,
        MonthDay yearlyStart,
        MonthDay yearlyEnd,
        IRecurrencePattern recurrence
    ) : base(time, yearlyStart, yearlyEnd, recurrence)
    {
    }

    public override void OnEvent()
    {
        // Only fires when date is within [yearlyStart, yearlyEnd]
    }
}
```

## Anti-Patterns

- **Using `Timer.StartTimer` for calendar events**: Timers drift across restarts and have no timezone support — use EventScheduler
- **Forgetting to specify timezone**: Event fires at UTC instead of expected local time — always pass `TimeZoneInfo`
- **Not cancelling events on cleanup**: Scheduled events keep firing after the system is disabled — call `Cancel()`
- **Using EventScheduler for sub-second timing**: 1-second granularity is too coarse — use `Timer.StartTimer`
- **Constructing `MonthDay` with invalid day**: Throws `ArgumentOutOfRangeException` — validate against `DateTime.DaysInMonth`

## See Also
- `dev-docs/event-scheduler.md` — Complete EventScheduler documentation
- `dev-docs/timers.md` — Game-tick timer system (Timer.StartTimer, TimerExecutionToken)
- `dev-docs/claude-skills/modernuo-timers.md` — Timer skill for game-tick delays
