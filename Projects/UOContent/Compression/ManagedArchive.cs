/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ManagedArchive.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using Server.Logging;
using ZstdNet;

namespace Server.Compression;

/// <summary>
/// Cross-platform archive creation and extraction using System.Formats.Tar + ZstdNet (native libzstd).
/// Single-pass streaming: TarWriter → CompressionStream → FileStream.
/// </summary>
public static class ManagedArchive
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ManagedArchive));

    /// <summary>
    /// Creates a .tar.zst archive from one or more source directories.
    /// Writes to a temp file first, then atomically renames to the destination.
    /// </summary>
    /// <param name="sourcePaths">Directories to include in the archive.</param>
    /// <param name="destinationFile">Final .tar.zst file path.</param>
    /// <param name="relativeTo">Base path for computing relative entry names.</param>
    /// <param name="compressionLevel">ZSTD compression level (1-22, default 3).</param>
    /// <returns>The number of entries written, or -1 on failure.</returns>
    public static int CreateTarZstd(
        IEnumerable<string> sourcePaths,
        string destinationFile,
        string relativeTo,
        int compressionLevel = 3)
    {
        var tempFile = destinationFile + ".tmp";

        try
        {
            new FileInfo(destinationFile).EnsureDirectory();

            using var fileStream = new FileStream(
                tempFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920
            );

            using var compressionOptions = new CompressionOptions(compressionLevel);
            using var zstdStream = new CompressionStream(fileStream, compressionOptions);
            using var tarWriter = new TarWriter(zstdStream, TarEntryFormat.Pax, leaveOpen: true);

            var entryCount = 0;
            long totalBytes = 0;

            foreach (var sourcePath in sourcePaths)
            {
                if (!Directory.Exists(sourcePath))
                {
                    logger.Warning("Source path does not exist, skipping: {Path}", sourcePath);
                    continue;
                }

                var sourceDir = new DirectoryInfo(sourcePath);
                var relativeName = Path.GetRelativePath(relativeTo, sourcePath);

                foreach (var file in sourceDir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    var entryName = Path.Combine(relativeName, Path.GetRelativePath(sourcePath, file.FullName))
                        .Replace('\\', '/');

                    tarWriter.WriteEntry(file.FullName, entryName);
                    entryCount++;
                    totalBytes += file.Length;

                    if (entryCount % 100 == 0)
                    {
                        logger.Debug(
                            "Archive progress: {Count} entries, {Size:F1} MB",
                            entryCount,
                            totalBytes / 1048576.0
                        );
                    }
                }
            }

            // Flush tar → zstd → file
            tarWriter.Dispose();
            zstdStream.Dispose();
            fileStream.Dispose();

            // Atomic rename on same filesystem
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }

            File.Move(tempFile, destinationFile);

            logger.Information(
                "Created archive at {Path} ({Count} entries, {Size:F1} MB)",
                destinationFile,
                entryCount,
                totalBytes / 1048576.0
            );

            return entryCount;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create archive at {Path}", destinationFile);

            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // Best-effort cleanup
            }

            return -1;
        }
    }

    /// <summary>
    /// Extracts a .tar.zst archive to a directory.
    /// </summary>
    public static bool ExtractTarZstd(string archiveFile, string outputDirectory)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);

            using var fileStream = new FileStream(
                archiveFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920
            );

            using var zstdStream = new DecompressionStream(fileStream);
            using var tarReader = new TarReader(zstdStream);

            while (tarReader.GetNextEntry() is { } entry)
            {
                var destPath = Path.Combine(outputDirectory, entry.Name);

                // Ensure parent directory exists
                var destDir = Path.GetDirectoryName(destPath);
                if (destDir != null)
                {
                    Directory.CreateDirectory(destDir);
                }

                if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                {
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to extract archive {File}", archiveFile);
            return false;
        }
    }

    /// <summary>
    /// Counts entries in a .tar.zst archive without extracting content.
    /// Used for verification after archive creation.
    /// </summary>
    public static int CountEntries(string archiveFile)
    {
        try
        {
            using var fileStream = new FileStream(
                archiveFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920
            );

            using var zstdStream = new DecompressionStream(fileStream);
            using var tarReader = new TarReader(zstdStream);

            var count = 0;
            while (tarReader.GetNextEntry() is not null)
            {
                count++;
            }

            return count;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to count entries in archive {File}", archiveFile);
            return -1;
        }
    }
}
