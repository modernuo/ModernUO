using System;
using System.Collections.Generic;
using System.IO;
using Server.Saves;
using Xunit;

namespace Server.Tests;

[Collection("Sequential UOContent Tests")]
public class ArchiveJournalTests : IDisposable
{
    private readonly string _testDir;

    public ArchiveJournalTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"modernuo-journal-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void Configure_LoadsCleanState()
    {
        // Arrange & Act
        ArchiveJournal.Configure(_testDir);

        // Assert - fresh directory should have no operations
        // Note: ArchiveJournal is static, so we check the journal file doesn't exist
        var journalPath = Path.Combine(_testDir, ".archive-journal.json");
        Assert.False(File.Exists(journalPath));
    }

    [Fact]
    public void BeginOperation_CreatesStartedEntry()
    {
        // Arrange
        ArchiveJournal.Configure(_testDir);

        // Act
        var entry = ArchiveJournal.BeginOperation(
            ArchivePeriod.Hourly,
            new DateTime(2026, 3, 22, 14, 0, 0, DateTimeKind.Utc),
            "/tmp/archive.tar.zst.tmp",
            "/archives/hourly/2026-03-22-14.tar.zst",
            ["backup1", "backup2"]
        );

        // Assert
        Assert.Equal(ArchiveOperationState.Started, entry.State);
        Assert.Equal(ArchivePeriod.Hourly, entry.Period);
        Assert.Equal("/tmp/archive.tar.zst.tmp", entry.TempFile);
        Assert.Equal(2, entry.SourceDirectories.Count);
        Assert.Contains(entry, ArchiveJournal.Operations);
    }

    [Fact]
    public void RecordArchived_TransitionsState()
    {
        // Arrange
        ArchiveJournal.Configure(_testDir);
        var entry = ArchiveJournal.BeginOperation(
            ArchivePeriod.Daily,
            new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc),
            "/tmp/test.tmp",
            "/archives/daily/2026-03-22.tar.zst",
            ["backup1"]
        );

        // Act
        ArchiveJournal.RecordArchived(entry, 42, 1024000);

        // Assert
        Assert.Equal(ArchiveOperationState.Archived, entry.State);
        Assert.Equal(42, entry.EntryCount);
        Assert.Equal(1024000, entry.ArchiveBytes);
        Assert.Null(entry.TempFile); // Temp file cleared after rename
    }

    [Fact]
    public void RecordDistributed_StoresResults()
    {
        // Arrange
        ArchiveJournal.Configure(_testDir);
        var entry = ArchiveJournal.BeginOperation(
            ArchivePeriod.Monthly,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            "/tmp/test.tmp",
            "/archives/monthly/2026-03.tar.zst",
            ["backup1"]
        );
        ArchiveJournal.RecordArchived(entry, 100, 5000000);

        var results = new Dictionary<string, bool>
        {
            { "Local Filesystem", true },
            { "S3 us-east-1", false }
        };

        // Act
        ArchiveJournal.RecordDistributed(entry, results);

        // Assert
        Assert.Equal(ArchiveOperationState.Distributed, entry.State);
        Assert.True(entry.DestinationResults["Local Filesystem"]);
        Assert.False(entry.DestinationResults["S3 us-east-1"]);
    }

    [Fact]
    public void RecordCompleted_IsTerminal()
    {
        // Arrange
        ArchiveJournal.Configure(_testDir);
        var entry = ArchiveJournal.BeginOperation(
            ArchivePeriod.Hourly,
            new DateTime(2026, 3, 22, 14, 0, 0, DateTimeKind.Utc),
            "/tmp/test.tmp",
            "/archives/hourly/test.tar.zst",
            ["backup1"]
        );
        ArchiveJournal.RecordArchived(entry, 10, 500);
        ArchiveJournal.RecordDistributed(entry, new Dictionary<string, bool>());

        // Act
        ArchiveJournal.RecordCompleted(entry);

        // Assert
        Assert.Equal(ArchiveOperationState.Completed, entry.State);
        Assert.NotNull(entry.CompletedAt);
    }

    [Fact]
    public void RecordFailure_RecordsReason()
    {
        // Arrange
        ArchiveJournal.Configure(_testDir);
        var entry = ArchiveJournal.BeginOperation(
            ArchivePeriod.Hourly,
            new DateTime(2026, 3, 22, 14, 0, 0, DateTimeKind.Utc),
            "/tmp/test.tmp",
            "/archives/hourly/test.tar.zst",
            ["backup1"]
        );

        // Act
        ArchiveJournal.RecordFailure(entry, "Disk full");

        // Assert
        Assert.Equal(ArchiveOperationState.Failed, entry.State);
        Assert.Equal("Disk full", entry.FailureReason);
        Assert.NotNull(entry.CompletedAt);
    }

    [Fact]
    public void Journal_PersistsToJsonFile()
    {
        // Arrange
        ArchiveJournal.Configure(_testDir);
        ArchiveJournal.BeginOperation(
            ArchivePeriod.Hourly,
            new DateTime(2026, 3, 22, 14, 0, 0, DateTimeKind.Utc),
            "/tmp/test.tmp",
            "/archives/hourly/test.tar.zst",
            ["backup1"]
        );

        // Assert - journal file should exist
        var journalPath = Path.Combine(_testDir, ".archive-journal.json");
        Assert.True(File.Exists(journalPath));

        var content = File.ReadAllText(journalPath);
        Assert.Contains("Hourly", content);
        Assert.Contains("Started", content);
    }

    [Fact]
    public void RecoverInterrupted_CleansUpStartedOperations()
    {
        // Arrange
        ArchiveJournal.Configure(_testDir);

        // Create a temp file that simulates an interrupted archive
        var tempFile = Path.Combine(_testDir, "interrupted.tar.zst.tmp");
        File.WriteAllText(tempFile, "partial archive data");

        var entry = ArchiveJournal.BeginOperation(
            ArchivePeriod.Hourly,
            new DateTime(2026, 3, 22, 14, 0, 0, DateTimeKind.Utc),
            tempFile,
            Path.Combine(_testDir, "final.tar.zst"),
            ["backup1"]
        );

        // Act
        ArchiveJournal.RecoverInterrupted();

        // Assert
        Assert.Equal(ArchiveOperationState.Failed, entry.State);
        Assert.Equal("Interrupted during archive creation", entry.FailureReason);
        Assert.False(File.Exists(tempFile)); // Temp file should be cleaned up
    }

    [Fact]
    public void RecoverInterrupted_CompletesDistributedOperations()
    {
        // Arrange
        ArchiveJournal.Configure(_testDir);
        var entry = ArchiveJournal.BeginOperation(
            ArchivePeriod.Daily,
            new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc),
            "/tmp/test.tmp",
            "/archives/daily/test.tar.zst",
            ["backup1"]
        );
        ArchiveJournal.RecordArchived(entry, 50, 2000);
        ArchiveJournal.RecordDistributed(entry, new Dictionary<string, bool> { { "Local", true } });

        // Act - simulate restart
        ArchiveJournal.RecoverInterrupted();

        // Assert - should be marked completed
        Assert.Equal(ArchiveOperationState.Completed, entry.State);
    }
}
