/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistBans.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Network;
using Server.Network.Bans.Blocklist;

namespace Server.Misc;

/// <summary>
/// Registers the file-backed blocklist with the Core <see cref="ConnectionFilters"/> registry during the
/// Configure sweep. Registration is unconditional: the filter self-disables when <c>blocklist.json</c>
/// names no file, or when that file does not exist yet, so no config gate is needed here.
/// </summary>
public static class BlocklistBans
{
    public static void Configure()
    {
        ConnectionFilters.Register(new BlocklistFilter());
    }
}
