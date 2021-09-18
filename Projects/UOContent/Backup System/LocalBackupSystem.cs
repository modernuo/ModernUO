using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;

namespace Server.Backup
{
    public static class LocalBackupSystem
    {
        private const string _libArchiveWindowsUrl = @"https://www.libarchive.org/downloads/libarchive-v3.5.2-win64.zip";
        private static string _pathToZstd;
        private static string _pathToTar;

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

        private static string CompressFiles(string pathToCompress, string outputFilePath, string outputFileName)
        {
            _pathToZstd ??= new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? "zstd";
            _pathToTar ??= GetPathToTar();
            var outputFile = $"{outputFilePath}/{outputFileName}.tar.zst";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pathToTar,
                    Arguments = $"--use-compress-program {_pathToZstd}/zstd -cf {outputFile} {pathToCompress}",
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            return outputFile;
        }

        public static void Configure()
        {
            var path = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? "";
            Console.WriteLine("Location: {0}", path);
            var pathToTar = GetPathToTar();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToTar,
                    Arguments = $"--use-compress-program {path}/zstd -cf test.tar.zst ./Assemblies",
                    UseShellExecute = true
                }
            };

            process.Start();
        }
    }
}
