using System;
using Server.Spells.First;
using Server.Targeting;

namespace Server.Spells.Mysticism;

public class NetherBoltSpell : MysticSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Nether Bolt",
        "In Corp Ylem",
        -1,
        9002,
        Reagent.BlackPearl,
        Reagent.SulfurousAsh
    );

    private static readonly Type[] _delayedDamageSpellFamilyStacking = Core.AOS ? [typeof(MagicArrowSpell)] : null;

    public NetherBoltSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public override bool SpellFocusingEligible => true;

    public override bool DelayedDamage => true;

    public override Type[] DelayedDamageSpellFamilyStacking => _delayedDamageSpellFamilyStacking;

    public void Target(Mobile m)
    {
        if (CheckHSequence(m))
        {
            var source = Caster;

            SpellHelper.Turn(source, m);

            if (Core.SA && HasDelayedDamageContext(m))
            {
                DoHurtFizzle();
                return;
            }

            SpellHelper.CheckReflect((int)Circle, ref source, ref m);

            source.MovingParticles(
                m,
                0x36D4,
                5,
                0,
                false,
                true,
                0x49A,
                0,
                9502,
                1,
                0,
                EffectLayer.Head,
                0x100
            );
            source.PlaySound(0x211);

            SpellHelper.Damage(this, m, GetNewAosDamage(10, 1, 4, m), 0, 0, 0, 0, 0, 100);
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }
}
