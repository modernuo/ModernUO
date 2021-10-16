using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using Server.Buffers;

namespace Server.Compression
{
    public static class TarArchive
    {
        private const string _libArchiveWindowsUrl = @"https://www.libarchive.org/downloads/libarchive-v3.5.2-win64.zip";
        private static string _pathToTar;

        private static string GetPathToTar()
        {
            if (!Core.IsWindows || File.Exists(@"C:\Windows\system32\tar.exe"))
            {
                return "tar";
            }

            var tarPath = Path.Combine(Core.BaseDirectory, "bsdtar/bsdtar.exe");
            return File.Exists(tarPath) ? tarPath : DownloadTarForWindows();
        }

        private static int RunTar(string arguments, string compressionProgramPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pathToTar,
                    Arguments = arguments
                }
            };

            if (compressionProgramPath != null)
            {
                var envPaths = $"{process.StartInfo.EnvironmentVariables["PATH"]}:{compressionProgramPath}";
                process.StartInfo.EnvironmentVariables["PATH"] = envPaths;
            }

            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }

        private static string DownloadTarForWindows()
        {
            var tempDir = PathUtility.EnsureRandomPath(Path.GetTempPath());

            var libarchiveFile = Path.Combine(tempDir, "libarchive.zip");
            using WebClient wc = new WebClient();
            wc.DownloadFile (new Uri(_libArchiveWindowsUrl), libarchiveFile);

            ZipFile.ExtractToDirectory(libarchiveFile, tempDir);
            var libArchivePath = Path.Combine(tempDir, "libarchive");
            Directory.Move(Path.Combine(libArchivePath, "bin"), "bsdtar");
            Directory.Delete(libArchivePath, true);
            File.Delete(libarchiveFile);

            return Path.Combine(Core.BaseDirectory, "bsdtar/bsdtar.exe");
        }

        public static bool ExtractToDirectory(
            string fileNamePath,
            string outputDirectory,
            string compressCommand = null,
            string compressionProgramPath = null
        )
        {
            _pathToTar ??= GetPathToTar();

            var useExternalCompression = compressCommand != null ? $"--use-compress-program \"{compressCommand}\" " : "";
            var arguments = $"{useExternalCompression} -xf \"{fileNamePath}\" -C \"{outputDirectory}\"";

            return RunTar(arguments, compressionProgramPath) == 0;
        }

        public static bool CreateFromPaths(
            IEnumerable<string> paths,
            string destinationArchiveFileName,
            string relativeTo,
            string compressCommand = null,
            string compressionProgramPath = null
        )
        {
            _pathToTar ??= GetPathToTar();

            new FileInfo(destinationArchiveFileName).EnsureDirectory();

            using var builder = new ValueStringBuilder();
            var i = 0;
            foreach (var path in paths)
            {
                builder.Append($"{(i++ > 0 ? " " : "")}\"{Path.GetRelativePath(relativeTo, path)}\"");
            }
            var pathsToCompress = builder.ToString();

            var tarFlags = compressCommand == null ? "-acf" : "-cf";
            var useExternalCompression = compressCommand != null ? $"--use-compress-program \"{compressCommand}\" " : "";
            var arguments = $"{useExternalCompression}{tarFlags} \"{destinationArchiveFileName}\" -C \"{relativeTo}\" {pathsToCompress}";

            return RunTar(arguments, compressionProgramPath) == 0;
        }
    }
}
