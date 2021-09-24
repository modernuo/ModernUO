using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Server.Compression
{
    public static class ZstdArchive
    {
        private static string _pathToZstd;

        private static string GetPathToZstd()
        {
            var assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? "./";
            var zstdFileName = $"zstd{(Core.IsWindows ? ".exe" : "")}";
            return Path.Combine(assemblyPath, zstdFileName);
        }

        public static bool ExtractToDirectory(string fileNamePath, string outputDirectory)
        {
            _pathToZstd ??= GetPathToZstd();

            var compressionProgram = $"--use-compress-program \"{_pathToZstd} -d";
            return TarArchive.ExtractToDirectory(fileNamePath, outputDirectory, compressionProgram);
        }

        public static bool CreateFromPaths(
            List<string> paths,
            string destinationArchiveFileName,
            int compressionLevel = 10
        )
        {
            Debug.Assert(compressionLevel is >= 1 and <= 22, $"{nameof(compressionLevel)} must be between 1 and 22");

            _pathToZstd ??= GetPathToZstd();
            var compressionProgram = $"--use-compress-program \"{_pathToZstd} -{compressionLevel}\"";
            return TarArchive.CreateFromPaths(paths, destinationArchiveFileName, compressionProgram);
        }
    }
}
