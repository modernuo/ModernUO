using Server.Engines.ConPVP;
using Server.Targeting;

namespace Server.Spells.Second
{
  public class CureSpell : MagerySpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Cure", "An Nox",
      212,
      9061,
      Reagent.Garlic,
      Reagent.Ginseng);

    public CureSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Second;

    public override bool CheckCast()
    {
      if (DuelContext.CheckSuddenDeath(Caster))
      {
        Caster.SendMessage(0x22, "You cannot cast this spell when in sudden death.");
        return false;
      }

      return base.CheckCast();
    }

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Beneficial, Core.ML ? 10 : 12);
    }

    public void Target(Mobile m)
    {
      if (m == null)
        return;

      if (!Caster.CanSee(m))
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      else if (CheckBSequence(m))
      {
        SpellHelper.Turn(Caster, m);

        Poison p = m.Poison;

        if (p != null)
        {
          int chanceToCure = 10000 + (int)(Caster.Skills.Magery.Value * 75) -
                             (p.Level + 1) * (Core.AOS ? p.Level < 4 ? 3300 : 3100 : 1750);
          chanceToCure /= 100;

          if (chanceToCure > Utility.Random(100))
          {
            if (m.CurePoison(Caster))
            {
              if (Caster != m)
                Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!

              m.SendLocalizedMessage(1010059); // You have been cured of all poisons.
            }
          }
          else
          {
            m.SendLocalizedMessage(1010060); // You have failed to cure your target!
          }
        }

        m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
        m.PlaySound(0x1E0);
      }

      FinishSequence();
    }
  }
}
