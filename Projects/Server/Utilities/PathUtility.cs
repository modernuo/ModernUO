/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        if (recursive)
        {
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                string destSubDir = Path.Combine(destDir, subdir.Name);
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

        foreach (FileInfo file in dir.GetFiles())
        {
            file.MoveTo(Path.Combine(destDir, file.Name));
        }

        if (recursive)
        {
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                string destSubDir = Path.Combine(destDir, subdir.Name);
                MoveDirectoryContents(subdir.FullName, destSubDir);
            }
        }

        try
        {
            dir.Delete(true);
        }
        catch
        {
            // ignored
        }
    }
}
