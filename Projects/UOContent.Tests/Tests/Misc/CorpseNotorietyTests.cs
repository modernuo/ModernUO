using Server;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CorpseNotorietyTests
{
    [Theory]
    [InlineData(0, Notoriety.Innocent)]   // Human body
    [InlineData(746, Notoriety.Innocent)] // Horrific Beast, a monster body
    [InlineData(0, Notoriety.Murderer, 5)]
    [InlineData(746, Notoriety.Murderer, 5)]
    public void PlayerCorpseNotorietyIgnoresTransformedBody(int bodyMod, int expected, int kills = 0)
    {
        var victim = new PlayerMobile(World.NewMobile);
        victim.DefaultMobileInit();
        victim.Kills = kills;
        victim.BodyMod = bodyMod;

        var looter = new PlayerMobile(World.NewMobile);
        looter.DefaultMobileInit();

        var corpse = new Corpse(victim, []);

        try
        {
            Assert.Equal(expected, NotorietyHandlers.CorpseNotoriety(looter, corpse));
        }
        finally
        {
            corpse.Delete();
            victim.Delete();
            looter.Delete();
        }
    }
}
