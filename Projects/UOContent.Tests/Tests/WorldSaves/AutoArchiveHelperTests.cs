using System;
using Server.Saves;
using Xunit;

namespace Server.Tests;

[Collection("Sequential UOContent Tests")]
public class AutoArchiveHelperTests
{
    [Fact]
    public void ArchiveDestinationRegistry_RegisterAndRetrieve()
    {
        // Arrange
        var dest = new LocalArchiveDestination(24, 30, 12);

        // Act
        ArchiveDestinationRegistry.Register(dest);

        // Assert
        Assert.Contains(dest, ArchiveDestinationRegistry.Destinations);

        // Cleanup
        ArchiveDestinationRegistry.Unregister(dest);
        Assert.DoesNotContain(dest, ArchiveDestinationRegistry.Destinations);
    }

    [Fact]
    public void LocalArchiveDestination_SendArchive_ReturnsTrue()
    {
        // Arrange
        var dest = new LocalArchiveDestination(24, 30, 12);

        // Act
        var result = dest.SendArchive("/fake/path.tar.zst", ArchivePeriod.Hourly, DateTime.UtcNow);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(ArchivePeriod.Hourly, 24)]
    [InlineData(ArchivePeriod.Daily, 30)]
    [InlineData(ArchivePeriod.Monthly, 12)]
    public void LocalArchiveDestination_RetentionCounts(ArchivePeriod period, int expected)
    {
        // Arrange
        var dest = new LocalArchiveDestination(24, 30, 12);

        // Act & Assert
        Assert.Equal(expected, dest.GetRetentionCount(period));
    }

    [Fact]
    public void ArchiveCompletedEventArgs_StoresValues()
    {
        // Arrange & Act
        var args = new ArchiveCompletedEventArgs(
            "/archives/hourly/test.tar.zst",
            ArchivePeriod.Hourly,
            new DateTime(2026, 3, 22, 14, 0, 0, DateTimeKind.Utc),
            1048576,
            2.5
        );

        // Assert
        Assert.Equal("/archives/hourly/test.tar.zst", args.ArchiveFilePath);
        Assert.Equal(ArchivePeriod.Hourly, args.Period);
        Assert.Equal(1048576, args.FileSizeBytes);
        Assert.Equal(2.5, args.ElapsedSeconds);
    }

    [Fact]
    public void ArchiveFailedEventArgs_StoresException()
    {
        // Arrange
        var ex = new InvalidOperationException("disk full");

        // Act
        var args = new ArchiveFailedEventArgs(
            ArchivePeriod.Daily,
            new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc),
            ex
        );

        // Assert
        Assert.Equal(ArchivePeriod.Daily, args.Period);
        Assert.Same(ex, args.Exception);
    }

    [Fact]
    public void ArchiveJournalEntry_DefaultState()
    {
        // Arrange & Act
        var entry = new ArchiveJournalEntry();

        // Assert
        Assert.NotNull(entry.SourceDirectories);
        Assert.Empty(entry.SourceDirectories);
        Assert.NotNull(entry.DestinationResults);
        Assert.Empty(entry.DestinationResults);
        Assert.Null(entry.FailureReason);
        Assert.Equal(DateTime.MaxValue, entry.CompletedAt);
    }
}
