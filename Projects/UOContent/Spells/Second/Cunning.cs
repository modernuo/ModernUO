using System;
using Server.Engines.ConPVP;
using Server.Targeting;

namespace Server.Spells.Second
{
  public class CunningSpell : MagerySpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Cunning", "Uus Wis",
      212,
      9061,
      Reagent.MandrakeRoot,
      Reagent.Nightshade);

    public CunningSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
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

        SpellHelper.AddStatBonus(Caster, m, StatType.Int);

        m.FixedParticles(0x375A, 10, 15, 5011, EffectLayer.Head);
        m.PlaySound(0x1EB);

        int percentage = (int)(SpellHelper.GetOffsetScalar(Caster, m, false) * 100);
        TimeSpan length = SpellHelper.GetDuration(Caster, m);

        BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Cunning, 1075843, length, m, percentage.ToString()));
      }

      FinishSequence();
    }
  }
}
