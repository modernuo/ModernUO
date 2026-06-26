using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BaseClothingPropertiesTests
{
    [Fact]
    public void Clothing_AttributeLineSet_Preserved()
    {
        var shirt = new FancyShirt();
        try
        {
            shirt.Attributes.DefendChance = 5;
            shirt.Attributes.Luck = 25;
            shirt.Attributes.SpellChanneling = 1;
            shirt.ClothingAttributes.MageArmor = 1;
            shirt.ClothingAttributes.SelfRepair = 2;
            shirt.ClothingAttributes.LowerStatReq = 30;
            shirt.ClothingAttributes.DurabilityBonus = 15;

            var map = ItemOplTestHelper.DecodeAttributeLines(shirt);

            Assert.Equal("5", map[1060408]);   // DefendChance
            Assert.Equal("25", map[1060436]);  // Luck (raw)
            Assert.Equal("", map[1060482]);    // SpellChanneling
            Assert.Equal("", map[1060437]);    // MageArmor
            Assert.Equal("2", map[1060450]);   // SelfRepair
            Assert.Equal("30", map[1060435]);  // LowerStatReq (direct)
            Assert.Equal("15", map[1060410]);  // DurabilityBonus (direct)
        }
        finally
        {
            shirt.Delete();
        }
    }
}
