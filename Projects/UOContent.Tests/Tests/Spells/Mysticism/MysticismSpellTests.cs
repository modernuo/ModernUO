using Server.Spells;
using Server.Spells.Mysticism;
using Xunit;

namespace Server.Tests.Spells.Mysticism;

[Collection("Sequential UOContent Tests")]
public class MysticismSpellTests
{
    // The skill check handler is not wired up in tests, so skip the fizzle roll.
    private sealed class CleansingWinds(Mobile caster) : CleansingWindsSpell(caster, null)
    {
        public override bool CheckFizzle() => true;
    }

    private sealed class NetherCyclone(Mobile caster) : NetherCycloneSpell(caster)
    {
        public override bool CheckFizzle() => true;
    }

    private static Mobile NewCaster()
    {
        var m = new Mobile(World.NewMobile);
        m.DefaultMobileInit();
        m.RawStr = m.RawDex = m.RawInt = 100;
        m.Hits = m.HitsMax;
        m.Mana = m.ManaMax;
        m.Stam = m.StamMax;
        m.Skills.Mysticism.Base = 120;
        m.Skills.Focus.Base = 120;
        m.MoveToWorld(new Point3D(2000, 1000, 0), Map.Felucca);
        return m;
    }

    private static void BeginSequence(Mobile caster, Spell spell)
    {
        caster.Spell = spell;
        spell.State = SpellState.Sequencing;
    }

    [Fact]
    public void CleansingWinds_GuaranteedCureBranchCuresPoison()
    {
        var caster = NewCaster();

        try
        {
            if (Poison.GetPoison("Lesser") == null)
            {
                PoisonKinds.Configure();
            }

            caster.Poison = Poison.Lesser;
            Assert.True(caster.Poisoned);
            var spell = new CleansingWinds(caster);
            BeginSequence(caster, spell);

            spell.Target(caster); // cure chance is well above 100%

            Assert.False(caster.Poisoned);
        }
        finally
        {
            caster.Delete();
        }
    }

    [Fact]
    public void NetherCyclone_HighResistTargetIsNotHealed()
    {
        var caster = NewCaster();
        caster.Skills.Mysticism.Base = 50;
        caster.Skills.Focus.Base = 50;
        var target = NewCaster();
        target.MoveToWorld(new Point3D(2001, 1000, 0), Map.Felucca);
        target.Skills.MagicResist.Base = 120;
        target.Stam = 50;
        target.Mana = 50;

        try
        {
            var spell = new NetherCyclone(caster);
            BeginSequence(caster, spell);

            spell.Target(new Point3D(2001, 1000, 0));

            Assert.True(target.Stam <= 50);
            Assert.True(target.Mana <= 50);
        }
        finally
        {
            caster.Delete();
            target.Delete();
        }
    }
}
