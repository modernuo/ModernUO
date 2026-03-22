/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AutoArchive.cs                                                  *
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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using CommunityToolkit.HighPerformance;
using Server.Compression;
using Server.Logging;

namespace Server.Saves;

public static class AutoArchive
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AutoArchive));

    private static string _tempArchivePath;
    private static int _isArchiving;
    private static DateTime _nextHourlyArchive;
    private static DateTime _nextDailyArchive;
    private static DateTime _nextMonthlyArchive;
    private static bool _enablePruning;
    private static int _compressionLevel;
    private static bool _verifyArchives;
    private static int _retryCount;
    private static int _retryDelayMs;
    private static int _backupMaxAgeDays;

    public static event Action<ArchiveCompletedEventArgs> ArchiveCompleted;
    public static event Action<ArchiveFailedEventArgs> ArchiveFailed;

    public static Action Archive { get; set; }
    public static Action Prune { get; set; }
    public static string ArchivePath { get; private set; }
    public static string BackupPath { get; private set; }
    public static string AutomaticBackupPath { get; private set; }

    public static DateTime NextHourlyArchive => _nextHourlyArchive;
    public static DateTime NextDailyArchive => _nextDailyArchive;
    public static DateTime NextMonthlyArchive => _nextMonthlyArchive;

    public static void Configure()
    {
        var tempArchivePath = ServerConfiguration.GetSetting("autoArchive.tempArchivePath", "temp");
        _tempArchivePath = PathUtility.GetFullPath(tempArchivePath);

        var backupPath = ServerConfiguration.GetOrUpdateSetting("autoArchive.backupPath", "Backups");
        BackupPath = PathUtility.GetFullPath(backupPath);
        AutomaticBackupPath = Path.Combine(BackupPath, "Automatic");

        var archivePath = ServerConfiguration.GetOrUpdateSetting("autoArchive.archivePath", "Archives");
        ArchivePath = PathUtility.GetFullPath(archivePath);

        var useLocalArchives = ServerConfiguration.GetOrUpdateSetting("autoArchive.archiveLocally", true);
        _enablePruning = ServerConfiguration.GetOrUpdateSetting("autoArchive.enableArchivePruning", true);

        _compressionLevel = ServerConfiguration.GetOrUpdateSetting("autoArchive.compressionLevel", 3);
        _verifyArchives = ServerConfiguration.GetOrUpdateSetting("autoArchive.verifyArchives", true);
        _retryCount = ServerConfiguration.GetOrUpdateSetting("autoArchive.retryCount", 3);
        _retryDelayMs = ServerConfiguration.GetOrUpdateSetting("autoArchive.retryDelayMs", 500);
        _backupMaxAgeDays = ServerConfiguration.GetOrUpdateSetting("autoArchive.backupMaxAge", 30);

        // Configurable retention
        var hourlyRetention = ServerConfiguration.GetOrUpdateSetting("autoArchive.hourlyRetention", 24);
        var dailyRetention = ServerConfiguration.GetOrUpdateSetting("autoArchive.dailyRetention", 30);
        var monthlyRetention = ServerConfiguration.GetOrUpdateSetting("autoArchive.monthlyRetention", 12);

        // Register local filesystem destination
        if (useLocalArchives)
        {
            ArchiveDestinationRegistry.Register(
                new LocalArchiveDestination(hourlyRetention, dailyRetention, monthlyRetention)
            );
            Archive = AutoArchiveLocally;
        }

        if (_enablePruning)
        {
            Prune = PruneBackups;
        }

        // Initialize journal and recover interrupted operations
        ArchiveJournal.Configure(ArchivePath);
        ArchiveJournal.RecoverInterrupted();

        // Restores an archive file placed in the Saves folder
        RestoreFromArchive();

        // Check for legacy bsdtar directory
        var bsdtarPath = Path.Combine(Core.BaseDirectory, "bsdtar");
        if (Directory.Exists(bsdtarPath))
        {
            logger.Information("Legacy bsdtar directory found. No longer needed and can be safely deleted.");
        }

        DateTimeOffset now = Core.Now;
        var date = now.Date;
        _nextHourlyArchive = date.AddHours(now.Hour);
        _nextDailyArchive = date;
        _nextMonthlyArchive = date.AddDays(1 - now.Day);

        // Do a local archive rollup & prune
        AutoArchiveLocally();
    }

    public static void Initialize()
    {
        EventSink.WorldSavePostSnapshot += Backup;
    }

    private static void Backup(WorldSavePostSnapshotEventArgs args)
    {
        if (!Directory.Exists(args.OldSavePath))
        {
            return;
        }

        Directory.CreateDirectory(AutomaticBackupPath);
        var backupPath = Path.Combine(AutomaticBackupPath, Utility.GetTimeStamp());
        PathUtility.MoveDirectoryContents(args.OldSavePath, backupPath);

        logger.Information("Created backup at {Path}", backupPath);

        Archive?.Invoke();
    }

    private static void RestoreFromArchive()
    {
        var savePath = Path.Combine(Core.BaseDirectory, ServerConfiguration.GetSetting("world.savePath", "Saves"));
        if (!Directory.Exists(savePath))
        {
            return;
        }

        foreach (var file in PathsByTimestampName(savePath, true))
        {
            if (RestoreFromFile(file, savePath))
            {
                File.Delete(file);
                break;
            }
        }
    }

    private static bool RestoreFromFile(string file, string savePath)
    {
        var fi = new FileInfo(file);
        var fileName = fi.Name;

        if (!TryGetDate(fileName[..fileName.IndexOfOrdinal(".")], out _))
        {
            return false;
        }

        if (!fileName.EndsWithOrdinal(".tar.zst"))
        {
            logger.Warning("Unsupported archive format: {File}. Only .tar.zst is supported.", fileName);
            return false;
        }

        logger.Information("Restoring latest world save from archive {File}", fileName);

        var tempPath = PathUtility.EnsureRandomPath(_tempArchivePath);
        var successful = ManagedArchive.ExtractTarZstd(fi.FullName, tempPath);

        if (!successful)
        {
            logger.Warning("Failed to extract {File}", fi.Name);
            return false;
        }

        foreach (var folder in PathsByTimestampName(tempPath))
        {
            if (Directory.Exists(savePath))
            {
                Directory.Delete(savePath, true);
            }

            var dirInfo = new DirectoryInfo(folder);
            logger.Information("Restoring backup {Directory}", dirInfo.Name);
            PathUtility.MoveDirectoryContents(folder, savePath);
            break;
        }

        try
        {
            Directory.Delete(tempPath, true);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to clean up temp directory {Path}", tempPath);
        }

        return true;
    }

    public static void PruneBackups()
    {
        if (Directory.Exists(ArchivePath))
        {
            // Get retention counts from registered destinations
            var hourlyRetention = GetMaxRetention(ArchivePeriod.Hourly);
            var dailyRetention = GetMaxRetention(ArchivePeriod.Daily);
            var monthlyRetention = GetMaxRetention(ArchivePeriod.Monthly);

            PruneLocalArchives(ArchivePeriod.Hourly, hourlyRetention);
            PruneLocalArchives(ArchivePeriod.Daily, dailyRetention);
            PruneLocalArchives(ArchivePeriod.Monthly, monthlyRetention);
        }

        if (!Directory.Exists(AutomaticBackupPath))
        {
            return;
        }

        var allFolders = Directory.EnumerateDirectories(AutomaticBackupPath);
        var threshold = Core.Now.AddDays(-_backupMaxAgeDays);

        foreach (var folder in allFolders)
        {
            var dirName = new DirectoryInfo(folder).Name;

            if (!TryGetDate(dirName, out var date))
            {
                continue;
            }

            if (date < threshold)
            {
                logger.Information("Pruning old backup {Directory}", folder);
                RetryFileOperation(() => Directory.Delete(folder, true));
            }
        }
    }

    private static int GetMaxRetention(ArchivePeriod period)
    {
        var max = 0;
        foreach (var dest in ArchiveDestinationRegistry.Destinations)
        {
            var count = dest.GetRetentionCount(period);
            if (count > max)
            {
                max = count;
            }
        }

        return max;
    }

    private static void PruneLocalArchives(ArchivePeriod period, int minRetained)
    {
        var periodStr = period.ToString();
        var archivePath = Path.Combine(ArchivePath, periodStr);
        if (!Directory.Exists(archivePath))
        {
            return;
        }

        var periodLowerStr = periodStr.ToLowerInvariant();
        foreach (var archive in PathsByTimestampName(archivePath, true))
        {
            if (minRetained > 0)
            {
                minRetained--;
                continue;
            }

            var fi = new FileInfo(archive);
            logger.Information("Pruning {Period} archive {File}", periodLowerStr, fi.Name);
            RetryFileOperation(() => File.Delete(archive));
        }
    }

    public static void AutoArchiveLocally()
    {
        var date = Core.Now;

        if (date < _nextHourlyArchive && date < _nextDailyArchive && date < _nextMonthlyArchive)
        {
            return;
        }

        // Fixed: was CompareExchange(ref _isArchiving, 0, 1) which never guarded
        if (Interlocked.CompareExchange(ref _isArchiving, 1, 0) == 1)
        {
            return;
        }

        ThreadPool.QueueUserWorkItem(
            now =>
            {
                try
                {
                    if (now >= _nextHourlyArchive)
                    {
                        Rollup(ArchivePeriod.Hourly);
                        _nextHourlyArchive = _nextHourlyArchive.AddHours(1);
                    }

                    if (now >= _nextDailyArchive)
                    {
                        Rollup(ArchivePeriod.Daily);
                        _nextDailyArchive = _nextDailyArchive.AddDays(1);
                    }

                    if (now >= _nextMonthlyArchive)
                    {
                        Rollup(ArchivePeriod.Monthly);
                        _nextMonthlyArchive = _nextMonthlyArchive.AddMonths(1);
                    }

                    Prune?.Invoke();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Archive operation failed");
                }
                finally
                {
                    _isArchiving = 0;
                }
            },
            date,
            false
        );
    }

    /// <summary>
    /// Forces an immediate rollup regardless of schedule.
    /// </summary>
    public static void ForceRollup()
    {
        if (Interlocked.CompareExchange(ref _isArchiving, 1, 0) == 1)
        {
            logger.Warning("Cannot force rollup — an archive operation is already in progress");
            return;
        }

        ThreadPool.QueueUserWorkItem(
            _ =>
            {
                try
                {
                    Rollup(ArchivePeriod.Hourly);
                    Rollup(ArchivePeriod.Daily);
                    Rollup(ArchivePeriod.Monthly);
                    Prune?.Invoke();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Forced archive operation failed");
                }
                finally
                {
                    _isArchiving = 0;
                }
            }
        );
    }

    private static void Rollup(ArchivePeriod archivePeriod)
    {
        if (!Directory.Exists(AutomaticBackupPath))
        {
            return;
        }

        var currentRangeStart = Core.Now.ArchivePeriodStart(archivePeriod);
        var allFolders = Directory.EnumerateDirectories(AutomaticBackupPath);

        var items = new Dictionary<DateTime, SortedDictionary<DateTime, string>>();
        foreach (var path in allFolders)
        {
            var dirName = new DirectoryInfo(path).Name;

            if (!TryGetDate(dirName, out var date))
            {
                continue;
            }

            var rangeStart = date.ArchivePeriodStart(archivePeriod);

            // Only archive the past
            if (rangeStart >= currentRangeStart)
            {
                continue;
            }

            if (items.TryGetValue(rangeStart, out var value))
            {
                value.Add(date, path);
            }
            else
            {
                value = new SortedDictionary<DateTime, string>(new DescendingComparer<DateTime>())
                {
                    { date, path }
                };

                items.Add(rangeStart, value);
            }
        }

        var archivePeriodStr = archivePeriod.ToString();
        var archivePath = PathUtility.EnsureDirectory(Path.Combine(ArchivePath, archivePeriodStr));
        var archivePeriodStrLower = archivePeriodStr.ToLowerInvariant();

        // Leave behind 1 hourly for daily, 1 daily for monthly.
        var minimum = archivePeriod != ArchivePeriod.Monthly ? 1 : 0;

        foreach (var (rangeStart, sortedBackups) in items)
        {
            var backups = sortedBackups.Values;
            if (backups.Count <= minimum)
            {
                continue;
            }

            var stopWatch = Stopwatch.StartNew();

            var fileName = $"{rangeStart.ToTimeStamp(archivePeriod)}.tar.zst";
            var archiveFilePath = Path.Combine(archivePath, fileName);

            // Journal: record start
            var sourceList = new List<string>(backups);
            var journalEntry = ArchiveJournal.BeginOperation(
                archivePeriod, rangeStart, archiveFilePath + ".tmp", archiveFilePath, sourceList
            );

            // Create archive using managed streaming compression
            var entryCount = ManagedArchive.CreateTarZstd(
                backups, archiveFilePath, AutomaticBackupPath, _compressionLevel
            );

            if (entryCount >= 0)
            {
                // Verify if enabled
                if (_verifyArchives)
                {
                    var verifiedCount = ManagedArchive.CountEntries(archiveFilePath);
                    if (verifiedCount != entryCount)
                    {
                        logger.Warning(
                            "Archive verification failed for {Path}: expected {Expected} entries, got {Actual}",
                            archiveFilePath, entryCount, verifiedCount
                        );
                        ArchiveJournal.RecordFailure(journalEntry, $"Verification failed: expected {entryCount}, got {verifiedCount}");

                        ArchiveFailed?.Invoke(new ArchiveFailedEventArgs(
                            archivePeriod, rangeStart, new InvalidOperationException("Archive verification failed")
                        ));
                        continue;
                    }
                }

                var archiveSize = new FileInfo(archiveFilePath).Length;
                ArchiveJournal.RecordArchived(journalEntry, entryCount, archiveSize);

                stopWatch.Stop();
                logger.Information(
                    "Created {Period} archive at {Path} ({EntryCount} entries, {Size:F1} MB, {Elapsed:F2}s)",
                    archivePeriodStrLower,
                    archiveFilePath,
                    entryCount,
                    archiveSize / 1048576.0,
                    stopWatch.Elapsed.TotalSeconds
                );

                // Distribute to registered destinations
                var allDestinationsSucceeded = DistributeToDestinations(
                    archiveFilePath, archivePeriod, rangeStart, journalEntry
                );

                // Only delete source backups if all destinations with retention succeeded
                if (allDestinationsSucceeded)
                {
                    var i = minimum;
                    foreach (var backup in backups)
                    {
                        // Keep the latest one, but delete the rest.
                        if (i-- > 0)
                        {
                            continue;
                        }

                        RetryFileOperation(() => Directory.Delete(backup, true));
                    }

                    ArchiveJournal.RecordCompleted(journalEntry);
                }
                else
                {
                    logger.Warning(
                        "Not pruning source backups for {Period} archive — some destinations failed",
                        archivePeriodStrLower
                    );
                }

                ArchiveCompleted?.Invoke(new ArchiveCompletedEventArgs(
                    archiveFilePath, archivePeriod, rangeStart, archiveSize, stopWatch.Elapsed.TotalSeconds
                ));
            }
            else
            {
                stopWatch.Stop();
                logger.Warning("Failed to create {Period} archive", archivePeriodStrLower);
                ArchiveJournal.RecordFailure(journalEntry, "Archive creation failed");

                ArchiveFailed?.Invoke(new ArchiveFailedEventArgs(
                    archivePeriod, rangeStart, new InvalidOperationException("Archive creation failed")
                ));
            }
        }
    }

    private static bool DistributeToDestinations(
        string archiveFilePath,
        ArchivePeriod period,
        DateTime rangeStart,
        ArchiveJournalEntry journalEntry)
    {
        var destinations = ArchiveDestinationRegistry.Destinations;
        if (destinations.Count == 0)
        {
            ArchiveJournal.RecordDistributed(journalEntry, new Dictionary<string, bool>());
            return true;
        }

        var results = new Dictionary<string, bool>(destinations.Count);
        var allSucceeded = true;

        foreach (var dest in destinations)
        {
            try
            {
                var success = dest.SendArchive(archiveFilePath, period, rangeStart);
                results[dest.Name] = success;

                if (success)
                {
                    logger.Debug("Archive sent to destination {Destination}", dest.Name);
                }
                else
                {
                    logger.Warning("Destination {Destination} rejected archive", dest.Name);
                    if (dest.GetRetentionCount(period) > 0)
                    {
                        allSucceeded = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to send archive to destination {Destination}", dest.Name);
                results[dest.Name] = false;
                if (dest.GetRetentionCount(period) > 0)
                {
                    allSucceeded = false;
                }
            }
        }

        ArchiveJournal.RecordDistributed(journalEntry, results);
        return allSucceeded;
    }

    private static void RetryFileOperation(Action operation)
    {
        for (var attempt = 0; attempt < _retryCount; attempt++)
        {
            try
            {
                operation();
                return;
            }
            catch (Exception ex) when (attempt < _retryCount - 1 && ex is IOException or UnauthorizedAccessException)
            {
                logger.Debug(
                    ex,
                    "File operation failed on attempt {Attempt}/{Max}, retrying",
                    attempt + 1,
                    _retryCount
                );
                Thread.Sleep(_retryDelayMs * (attempt + 1));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ToTimeStamp(this DateTime date, ArchivePeriod archivePeriod) =>
        archivePeriod switch
        {
            ArchivePeriod.Monthly => date.ToString("yyyy-MM"),
            ArchivePeriod.Daily   => date.ToString("yyyy-MM-dd"),
            ArchivePeriod.Hourly  => date.ToString("yyyy-MM-dd-HH"),
            _                     => date.ToTimeStamp()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTime ArchivePeriodStart(this DateTime date, ArchivePeriod archivePeriod) =>
        archivePeriod switch
        {
            ArchivePeriod.Monthly => date.Date.AddDays(-(date.Day - 1)),
            ArchivePeriod.Daily   => date.Date,
            ArchivePeriod.Hourly  => date.Date.AddHours(date.Hour),
            _                     => date
        };

    internal static SortedDictionary<DateTime, string>.ValueCollection PathsByTimestampName(
        string path, bool files = false)
    {
        var allItems = files ? Directory.EnumerateFiles(path) : Directory.GetDirectories(path);
        var items = new SortedDictionary<DateTime, string>(new DescendingComparer<DateTime>());
        foreach (var item in allItems)
        {
            string name;
            if (files)
            {
                var fileName = new FileInfo(item).Name;
                name = fileName[..fileName.IndexOfOrdinal('.')];
            }
            else
            {
                name = new DirectoryInfo(item).Name;
            }

            if (TryGetDate(name, out var date))
            {
                // Might give wrong results if there is a file that matches:
                // Example: 2021-09-01, and 2021-09-01-00
                items[date] = item;
            }
        }

        return items.Values;
    }

    private static bool TryGetDate(string value, out DateTime date)
    {
        Span<int> parts = stackalloc int[] { 0, 1, 1, 0, 0, 0 };

        try
        {
            var i = 0;
            foreach (var part in value.Tokenize('-'))
            {
                if (!int.TryParse(part, out var partValue))
                {
                    break;
                }

                parts[i++] = partValue;
            }

            if (i == 0)
            {
                date = DateTime.MinValue;
                return false;
            }

            date = new DateTime(
                parts[0],
                parts[1],
                parts[2],
                parts[3],
                parts[4],
                parts[5],
                DateTimeKind.Utc
            );
            return true;
        }
        catch
        {
            date = DateTime.MinValue;
            return false;
        }
    }
}
