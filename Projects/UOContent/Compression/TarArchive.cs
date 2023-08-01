using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Server.Text;

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
            // This isn't called often so we don't need to optimize
            using (HttpClient hc = new HttpClient())
            {
                var result = hc.Send(new HttpRequestMessage(HttpMethod.Get, new Uri(_libArchiveWindowsUrl)));
                using var stream = result.Content.ReadAsStream();
                using FileStream fs = new FileStream(libarchiveFile, FileMode.Create, FileAccess.Write, FileShare.None);
                stream.CopyTo(fs);
            }

            ZipFile.ExtractToDirectory(libarchiveFile, tempDir);
            var libArchivePath = Path.Combine(tempDir, "libarchive");
            PathUtility.MoveDirectory(Path.Combine(libArchivePath, "bin"), Path.Combine(Core.BaseDirectory, "bsdtar"));
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

            var sb = ValueStringBuilder.Create();
            var i = 0;
            foreach (var path in paths)
            {
                var relativePath = Path.GetRelativePath(relativeTo, path);
                if (i++ > 0)
                {
                    sb.Append($" \"{relativePath}\"");
                }
                else
                {
                    sb.Append($"\"{relativePath}\"");
                }
            }
            var pathsToCompress = sb.ToString();
            sb.Dispose();

            var tarFlags = compressCommand == null ? "-acf" : "-cf";
            var useExternalCompression = compressCommand != null ? $"--use-compress-program \"{compressCommand}\" " : "";
            var arguments = $"{useExternalCompression}{tarFlags} \"{destinationArchiveFileName}\" -C \"{relativeTo}\" {pathsToCompress}";

            return RunTar(arguments, compressionProgramPath) == 0;
        }
    }
}
