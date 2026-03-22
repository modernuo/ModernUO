/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PathUtility.cs                                                  *
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
using System.IO;
using System.Threading;
using Server.Logging;
using Server.Text;

namespace Server;

public static class PathUtility
{
    public static string EnsureDirectory(string dir)
    {
        var path = GetFullPath(dir, Core.BaseDirectory);
        Directory.CreateDirectory(path);

        return path;
    }

    public static void EnsureDirectory(this FileInfo fi)
    {
        var dir = GetFullPath(fi.DirectoryName, Core.BaseDirectory);
        if (dir != null)
        {
            Directory.CreateDirectory(dir);
        }
    }

    public static void EnsureDirectory(this DirectoryInfo di)
    {
        var file = GetFullPath(di.FullName, Core.BaseDirectory);
        Directory.CreateDirectory(file);
    }

    public static string GetFullPath(string relativeOrAbsolutePath) =>
        GetFullPath(relativeOrAbsolutePath, Core.BaseDirectory);

    public static string GetFullPath(string relativeOrAbsolutePath, string basePath) =>
        relativeOrAbsolutePath switch
        {
            null => null,
            ""   => basePath,
            _ => Path.IsPathRooted(relativeOrAbsolutePath)
                ? relativeOrAbsolutePath
                : Path.GetFullPath(relativeOrAbsolutePath, basePath)
        };

    public static string EnsureRandomPath(string basePath)
    {
        Span<byte> bytes = stackalloc byte[8];
        Utility.RandomBytes(bytes);
        return EnsureDirectory(Path.Combine(basePath, bytes.ToHexString()));
    }

    public static void CopyDirectoryContents(string sourceDir, string destDir, bool recursive = true)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        Directory.CreateDirectory(destDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        if (recursive)
        {
            foreach (var subdir in dir.GetDirectories())
            {
                var destSubDir = Path.Combine(destDir, subdir.Name);
                CopyDirectoryContents(subdir.FullName, destSubDir);
            }
        }
    }

    public static void MoveDirectoryContents(string sourceDir, string destDir, bool recursive = true)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        Directory.CreateDirectory(destDir);

        foreach (var file in dir.GetFiles())
        {
            file.MoveTo(Path.Combine(destDir, file.Name));
        }

        if (recursive)
        {
            foreach (var subdir in dir.GetDirectories())
            {
                var destSubDir = Path.Combine(destDir, subdir.Name);
                MoveDirectoryContents(subdir.FullName, destSubDir);
            }
        }

        try
        {
            dir.Delete(true);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to delete source directory after move: {Path}", sourceDir);
        }
    }

    /// <summary>
    /// Attempts to move a file with retry logic for transient I/O failures (antivirus, file locks).
    /// Safe to call from ThreadPool threads.
    /// </summary>
    public static bool TryMoveFile(string source, string dest, int maxRetries = 3, int delayMs = 200)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                File.Move(source, dest);
                return true;
            }
            catch (Exception ex) when (i < maxRetries - 1 && ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(delayMs * (i + 1));
            }
        }

        return false;
    }

    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(PathUtility));
}
