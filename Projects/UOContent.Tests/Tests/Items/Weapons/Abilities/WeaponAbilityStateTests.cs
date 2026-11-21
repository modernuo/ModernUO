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

    // Hits regenerate while the timer wheel runs, so count the damage taken instead of reading Hits.
    private sealed class DamageCounter : Mobile
    {
        public DamageCounter() : base(World.NewMobile)
        {
        }

        public int Damaged { get; private set; }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            Damaged += amount;
            base.OnDamage(amount, from, willKill);
        }
    }

    // The skill check handler is not wired up in tests, so make every throw land.
    private sealed class SureBoomerang : Boomerang
    {
        public override bool CheckHit(Mobile attacker, Mobile defender) => true;
    }

    private sealed class FeintKatana : Katana
    {
        public override WeaponAbility PrimaryAbility => WeaponAbility.Feint;
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

    [Fact]
    public void Feint_ReducesDamageTheOpponentDealsToTheFeinter()
    {
        var katana = new FeintKatana { Layer = Layer.OneHanded };
        var feinter = NewFighter(katana);
        var opponent = NewMobile();

        try
        {
            WeaponAbility.Feint.OnHit(feinter, opponent, 10, new WorldLocation(opponent));

            Assert.True(Feint.GetDamageReduction(opponent, feinter, out var reduction));
            Assert.True(reduction > 0);
            Assert.False(Feint.GetDamageReduction(feinter, opponent, out _));
        }
        finally
        {
            if (Feint.Registry.Remove(feinter, out var timer))
            {
                timer.Stop();
            }

            feinter.Delete();
            opponent.Delete();
            katana.Delete();
        }
    }

    [Fact]
    public void MysticArc_DelayedHitsKeepTheirOwnCasterAndTarget()
    {
        Mobile Place(Mobile m, int x)
        {
            m.RawStr = 100;
            m.Hits = m.HitsMax;
            m.MoveToWorld(new Point3D(x, 1000, 0), Map.Felucca);
            return m;
        }

        Mobile Caster(int x)
        {
            var caster = NewMobile();
            caster.Skills.Throwing.Base = 100;
            caster.EquipItem(new SureBoomerang { Layer = Layer.OneHanded });
            return Place(caster, x);
        }

        DamageCounter Foe(Mobile caster, int x)
        {
            var foe = new DamageCounter();
            foe.DefaultMobileInit();
            Place(foe, x);
            foe.Combatant = caster;
            return foe;
        }

        var first = Caster(1000);
        var firstPrimary = Foe(first, 1002);
        var firstSecondary = Foe(first, 1003);
        var second = Caster(1100);
        var secondPrimary = Foe(second, 1102);
        var secondSecondary = Foe(second, 1103);

        try
        {
            using (new PredictableRandom(0))
            {
                WeaponAbility.MysticArc.OnHit(first, firstPrimary, 0, new WorldLocation(firstPrimary));
                WeaponAbility.MysticArc.OnHit(second, secondPrimary, 0, new WorldLocation(secondPrimary));
                Timer.Slice(Core.TickCount + 400);
            }

            Assert.True(firstSecondary.Damaged > 0);
            Assert.True(secondSecondary.Damaged > 0);
        }
        finally
        {
            foreach (var m in new Mobile[] { first, firstPrimary, firstSecondary, second, secondPrimary, secondSecondary })
            {
                m.Delete();
            }
        }
    }
}
