using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BaseJewelPropertiesTests
{
    [Fact]
    public void Jewel_AttributeLineSet_Preserved()
    {
        var ring = new GoldRing();
        try
        {
            ring.Attributes.DefendChance = 5;
            ring.Attributes.BonusStr = 10;
            ring.Attributes.Luck = 100;
            ring.Attributes.NightSight = 1;
            ring.Attributes.SpellChanneling = 1;
            ring.Attributes.IncreasedKarmaLoss = 3;

            var map = ItemOplTestHelper.DecodeAttributeLines(ring);

            Assert.Equal("5", map[1060408]);   // DefendChance
            Assert.Equal("10", map[1060485]);  // BonusStr
            Assert.Equal("100", map[1060436]); // Luck (raw; jewel has no luck bonus)
            Assert.Equal("", map[1060441]);    // NightSight
            Assert.Equal("", map[1060482]);    // SpellChanneling
            Assert.Equal("3", map[1075210]);   // IncreasedKarmaLoss (Core.ML EJ)
        }
        finally
        {
            ring.Delete();
        }
    }
}
