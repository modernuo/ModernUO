using System;
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

        private static string DownloadTarForWindows()
        {
            var tempDir = Path.Combine(Core.BaseDirectory, "temp");
            AssemblyHandler.EnsureDirectory("temp");

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

        public static bool ExtractToDirectory(string fileNamePath, string outputDirectory, string compressionProgram = null)
        {
            _pathToTar ??= GetPathToTar();

            var compressionArg = compressionProgram ?? "-a";

            Console.WriteLine($"tar {compressionArg} -xf \"{fileNamePath}\" -C \"{outputDirectory}\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pathToTar,
                    Arguments = $"{compressionArg} -xf \"{fileNamePath}\" -C \"{outputDirectory}\"",
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        public static bool CreateFromPaths(
            List<string> paths,
            string destinationArchiveFileName,
            string compressionProgram = null
        )
        {
            _pathToTar ??= GetPathToTar();

            AssemblyHandler.EnsureDirectory(new FileInfo(destinationArchiveFileName));
            var di = new DirectoryInfo(paths[0]);
            var directory = di.Parent!.FullName;

            using var builder = new ValueStringBuilder();
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                builder.Append($"{(i > 0 ? " " : "")}\"{Path.GetRelativePath(directory, path)}\"");
            }

            var pathsToCompress = builder.ToString();
            var compressionArg = compressionProgram ?? "-a";
            var arguments = $"{compressionArg} -cf \"{destinationArchiveFileName}\" -C \"{directory}\" {pathsToCompress}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pathToTar,
                    Arguments = arguments,
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
    }
}
