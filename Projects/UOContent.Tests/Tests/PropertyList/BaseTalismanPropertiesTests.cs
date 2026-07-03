using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BaseTalismanPropertiesTests
{
    [Fact]
    public void GetProperties_EmitsAosAttributeLines()
    {
        var item = new RandomTalisman();
        try
        {
            item.Attributes.DefendChance = 10;
            item.Attributes.BonusStr = 5;
            item.Attributes.Luck = 50;
            item.Attributes.NightSight = 1;
            item.Attributes.SpellChanneling = 1;
            item.Attributes.IncreasedKarmaLoss = 2;

            var lines = ItemOplTestHelper.DecodeAttributeLines(item);

            Assert.True(lines.ContainsKey(1060408)); // DefendChance
            Assert.Equal("10", lines[1060408]);
            Assert.True(lines.ContainsKey(1060485)); // BonusStr
            Assert.Equal("5", lines[1060485]);
            Assert.True(lines.ContainsKey(1060436)); // Luck
            Assert.Equal("50", lines[1060436]);
            Assert.True(lines.ContainsKey(1060441)); // NightSight
            Assert.Equal("", lines[1060441]);
            Assert.True(lines.ContainsKey(1060482)); // SpellChanneling
            Assert.Equal("", lines[1060482]);
            Assert.True(lines.ContainsKey(1075210)); // IncreasedKarmaLoss
            Assert.Equal("2", lines[1075210]);
        }
        finally
        {
            item.Delete();
        }
    }
}
