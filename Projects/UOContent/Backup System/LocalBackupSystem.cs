using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            BackupPath = ServerConfiguration.GetOrUpdateSetting("autosave.backupPath", "Backups/Automatic");
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

        private class LocalBackupTimer : RealWorldTimer
        {
            public LocalBackupTimer() : base(RealWorldTimerResolution.Hours)
            {
            }

            public override void NewHourTick()
            {

            }
        }
    }
}
