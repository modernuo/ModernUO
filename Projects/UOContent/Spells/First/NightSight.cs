using System;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.First;

public class NightSightSpell : MagerySpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Night Sight",
        "In Lor",
        236,
        9031,
        Reagent.SulfurousAsh,
        Reagent.SpidersSilk
    );

    public int TargetRange => 12;

    public NightSightSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Beneficial);
    }

    public void Target(Mobile m)
    {
        if (CheckBSequence(m))
        {
            SpellHelper.Turn(Caster, m);

            if (m.BeginAction<LightCycle>())
            {
                new LightCycle.NightSightTimer(m).Start();

                var skill = (Core.AOS ? m.Skills.Magery.Value : Caster.Skills.Magery.Value) / 100;
                var level = (int)(LightCycle.DungeonLevel * skill);

                m.LightLevel = Math.Max(level, 0);

                m.FixedParticles(0x376A, 9, 32, 5007, EffectLayer.Waist);
                m.PlaySound(0x1E3);

                // Night Sight/You ignore lighting effects
                (m as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.NightSight, 1075643));
            }
            else if (m == Caster)
            {
                m.SendMessage("You already have nightsight.");
            }
            else
            {
                m.SendMessage("They already have nightsight.");
            }
        }
    }
}
