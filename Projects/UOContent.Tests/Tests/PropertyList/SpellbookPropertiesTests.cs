using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SpellbookPropertiesTests
{
    [Fact]
    public void GetProperties_EmitsAosAttributeLines()
    {
        var item = new Spellbook();
        try
        {
            item.Attributes.CastRecovery = 3;
            item.Attributes.LowerManaCost = 8;
            item.Attributes.Luck = 75;
            item.Attributes.NightSight = 1;
            item.Attributes.SpellChanneling = 1;
            item.Attributes.IncreasedKarmaLoss = 2;

            var lines = ItemOplTestHelper.DecodeAttributeLines(item);

            Assert.True(lines.ContainsKey(1060412)); // CastRecovery
            Assert.Equal("3", lines[1060412]);
            Assert.True(lines.ContainsKey(1060433)); // LowerManaCost
            Assert.Equal("8", lines[1060433]);
            Assert.True(lines.ContainsKey(1060436)); // Luck
            Assert.Equal("75", lines[1060436]);
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
