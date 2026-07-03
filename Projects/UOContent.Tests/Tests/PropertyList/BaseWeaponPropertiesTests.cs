using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BaseWeaponPropertiesTests
{
    [Fact]
    public void Weapon_AttributeLineSet_Preserved()
    {
        var weapon = new Longsword();
        try
        {
            weapon.Attributes.DefendChance = 5;
            weapon.Attributes.BonusInt = 6;
            weapon.Attributes.SpellChanneling = 1;
            weapon.WeaponAttributes.UseBestSkill = 1;
            weapon.WeaponAttributes.HitFireball = 12;
            weapon.WeaponAttributes.MageWeapon = 25;
            weapon.WeaponAttributes.SelfRepair = 3;

            var map = ItemOplTestHelper.DecodeAttributeLines(weapon);

            Assert.Equal("5", map[1060408]);   // DefendChance
            Assert.Equal("6", map[1060432]);   // BonusInt
            Assert.Equal("", map[1060482]);    // SpellChanneling
            Assert.Equal("", map[1060400]);    // UseBestSkill
            Assert.Equal("12", map[1060420]);  // HitFireball
            Assert.Equal("5", map[1060438]);   // MageWeapon (30 - 25)
            Assert.Equal("3", map[1060450]);   // SelfRepair
        }
        finally
        {
            weapon.Delete();
        }
    }
}
