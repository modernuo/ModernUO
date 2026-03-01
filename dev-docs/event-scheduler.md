# ModernUO EventScheduler System

This document covers ModernUO's wall-clock and calendar-based event scheduling system. For game-tick timers (sub-second precision, combat ticks, decay, etc.), see `dev-docs/timers.md`.

## Overview

The `EventScheduler` provides **wall-clock scheduling** — firing events at real-world times, dates, and calendar patterns. It runs as a 1-second `Timer` on the game loop and uses a `PriorityQueue` ordered by next UTC occurrence.

### Timer vs EventScheduler

| Aspect | `Timer.StartTimer` | `EventScheduler` |
|---|---|---|
| Clock basis | Game-tick (8ms wheel) | Wall-clock (1s poll) |
| Precision | 8ms | 1 second |
| Use case | Combat ticks, decay, delays | Holidays, daily resets, scheduled maintenance |
| Recurrence | Fixed interval | Calendar patterns (hourly, daily, weekly, monthly, yearly) |
| Timezone | N/A | Full `TimeZoneInfo` + DST handling |
| Seasonal windows | No | Yes (`YearlyScheduledEvent`) |

**Rule of thumb**: If the event must happen "at 9:00 AM EST every Monday" or "during October through November each year," use `EventScheduler`. If it must happen "5 seconds from now" or "every 2 seconds," use `Timer`.

## Architecture

`EventScheduler` is a singleton `Timer` that ticks every second. Internally it holds a `PriorityQueue<BaseScheduledEvent, DateTime>` sorted by `NextOccurrence` (UTC). Each tick it dequeues and fires all events whose time has passed, then each event self-re-enqueues for its next occurrence.

```
EventScheduler (Timer, 1s tick)
  └─ PriorityQueue<BaseScheduledEvent, DateTime>
       ├─ CallbackScheduledEvent (fires Action)
       ├─ YearlyCallbackScheduledEvent (fires Action within seasonal window)
       └─ Your custom subclass
```

## Class Hierarchy

```
BaseScheduledEvent              (abstract: Schedule, Cancel, Advance, OnEvent)
  └─ ScheduledEvent             (adds IRecurrencePattern, TimeOnly, EndDate)
       ├─ CallbackScheduledEvent      (sealed: fires an Action callback)
       └─ YearlyScheduledEvent        (abstract: adds MonthDay start/end seasonal window)
            └─ YearlyCallbackScheduledEvent  (fires an Action within seasonal window)
```

### BaseScheduledEvent

The abstract root. Key members:

| Member | Description |
|---|---|
| `TimeZone` | `TimeZoneInfo` — defaults to UTC |
| `NextOccurrence` | `DateTime` (UTC) of the next fire time |
| `Cancelled` | `bool` — set by `Cancel()` |
| `Scheduler` | Back-reference to the owning `EventScheduler` |
| `Schedule(startAfter, timeZone?)` | Schedule on the shared instance |
| `Schedule(scheduler, startAfter, timeZone?)` | Schedule on a specific scheduler |
| `Cancel()` | Cancel and unschedule the event |
| `Advance()` | Called by the scheduler — fires `OnEvent()`, then re-schedules |
| `OnEvent()` | Abstract — your event logic goes here |

### ScheduledEvent

Extends `BaseScheduledEvent` with recurrence support:

| Member | Description |
|---|---|
| `Recurrence` | `IRecurrencePattern` — determines when the event recurs |
| `Time` | `TimeOnly` — the time-of-day component for recurrence calculation |
| `EndDate` | `DateTime` — stop recurring after this date (default: never) |

### CallbackScheduledEvent

Sealed concrete class. Wraps an `Action` callback:

```csharp
var evt = new CallbackScheduledEvent(new TimeOnly(9, 0), myAction, EventScheduler.Daily);
evt.Schedule(DateTime.UtcNow);
```

Most users should use the static convenience methods on `EventScheduler` instead of constructing directly.

### YearlyScheduledEvent

Abstract. Adds a seasonal window defined by `MonthDay` start and end:

| Member | Description |
|---|---|
| `YearlyStart` | `MonthDay` — first day of the active window |
| `YearlyEnd` | `MonthDay` — last day of the active window |

The event only fires when the next occurrence falls within the `[YearlyStart, YearlyEnd]` range. If it falls outside, the scheduler fast-forwards to the next year's window start. Supports year-boundary wrapping (e.g., Nov 15 through Feb 15).

### YearlyCallbackScheduledEvent

Concrete version of `YearlyScheduledEvent` that fires an `Action`:

```csharp
var halloween = new YearlyCallbackScheduledEvent(
    new TimeOnly(0, 0),
    new MonthDay(2025, 10, 1),   // Oct 1
    new MonthDay(2025, 11, 1),   // Nov 1
    SpawnHalloweenContent,
    EventScheduler.Daily
);
halloween.Schedule(DateTime.UtcNow, easternTimeZone);
```

## Recurrence Patterns

All patterns implement `IRecurrencePattern`:

```csharp
public interface IRecurrencePattern
{
    DateTime GetNextOccurrence(DateTime afterUtc, TimeOnly time, TimeZoneInfo timeZone);
}
```

### Built-In Patterns

| Pattern | Static accessor | Behavior |
|---|---|---|
| `HourlyRecurrencePattern` | `EventScheduler.Hourly` | Every N hours (default 1) at the same minute |
| `DailyRecurrencePattern` | `EventScheduler.Daily` | Every N days (default 1) at the specified time |
| `WeeklyRecurrencePattern` | `EventScheduler.Weekly` | Every N weeks (default 1), with optional `AllowedDays` and `AllowedMonths` filters |
| `WeeklyRecurrencePattern(2)` | `EventScheduler.Biweekly` | Every 2 weeks |
| `MonthlyRecurrencePattern` | `EventScheduler.Monthly` | Every N months (default 1) on a specific day-of-month |
| `MonthlyRecurrencePattern(-1, 12)` | `EventScheduler.Yearly` | Every 12 months (yearly) |
| `MonthlyOrdinalRecurrencePattern` | (construct directly) | E.g., "second Tuesday of every month" or "last Friday" |

### MonthlyOrdinalRecurrencePattern

For patterns like "the third Wednesday of every month":

```csharp
// Second Tuesday of every month
var pattern = new MonthlyOrdinalRecurrencePattern(
    OrdinalDayOccurrence.Second,
    DayOfWeek.Tuesday
);

// Last Friday of every month
var pattern = new MonthlyOrdinalRecurrencePattern(
    OrdinalDayOccurrence.Last,
    DayOfWeek.Friday
);
```

The `OrdinalDayOccurrence` enum:

| Value | Meaning |
|---|---|
| `Last` (-1) | Last occurrence in the month |
| `First` (0) | First occurrence |
| `Second` (1) | Second occurrence |
| `Third` (2) | Third occurrence |
| `Fourth` (3) | Fourth occurrence |
| `Fifth` (4) | Fifth occurrence (skipped if doesn't exist) |

### WeeklyRecurrencePattern with Filters

```csharp
// Every week on Monday and Wednesday, only in January through March
var pattern = new WeeklyRecurrencePattern(
    intervalWeeks: 1,
    allowedMonths: AllowedMonths.January | AllowedMonths.February | AllowedMonths.March,
    allowedDays: AllowedDays.Monday | AllowedDays.Wednesday
);
```

## Supporting Types

### AllowedDays (Flags Enum)

```csharp
[Flags]
public enum AllowedDays : byte
{
    None, Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, All
}
```

Extension: `DayOfWeek.Monday.ToDaysOfWeek()` → `AllowedDays.Monday`

### AllowedMonths (Flags Enum)

```csharp
[Flags]
public enum AllowedMonths
{
    None, January, February, ..., December, All
}
```

### MonthDay (Record Struct)

Represents a month/day pair without a year. Used for seasonal window boundaries:

```csharp
var oct1 = new MonthDay(2025, 10, 1);   // Year is used only for day-count validation
var nov1 = new MonthDay(2025, 11, 1);
```

Extension method `DateTime.IsBetween(MonthDay start, MonthDay end)` handles year-boundary wrapping:
- `IsBetween(Oct 1, Nov 1)` — standard range within one year
- `IsBetween(Nov 15, Feb 15)` — wraps across year boundary

## Static Convenience Methods

`EventScheduler` provides static methods that create, schedule, and return a `ScheduledEvent`:

```csharp
// All methods accept: DateTime startOn, Action action, TimeZoneInfo timeZone = null

EventScheduler.HourlyAt(startOn, action, timeZone);
EventScheduler.DailyAt(startOn, action, timeZone);
EventScheduler.WeeklyAt(startOn, action, timeZone);
EventScheduler.BiweeklyAt(startOn, action, timeZone);
EventScheduler.MonthlyAt(startOn, action, timeZone);
EventScheduler.YearlyAt(startOn, action, timeZone);
```

The `startOn` parameter determines:
1. The `TimeOnly` component (hour/minute for recurrence)
2. The starting reference date
3. For `MonthlyAt`/`YearlyAt`, the day-of-month

Example:
```csharp
// Fire at 6:00 AM Eastern every day, starting tomorrow
var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
var tomorrow6am = new DateTime(2025, 6, 15, 6, 0, 0);
EventScheduler.DailyAt(tomorrow6am, ResetDailyQuests, eastern);
```

## Timezone & DST Handling

The system fully supports timezones via `TimeZoneInfo`:

- **Default**: UTC if no timezone specified
- **DST-safe**: Uses `LocalToUtc()` extension which handles:
  - **Ambiguous times** (fall-back): Uses the later offset (standard time)
  - **Invalid times** (spring-forward): Recurrence patterns skip invalid candidates
- **Conversion**: All internal scheduling uses UTC; local time is only for calculating the next occurrence

```csharp
// Extension method in Server namespace (Utility.cs)
public static DateTime LocalToUtc(this DateTime local, TimeZoneInfo tz)
```

**Always specify a timezone** when your event needs to fire at a local time. Omitting it defaults to UTC.

## Custom Event Classes

For complex logic, inherit from `ScheduledEvent` or `YearlyScheduledEvent`:

```csharp
public class WeekendBonusEvent : ScheduledEvent
{
    public WeekendBonusEvent()
        : base(
            new TimeOnly(18, 0),    // 6:00 PM
            new WeeklyRecurrencePattern(
                intervalWeeks: 1,
                allowedDays: AllowedDays.Friday | AllowedDays.Saturday
            ))
    {
    }

    public override void OnEvent()
    {
        // Enable weekend bonus XP
        BonusSystem.ActivateWeekendBonus();
    }
}

// Usage:
var evt = new WeekendBonusEvent();
evt.Schedule(DateTime.UtcNow, easternTimeZone);
```

### Yearly Seasonal Custom Event

```csharp
public class HalloweenSpawnEvent : YearlyScheduledEvent
{
    protected HalloweenSpawnEvent()
        : base(
            new TimeOnly(0, 0),
            new MonthDay(2025, 10, 15),   // Oct 15
            new MonthDay(2025, 11, 1),    // Nov 1
            EventScheduler.Daily)
    {
    }

    public override void OnEvent()
    {
        // Spawn Halloween creatures daily during the window
        HalloweenSystem.SpawnCreatures();
    }
}
```

## Cancellation

Call `Cancel()` on any `BaseScheduledEvent` to remove it from the scheduler:

```csharp
private BaseScheduledEvent _dailyReset;

public void StartDailyResets()
{
    var tomorrow = new DateTime(2025, 6, 15, 0, 0, 0);
    _dailyReset = EventScheduler.DailyAt(tomorrow, ResetDaily, easternTz);
}

public void StopDailyResets()
{
    _dailyReset?.Cancel();
    _dailyReset = null;
}
```

## Common Mistakes

| Mistake | Problem | Fix |
|---|---|---|
| Using `Timer` for calendar events | Drifts with server restarts, no timezone support | Use `EventScheduler` |
| Forgetting timezone | Event fires at UTC instead of local time | Always pass `TimeZoneInfo` for local-time events |
| Not cancelling on cleanup | Event keeps firing after system disabled | Call `Cancel()` in cleanup/shutdown |
| Using `EventScheduler` for sub-second timing | 1-second granularity too coarse | Use `Timer.StartTimer` instead |
| Constructing `MonthDay` with invalid days | Throws `ArgumentOutOfRangeException` | Check `DateTime.DaysInMonth` for the given month |

## Key File References

| File | Description |
|---|---|
| `Projects/UOContent/Engines/Events/EventScheduler.cs` | Singleton scheduler, PriorityQueue, static factory methods |
| `Projects/UOContent/Engines/Events/BaseScheduledEvent.cs` | Abstract base: Schedule, Cancel, Advance, OnEvent |
| `Projects/UOContent/Engines/Events/ScheduledEvent.cs` | Adds IRecurrencePattern, TimeOnly, EndDate |
| `Projects/UOContent/Engines/Events/CallbackScheduledEvent.cs` | Sealed Action-callback concrete class |
| `Projects/UOContent/Engines/Events/YearlyScheduledEvent.cs` | Seasonal window with MonthDay start/end |
| `Projects/UOContent/Engines/Events/YearlyCallbackScheduledEvent.cs` | Yearly seasonal + Action callback |
| `Projects/UOContent/Engines/Events/CommonRecurrencePatterns.cs` | All IRecurrencePattern implementations |
| `Projects/UOContent/Engines/Events/AllowedDays.cs` | Flags enum for day-of-week filtering |
| `Projects/UOContent/Engines/Events/AllowedMonths.cs` | Flags enum for month filtering |
| `Projects/UOContent/Engines/Events/MonthDay.cs` | Record struct + IsBetween extension |
| `Projects/Server/Utilities/Utility.cs` | `LocalToUtc()` extension method |
