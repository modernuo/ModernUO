using System;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests.Network.Bans.Blocklist;

public class BlocklistBanSettingsTests
{
    [Fact]
    public void Blocklist_defaults_are_present()
    {
        var s = new BanSettings();

        Assert.Equal("", s.BlocklistFile);
        Assert.Equal(TimeSpan.FromSeconds(60), s.BlocklistReloadInterval);
        Assert.True(s.ReportBlocklistHits);
        Assert.Equal(TimeSpan.FromHours(6), s.BlocklistBanDuration);
        Assert.Equal(TimeSpan.FromSeconds(60), s.BlocklistPromoteSuppression);
    }
}
