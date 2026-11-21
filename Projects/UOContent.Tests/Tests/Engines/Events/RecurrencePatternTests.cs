using System;
using Server;
using Server.Engines.Events;
using Xunit;

namespace UOContent.Tests;

public class RecurrencePatternTests
{
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, NewYork);

    [Theory]
    [InlineData(2024, 5, 15, 12, 30, 1, 2024, 5, 15, 13, 30)] // Normal case
    [InlineData(2024, 5, 15, 23, 30, 2, 2024, 5, 16, 1, 30)]  // Cross day boundary
    public void HourlyRecurrencePattern_GetNextOccurrence_ReturnsCorrectTime(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalHours,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        var pattern = new HourlyRecurrencePattern(intervalHours);
        var afterUtc = Utc(startYear, startMonth, startDay, startHour, startMinute);

        var result = pattern.GetNextOccurrence(afterUtc, afterUtc, new TimeOnly(0, expectedMinute), TimeZoneInfo.Utc);

        Assert.Equal(Utc(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute), result);
    }

    [Fact]
    public void HourlyRecurrencePattern_FallBack_RunsBothRepeatedHoursAndAlwaysAdvances()
    {
        var pattern = new HourlyRecurrencePattern();
        var time = new TimeOnly(0, 30);
        var start = Utc(2024, 11, 3, 4, 30); // 00:30 EDT

        var first = pattern.GetNextOccurrence(start, start, time, NewYork);
        var second = pattern.GetNextOccurrence(first, start, time, NewYork);
        var third = pattern.GetNextOccurrence(second, start, time, NewYork);

        Assert.Equal(Utc(2024, 11, 3, 5, 30), first);  // 01:30 EDT
        Assert.Equal(Utc(2024, 11, 3, 6, 30), second); // 01:30 EST
        Assert.Equal(Utc(2024, 11, 3, 7, 30), third);  // 02:30 EST
    }

    [Fact]
    public void HourlyRecurrencePattern_OldStart_KeepsCadence()
    {
        var pattern = new HourlyRecurrencePattern(5);
        var start = Utc(2020, 1, 1, 0, 15);
        var after = Utc(2024, 7, 9, 13, 2);

        var result = pattern.GetNextOccurrence(after, start, new TimeOnly(0, 15), TimeZoneInfo.Utc);

        Assert.True(result > after && result - after <= TimeSpan.FromHours(5));
        Assert.Equal(0, (result - start).Ticks % TimeSpan.FromHours(5).Ticks);
    }

    [Theory]
    [InlineData(2024, 5, 15, 12, 30, 1, 2024, 5, 16, 12, 30)] // Next day
    [InlineData(2024, 5, 31, 12, 30, 2, 2024, 6, 2, 12, 30)]  // Across month boundary
    public void DailyRecurrencePattern_GetNextOccurrence_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        var pattern = new DailyRecurrencePattern(intervalDays);
        var afterUtc = Utc(startYear, startMonth, startDay, startHour, startMinute);
        var time = new TimeOnly(expectedHour, expectedMinute);

        var result = pattern.GetNextOccurrence(afterUtc, afterUtc, time, TimeZoneInfo.Utc);

        Assert.Equal(Utc(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute), result);
    }

    [Theory]
    [InlineData(2024, 3, 8, 6, 0)]   // Across spring forward at 06:00
    [InlineData(2024, 10, 31, 6, 0)] // Across fall back at 06:00
    [InlineData(2024, 10, 31, 0, 0)] // Across fall back at midnight
    [InlineData(2024, 10, 31, 0, 30)]
    [InlineData(2024, 10, 31, 1, 30)] // Repeated hour on Nov 3: runs once, on the first pass
    public void DailyRecurrencePattern_AcrossDst_KeepsLocalTime(int year, int month, int day, int hour, int minute)
    {
        var pattern = new DailyRecurrencePattern();
        var time = new TimeOnly(hour, minute);
        var start = new DateTime(year, month, day, hour, minute, 0).LocalToUtc(NewYork);

        var previous = start;
        for (var i = 1; i <= 6; i++)
        {
            var next = pattern.GetNextOccurrence(previous, start, time, NewYork);

            Assert.True(next > previous);
            Assert.Equal(new DateTime(year, month, day, hour, minute, 0).AddDays(i), Local(next));
            previous = next;
        }
    }

    [Fact]
    public void DailyRecurrencePattern_SpringForwardGap_MovesForwardByTheGap()
    {
        var pattern = new DailyRecurrencePattern();
        var start = new DateTime(2024, 3, 9, 2, 30, 0).LocalToUtc(NewYork);

        var result = pattern.GetNextOccurrence(start, start, new TimeOnly(2, 30), NewYork);

        Assert.Equal(new DateTime(2024, 3, 10, 3, 30, 0), Local(result));
    }

    [Fact]
    public void DailyRecurrencePattern_OldStart_KeepsCadence()
    {
        var pattern = new DailyRecurrencePattern(3);
        var start = Utc(2024, 1, 1, 9, 0);

        var result = pattern.GetNextOccurrence(Utc(2024, 3, 15, 12, 0), start, new TimeOnly(9, 0), TimeZoneInfo.Utc);

        // Jan 1 + 75 days
        Assert.Equal(Utc(2024, 3, 16, 9, 0), result);
    }

    [Theory]
    [InlineData(2024, 5, 15, 12, 30, 1, AllowedDays.All, 2024, 5, 16, 12, 30)] // Next day (Thursday)
    [InlineData(2024, 5, 15, 12, 30, 1, AllowedDays.Monday, 2024, 5, 20, 12, 30)] // Next Monday
    [InlineData(2024, 5, 15, 12, 30, 2, AllowedDays.Wednesday, 2024, 5, 29, 12, 30)] // Biweekly Wednesday
    [InlineData(2024, 12, 30, 12, 30, 1, AllowedDays.Monday, 2025, 1, 6, 12, 30)] // Across year boundary
    [InlineData(2024, 5, 31, 12, 30, 1, AllowedDays.Saturday, 2024, 6, 1, 12, 30)] // Across month boundary
    public void WeeklyRecurrencePattern_BasicPatterns_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalWeeks, AllowedDays allowedDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        var pattern = new WeeklyRecurrencePattern(intervalWeeks, AllowedMonths.All, allowedDays);
        var afterUtc = Utc(startYear, startMonth, startDay, startHour, startMinute);
        var time = new TimeOnly(expectedHour, expectedMinute);

        var result = pattern.GetNextOccurrence(afterUtc, afterUtc, time, TimeZoneInfo.Utc);

        Assert.Equal(Utc(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute), result);
    }

    [Theory]
    [InlineData(2024, 5, 15, 12, 30, AllowedMonths.January | AllowedMonths.February, AllowedDays.Wednesday, 2025, 1, 1, 12, 30)] // Skip to January
    [InlineData(2024, 12, 15, 12, 30, AllowedMonths.January | AllowedMonths.December, AllowedDays.Wednesday, 2024, 12, 18, 12, 30)] // Same month
    [InlineData(2024, 1, 31, 12, 30, AllowedMonths.February | AllowedMonths.April, AllowedDays.Saturday, 2024, 2, 3, 12, 30)] // Next month allowed
    public void WeeklyRecurrencePattern_MonthFiltering_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        AllowedMonths allowedMonths, AllowedDays allowedDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        var pattern = new WeeklyRecurrencePattern(1, allowedMonths, allowedDays);
        var afterUtc = Utc(startYear, startMonth, startDay, startHour, startMinute);
        var time = new TimeOnly(expectedHour, expectedMinute);

        var result = pattern.GetNextOccurrence(afterUtc, afterUtc, time, TimeZoneInfo.Utc);

        Assert.Equal(Utc(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute), result);
    }

    [Theory]
    [InlineData(2024, 3, 9, 12, 0, AllowedDays.Sunday, 2024, 3, 10, 12, 0)] // Before spring forward
    [InlineData(2024, 3, 10, 2, 30, AllowedDays.Sunday, 2024, 3, 17, 2, 30)] // During spring forward (invalid time)
    [InlineData(2024, 11, 2, 12, 0, AllowedDays.Sunday, 2024, 11, 3, 12, 0)] // Before fall back
    [InlineData(2024, 11, 3, 1, 30, AllowedDays.Sunday, 2024, 11, 10, 1, 30)] // During fall back (ambiguous time)
    public void WeeklyRecurrencePattern_DSTTransitions_HandlesCorrectly(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        AllowedDays allowedDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        var pattern = new WeeklyRecurrencePattern(1, AllowedMonths.All, allowedDays);
        var afterUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0).LocalToUtc(NewYork);
        var time = new TimeOnly(expectedHour, expectedMinute);

        var result = pattern.GetNextOccurrence(afterUtc, afterUtc, time, NewYork);

        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0), Local(result));
    }

    [Fact]
    public void WeeklyRecurrencePattern_SecondPassOfFallBack_NeverReturnsEarlierRun()
    {
        var pattern = new WeeklyRecurrencePattern(1, AllowedMonths.All, AllowedDays.Sunday);
        var after = Utc(2024, 11, 3, 6, 0); // 01:00 EST; 01:30 EDT is already past

        var result = pattern.GetNextOccurrence(after, after, new TimeOnly(1, 30), NewYork);

        Assert.Equal(new DateTime(2024, 11, 10, 1, 30, 0), Local(result));
    }

    [Theory]
    [InlineData(2024, 5, 29, 12, 0, AllowedDays.None, 2024, 5, 29, 12, 30)] // Weekday of the start
    [InlineData(2024, 5, 29, 12, 0, AllowedDays.Wednesday | AllowedDays.Friday, 2024, 5, 29, 12, 30)] // Current day is Wednesday
    [InlineData(2024, 5, 29, 12, 0, AllowedDays.Monday | AllowedDays.Friday, 2024, 5, 31, 12, 30)] // Next allowed day (Friday)
    public void WeeklyRecurrencePattern_DaysOfWeekHandling_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        AllowedDays allowedDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        var pattern = new WeeklyRecurrencePattern(1, AllowedMonths.All, allowedDays);
        var afterUtc = Utc(startYear, startMonth, startDay, startHour, startMinute);
        var time = new TimeOnly(expectedHour, expectedMinute);

        var result = pattern.GetNextOccurrence(afterUtc, afterUtc, time, TimeZoneInfo.Utc);

        Assert.Equal(Utc(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute), result);
    }

    [Fact]
    public void WeeklyRecurrencePattern_NoneUsesStartWeekdayNotAfterWeekday()
    {
        var pattern = new WeeklyRecurrencePattern();
        var start = Utc(2024, 5, 15, 12, 0); // Wednesday

        var result = pattern.GetNextOccurrence(Utc(2024, 6, 1, 0, 0), start, new TimeOnly(12, 0), TimeZoneInfo.Utc);

        Assert.Equal(Utc(2024, 6, 5, 12, 0), result);
    }

    [Theory]
    [InlineData(2024, 1, 29, 12, 0, 1, 2024, 1, 29, 12, 30, AllowedMonths.January | AllowedMonths.March, 2024, 1, 30, 12, 30)] // Current month allowed
    [InlineData(2024, 1, 31, 12, 0, 1, 2024, 1, 31, 12, 30, AllowedMonths.January | AllowedMonths.March, 2024, 3, 1, 12, 30)] // Skip February
    public void WeeklyRecurrencePattern_WeekSpanningMonths_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalWeeks,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute,
        AllowedMonths allowedMonths,
        int nextExpectedYear, int nextExpectedMonth, int nextExpectedDay, int nextExpectedHour, int nextExpectedMinute)
    {
        var pattern = new WeeklyRecurrencePattern(intervalWeeks, allowedMonths, AllowedDays.All);
        var startUtc = Utc(startYear, startMonth, startDay, startHour, startMinute);
        var time = new TimeOnly(expectedHour, expectedMinute);

        var result = pattern.GetNextOccurrence(startUtc, startUtc, time, TimeZoneInfo.Utc);
        Assert.Equal(Utc(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute), result);

        var nextResult = pattern.GetNextOccurrence(result, startUtc, time, TimeZoneInfo.Utc);
        Assert.Equal(Utc(nextExpectedYear, nextExpectedMonth, nextExpectedDay, nextExpectedHour, nextExpectedMinute), nextResult);
    }

    [Fact]
    public void WeeklyRecurrencePattern_BiweeklyFromOldStart_KeepsItsWeeks()
    {
        var pattern = new WeeklyRecurrencePattern(2);
        var start = new DateTime(2024, 10, 4, 19, 0, 0).LocalToUtc(NewYork); // Friday

        var result = pattern.GetNextOccurrence(Utc(2024, 12, 1, 0, 0), start, new TimeOnly(19, 0), NewYork);

        // Oct 4, Oct 18, Nov 1, Nov 15, Nov 29, Dec 13
        Assert.Equal(new DateTime(2024, 12, 13, 19, 0, 0), Local(result));
    }

    [Fact]
    public void WeeklyRecurrencePattern_FridaysInOctober_RunsEachFridayAndResumesNextOctober()
    {
        var pattern = new WeeklyRecurrencePattern(1, AllowedMonths.October, AllowedDays.Friday);
        var time = new TimeOnly(19, 0);
        var start = Utc(2024, 1, 1, 0, 0);

        DateTime[] expected =
        [
            new(2024, 10, 4, 19, 0, 0),
            new(2024, 10, 11, 19, 0, 0),
            new(2024, 10, 18, 19, 0, 0),
            new(2024, 10, 25, 19, 0, 0),
            new(2025, 10, 3, 19, 0, 0),
            new(2025, 10, 31, 19, 0, 0) // EDT until Nov 2, 2025
        ];

        var previous = start;
        var results = new DateTime[expected.Length];
        for (var i = 0; i < 5; i++)
        {
            previous = pattern.GetNextOccurrence(previous, start, time, NewYork);
            results[i] = Local(previous);
        }

        results[5] = Local(pattern.GetNextOccurrence(Utc(2025, 10, 25, 0, 0), start, time, NewYork));

        Assert.Equal(expected, results);
    }

    [Theory]
    [InlineData(2024, 11, 3, 1, 30, DayOfWeek.Sunday, OrdinalDayOccurrence.First, 2024, 11, 3)]
    [InlineData(2024, 3, 31, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Last, 2024, 3, 31)]
    [InlineData(2024, 3, 31, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Fifth, 2024, 3, 31)]
    [InlineData(2024, 4, 3, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Fifth, 2024, 6, 30)] // June has 5 Sundays (2, 9, 16, 23, 30)
    public void MonthlyOrdinalRecurrencePattern_GetNextOccurrence_ReturnsCorrectDate(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        DayOfWeek dow, OrdinalDayOccurrence ordinal,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var pattern = new MonthlyOrdinalRecurrencePattern(ordinal, dow);
        var beforeUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0).AddDays(-1).LocalToUtc(NewYork);

        var result = pattern.GetNextOccurrence(beforeUtc, beforeUtc, new TimeOnly(startHour, startMinute), NewYork);

        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay, startHour, startMinute, 0), Local(result));
    }

    [Theory]
    // 2:30 AM does not exist on the spring-forward day, so the run moves forward by the gap to 3:30 EDT.
    [InlineData(2024, 3, 10, 2, 30, 2024, 3, 10, 7, 30)]
    // 1:30 AM happens twice on the fall-back day; LocalToUtc resolves it to the first (EDT) pass.
    [InlineData(2024, 11, 3, 1, 30, 2024, 11, 3, 5, 30)]
    public void MonthlyRecurrencePattern_DSTTransitions_HandlesCorrectly(
        int year, int month, int day, int hour, int minute,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        var pattern = new MonthlyRecurrencePattern(day);
        var beforeUtc = new DateTime(year, month, day, 0, 0, 0).LocalToUtc(NewYork);

        var result = pattern.GetNextOccurrence(beforeUtc, beforeUtc, new TimeOnly(hour, minute), NewYork);

        Assert.Equal(Utc(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute), result);
    }

    [Fact]
    public void MonthlyRecurrence_MonthWithoutDay_SkipsToNextValidMonth()
    {
        var pattern = new MonthlyRecurrencePattern(31);
        var february = Utc(2024, 2, 1, 12, 0);

        var result = pattern.GetNextOccurrence(february, february, new TimeOnly(12, 0), TimeZoneInfo.Utc);

        Assert.Equal(Utc(2024, 3, 31, 12, 0), result);
    }

    [Fact]
    public void MonthlyRecurrence_StartDay_SkipsMonthsWithoutIt()
    {
        var pattern = new MonthlyRecurrencePattern();
        var start = Utc(2024, 1, 31, 12, 0);

        var result = pattern.GetNextOccurrence(start, start, new TimeOnly(12, 0), TimeZoneInfo.Utc);

        Assert.Equal(Utc(2024, 3, 31, 12, 0), result);
    }

    [Fact]
    public void YearlyRecurrence_LeapDay_RunsOnlyInLeapYears()
    {
        var pattern = new MonthlyRecurrencePattern(-1, 12);
        var start = Utc(2024, 2, 29, 12, 0);

        var result = pattern.GetNextOccurrence(start, start, new TimeOnly(12, 0), TimeZoneInfo.Utc);

        Assert.Equal(Utc(2028, 2, 29, 12, 0), result);
    }

    [Theory]
    [InlineData(2024, 11, 3, 1, 30)] // Fall back - ambiguous time
    public void LocalToUtc_AmbiguousTime_HandlesConsistently(int year, int month, int day, int hour, int minute)
    {
        var localTime = new DateTime(year, month, day, hour, minute, 0);

        var result = localTime.LocalToUtc(NewYork);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(localTime, Local(result));
    }
}
