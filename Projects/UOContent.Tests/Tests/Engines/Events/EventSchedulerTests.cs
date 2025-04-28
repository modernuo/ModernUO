using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Events;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential Tests")]
public class EventSchedulerTests
{
    private static void Init()
    {
        Core._now = DateTime.UtcNow;
        Timer.Init(0);
        EventScheduler.Configure();
    }

    private static void Finish()
    {
        EventScheduler.Shared.Stop();
    }

    [Fact]
    public void ScheduleEvent_ExecutesCallback_HappyPath()
    {
        Init();

        try
        {

        }
        finally
        {
            bool called = false;
            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now,
                TimeOnly.FromDateTime(Core._now),
                () => called = true
            );

            Timer.Slice(8);

            Assert.True(called);
            Assert.Equal(Core._now, evt.NextOccurrence);

            EventScheduler.Shared.StopEvent(evt);
        }

        Finish();
    }

    [Theory]
    [InlineData(2024, 3, 10, 2, 0, "America/New_York")] // DST spring forward gap (invalid)
    [InlineData(2024, 11, 3, 1, 0, "America/New_York")] // DST fall back (ambiguous)
    [InlineData(2024, 6, 1, 5, 0, "America/New_York")]  // Normal time
    public void HourlyRecurrence(
        int year, int month, int day, int hour, int minute, string tzId)
    {
        Init();

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

            // Set Core._now to just before the target hour
            var beforeLocal = local.AddHours(-1);
            Core._now = beforeLocal.LocalToUtc(tz);

            bool called = false;
            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now,
                TimeOnly.FromDateTime(local),
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

            EventScheduler.Shared.StopEvent(evt);
        }
        finally
        {
            Finish();
        }
    }

    [Theory]
    [InlineData(2024, 3, 10, 2, 0, "America/New_York")] // DST spring forward gap
    [InlineData(2024, 11, 3, 1, 30, "America/New_York")] // DST fall back
    [InlineData(2024, 6, 2, 5, 0, "America/New_York")] // Normal time
    public void DailyRecurrence_ExecutesEveryDay(
        int year, int month, int day, int hour, int minute, string tzId)
    {
        Init();

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

            // Set Core._now to just before the target day
            var beforeLocal = local.AddDays(-1);
            Core._now = beforeLocal.LocalToUtc(tz);

            bool called = false;
            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now,
                TimeOnly.FromDateTime(local),
                () => called = true,
                EventScheduler.Daily,
                tz
            );

            // Advance to the target day
            Core._now = local.LocalToUtc(tz);
            Assert.Equal(Core._now, evt.NextOccurrence);
            Timer.Slice(8);

            // Daily recurrence should always
            // execute, even in DST gaps/ambiguous times
            Assert.True(called);
            Assert.Equal(Core._now.AddDays(1), evt.NextOccurrence);

            EventScheduler.Shared.StopEvent(evt);
        }
        finally
        {
            Finish();
        }
    }

    [Fact]
    public void WeeklyRecurrence_MultipleDays_ExecutesOnCorrectDays()
    {
        Init();

        try
        {
            var tz = TimeZoneInfo.Utc;
            // Start on Sunday
            var start = new DateTime(2024, 6, 2, 8, 0, 0, DateTimeKind.Utc);
            Core._now = start;

            int callCount = 0;
            var pattern = new WeeklyRecurrencePattern(1, DaysOfWeek.Monday | DaysOfWeek.Friday);
            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now,
                TimeOnly.FromDateTime(Core._now),
                () => callCount++,
                pattern,
                tz
            );

            // Simulate a week
            for (int i = 1; i <= 7; i++)
            {
                Core._now = start.AddDays(i);
                Timer.Slice(8 + 1024 * (i - 1));
            }

            // Should fire on Monday (June 3) and Friday (June 7)
            Assert.Equal(2, callCount);

            EventScheduler.Shared.StopEvent(evt);
        }
        finally
        {
            Finish();
        }
    }

    [Theory]
    [InlineData(2024, 3, 10, 2, 0, "America/New_York")] // DST spring forward gap
    [InlineData(2024, 11, 3, 1, 30, "America/New_York")] // DST fall back
    [InlineData(2024, 6, 2, 5, 0, "America/New_York")] // Normal time
    public void MonthlyRecurrence(
        int year, int month, int day, int hour, int minute, string tzId)
    {
        Init();

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

            Core._now = local.AddDays(-1).LocalToUtc(tz);

            bool called = false;
            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now,
                TimeOnly.FromDateTime(local),
                () => called = true,
                EventScheduler.GetMonthlyRecurrence(day),
                tz
            );

            Core._now = local.LocalToUtc(tz);
            var invalidTime = tz.IsInvalidTime(local);

            // Invalid time ranges are not executed, so the occurrence is an additional month later
            Assert.Equal(invalidTime ? local.AddMonths(1).LocalToUtc(tz) : Core._now, evt.NextOccurrence);

            Timer.Slice(8);
            if (invalidTime)
            {
                Assert.False(called);
            }
            else
            {
                Assert.True(called);
            }

            EventScheduler.Shared.StopEvent(evt);
        }
        finally
        {
            Finish();
        }
    }

    [Fact]
    public void MonthlyRecurrence_MonthWithoutDay_SkipsToNextValidMonth()
    {
        Init();

        try
        {
            var tz = TimeZoneInfo.Utc;

            // January 31st
            var start = new DateTime(2024, 2, 1, 8, 0, 0, DateTimeKind.Utc);
            Core._now = start;

            int callCount = 0;
            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now,
                TimeOnly.FromDateTime(Core._now),
                () => callCount++,
                EventScheduler.GetMonthlyRecurrence(31),
                tz
            );

            // February does not have 31st, so next should be March 31st
            Core._now = new DateTime(2024, 2, 28, 8, 0, 0, DateTimeKind.Utc);
            Timer.Slice(8);
            Assert.Equal(0, callCount);

            Core._now = new DateTime(2024, 3, 31, 8, 0, 0, DateTimeKind.Utc);
            Timer.Slice(1024);
            Assert.Equal(1, callCount);

            EventScheduler.Shared.StopEvent(evt);
        }
        finally
        {
            Finish();
        }
    }

    [Theory]
    [InlineData(2024, 11, 3, 1, 30, DayOfWeek.Sunday, OrdinalDayOccurrence.First, "America/New_York")] // DST fallback
    [InlineData(2024, 3, 31, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Last, "America/New_York")]   // Last Sunday
    [InlineData(2024, 3, 31, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Fifth, "America/New_York")]  // Fifth Sunday
    [InlineData(2024, 4, 3, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Fifth, "America/New_York")]  // Fifth Sunday (Doesn't exist)
    public void MonthlyOrdinalRecurrence(
        int year, int month, int day, int hour, int minute, DayOfWeek dow, OrdinalDayOccurrence ordinal, string tzId)
    {
        Init();

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var local = new DateTime(year, month, day, hour, minute, 0).AddDays(-1);
            Core._now = local.LocalToUtc(tz);

            bool called = false;
            var pattern = new MonthlyOrdinalRecurrencePattern(ordinal, dow);
            var evt = EventScheduler.Shared.ScheduleEvent(
                Core._now,
                TimeOnly.FromDateTime(local),
                () => called = true,
                pattern,
                tz
            );

            var testTime = Core._now.AddDays(1);

            if (month == 4 && ordinal == OrdinalDayOccurrence.Fifth)
            {
                // If there is no fifth occurrence, NextOccurrence should be in the next month
                var expectedNext = pattern.GetNextOccurrence(Core._now, TimeOnly.FromDateTime(local), tz);
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

            EventScheduler.Shared.StopEvent(evt);
        }
        finally
        {
            Finish();
        }
    }

    [Fact]
    public void ScheduleEvent_NullCallback_Throws()
    {
        Init();

        try
        {
            ScheduledEvent evt = null;
            Assert.Throws<ArgumentNullException>(() =>
                evt = EventScheduler.Shared.ScheduleEvent(Core._now, TimeOnly.FromDateTime(Core._now), null)
            );
            EventScheduler.Shared.StopEvent(evt);
        }
        finally
        {
            Finish();
        }
    }

    [Fact]
    public void AdvanceEvent_PastEndDate_DoesNotReschedule()
    {
        Init();

        try
        {
            bool called = false;
            var evt = new CallbackScheduledEvent(
                Core._now,
                Core._now,
                TimeOnly.FromDateTime(Core._now),
                () => called = true
            );

            EventScheduler.Shared.ScheduleEvent(evt);

            Timer.Slice(8);
            Assert.True(called);

            Assert.DoesNotContain(
                evt,
                typeof(EventScheduler)
                    .GetField(
                        "_schedule",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                    )!
                    .GetValue(EventScheduler.Shared) as IEnumerable<CallbackScheduledEvent> ?? []
            );

            EventScheduler.Shared.StopEvent(evt);
        }
        finally
        {
            Finish();
        }
    }

    [Theory]
    [InlineData("UTC")]            // No DST adjustments
    [InlineData("Asia/Kathmandu")] // Unusual offset (UTC+5:45)
    public void LocalToUtc_SpecialTimeZones_HandlesCorrectly(string tzId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var local = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified);
        var expected = TimeZoneInfo.ConvertTimeToUtc(local, tz);

        var actual = local.LocalToUtc(tz);

        Assert.Equal(expected, actual);
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }

    [Fact]
    public void LocalToUtc_EdgeCases_BeforeAndAfterTransition()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        // 1:59 AM (just before spring forward)
        var beforeSpring = new DateTime(2024, 3, 10, 1, 59, 0);
        // 3:01 AM (just after spring forward)
        var afterSpring = new DateTime(2024, 3, 10, 3, 1, 0);

        var beforeUtc = beforeSpring.LocalToUtc(tz);
        var afterUtc = afterSpring.LocalToUtc(tz);

        // Should be 1 hour + 2 minutes apart in UTC (not 1 hour 2 minutes)
        Assert.Equal(2, (afterUtc - beforeUtc).TotalMinutes);
    }
}
