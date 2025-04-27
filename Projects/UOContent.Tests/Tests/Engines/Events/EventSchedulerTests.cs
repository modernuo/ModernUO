using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Events;
using Xunit;

[Collection("Sequential Tests")]
public class EventSchedulerTests
{
    [Fact]
    public void ScheduleEvent_ExecutesCallback_HappyPath()
    {
        Core._now = DateTime.UtcNow;
        Timer.Init(0);
        EventScheduler.Configure();

        bool called = false;
        var now = DateTime.UtcNow;
        Core._now = now;

        var evt = EventScheduler.Instance.ScheduleEvent(now, () => called = true);

        Timer.Slice(8);

        Assert.True(called);
        Assert.Equal(now, evt.NextOccurrence);

        EventScheduler.Instance.Stop();
    }

    [Theory]
    [InlineData(2024, 3, 10, 2, 0, "America/New_York")] // DST spring forward gap (invalid)
    [InlineData(2024, 11, 3, 1, 0, "America/New_York")] // DST fall back (ambiguous)
    [InlineData(2024, 6, 1, 5, 0, "America/New_York")]  // Normal time
    public void HourlyRecurrence_AdvancesCorrectly_DstEdgeCases(
        int year, int month, int day, int hour, int minute, string tzId)
    {
        Core._now = DateTime.UtcNow;
        Timer.Init(0);
        EventScheduler.Configure();

        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

        // Set Core._now to just before the target hour
        var beforeLocal = local.AddHours(-1);
        Core._now = beforeLocal.LocalToUtc(tz);

        bool called = false;
        var evt = EventScheduler.Instance.ScheduleEvent(
            Core._now,
            () => called = true,
            EventScheduler.Hourly,
            tz
        );

        // Advance to the target hour
        Core._now = local.LocalToUtc(tz);
        Assert.Equal(Core._now, evt.NextOccurrence);
        Timer.Slice(8);

        // Hourly recurrence should always execute, even in DST gaps/ambiguous times
        Assert.True(called);
        Assert.Equal(Core._now.AddHours(1), evt.NextOccurrence);

        EventScheduler.Instance.Stop();
    }

    [Theory]
    [InlineData(2024, 3, 10, 2, 0, "America/New_York")] // DST spring forward gap
    [InlineData(2024, 11, 3, 1, 30, "America/New_York")] // DST fall back
    public void MonthlyRecurrence_DaylightSavingTime_EdgeCases(
        int year, int month, int day, int hour, int minute, string tzId)
    {
        Core._now = DateTime.UtcNow;
        Timer.Init(0);
        EventScheduler.Configure();

        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

        Core._now = local.AddDays(-1).LocalToUtc(tz);

        bool called = false;
        var evt = EventScheduler.Instance.ScheduleEvent(
            Core._now,
            () => called = true,
            EventScheduler.GetMonthlyRecurrence(day),
            tz
        );

        Core._now = local.LocalToUtc(tz);
        var invalidTime = tz.IsInvalidTime(local);

        // Invalid time ranges are not executed, so the occurrence is an additional month later
        Assert.Equal(invalidTime ? local.AddMonths(1).LocalToUtc(tz) : Core._now, evt.NextOccurrence);

        Timer.Slice(8);
        Assert.NotEqual(invalidTime, called);

        EventScheduler.Instance.Stop();
    }

    [Theory]
    [InlineData(2024, 3, 10, 2, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Second, "America/New_York")] // DST gap
    [InlineData(2024, 11, 3, 1, 30, DayOfWeek.Sunday, OrdinalDayOccurrence.First, "America/New_York")] // DST fallback
    [InlineData(2024, 3, 31, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Last, "America/New_York")]   // Last Sunday
    [InlineData(2024, 3, 31, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Fifth, "America/New_York")]  // Fifth Sunday
    [InlineData(2024, 4, 3, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Fifth, "America/New_York")]  // Fifth Sunday (Doesn't exist)
    public void MonthlyOrdinalRecurrence_DaylightSavingTime_EdgeCases(
        int year, int month, int day, int hour, int minute, DayOfWeek dow, OrdinalDayOccurrence ordinal, string tzId)
    {
        Timer.Init(0);
        EventScheduler.Configure();

        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var local = new DateTime(year, month, day, hour, minute, 0).AddDays(-1);
        Core._now = local.LocalToUtc(tz);

        bool called = false;
        var pattern = new MonthlyOrdinalRecurrencePattern(ordinal, dow);
        var evt = EventScheduler.Instance.ScheduleEvent(
            Core._now,
            () => called = true,
            pattern,
            tz
        );

        // Advance time so the next occurrence happens
        var testTime = Core._now.AddDays(1);

        // If the time is invalid (e.g., 2:30am on DST spring forward), assert not called
        if (tz.IsInvalidTime(local.AddDays(1)))
        {
            Core._now = testTime;
            Timer.Slice(8);
            Assert.False(called);
            EventScheduler.Instance.Stop();
            return;
        }

        if (month == 4 && ordinal == OrdinalDayOccurrence.Fifth)
        {
            // If there is no fifth occurrence, NextOccurrence should be in the next month
            var expectedNext = pattern.GetNextOccurrence(Core._now, tz);
            Assert.Equal(expectedNext, evt.NextOccurrence);
            Core._now = testTime;
            Timer.Slice(8);
            Assert.False(called);
        }
        else
        {
            Assert.Equal(testTime, evt.NextOccurrence);
            Core._now = testTime;
            Timer.Slice(8);
            Assert.True(called);
        }

        EventScheduler.Instance.Stop();
    }

    [Fact]
    public void ScheduleEvent_NullCallback_Throws()
    {
        Core._now = DateTime.UtcNow;
        Timer.Init(0);
        EventScheduler.Configure();

        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentNullException>(() =>
            EventScheduler.Instance.ScheduleEvent(now, null)
        );

        EventScheduler.Instance.Stop();
    }

    [Fact]
    public void AdvanceEvent_PastEndDate_DoesNotReschedule()
    {
        Core._now = DateTime.UtcNow;
        Timer.Init(0);
        EventScheduler.Configure();

        bool called = false;
        var now = DateTime.UtcNow;
        Core._now = now;

        var evt = new CallbackScheduledEvent(now, now, () => called = true);

        typeof(EventScheduler)
            .GetMethod("ScheduleEvent", new[] { typeof(ScheduledEvent) })!
            .Invoke(EventScheduler.Instance, new object[] { evt });

        Timer.Slice(8);

        Assert.True(called);

        Assert.DoesNotContain(evt, typeof(EventScheduler)
            .GetField("_schedule", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(EventScheduler.Instance) as IEnumerable<CallbackScheduledEvent> ?? []);

        EventScheduler.Instance.Stop();
    }
}
