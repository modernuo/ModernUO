using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using Server.Logging;

namespace Server.Backup
{
    public static class LocalBackupSystem
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(LocalBackupSystem));

        private const string _libArchiveWindowsUrl = @"https://www.libarchive.org/downloads/libarchive-v3.5.2-win64.zip";
        private static string _pathToZstd;
        private static string _pathToTar;

        public static string BackupPath { get; private set; }

        public static void Configure()
        {
            var defaultPath = Path.Combine(Core.BaseDirectory, "Backups/Automatic");
            BackupPath = ServerConfiguration.GetOrUpdateSetting("autosave.backupPath", defaultPath);
        }

        public static void Initialize()
        {
            EventSink.WorldSavePostSnapshot += Backup;
        }

        private static string GetPathToTar()
        {
            if (!Core.IsWindows || File.Exists(@"C:\Windows\system32\tar.exe"))
            {
                return "tar";
            }

            return File.Exists("bsdtar/bsdtar.exe") ? "bsdtar/bsdtar.exe" : DownloadTarForWindows();
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

        private static string CompressFiles(string pathsToCompress, string outputFilePath, string outputFileName)
        {
            _pathToZstd ??= new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? "zstd";
            _pathToTar ??= GetPathToTar();
            var outputFile = $"{outputFilePath}/{outputFileName}.tar.zst";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pathToTar,
                    Arguments = $"--use-compress-program {_pathToZstd}/zstd -cf {outputFile} {pathsToCompress}",
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            return outputFile;
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
        }

        private static void RollupEveryHour(DateTime now)
        {
            var lastHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            var allFolders = Directory.EnumerateDirectories(BackupPath, "????-??-??-??-??-??");
            var latestFolders = GetLatestFoldersForRange(allFolders, TimePeriod.Hour, lastHour);

            // Compress the entire list
            // Delete all except for the first in the list
        }

        private static void RollupEveryDay(DateTime now)
        {
            // Compress the saves from 2days+ earlier
            // Keep the latest for each day period
        }

        private static void RollupEveryMonth(DateTime now)
        {
            // Compress the month before last using 1 save from each day.
        }

        private enum TimePeriod
        {
            Hour,
            Day,
            Month
        }

        private static IEnumerable<string> GetLatestFoldersForRange(
            IEnumerable<string> allFolders,
            TimePeriod timePeriod,
            DateTime rangeStart
        )
        {
            var folders = new SortedDictionary<DateTime, string>(new DescendingComparer<DateTime>());
            Span<int> dateComponents = stackalloc int[6];
            foreach (var folder in allFolders)
            {
                var di = new DirectoryInfo(folder);
                var split = di.Name.Split("-");
                // Expecting YYYY-MM-DD-HH-mm-ss
                if (split.Length != 6)
                {
                    continue;
                }

                try
                {
                    dateComponents[0] = int.Parse(split[0]);
                    dateComponents[1] = int.Parse(split[1]);
                    dateComponents[2] = int.Parse(split[2]);
                    dateComponents[3] = int.Parse(split[3]);
                    dateComponents[4] = int.Parse(split[4]);
                    dateComponents[5] = int.Parse(split[5]);
                }
                catch
                {
                    continue;
                }

                var timeFrame = timePeriod switch
                {
                    TimePeriod.Month => new DateTime(dateComponents[0], dateComponents[1], 1),
                    TimePeriod.Day => new DateTime(dateComponents[0], dateComponents[1], dateComponents[2]),
                    TimePeriod.Hour => new DateTime(dateComponents[0], dateComponents[1], dateComponents[2], dateComponents[3], 0, 0)
                };

                if (timeFrame == rangeStart)
                {
                    timeFrame = new DateTime(
                        dateComponents[0],
                        dateComponents[1],
                        dateComponents[2],
                        dateComponents[3],
                        dateComponents[4],
                        dateComponents[5]
                    );

                    folders.Add(timeFrame, folder);
                }
            }

            return folders.Values;
        }

        private class LocalBackupTimer : RealWorldTimer
        {
            private AsyncBackupExecutor _executor;

            protected override void OnTick()
            {
                // Do not do any backups if we are closing/crashing
                if (!Core.Closing)
                {
                    base.OnTick();
                }
            }

            public override void NewHourTick()
            {
                if (_executor?.Executed != false)
                {
                    _executor = new AsyncBackupExecutor(Core.Now);
                }

                _executor.AsyncActions += RollupEveryHour;
            }

            public override void NewDayTick()
            {
                _executor.AsyncActions += RollupEveryDay;
            }

            public override void NewMonthTick()
            {
                _executor.AsyncActions += RollupEveryMonth;
            }
        }
    }
}
