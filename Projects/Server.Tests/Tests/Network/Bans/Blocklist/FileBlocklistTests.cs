/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FileBlocklistTests.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Net;
using Server.Network.Bans.Blocklist;
using Xunit;

namespace Server.Tests.Network.Bans.Blocklist;

[Collection("Sequential Server Tests")]
public class FileBlocklistTests
{
    [Fact]
    public void IsBanned_reflects_loaded_snapshot()
    {
        FileBlocklist.LoadForTesting(BlocklistSnapshot.Build(System.Text.Encoding.ASCII.GetBytes("1.2.3.4"), out _, out _));
        Assert.True(FileBlocklist.IsBanned(IPAddress.Parse("1.2.3.4")));
        Assert.False(FileBlocklist.IsBanned(IPAddress.Parse("1.2.3.5")));
    }

    [Fact]
    public void Empty_when_unloaded_never_throws()
    {
        FileBlocklist.LoadForTesting(BlocklistSnapshot.Empty);
        Assert.False(FileBlocklist.IsBanned(IPAddress.Parse("8.8.8.8")));
    }
}
