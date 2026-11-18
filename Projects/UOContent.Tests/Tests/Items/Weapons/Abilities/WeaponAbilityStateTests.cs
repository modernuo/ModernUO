using Server.Items;
using Server.Mobiles;
using Xunit;

namespace Server.Tests.Items.Weapons.Abilities;

[Collection("Sequential UOContent Tests")]
public class WeaponAbilityStateTests
{
    private sealed class BladeweaveKatana : Katana
    {
        public override WeaponAbility PrimaryAbility => WeaponAbility.Bladeweave;
    }

    private static Mobile NewMobile()
    {
        var m = new Mobile(World.NewMobile);
        m.DefaultMobileInit();
        m.RawInt = 100;
        m.Mana = 100;
        return m;
    }

    private static Mobile NewFighter(Katana katana)
    {
        var m = NewMobile();
        m.Skills.Swords.Base = 100;
        m.Skills.Tactics.Base = 100;
        m.Skills.Ninjitsu.Base = 100;
        m.Skills.Bushido.Base = 100;
        Assert.True(m.EquipItem(katana));
        return m;
    }

    [Fact]
    public void BladeweaveRedirect_IsIgnoredOnceTheAbilityIsNoLongerArmed()
    {
        var katana = new BladeweaveKatana { Layer = Layer.OneHanded };
        var attacker = NewFighter(katana);

        try
        {
            using (new PredictableRandom(0)) // rolls Armor Ignore
            {
                Assert.True(WeaponAbility.Bladeweave.OnBeforeSwing(attacker, attacker));
            }

            // The swing was parried: the armed ability is cleared but the redirect entry stays behind.
            WeaponAbility.Table.Remove(attacker);
            Assert.False(Bladeweave.BladeWeaving(attacker, out _));

            WeaponAbility.Table[attacker] = WeaponAbility.Bladeweave;
            Assert.True(Bladeweave.BladeWeaving(attacker, out var redirect));
            Assert.Same(WeaponAbility.ArmorIgnore, redirect);
        }
        finally
        {
            WeaponAbility.Table.Remove(attacker);
            WeaponAbility.Bladeweave.OnMiss(attacker, attacker);
            attacker.Delete();
            katana.Delete();
        }
    }
}
