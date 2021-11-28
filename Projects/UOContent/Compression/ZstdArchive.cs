using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Server.Compression
{
    public static class ZstdArchive
    {
        private static readonly string _pathToZstd = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;

        public static bool ExtractToDirectory(string fileNamePath, string outputDirectory)
        {
            // bsdtar has a bug and hangs, so we are doing it in two steps.
            if (Core.IsWindows)
            {
                var tempDir = PathUtility.EnsureRandomPath(Path.GetTempPath());
                var tempTarArchive = Path.Combine(tempDir, "temp-file.tar");

                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Path.Combine(_pathToZstd, "zstd.exe"),
                            Arguments = $"-q -d \"{fileNamePath}\" -o \"{tempTarArchive}\""
                        }
                    };

                    process.Start();
                    process.WaitForExit();

                    return process.ExitCode == 0 && TarArchive.ExtractToDirectory(tempTarArchive, outputDirectory);
                }
                catch
                {
                    return false;
                }
                finally
                {
                    File.Delete(tempTarArchive);
                }
            }

            return TarArchive.ExtractToDirectory(fileNamePath, outputDirectory, "zstd -q -d", _pathToZstd);
        }

        public static bool CreateFromPaths(
            IEnumerable<string> paths,
            string destinationArchiveFileName,
            string relativeTo
        )
        {
            // bsdtar has a bug and hangs, so we are doing it in two steps.
            if (Core.IsWindows)
            {
                var tempDir = PathUtility.EnsureRandomPath(Path.GetTempPath());
                var tempTarArchive = Path.Combine(tempDir, "temp-file.tar");

                try
                {
                    if (!TarArchive.CreateFromPaths(paths, tempTarArchive, relativeTo))
                    {
                        return false;
                    }

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Path.Combine(_pathToZstd, "zstd.exe"),
                            Arguments = $"-q \"{tempTarArchive}\" -o \"{destinationArchiveFileName}\""
                        }
                    };

                    process.Start();
                    process.WaitForExit();

                    return process.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    Directory.Delete(tempDir, true);
                }
            }

            return TarArchive.CreateFromPaths(paths, destinationArchiveFileName, relativeTo, "zstd -q", _pathToZstd);
        }
    }
}
