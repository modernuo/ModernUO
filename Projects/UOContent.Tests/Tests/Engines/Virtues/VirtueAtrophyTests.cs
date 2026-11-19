using Server.Engines.Virtues;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class VirtueAtrophyTests
{
    [Fact]
    public void FirstSacrificeGainSurvivesImmediateAtrophyCheck()
    {
        var pm = new PlayerMobile { Name = "virtue-atrophy", Map = null };

        var gainedPath = false;
        Assert.True(VirtueSystem.Award(pm, VirtueName.Sacrifice, 400, ref gainedPath));
        SacrificeVirtue.CheckAtrophy(pm);

        Assert.Equal(400, pm.Virtues.Sacrifice);
        pm.Delete();
    }
}
