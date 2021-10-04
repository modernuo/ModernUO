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
                var tempTarArchive = Path.Combine(Core.BaseDirectory, "temp/temp-file.tar");

                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = _pathToZstd,
                            Arguments = $"-q -d \"{fileNamePath}\" -o \"${tempTarArchive}\""
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

            return TarArchive.ExtractToDirectory(fileNamePath, outputDirectory, "zstd -d", _pathToZstd);
        }

        public static bool CreateFromPaths(
            List<string> paths,
            string destinationArchiveFileName,
            int compressionLevel = 10
        )
        {
            Debug.Assert(compressionLevel is >= 1 and <= 22, $"{nameof(compressionLevel)} must be between 1 and 22");

            // bsdtar has a bug and hangs, so we are doing it in two steps.
            if (Core.IsWindows)
            {
                var tempTarArchive = Path.Combine(Core.BaseDirectory, "temp/temp-file.tar");

                try
                {
                    if (!TarArchive.CreateFromPaths(paths, tempTarArchive))
                    {
                        return false;
                    }

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Path.Combine(_pathToZstd, "zstd.exe"),
                            Arguments = $"-q -10 \"{tempTarArchive}\" -o \"{destinationArchiveFileName}\""
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
                    File.Delete(tempTarArchive);
                }
            }

            return TarArchive.CreateFromPaths(paths, destinationArchiveFileName, "zstd -10", _pathToZstd);
        }
    }
}
