using System;
using Server.Targeting;

namespace Server.Spells.First
{
  public class WeakenSpell : MagerySpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Weaken", "Des Mani",
      212,
      9031,
      Reagent.Garlic,
      Reagent.Nightshade);

    public WeakenSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
    }

    public void Target(Mobile m)
    {
      if (m == null)
        return;

      if (!Caster.CanSee(m))
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      else if (CheckHSequence(m))
      {
        SpellHelper.Turn(Caster, m);

        SpellHelper.CheckReflect((int)Circle, Caster, ref m);

        SpellHelper.AddStatCurse(Caster, m, StatType.Str);

        m.Spell?.OnCasterHurt();

        m.Paralyzed = false;

        m.FixedParticles(0x3779, 10, 15, 5009, EffectLayer.Waist);
        m.PlaySound(0x1E6);

        int percentage = (int)(SpellHelper.GetOffsetScalar(Caster, m, true) * 100);
        TimeSpan length = SpellHelper.GetDuration(Caster, m);

        BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Weaken, 1075837, length, m, percentage.ToString()));

        HarmfulSpell(m);
      }

      FinishSequence();
    }
  }
}
