using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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

        public static void Configure()
        {
            BackupPath = ServerConfiguration.GetOrUpdateSetting("autoArchive.backupPath", "Backups/Automatic");
            ArchivePath = ServerConfiguration.GetOrUpdateSetting("autoArchive.archivePath", "Archives");
            _compressionFormat = ServerConfiguration.GetOrUpdateSetting("autoArchive.compressionFormat", CompressionFormat.Zstd);

            var useLocalArchives = ServerConfiguration.GetOrUpdateSetting("autoArchive.archiveLocally", true);
            _enablePruning = ServerConfiguration.GetOrUpdateSetting("autoArchive.enableArchivePruning", true);

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

            var backupPath = Path.Combine(BackupPath, Utility.GetTimeStamp());
            AssemblyHandler.EnsureDirectory(BackupPath);
            Directory.Move(args.OldSavePath, backupPath);

            logger.Information($"Created backup at {backupPath}");

            Archive?.Invoke();
        }

        private static void RestoreFromArchive()
        {
            var savePath = Path.Combine(Core.BaseDirectory, ServerConfiguration.GetSetting("world.savePath", "Saves"));

            var files = Directory.Exists(savePath) ? Directory.GetFiles(savePath, "????-??-??-??-??-??.*") : null;

            if (files == null || files.Length == 0)
            {
                return;
            }

            var latestFiles = GetInRange(files, DateTime.MinValue, DateTime.MaxValue, true);

            foreach (var file in latestFiles)
            {
                if (RestoreFromFile(file, savePath))
                {
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

            var tempFolder = Path.Combine(Core.BaseDirectory, "temp");
            AssemblyHandler.EnsureDirectory(tempFolder);
            bool successful;

            if (fileName.EndsWithOrdinal(".tar.zst"))
            {
                successful = ZstdArchive.ExtractToDirectory(fi.FullName, tempFolder);
            }
            else
            {
                TarArchive.ExtractToDirectory(fi.FullName, tempFolder);
                successful = true;
            }

            if (!successful)
            {
                logger.Information($"Failed to extract {fi.Name}");
                return false;
            }

            var allFolders = Directory.EnumerateDirectories(tempFolder, "????-??-??-??-??-??");
            var worldSaveFolders = GetInRange(allFolders, DateTime.MinValue, DateTime.MaxValue);
            var first = true;
            foreach (var folder in worldSaveFolders)
            {
                if (first)
                {
                    first = false;
                    Directory.Delete(savePath, true);
                    var dirInfo = new DirectoryInfo(folder);
                    logger.Information($"Restoring backup {dirInfo.Name}");
                    Directory.Move(folder, savePath);
                }
                else
                {
                    Directory.Delete(folder, true);
                }
            }

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

            if (Directory.Exists(BackupPath))
            {
                var allFolders = Directory.EnumerateDirectories(BackupPath, "????-??-??-??-??-??", SearchOption.AllDirectories);
                var latestBackupFolders = GetInRange(allFolders, DateTime.MinValue, Core.Now.AddMonths(-1));

                foreach (var folder in latestBackupFolders)
                {
                    logger.Information($"Pruning backup {folder}");
                    Directory.Delete(folder, true);
                }
            }
        }

        private static void PruneLocalArchives(ArchivePeriod period, int minRetained)
        {
            var periodStr = period.ToString();
            var path = Path.Combine(ArchivePath, periodStr);
            var allFiles = Directory.EnumerateFiles(path, "????-??-??-??-??-??.*");
            var archives = GetInRange(allFiles, DateTime.MinValue, DateTime.MaxValue, true);

            var periodLowerStr = periodStr.ToLowerInvariant();
            foreach (var archive in archives)
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

        public static void AutoArchiveLocally()
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

            ThreadPool.UnsafeQueueUserWorkItem(
                _ =>
                {
                    if (date >= _nextHourlyArchive)
                    {
                        var lastHour = date.Date.AddHours(date.Hour);
                        Rollup(ArchivePeriod.Hourly, lastHour.AddHours(-1), lastHour.ToTimeStamp());
                        _nextHourlyArchive = _nextHourlyArchive.AddHours(2);
                    }

                    if (date >= _nextDailyArchive)
                    {
                        var yesterday = date.Date.AddDays(-1);
                        Rollup(ArchivePeriod.Daily, yesterday, yesterday.ToTimeStamp());
                        _nextDailyArchive = _nextDailyArchive.AddDays(2);
                    }

                    if (date >= _nextMonthlyArchive)
                    {
                        var lastMonth = new DateTime(date.Year, date.Month, 1).AddMonths(-1);
                        Rollup(ArchivePeriod.Monthly, lastMonth, lastMonth.ToTimeStamp());
                        _nextMonthlyArchive = _nextMonthlyArchive.AddMonths(2);
                    }

                    Prune?.Invoke();

                    _isArchiving = 0;
                },
                null
            );
        }

        private static void Rollup(ArchivePeriod archivePeriod, DateTime rangeStart, string archiveNameNoExtension)
        {
            if (!Directory.Exists(BackupPath))
            {
                return;
            }

            var archivePeriodStr = archivePeriod.ToString();
            var archivePeriodStrLower = archivePeriodStr.ToLowerInvariant();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var allFolders = Directory.EnumerateDirectories(BackupPath, "????-??-??-??-??-??");

            var rangeEnd = archivePeriod switch
            {
                ArchivePeriod.Monthly => rangeStart.AddMonths(1),
                ArchivePeriod.Daily   => rangeStart.AddDays(1),
                _  => rangeStart.AddHours(1),
            };

            var latestFolders = GetInRange(allFolders, rangeStart, rangeEnd).ToList();

            // We don't want to re-archive a single save. This usually means we rebooted and found the one we purposefully left behind
            if (latestFolders.Count <= 1)
            {
                return;
            }

            var archivePath = Path.Combine(ArchivePath, archivePeriodStr);
            AssemblyHandler.EnsureDirectory(archivePath);

            var extension = _compressionFormat.GetFileExtension();
            var archiveFilePath = Path.Combine(archivePath, $"{archiveNameNoExtension}{extension}");

            logger.Information($"Creating {archivePeriodStrLower} archive");
            var archiveCreated = _compressionFormat == CompressionFormat.Zstd
                ? ZstdArchive.CreateFromPaths(latestFolders, archiveFilePath)
                : TarArchive.CreateFromPaths(latestFolders, archiveFilePath);

            if (archiveCreated)
            {
                stopWatch.Stop();
                var elapsed = stopWatch.Elapsed.TotalSeconds;
                logger.Information($"Created {archivePeriodStrLower} archive at {archiveFilePath} ({elapsed:F2} seconds)");

                // Keep the latest one, but delete the rest.
                for (var i = 1; i < latestFolders.Count; i++)
                {
                    Directory.Delete(latestFolders[i], true);
                }
            }
            else
            {
                logger.Warning($"Failed to create {archivePeriodStrLower} archive");
            }
        }

        private static IEnumerable<string> GetInRange(
            IEnumerable<string> allItems,
            DateTime rangeStart,
            DateTime rangeEnd,
            bool files = false
        )
        {
            var items = new SortedDictionary<DateTime, string>(new DescendingComparer<DateTime>());
            foreach (var item in allItems)
            {
                string name;
                if (files)
                {
                    var fileName = new FileInfo(item).Name;
                    name = fileName[..fileName.IndexOfOrdinal(".")];
                }
                else
                {
                    name = new DirectoryInfo(item).Name;
                }

                if (IsInRange(name, rangeStart, rangeEnd, out var date))
                {
                    items.Add(date, item);
                }
            }

            return items.Values;
        }

        private static bool IsInRange(string name, DateTime start, DateTime end, out DateTime date) =>
            TryGetDate(name, out date) && start <= date && end >= date;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetDate(string value, out DateTime date)
        {
            // Expecting YYYY-MM-DD-HH-mm-ss
            var split = value.Split("-");
            if (split.Length != 6)
            {
                date = DateTime.MinValue;
                return false;
            }

            try
            {
                date = new DateTime(
                    int.Parse(split[0]),
                    int.Parse(split[1]),
                    int.Parse(split[2]),
                    int.Parse(split[3]),
                    int.Parse(split[4]),
                    int.Parse(split[5])
                );
            }
            catch
            {
                date = DateTime.MinValue;
                return false;
            }

            return true;
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
