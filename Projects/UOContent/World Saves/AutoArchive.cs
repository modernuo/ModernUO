using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.HighPerformance;
using Server.Compression;
using Server.Logging;

namespace Server.Saves
{
    public enum ArchivePeriod
    {
        Hourly,
        Daily,
        Monthly
    }

    public enum CompressionFormat
    {
        None,
        Zip,
        GZip,
        Zstd,
    }

    public static class AutoArchive
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(AutoArchive));

        private static string _tempArchivePath;
        private static int _isArchiving;
        private static DateTime _nextHourlyArchive;
        private static DateTime _nextDailyArchive;
        private static DateTime _nextMonthlyArchive;
        private static CompressionFormat _compressionFormat;
        private static bool _enablePruning;

        public static Action Archive { get; set; }
        public static Action Prune { get; set; }
        public static string ArchivePath { get; private set; }
        public static string BackupPath { get; private set; }
        public static string AutomaticBackupPath { get; private set; }

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

            _compressionFormat = ServerConfiguration.GetOrUpdateSetting("autoArchive.compressionFormat", CompressionFormat.Zstd);

            if (useLocalArchives)
            {
                Archive = AutoArchiveLocally;
            }

            if (_enablePruning)
            {
                Prune = PruneBackups;
            }

            // Restores an archive file placed in the Saves folder. Supports all compression formats.
            RestoreFromArchive();

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
            PathUtility.MoveDirectory(args.OldSavePath, backupPath);

            logger.Information($"Created backup at {backupPath}");

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

            logger.Information($"Restoring latest world save from archive {fileName}");

            var tempPath = PathUtility.EnsureRandomPath(_tempArchivePath);
            var successful = fileName.EndsWithOrdinal(".tar.zst")
                ? ZstdArchive.ExtractToDirectory(fi.FullName, tempPath)
                : TarArchive.ExtractToDirectory(fi.FullName, tempPath);

            if (!successful)
            {
                logger.Information($"Failed to extract {fi.Name}");
                return false;
            }

            foreach (var folder in PathsByTimestampName(tempPath))
            {
                Directory.Delete(savePath, true);
                var dirInfo = new DirectoryInfo(folder);
                logger.Information($"Restoring backup {dirInfo.Name}");
                PathUtility.MoveDirectory(folder, savePath);
                break;
            }

            Directory.Delete(tempPath, true);

            return true;
        }

        public static void PruneBackups()
        {
            if (Directory.Exists(ArchivePath))
            {
                // Maintain a total of 66 of the most recent archives
                PruneLocalArchives(ArchivePeriod.Hourly, 24);
                PruneLocalArchives(ArchivePeriod.Daily, 30);
                PruneLocalArchives(ArchivePeriod.Monthly, 12);
            }

            if (!Directory.Exists(AutomaticBackupPath))
            {
                return;
            }

            var allFolders = Directory.EnumerateDirectories(AutomaticBackupPath);
            var threshold = Core.Now.AddMonths(-1);

            foreach (var folder in allFolders)
            {
                var dirName = new DirectoryInfo(folder).Name;

                if (!TryGetDate(dirName, out var date))
                {
                    continue;
                }

                if (date < threshold)
                {
                    logger.Information($"Pruning old backup {folder}");
                    Directory.Delete(folder, true);
                }
            }
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
                logger.Information($"Pruning {periodLowerStr} archive {fi.Name}");
                File.Delete(archive);
            }
        }

        public static async void AutoArchiveLocally()
        {
            var date = Core.Now;

            if (date < _nextHourlyArchive && date < _nextDailyArchive && date < _nextMonthlyArchive)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _isArchiving, 0, 1) == 1)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(
                now =>
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
                    _isArchiving = 0;
                },
                date,
                false
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
            var extension = _compressionFormat.GetFileExtension();

            // Leave behind 1 hourly for daily, 1 daily for monthly.
            var minimum = archivePeriod != ArchivePeriod.Monthly ? 1 : 0;

            foreach (var (rangeStart, sortedBackups) in items)
            {
                var backups = sortedBackups.Values;
                if (backups.Count <= minimum)
                {
                    continue;
                }

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var fileName = $"{rangeStart.ToTimeStamp(archivePeriod)}{extension}";
                var archiveFilePath = Path.Combine(archivePath, fileName);

                var archiveCreated = CreateArchive(archiveFilePath, AutomaticBackupPath, backups);

                if (archiveCreated)
                {
                    var elapsed = stopWatch.Elapsed.TotalSeconds;
                    logger.Information($"Created {archivePeriodStrLower} archive at {archiveFilePath} ({elapsed:F2} seconds)");

                    var i = minimum;
                    foreach (var backup in backups)
                    {
                        // Keep the latest one, but delete the rest.
                        if (i-- > 0)
                        {
                            continue;
                        }

                        Directory.Delete(backup, true);
                    }
                }
                else
                {
                    logger.Warning($"Failed to create {archivePeriodStrLower} archive");
                }

                stopWatch.Stop();
            }
        }

        private static bool CreateArchive(string archiveFilePath, string relativeTo, IEnumerable<string> backups) =>
            _compressionFormat == CompressionFormat.Zstd
                ? ZstdArchive.CreateFromPaths(backups, archiveFilePath, relativeTo)
                : TarArchive.CreateFromPaths(backups, archiveFilePath, relativeTo);

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

        private static IEnumerable<string> PathsByTimestampName(string path, bool files = false)
        {
            var allItems = files ? Directory.EnumerateFiles(path) : Directory.GetDirectories(path);
            var items = new SortedDictionary<DateTime, string>(new DescendingComparer<DateTime>());
            foreach (var item in allItems)
            {
                string name;
                if (files)
                {
                    var fileName = new FileInfo(item).Name;
                    name = fileName[..fileName.IndexOf('.')];
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
                    parts[i++] = int.Parse(part);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetFileExtension(this CompressionFormat compressionFormat) =>
            compressionFormat switch
            {
                CompressionFormat.Zip  => ".zip",
                CompressionFormat.GZip => ".tar.gz",
                CompressionFormat.Zstd => ".tar.zst",
                _                      => ".tar"
            };
    }
}
