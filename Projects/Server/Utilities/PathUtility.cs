/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
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

    public static void CopyDirectory(string sourcePath, string destinationPath, bool recursive = true)
    {
        var searchOptions = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var file in Directory.EnumerateFiles(sourcePath, "*", searchOptions))
        {
            var fi = new FileInfo(file);
            var relativePath = Path.GetRelativePath(sourcePath, fi.DirectoryName!);
            var destFolder = Path.Combine(destinationPath, relativePath);
            EnsureDirectory(destFolder);
            fi.CopyTo(Path.Combine(destFolder, fi.Name));
        }
    }

    public static void MoveDirectory(string sourcePath, string destinationPath)
    {
        CopyDirectory(sourcePath, destinationPath);
        Directory.Delete(sourcePath, true);
    }
}
