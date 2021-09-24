using System;
using System.Collections.Generic;
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
        LZip,
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
        private static ArchivePeriod _prunePeriod;

        public static Action Archive { get; set; }
        public static Action Prune { get; set; }
        public static string ArchivePath { get; private set; }
        public static string BackupPath { get; private set; }

        public static void Configure()
        {
            var defaultBackupPath = Path.Combine(Core.BaseDirectory, "Backups/Automatic");
            BackupPath = ServerConfiguration.GetOrUpdateSetting("autoArchive.backupPath", defaultBackupPath);
            ArchivePath = ServerConfiguration.GetOrUpdateSetting("autoArchive.archivePath", "Archives");
            _compressionFormat = ServerConfiguration.GetOrUpdateSetting("autoArchive.compressionFormat", CompressionFormat.Zstd);

            var useLocalArchives = ServerConfiguration.GetOrUpdateSetting("autoArchive.archiveLocally", true);
            _enablePruning = ServerConfiguration.GetOrUpdateSetting("autoArchive.enableArchivePruning", false);
            _prunePeriod = ServerConfiguration.GetOrUpdateSetting("autoArchive.pruneArchives", ArchivePeriod.Monthly);

            if (useLocalArchives)
            {
                Archive = ArchiveLocally;
                Prune = PruneLocalArchives;
            }

            // Support the Saves folder containing exactly one .tar.zst or .zip file
            RestoreFromArchive();

            // Prune local archives on startup if pruning is enabled
            Prune?.Invoke();
        }

        public static void Initialize()
        {
            EventSink.WorldSavePostSnapshot += Backup;

            DateTimeOffset now = Core.Now.ToSystemLocalTime();

            var date = now.Date;
            _nextHourlyArchive = date.AddHours(now.Hour + 1).ToUniversalTime();
            _nextDailyArchive = date.AddDays(1).ToUniversalTime();
            _nextMonthlyArchive = date.AddDays(1 - now.Day).AddMonths(1).ToUniversalTime();
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

        public static void PruneLocalArchives()
        {
            if (!_enablePruning)
            {
                return;
            }

            var date = Core.Now;

            var rangeEnd = _prunePeriod switch
            {
                ArchivePeriod.Monthly => date.AddMonths(-2),
                ArchivePeriod.Daily   => date.AddDays(-2),
                _                     => date.AddHours(-2),
            };

            var allFolders = Directory.GetFiles(ArchivePath, "????-??-??-??-??-??.*", SearchOption.AllDirectories);
            var backups = GetInRange(allFolders, DateTime.MinValue, rangeEnd, true);

            foreach (var backup in backups)
            {
                Directory.Delete(backup, true);
            }
        }

        public static void ArchiveLocally()
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
                        Rollup(ArchivePeriod.Hourly, lastHour.AddHours(-1), date);
                        _nextHourlyArchive = lastHour.AddHours(2);
                    }

                    if (date >= _nextDailyArchive)
                    {
                        var yesterday = date.Date.AddDays(-1);
                        Rollup(ArchivePeriod.Daily, yesterday, date);
                        _nextDailyArchive = yesterday.AddDays(2);
                    }

                    if (date >= _nextMonthlyArchive)
                    {
                        var lastMonth = new DateTime(date.Year, date.Month, 1).AddMonths(-1);
                        Rollup(ArchivePeriod.Monthly, lastMonth, date);
                        _nextMonthlyArchive = lastMonth.AddMonths(2);
                    }

                    Prune?.Invoke();

                    _isArchiving = 0;
                },
                null
            );
        }

        private static void Rollup(ArchivePeriod archivePeriod, DateTime rangeStart, DateTime now)
        {
            var allFolders = Directory.EnumerateDirectories(BackupPath, "????-??-??-??-??-??");

            var rangeEnd = archivePeriod switch
            {
                ArchivePeriod.Monthly => rangeStart.AddMonths(1),
                ArchivePeriod.Daily   => rangeStart.AddDays(1),
                _  => rangeStart.AddHours(1),
            };

            var latestFolders = GetInRange(allFolders, rangeStart, rangeEnd).ToList();

            if (latestFolders.Count == 0)
            {
                return;
            }

            var extension = _compressionFormat.GetFileExtension();

            var archivePeriodStr = archivePeriod.ToString();
            var archiveFilePath = Path.Combine(ArchivePath, archivePeriodStr, $"{now.ToTimeStamp()}{extension}");

            var archiveCreated = _compressionFormat == CompressionFormat.Zstd
                ? ZstdArchive.CreateFromPaths(latestFolders, archiveFilePath)
                : TarArchive.CreateFromPaths(latestFolders, archiveFilePath);

            if (archiveCreated)
            {
                logger.Information($"Created {archivePeriodStr.ToLowerInvariant()} archive at {archiveFilePath}");

                // Keep the latest one, but delete the rest.
                for (var i = 1; i < latestFolders.Count; i++)
                {
                    Directory.Delete(latestFolders[i], true);
                }
            }
            else
            {
                logger.Warning($"Failed to create {archivePeriodStr.ToLowerInvariant()} archive.");
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
                CompressionFormat.LZip => ".tar.lzip",
                CompressionFormat.Zstd => ".tar.zst",
                _                      => ".tar"
            };
    }
}
