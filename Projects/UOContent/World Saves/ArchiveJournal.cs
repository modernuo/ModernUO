/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArchiveJournal.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.Saves;

public enum ArchiveOperationState
{
    Started,
    Archived,
    Distributed,
    Completed,
    Failed
}

public class ArchiveJournalEntry
{
    public string Id { get; set; }
    public ArchivePeriod Period { get; set; }
    public DateTime RangeStart { get; set; }
    public ArchiveOperationState State { get; set; }
    public string ArchiveFile { get; set; }
    public string TempFile { get; set; }
    public List<string> SourceDirectories { get; set; } = [];
    public int EntryCount { get; set; }
    public long TotalSourceBytes { get; set; }
    public long ArchiveBytes { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string FailureReason { get; set; }
    public Dictionary<string, bool> DestinationResults { get; set; } = new();
}

public class ArchiveJournalData
{
    public List<ArchiveJournalEntry> Operations { get; set; } = [];
}

public static class ArchiveJournal
{
    private const int MaxJournalEntries = 100;

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ArchiveJournal));

    private static string _journalPath;
    private static ArchiveJournalData _data = new();

    public static IReadOnlyList<ArchiveJournalEntry> Operations => _data.Operations;

    public static void Configure(string archivePath)
    {
        _journalPath = Path.Combine(archivePath, ".archive-journal.json");
        Load();
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(_journalPath))
            {
                _data = JsonConfig.Deserialize<ArchiveJournalData>(_journalPath) ?? new ArchiveJournalData();
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load archive journal, starting fresh");
            _data = new ArchiveJournalData();
        }
    }

    private static void Save()
    {
        try
        {
            var tempPath = _journalPath + ".tmp";
            JsonConfig.Serialize(tempPath, _data);

            if (File.Exists(_journalPath))
            {
                File.Delete(_journalPath);
            }

            File.Move(tempPath, _journalPath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save archive journal");
        }
    }

    private static void Prune()
    {
        if (_data.Operations.Count > MaxJournalEntries)
        {
            // Remove oldest completed/failed entries beyond the limit
            var toRemove = _data.Operations.Count - MaxJournalEntries;
            for (var i = _data.Operations.Count - 1; i >= 0 && toRemove > 0; i--)
            {
                if (_data.Operations[i].State is ArchiveOperationState.Completed or ArchiveOperationState.Failed)
                {
                    _data.Operations.RemoveAt(i);
                    toRemove--;
                }
            }
        }
    }

    public static ArchiveJournalEntry BeginOperation(
        ArchivePeriod period,
        DateTime rangeStart,
        string tempFile,
        string archiveFile,
        List<string> sourceDirectories)
    {
        var entry = new ArchiveJournalEntry
        {
            Id = $"{rangeStart:O}-{period.ToString().ToLowerInvariant()}",
            Period = period,
            RangeStart = rangeStart,
            State = ArchiveOperationState.Started,
            TempFile = tempFile,
            ArchiveFile = archiveFile,
            SourceDirectories = sourceDirectories,
            StartedAt = Core.Now
        };

        _data.Operations.Add(entry);
        Prune();
        Save();
        return entry;
    }

    public static void RecordArchived(ArchiveJournalEntry entry, int entryCount, long archiveBytes)
    {
        entry.State = ArchiveOperationState.Archived;
        entry.EntryCount = entryCount;
        entry.ArchiveBytes = archiveBytes;
        entry.TempFile = null; // Temp file has been renamed to final
        Save();
    }

    public static void RecordDistributed(ArchiveJournalEntry entry, Dictionary<string, bool> results)
    {
        entry.State = ArchiveOperationState.Distributed;
        entry.DestinationResults = results;
        Save();
    }

    public static void RecordCompleted(ArchiveJournalEntry entry)
    {
        entry.State = ArchiveOperationState.Completed;
        entry.CompletedAt = Core.Now;
        Save();
    }

    public static void RecordFailure(ArchiveJournalEntry entry, string reason)
    {
        entry.State = ArchiveOperationState.Failed;
        entry.FailureReason = reason;
        entry.CompletedAt = Core.Now;
        Save();
    }

    /// <summary>
    /// Recovers from interrupted operations on startup.
    /// </summary>
    public static void RecoverInterrupted()
    {
        var recovered = false;

        foreach (var entry in _data.Operations)
        {
            switch (entry.State)
            {
                case ArchiveOperationState.Started:
                    {
                        // Archive creation was interrupted. Clean up temp file.
                        if (entry.TempFile != null && File.Exists(entry.TempFile))
                        {
                            try
                            {
                                File.Delete(entry.TempFile);
                            }
                            catch (Exception ex)
                            {
                                logger.Warning(ex, "Failed to clean up temp file {File}", entry.TempFile);
                            }
                        }

                        logger.Warning(
                            "Recovered interrupted archive operation {Id} (was in Started state)",
                            entry.Id
                        );
                        entry.State = ArchiveOperationState.Failed;
                        entry.FailureReason = "Interrupted during archive creation";
                        entry.CompletedAt = Core.Now;
                        recovered = true;
                        break;
                    }
                case ArchiveOperationState.Archived:
                    {
                        // Archive was created but not distributed. The archive file exists.
                        // Distribution will be re-attempted on next rollup cycle.
                        logger.Warning(
                            "Found unfinished archive operation {Id} (Archived but not distributed). " +
                            "Will re-attempt distribution on next cycle.",
                            entry.Id
                        );
                        break;
                    }
                case ArchiveOperationState.Distributed:
                    {
                        // Sources should have been pruned but weren't.
                        logger.Information(
                            "Completing interrupted operation {Id} (was Distributed, pruning sources)",
                            entry.Id
                        );
                        entry.State = ArchiveOperationState.Completed;
                        entry.CompletedAt = Core.Now;
                        recovered = true;
                        break;
                    }
            }
        }

        if (recovered)
        {
            Save();
        }
    }
}
