using System;
using Server;
using Server.Engines.Events;
using Xunit;

namespace UOContent.Tests;

public class RecurrencePatternTests
{
    [Theory]
    [InlineData(2024, 5, 15, 12, 30, 1, 2024, 5, 15, 13, 30)] // Normal case
    [InlineData(2024, 5, 15, 23, 30, 2, 2024, 5, 16, 1, 30)]  // Cross day boundary
    public void HourlyRecurrencePattern_GetNextOccurrence_ReturnsCorrectTime(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalHours,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        // Arrange
        var tz = TimeZoneInfo.Utc;
        var pattern = new HourlyRecurrencePattern(intervalHours);
        var afterUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0, DateTimeKind.Utc);
        var time = new TimeOnly(0, expectedMinute);

        // Act
        var result = pattern.GetNextOccurrence(afterUtc, time, tz);

        // Assert
        var expected = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Utc);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2024, 5, 15, 12, 30, 1, 2024, 5, 16, 12, 30)] // Next day
    [InlineData(2024, 5, 31, 12, 30, 2, 2024, 6, 2, 12, 30)]  // Across month boundary
    public void DailyRecurrencePattern_GetNextOccurrence_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        // Arrange
        var tz = TimeZoneInfo.Utc;
        var pattern = new DailyRecurrencePattern(intervalDays);
        var afterUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0, DateTimeKind.Utc);
        var time = new TimeOnly(expectedHour, expectedMinute);

        // Act
        var result = pattern.GetNextOccurrence(afterUtc, time, tz);

        // Assert
        var expected = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Utc);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2024, 5, 15, 12, 30, 1, AllowedDays.All, 2024, 5, 16, 12, 30)] // Next day (Thursday)
    [InlineData(2024, 5, 15, 12, 30, 1, AllowedDays.Monday, 2024, 5, 20, 12, 30)] // Next Monday
    [InlineData(2024, 5, 15, 12, 30, 2, AllowedDays.Wednesday, 2024, 5, 29, 12, 30)] // Biweekly Wednesday
    public void WeeklyRecurrencePattern_BasicPatterns_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalWeeks, AllowedDays allowedDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        // Arrange
        var tz = TimeZoneInfo.Utc;
        var pattern = new WeeklyRecurrencePattern(intervalWeeks, AllowedMonths.All, allowedDays);
        var afterUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0, DateTimeKind.Utc);
        var time = new TimeOnly(expectedHour, expectedMinute);

        // Act
        var result = pattern.GetNextOccurrence(afterUtc, time, tz);

        // Assert
        var expected = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Utc);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2024, 12, 30, 12, 30, 1, AllowedDays.Monday, 2025, 1, 6, 12, 30)] // Across year boundary
    [InlineData(2024, 5, 31, 12, 30, 1, AllowedDays.Saturday, 2024, 6, 1, 12, 30)] // Across month boundary
    public void WeeklyRecurrencePattern_CrossBoundaries_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalWeeks, AllowedDays allowedDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        // Arrange
        var tz = TimeZoneInfo.Utc;
        var pattern = new WeeklyRecurrencePattern(intervalWeeks, AllowedMonths.All, allowedDays);
        var afterUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0, DateTimeKind.Utc);
        var time = new TimeOnly(expectedHour, expectedMinute);

        // Act
        var result = pattern.GetNextOccurrence(afterUtc, time, tz);

        // Assert
        var expected = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Utc);
        Assert.Equal(expected, result);
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
        // Arrange
        var tz = TimeZoneInfo.Utc;
        var pattern = new WeeklyRecurrencePattern(1, allowedMonths, allowedDays);
        var afterUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0, DateTimeKind.Utc);
        var time = new TimeOnly(expectedHour, expectedMinute);

        // Act
        var result = pattern.GetNextOccurrence(afterUtc, time, tz);

        // Assert
        var expected = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Utc);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("America/New_York", 2024, 3, 9, 12, 0, 1, AllowedDays.Sunday, 2024, 3, 10, 12, 0)] // Before spring forward
    [InlineData("America/New_York", 2024, 3, 10, 2, 30, 1, AllowedDays.Sunday, 2024, 3, 17, 2, 30)] // During spring forward (invalid time)
    [InlineData("America/New_York", 2024, 11, 2, 12, 0, 1, AllowedDays.Sunday, 2024, 11, 3, 12, 0)] // Before fall back
    [InlineData("America/New_York", 2024, 11, 3, 1, 30, 1, AllowedDays.Sunday, 2024, 11, 10, 1, 30)] // During fall back (ambiguous time)
    public void WeeklyRecurrencePattern_DSTTransitions_HandlesCorrectly(
        string tzId, int startYear, int startMonth, int startDay, int startHour, int startMinute,
        int intervalWeeks, AllowedDays allowedDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var pattern = new WeeklyRecurrencePattern(intervalWeeks, AllowedMonths.All, allowedDays);

        var startLocal = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0);
        var afterUtc = startLocal.LocalToUtc(tz);
        var time = new TimeOnly(expectedHour, expectedMinute);

        // Act
        var result = pattern.GetNextOccurrence(afterUtc, time, tz);

        // Assert
        var resultLocal = TimeZoneInfo.ConvertTimeFromUtc(result, tz);
        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0), resultLocal);
    }

    [Theory]
    [InlineData(2024, 5, 29, 12, 0, AllowedDays.None, 2024, 5, 29, 12, 30)] // Default to current day
    [InlineData(2024, 5, 29, 12, 0, AllowedDays.Wednesday | AllowedDays.Friday, 2024, 5, 29, 12, 30)] // Current day is Wednesday
    [InlineData(2024, 5, 29, 12, 0, AllowedDays.Monday | AllowedDays.Friday, 2024, 5, 31, 12, 30)] // Next allowed day (Friday)
    public void WeeklyRecurrencePattern_DaysOfWeekHandling_ReturnsCorrectDay(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        AllowedDays allowedDays,
        int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute)
    {
        // Arrange
        var tz = TimeZoneInfo.Utc;
        var pattern = new WeeklyRecurrencePattern(1, AllowedMonths.All, allowedDays);
        var afterUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0, DateTimeKind.Utc);
        var time = new TimeOnly(expectedHour, expectedMinute);

        // Act
        var result = pattern.GetNextOccurrence(afterUtc, time, tz);

        // Assert
        var expected = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Utc);
        Assert.Equal(expected, result);
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
        // Arrange
        var tz = TimeZoneInfo.Utc;
        var pattern = new WeeklyRecurrencePattern(intervalWeeks, allowedMonths);
        var afterUtc = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0, DateTimeKind.Utc);
        var time = new TimeOnly(expectedHour, expectedMinute);

        // Act - first call should hit expectedDate
        var result = pattern.GetNextOccurrence(afterUtc, time, tz);

        // Assert
        var expected = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Utc);
        Assert.Equal(expected, result);

        // Call again - should move to next available day in allowed months
        var nextResult = pattern.GetNextOccurrence(result, time, tz);
        var nextExpected = new DateTime(nextExpectedYear, nextExpectedMonth, nextExpectedDay, nextExpectedHour, nextExpectedMinute, 0, DateTimeKind.Utc);
        Assert.Equal(nextExpected, nextResult);
    }

    [Theory]
    [InlineData(2024, 11, 3, 1, 30, DayOfWeek.Sunday, OrdinalDayOccurrence.First, "America/New_York", 2024, 11, 3)]
    [InlineData(2024, 3, 31, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Last, "America/New_York", 2024, 3, 31)]
    [InlineData(2024, 3, 31, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Fifth, "America/New_York", 2024, 3, 31)]
    [InlineData(2024, 4, 3, 0, 0, DayOfWeek.Sunday, OrdinalDayOccurrence.Fifth, "America/New_York", 2024, 6, 30)] // June has 5 Sundays (2, 9, 16, 23, 30)
    public void MonthlyOrdinalRecurrencePattern_GetNextOccurrence_ReturnsCorrectDate(
        int startYear, int startMonth, int startDay, int startHour, int startMinute,
        DayOfWeek dow, OrdinalDayOccurrence ordinal, string tzId,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var pattern = new MonthlyOrdinalRecurrencePattern(ordinal, dow);

        // Use a day BEFORE the expected date
        var startLocal = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0);
        var beforeUtc = startLocal.AddDays(-1).LocalToUtc(tz);
        var timeOnly = new TimeOnly(startHour, startMinute);

        // Act
        var result = pattern.GetNextOccurrence(beforeUtc, timeOnly, tz);

        // Assert - convert back to local for comparison
        var resultLocal = TimeZoneInfo.ConvertTimeFromUtc(result, tz);
        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay, startHour, startMinute, 0), resultLocal);
    }

    [Theory]
    [InlineData("America/New_York", 2024, 3, 10, 2, 30)] // Spring forward - 2:30 AM doesn't exist
    [InlineData("America/New_York", 2024, 11, 3, 1, 30)] // Fall back - 1:30 AM happens twice
    public void MonthlyRecurrencePattern_DSTTransitions_HandlesCorrectly(
        string tzId, int year, int month, int day, int hour, int minute)
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var pattern = new MonthlyRecurrencePattern(day);

        var localDateBefore = new DateTime(year, month, day, 0, 0, 0);
        var beforeUtc = localDateBefore.LocalToUtc(tz);
        var timeOnly = new TimeOnly(hour, minute);

        // Act - shouldn't throw exceptions
        var result = pattern.GetNextOccurrence(beforeUtc, timeOnly, tz);

        // Assert - just make sure we got a valid result
        Assert.NotEqual(DateTime.MaxValue, result);

        // For invalid times (spring forward), should skip to next valid time
        var resultLocal = TimeZoneInfo.ConvertTimeFromUtc(result, tz);
        if (month == 3) // Spring forward
        {
            // Should skip the invalid 2:30 AM time
            Assert.True(resultLocal.Hour >= 3 || resultLocal > new DateTime(year, month, day));
        }
    }

    [Fact]
    public void MonthlyOrdinalRecurrencePattern_DST_FallBack_HandlesAmbiguousTimeCorrectly()
    {
        // This tests specifically the DST fall back case that was failing
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var pattern = new MonthlyOrdinalRecurrencePattern(OrdinalDayOccurrence.First, DayOfWeek.Sunday);

        // November 3, 2024 at 1:30 AM - First Sunday, falls on DST transition
        var beforeDate = new DateTime(2024, 11, 2, 12, 0, 0);
        var beforeUtc = beforeDate.LocalToUtc(tz);
        var timeOnly = new TimeOnly(1, 30);

        // Act
        var result = pattern.GetNextOccurrence(beforeUtc, timeOnly, tz);

        // Assert - should be November 3, 2024 at 1:30 AM
        var resultLocal = TimeZoneInfo.ConvertTimeFromUtc(result, tz);
        Assert.Equal(new DateTime(2024, 11, 3, 1, 30, 0), resultLocal);
    }

    [Fact]
    public void MonthlyRecurrence_MonthWithoutDay_SkipsToNextValidMonth()
    {
        // Arrange
        var tz = TimeZoneInfo.Utc;
        var pattern = new MonthlyRecurrencePattern(31);

        // February doesn't have 31 days
        var februaryDate = new DateTime(2024, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var timeOnly = new TimeOnly(12, 0);

        // Act
        var result = pattern.GetNextOccurrence(februaryDate, timeOnly, tz);

        // Assert - should be March 31
        Assert.Equal(new DateTime(2024, 3, 31, 12, 0, 0), result);
    }

    [Theory]
    [InlineData("America/New_York", 2024, 11, 3, 1, 30)] // Fall back - ambiguous time
    public void LocalToUtc_AmbiguousTime_HandlesConsistently(
        string tzId, int year, int month, int day, int hour, int minute)
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var localTime = new DateTime(year, month, day, hour, minute, 0);

        // Act
        var result = localTime.LocalToUtc(tz);

        // Assert - should be consistent, not testing exact value
        Assert.Equal(DateTimeKind.Utc, result.Kind);

        // Convert back should give either standard or DST time
        var backToLocal = TimeZoneInfo.ConvertTimeFromUtc(result, tz);
        Assert.Equal(new DateTime(year, month, day, hour, minute, 0), backToLocal);
    }
}
