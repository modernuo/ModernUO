using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Buffers;
using Server.Logging;

namespace Server.Saves
{
    public enum ArchivePeriod
    {
        Hourly,
        Daily,
        Monthly
    }

    public static class AutoArchive
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(AutoArchive));

        private const string _libArchiveWindowsUrl = @"https://www.libarchive.org/downloads/libarchive-v3.5.2-win64.zip";
        private static string _pathToZstd;
        private static string _pathToTar;
        private static int _isArchiving;
        private static DateTime _nextHourlyArchive;
        private static DateTime _nextDailyArchive;
        private static DateTime _nextMonthlyArchive;
        private static bool _enablePruning;
        private static ArchivePeriod _prunePeriod;

        public static Action<DateTime> Archive { get; set; }
        public static Action<DateTime> Prune { get; set; }
        public static string ArchivePath { get; private set; }
        public static string BackupPath { get; private set; }

        public static void Configure()
        {
            var defaultBackupPath = Path.Combine(Core.BaseDirectory, "Backups/Automatic");
            BackupPath = ServerConfiguration.GetOrUpdateSetting("autosave.backupPath", defaultBackupPath);

            ArchivePath = ServerConfiguration.GetOrUpdateSetting("autosave.archivePath", "Archives");

            var useLocalArchives = ServerConfiguration.GetOrUpdateSetting("autosave.archiveLocally", true);
            _enablePruning = ServerConfiguration.GetOrUpdateSetting("autosave.enableArchivePruning", false);
            _prunePeriod = ServerConfiguration.GetOrUpdateSetting("autosave.pruneArchives", ArchivePeriod.Monthly);

            if (useLocalArchives)
            {
                Archive = ArchiveLocally;
                Prune = PruneLocalArchives;
            }

            // Support the Saves folder containing exactly one .tar.zst or .zip file
            RestoreArchive();
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

            Archive?.Invoke(Core.Now);
        }

        private static void RestoreArchive()
        {
            var savePath = Path.Combine(Core.BaseDirectory, ServerConfiguration.GetSetting("world.savePath", "Saves"));
            var files = Directory.EnumerateFiles(savePath, "????-??-??-??-??-??.*");
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
                successful = ExtractZstdArchive(fi.FullName, tempFolder);
            }
            else if (fileName.EndsWithOrdinal(".zip"))
            {
                ZipFile.ExtractToDirectory(fi.FullName, tempFolder);
                successful = true;
            }
            else
            {
                logger.Information($"Unsupported archive file {fi.Name}");
                return false;
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

        private static bool ExtractZstdArchive(string fileNamePath, string outputDirectory)
        {
            _pathToZstd ??= GetPathToZstd();
            _pathToTar ??= GetPathToTar();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pathToTar,
                    Arguments = $"--use-compress-program \"{_pathToZstd} -d\" -xf \"{fileNamePath}\" -C {outputDirectory}",
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        private static string GetPathToTar()
        {
            if (!Core.IsWindows || File.Exists(@"C:\Windows\system32\tar.exe"))
            {
                return "tar";
            }

            return File.Exists("bsdtar/bsdtar.exe") ? "bsdtar/bsdtar.exe" : DownloadTarForWindows();
        }

        private static string GetPathToZstd()
        {
            var assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? "./";
            var zstdFileName = $"zstd{(Core.IsWindows ? ".exe" : "")}";
            return Path.Combine(assemblyPath, zstdFileName);
        }

        private static string DownloadTarForWindows()
        {
            AssemblyHandler.EnsureDirectory("temp");

            using WebClient wc = new WebClient();
            wc.DownloadFile (new Uri(_libArchiveWindowsUrl), "temp/libarchive.zip");

            ZipFile.ExtractToDirectory("temp/libarchive.zip", "temp");
            Directory.Move("temp/libarchive/bin", "bsdtar");
            Directory.Delete("temp", true);

            return "bsdtar/bsdtar.exe";
        }

        private static string CompressFiles(List<string> paths, string relativePath, string outputFilePath, string outputFileName)
        {
            _pathToZstd ??= GetPathToZstd();
            _pathToTar ??= GetPathToTar();
            AssemblyHandler.EnsureDirectory(outputFilePath);
            var outputFile = $"{outputFilePath}/{outputFileName}.tar.zst";

            using var builder = new ValueStringBuilder();
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                builder.Append($"{(i > 0 ? " " : "")}\"{Path.GetRelativePath(relativePath, path)}\"");
            }

            var pathsToCompress = builder.ToString();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pathToTar,
                    Arguments = $"--use-compress-program \"{_pathToZstd} -10\" -cf \"{outputFile}\" -C \"{relativePath}\" {pathsToCompress}",
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0 ? outputFile : null;
        }

        public static void PruneLocalArchives(DateTime threshold)
        {
            if (!_enablePruning)
            {
                return;
            }

            var rangeEnd = _prunePeriod switch
            {
                ArchivePeriod.Monthly => threshold.AddMonths(-2),
                ArchivePeriod.Daily   => threshold.AddDays(-2),
                _                     => threshold.AddHours(-2),
            };

            var allFolders = Directory.GetFiles(ArchivePath, "????-??-??-??-??-??.*", SearchOption.AllDirectories);
            var backups = GetInRange(allFolders, DateTime.MinValue, rangeEnd, true);

            foreach (var backup in backups)
            {
                Directory.Delete(backup, true);
            }
        }

        public static void ArchiveLocally(DateTime date)
        {
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

                    Prune?.Invoke(date);

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

            var archive = CompressFiles(
                latestFolders,
                BackupPath,
                Path.Combine(ArchivePath, archivePeriod.ToString()),
                now.ToTimeStamp()
            );

            if (archive != null)
            {
                logger.Information($"Created {archivePeriod.ToString()?.ToLowerInvariant()} archive at {archive}");

                // Keep the latest one, but delete the rest.
                for (var i = 1; i < latestFolders.Count; i++)
                {
                    Directory.Delete(latestFolders[i], true);
                }
            }
            else
            {
                logger.Warning($"Failed to create {archivePeriod.ToString()?.ToLowerInvariant()} archive.");
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
    }
}
