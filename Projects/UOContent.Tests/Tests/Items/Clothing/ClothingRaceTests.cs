using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ClothingRaceTests
{
    [Fact]
    public void CanEquip_RefusesRaceRestrictedClothing_ForWrongRace()
    {
        var human = new Mobile((Serial)0x2101u);
        human.DefaultMobileInit();
        human.Player = true;
        human.RawStr = 100;
        var pants = new ElvenPants();

        try
        {
            Assert.Equal(Race.Human, human.Race);
            Assert.False(pants.CanEquip(human));
        }
        finally
        {
            pants.Delete();
            human.Delete();
        }
    }
}
