using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BaseArmorPropertiesTests
{
    [Fact]
    public void Armor_AttributeLineSet_Preserved()
    {
        var armor = new PlateChest();
        try
        {
            armor.Attributes.DefendChance = 5;
            armor.Attributes.BonusDex = 8;
            armor.Attributes.Luck = 40;
            armor.Attributes.SpellChanneling = 1;
            armor.Attributes.IncreasedKarmaLoss = 2;
            armor.ArmorAttributes.MageArmor = 1;
            armor.ArmorAttributes.SelfRepair = 3;
            armor.ArmorAttributes.LowerStatReq = 50;

            var map = ItemOplTestHelper.DecodeAttributeLines(armor);

            Assert.Equal("5", map[1060408]);   // DefendChance
            Assert.Equal("8", map[1060409]);   // BonusDex
            Assert.Equal("40", map[1060436]);  // Luck (GetLuckBonus()==0 unequipped)
            Assert.Equal("", map[1060482]);    // SpellChanneling
            Assert.Equal("2", map[1075210]);   // IncreasedKarmaLoss
            Assert.Equal("", map[1060437]);    // MageArmor
            Assert.Equal("3", map[1060450]);   // SelfRepair
            Assert.Equal("50", map[1060435]);  // LowerStatReq (inline via GetLowerStatReq)
        }
        finally
        {
            armor.Delete();
        }
    }
}
