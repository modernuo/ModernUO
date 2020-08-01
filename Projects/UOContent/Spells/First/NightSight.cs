using System;
using Server.Targeting;

namespace Server.Spells.First
{
  public class NightSightSpell : MagerySpell
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Night Sight", "In Lor",
      236,
      9031,
      Reagent.SulfurousAsh,
      Reagent.SpidersSilk);

    public NightSightSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public override void OnCast()
    {
      Caster.Target = new NightSightTarget(this);
    }

    private class NightSightTarget : Target
    {
      private readonly Spell m_Spell;

      public NightSightTarget(Spell spell) : base(12, false, TargetFlags.Beneficial) => m_Spell = spell;

      protected override void OnTarget(Mobile from, object targeted)
      {
        if (targeted is Mobile targ && m_Spell.CheckBSequence(targ))
        {
          SpellHelper.Turn(m_Spell.Caster, targ);

          if (targ.BeginAction<LightCycle>())
          {
            new LightCycle.NightSightTimer(targ).Start();
            int level =
              (int)(LightCycle.DungeonLevel *
                    ((Core.AOS
                      ? targ.Skills.Magery.Value
                      : from.Skills.Magery.Value) / 100));

            targ.LightLevel = Math.Max(level, 0);

            targ.FixedParticles(0x376A, 9, 32, 5007, EffectLayer.Waist);
            targ.PlaySound(0x1E3);

            BuffInfo.AddBuff(targ,
              new BuffInfo(BuffIcon.NightSight, 1075643)); // Night Sight/You ignore lighting effects
          }
          else
          {
            from.SendMessage("{0} already have nightsight.", from == targ ? "You" : "They");
          }
        }

        m_Spell.FinishSequence();
      }

      protected override void OnTargetFinish(Mobile from)
      {
        m_Spell.FinishSequence();
      }
    }
  }
}
