/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistFile.cs                                                *
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

namespace Server.Network.Bans.Blocklist;

public readonly record struct BlocklistHeader(string Generated, int Count, bool Present);

/// <summary>Reads the versioned blocklist file: cheap header probe + full snapshot load.</summary>
public static class BlocklistFile
{
    public static bool TryReadHeader(string path, out BlocklistHeader header)
    {
        header = new BlocklistHeader(null, 0, false);
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }
            using var reader = new StreamReader(path);
            var first = reader.ReadLine();
            if (first == null)
            {
                header = new BlocklistHeader(null, 0, true);
                return true;
            }
            string generated = null;
            var count = 0;
            if (first.StartsWith('#'))
            {
                foreach (var tok in first.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (tok.StartsWith("generated=", StringComparison.Ordinal))
                    {
                        generated = tok["generated=".Length..];
                    }
                    else if (tok.StartsWith("count=", StringComparison.Ordinal))
                    {
                        int.TryParse(tok["count=".Length..], out count);
                    }
                }
            }
            header = new BlocklistHeader(generated, count, true);
            return true;
        }
        catch
        {
            return false; // treat as absent; caller keeps last-good / empty
        }
    }

    public static BlocklistSnapshot Load(string path, out int parsed, out int skipped)
    {
        parsed = 0;
        skipped = 0;
        try
        {
            if (!File.Exists(path))
            {
                return BlocklistSnapshot.Empty;
            }
            return BlocklistSnapshot.Build(File.ReadAllBytes(path), out parsed, out skipped);
        }
        catch
        {
            return BlocklistSnapshot.Empty;
        }
    }
}
