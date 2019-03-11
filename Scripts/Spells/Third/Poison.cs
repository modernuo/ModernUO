using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Third
{
  public class PoisonSpell : MagerySpell
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Poison", "In Nox",
      203,
      9051,
      Reagent.Nightshade
    );

    public PoisonSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Third;

    public override void OnCast()
    {
      Caster.Target = new InternalTarget(this);
    }

    public void Target(Mobile m)
    {
      if (!Caster.CanSee(m))
      {
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      }
      else if (CheckHSequence(m))
      {
        SpellHelper.Turn(Caster, m);

        SpellHelper.CheckReflect((int)Circle, Caster, ref m);

        m.Spell?.OnCasterHurt();

        m.Paralyzed = false;

        if (CheckResisted(m))
        {
          m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
        }
        else
        {
          int level;

          if (Core.AOS)
          {
            if (Caster.InRange(m, 2))
            {
              int total = (Caster.Skills.Magery.Fixed + Caster.Skills.Poisoning.Fixed) / 2;

              if (total >= 1000)
                level = 3;
              else if (total > 850)
                level = 2;
              else if (total > 650)
                level = 1;
              else
                level = 0;
            }
            else
            {
              level = 0;
            }
          }
          else
          {
            //double total = Caster.Skills.Magery.Value + Caster.Skills.Poisoning.Value;

            #region Dueling

            double total = Caster.Skills.Magery.Value;

            if (Caster is PlayerMobile pm)
            {
              if (pm.DuelContext != null && pm.DuelContext.Started && !pm.DuelContext.Finished &&
                  !pm.DuelContext.Ruleset.GetOption("Skills", "Poisoning"))
              {
              }
              else
              {
                total += pm.Skills.Poisoning.Value;
              }
            }
            else
            {
              total += Caster.Skills.Poisoning.Value;
            }

            #endregion

            double dist = Caster.GetDistanceToSqrt(m);

            if (dist >= 3.0)
              total -= (dist - 3.0) * 10.0;

            if (total >= 200.0 && 1 > Utility.Random(10))
              level = 3;
            else if (total > (Core.AOS ? 170.1 : 170.0))
              level = 2;
            else if (total > (Core.AOS ? 130.1 : 130.0))
              level = 1;
            else
              level = 0;
          }

          m.ApplyPoison(Caster, Poison.GetPoison(level));
        }

        m.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
        m.PlaySound(0x205);

        HarmfulSpell(m);
      }

      FinishSequence();
    }

    private class InternalTarget : Target
    {
      private PoisonSpell m_Owner;

      public InternalTarget(PoisonSpell owner) : base(Core.ML ? 10 : 12, false, TargetFlags.Harmful)
      {
        m_Owner = owner;
      }

      protected override void OnTarget(Mobile from, object o)
      {
        if (o is Mobile mobile) m_Owner.Target(mobile);
      }

      protected override void OnTargetFinish(Mobile from)
      {
        m_Owner.FinishSequence();
      }
    }
  }
}
