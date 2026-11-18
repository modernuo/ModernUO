using Server.Guilds;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class GuildSearchTests
{
    [Fact]
    public void SearchReturnsGuildsWhoseNameContainsTheWords()
    {
        var leader1 = new PlayerMobile { Player = true };
        var leader2 = new PlayerMobile { Player = true };
        var wolves = new Guild(leader1, "Alpha Wolves", "AW");
        var bears = new Guild(leader2, "Beta Bears", "BB");

        try
        {
            var results = BaseGuild.Search("wolves");

            Assert.Single(results);
            Assert.Contains(wolves, results);
        }
        finally
        {
            wolves.Disband();
            bears.Disband();
            leader1.Delete();
            leader2.Delete();
        }
    }
}
