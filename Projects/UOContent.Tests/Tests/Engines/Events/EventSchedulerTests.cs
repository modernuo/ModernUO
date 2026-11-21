using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Events;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class EventSchedulerTests : IDisposable
{
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    // Pins the process-wide loop clock and timer wheel; every other test in the host reads them.
    private readonly TimerWheelScope _clock = new();
    private readonly List<BaseScheduledEvent> _events = [];

    public EventSchedulerTests()
    {
        Core._now = Utc(2024, 1, 1, 12, 0);
        EventScheduler.Configure();
    }

    public void Dispose()
    {
        foreach (var evt in _events)
        {
            evt.Cancel();
        }

        EventScheduler.Shared.Stop();
        _clock.Dispose();
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private T Track<T>(T evt) where T : BaseScheduledEvent
    {
        _events.Add(evt);
        return evt;
    }

    private static void AdvanceTo(DateTime utc) => TimerWheelScope.Advance(utc - Core._now);

    private class StepPattern(TimeSpan interval) : IRecurrencePattern
    {
        public DateTime GetNextOccurrence(DateTime afterUtc, DateTime startUtc, TimeOnly time, TimeZoneInfo timeZone) =>
            afterUtc < startUtc ? startUtc : startUtc + interval * ((afterUtc - startUtc).Ticks / interval.Ticks + 1);
    }

    private class StuckPattern : IRecurrencePattern
    {
        public DateTime GetNextOccurrence(DateTime afterUtc, DateTime startUtc, TimeOnly time, TimeZoneInfo timeZone) =>
            afterUtc;
    }

    private class TestEvent(TimeOnly time, DateTime endOn, IRecurrencePattern recurrence, Action callback)
        : ScheduledEvent(time, endOn, recurrence)
    {
        public override void OnEvent() => callback?.Invoke();
    }

    private class TestYearlyEvent(MonthDay start, MonthDay end, IRecurrencePattern recurrence, DateTime endOn = default)
        : YearlyScheduledEvent(new TimeOnly(12, 0), start, end, endOn == default ? DateTime.MaxValue : endOn, recurrence)
    {
        public DateTime Next(DateTime after) => GetNextOccurrence(after);

        public override void OnEvent()
        {
        }
    }

    [Fact]
    public void OneShot_AtNow_Fires()
    {
        var called = 0;
        var evt = Track(EventScheduler.Shared.ScheduleEvent(Core._now, () => called++));

        Assert.Equal(Core._now, evt.NextOccurrence);
        AdvanceTo(Core._now.AddSeconds(1));

        Assert.Equal(1, called);
        Assert.Equal(DateTime.MaxValue, evt.NextOccurrence);
    }

    [Fact]
    public void OneShot_InThePast_DoesNotFire()
    {
        var evt = Track(EventScheduler.Shared.ScheduleEvent(Core._now.AddDays(-1), () => { }));

        Assert.Equal(DateTime.MaxValue, evt.NextOccurrence);
    }

    [Fact]
    public void Scheduler_OrdersEventsByNextOccurrence()
    {
        var executionOrder = new List<int>();
        var start = Core._now;

        Track(EventScheduler.Shared.ScheduleEvent(start.AddSeconds(30), () => executionOrder.Add(3)));
        Track(EventScheduler.Shared.ScheduleEvent(start.AddSeconds(10), () => executionOrder.Add(1)));
        Track(EventScheduler.Shared.ScheduleEvent(start.AddSeconds(20), () => executionOrder.Add(2)));

        AdvanceTo(start.AddSeconds(40));

        Assert.Equal([1, 2, 3], executionOrder);
    }

    [Fact]
    public void Scheduler_ContinuesAfterACallbackThrows()
    {
        var laterEventCalled = false;
        var start = Core._now;

        Track(EventScheduler.Shared.ScheduleEvent(start.AddSeconds(10), () => throw new Exception("Test exception")));
        Track(EventScheduler.Shared.ScheduleEvent(start.AddSeconds(20), () => laterEventCalled = true));

        AdvanceTo(start.AddSeconds(30));

        Assert.True(laterEventCalled);
    }

    [Fact]
    public void Cancel_BeforeRun_DoesNotFire()
    {
        var eventCalled = false;
        var evt = EventScheduler.Shared.ScheduleEvent(Core._now.AddSeconds(10), () => eventCalled = true);

        evt.Cancel();
        AdvanceTo(Core._now.AddSeconds(20));

        Assert.False(eventCalled);
    }

    [Fact]
    public void Recurring_FiresOncePerRunAndStopsAtEndDate()
    {
        var calls = 0;
        var start = Core._now.AddSeconds(10);
        var evt = Track(new TestEvent(TimeOnly.FromDateTime(start), start.AddSeconds(25), new StepPattern(TimeSpan.FromSeconds(10)), () => calls++));
        evt.Schedule(start);

        for (var i = 1; i <= 5; i++)
        {
            AdvanceTo(start.AddSeconds(10 * i - 9));
        }

        // Runs at +0, +10 and +20; +30 is past the end date.
        Assert.Equal(3, calls);
        Assert.Equal(DateTime.MaxValue, evt.NextOccurrence);
    }

    [Fact]
    public void StartOnInTheFuture_FirstRunIsStartOn()
    {
        var yearly = Track(EventScheduler.YearlyAt(Utc(2024, 5, 15, 12, 30), () => { }));
        var weekly = Track(EventScheduler.WeeklyAt(Utc(2024, 5, 15, 12, 30), () => { }));

        Assert.Equal(Utc(2024, 5, 15, 12, 30), yearly.NextOccurrence);
        Assert.Equal(Utc(2024, 5, 15, 12, 30), weekly.NextOccurrence);
    }

    [Fact]
    public void StartOnInThePast_JoinsTheSeriesAfterNowWithoutFiring()
    {
        var calls = 0;
        var daily = Track(EventScheduler.DailyAt(Utc(2023, 6, 1, 6, 0), () => calls++));
        var biweekly = Track(EventScheduler.BiweeklyAt(Utc(2023, 12, 1, 9, 0), () => calls++)); // Friday

        AdvanceTo(Core._now.AddSeconds(1));

        Assert.Equal(0, calls);
        Assert.Equal(Utc(2024, 1, 2, 6, 0), daily.NextOccurrence);
        Assert.Equal(Utc(2024, 1, 12, 9, 0), biweekly.NextOccurrence); // Dec 1, 15, 29, Jan 12
    }

    [Fact]
    public void WeeklyAt_RecursOnTheStartWeekday()
    {
        var evt = Track(EventScheduler.WeeklyAt(Utc(2024, 1, 3, 9, 0), () => { })); // Wednesday

        AdvanceTo(Utc(2024, 1, 3, 9, 0, 1));

        Assert.Equal(Utc(2024, 1, 10, 9, 0), evt.NextOccurrence);
    }

    [Fact]
    public void RunDuringAWorldSaveFreeze_FiresOnceWhenTheLoopResumes()
    {
        var first = 0;
        var second = 0;
        var a = Track(EventScheduler.HourlyAt(Utc(2024, 1, 1, 13, 0), () => first++));
        Track(EventScheduler.DailyAt(Utc(2024, 1, 1, 13, 0), () => second++));

        AdvanceTo(Utc(2024, 1, 1, 13, 0, 10));

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(Utc(2024, 1, 1, 14, 0), a.NextOccurrence);
    }

    [Fact]
    public void MissedRun_IsSkippedToTheNextRunAfterNow()
    {
        var calls = 0;
        var evt = Track(EventScheduler.HourlyAt(Utc(2024, 1, 1, 13, 0), () => calls++));

        AdvanceTo(Utc(2024, 1, 1, 15, 30));

        Assert.Equal(0, calls);
        Assert.Equal(Utc(2024, 1, 1, 16, 0), evt.NextOccurrence);
    }

    [Fact]
    public void CancelInsideCallback_StopsTheSeries()
    {
        var calls = 0;
        ScheduledEvent evt = null;
        evt = Track(EventScheduler.HourlyAt(Utc(2024, 1, 1, 13, 0), () =>
        {
            calls++;
            evt.Cancel();
        }));

        AdvanceTo(Utc(2024, 1, 1, 13, 0, 1));
        AdvanceTo(Utc(2024, 1, 1, 14, 0, 1));

        Assert.Equal(1, calls);
        Assert.Equal(DateTime.MaxValue, evt.NextOccurrence);
    }

    [Fact]
    public void RescheduleInsideCallback_KeepsOnlyTheNewSeries()
    {
        var calls = 0;
        ScheduledEvent evt = null;
        evt = Track(EventScheduler.HourlyAt(Utc(2024, 1, 1, 13, 0), () =>
        {
            if (++calls == 1)
            {
                evt.Schedule(Utc(2024, 1, 1, 15, 0));
            }
        }));

        AdvanceTo(Utc(2024, 1, 1, 13, 0, 1));
        AdvanceTo(Utc(2024, 1, 1, 14, 0, 1));
        Assert.Equal(1, calls);

        AdvanceTo(Utc(2024, 1, 1, 15, 0, 1));
        Assert.Equal(2, calls);
        Assert.Equal(Utc(2024, 1, 1, 16, 0), evt.NextOccurrence);
    }

    [Fact]
    public void CallbackSchedulingARunForNow_WaitsForTheNextTick()
    {
        var inner = 0;
        Track(EventScheduler.Shared.ScheduleEvent(Core._now, () => Track(EventScheduler.Shared.ScheduleEvent(Core._now, () => inner++))));

        AdvanceTo(Core._now.AddSeconds(1));
        Assert.Equal(0, inner);

        AdvanceTo(Core._now.AddSeconds(1));
        Assert.Equal(1, inner);
    }

    [Fact]
    public void PatternThatDoesNotAdvance_StopsInsteadOfSpinning()
    {
        var calls = 0;
        var start = Core._now.AddSeconds(5);
        var evt = Track(EventScheduler.Shared.ScheduleEvent(start, () => calls++, new StuckPattern()));

        Assert.Equal(DateTime.MaxValue, evt.NextOccurrence);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void UnspecifiedStartWithoutTimeZone_IsUtc()
    {
        var evt = Track(EventScheduler.DailyAt(new DateTime(2024, 6, 1, 6, 0, 0), () => { }));

        Assert.Equal(Utc(2024, 6, 1, 6, 0), evt.NextOccurrence);
    }

    [Fact]
    public void UtcStartWithTimeZone_RecursAtTheLocalTimeOfTheStart()
    {
        var evt = Track(EventScheduler.DailyAt(Utc(2024, 6, 15, 10, 0), () => { }, NewYork)); // 06:00 EDT

        AdvanceTo(Utc(2024, 6, 15, 10, 0, 1));

        Assert.Equal(new DateTime(2024, 6, 16, 6, 0, 0), TimeZoneInfo.ConvertTimeFromUtc(evt.NextOccurrence, NewYork));
    }

    [Fact]
    public void EndDate_UnspecifiedKind_IsReadInTheEventTimeZone()
    {
        var evt = Track(new TestEvent(new TimeOnly(12, 0), new DateTime(2024, 6, 1, 0, 0, 0), EventScheduler.Daily, null));
        evt.Schedule(new DateTime(2024, 5, 1, 12, 0, 0), NewYork);

        Assert.Equal(Utc(2024, 6, 1, 4, 0), evt.EndDate);
    }

    [Fact]
    public void YearlyWindow_DailyRunsOnTheFirstDayOfTheWindow()
    {
        var evt = new TestYearlyEvent(new MonthDay(2024, 10, 1), new MonthDay(2024, 10, 31), EventScheduler.Daily);
        evt.Schedule(EventScheduler.Shared, Utc(2024, 1, 1, 12, 0));

        Assert.Equal(Utc(2024, 10, 1, 12, 0), evt.NextOccurrence);
        Assert.Equal(Utc(2025, 10, 1, 12, 0), evt.Next(Utc(2024, 10, 31, 12, 0)));
        evt.Cancel();
    }

    [Fact]
    public void YearlyWindow_NextWindowStartsLaterThisYear()
    {
        // Monthly on the 15th: after Dec 15, 2028 the next candidate (Jan 15) is before the 2029 window.
        var evt = new TestYearlyEvent(new MonthDay(2024, 11, 10), new MonthDay(2024, 12, 20), EventScheduler.GetMonthlyRecurrence(15));
        evt.Schedule(EventScheduler.Shared, Utc(2024, 1, 1, 0, 0));

        Assert.Equal(Utc(2029, 11, 15, 12, 0), evt.Next(Utc(2028, 12, 15, 12, 0)));
        evt.Cancel();
    }

    [Fact]
    public void YearlyWindow_WrappingWindow_ResumesThisYearFromTheEndMonth()
    {
        var evt = new TestYearlyEvent(new MonthDay(2024, 11, 15), new MonthDay(2024, 2, 15), EventScheduler.Daily);
        evt.Schedule(EventScheduler.Shared, Utc(2024, 1, 1, 0, 0));

        Assert.Equal(Utc(2025, 11, 15, 12, 0), evt.Next(Utc(2025, 2, 15, 12, 0)));
        evt.Cancel();
    }

    [Fact]
    public void YearlyWindow_RunNeverInTheWindow_HasNoNextRun()
    {
        var evt = new TestYearlyEvent(new MonthDay(2024, 11, 10), new MonthDay(2024, 11, 12), EventScheduler.GetMonthlyRecurrence(15));
        evt.Schedule(EventScheduler.Shared, Utc(2024, 1, 1, 0, 0));

        Assert.Equal(DateTime.MaxValue, evt.NextOccurrence);
    }

    [Fact]
    public void YearlyWindow_RespectsEndDateAcrossTheWindowJump()
    {
        var evt = new TestYearlyEvent(new MonthDay(2024, 10, 1), new MonthDay(2024, 10, 31), EventScheduler.Daily, Utc(2024, 12, 31, 0, 0));
        evt.Schedule(EventScheduler.Shared, Utc(2024, 1, 1, 0, 0));

        Assert.Equal(DateTime.MaxValue, evt.Next(Utc(2024, 10, 31, 12, 0)));
        evt.Cancel();
    }

    [Fact]
    public void YearlyWindow_LeapDayStart_OpensMarchFirstInCommonYears()
    {
        var evt = new TestYearlyEvent(new MonthDay(2024, 2, 29), new MonthDay(2024, 3, 5), EventScheduler.Daily);
        evt.Schedule(EventScheduler.Shared, Utc(2024, 1, 1, 0, 0));

        Assert.Equal(Utc(2025, 3, 1, 12, 0), evt.Next(Utc(2025, 1, 1, 0, 0)));
        evt.Cancel();
    }
}
