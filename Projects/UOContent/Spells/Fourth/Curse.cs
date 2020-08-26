using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Fourth
{
  public class CurseSpell : MagerySpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Curse", "Des Sanct",
      227,
      9031,
      Reagent.Nightshade,
      Reagent.Garlic,
      Reagent.SulfurousAsh);

    private static readonly HashSet<Mobile> m_UnderEffect = new HashSet<Mobile>();

    public CurseSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fourth;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
    }

    public static void RemoveEffect(Mobile m)
    {
      m_UnderEffect.Remove(m);

      m.UpdateResistances();
    }

    public static bool UnderEffect(Mobile m) => m_UnderEffect.Contains(m);

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
        SpellHelper.DisableSkillCheck = true;
        SpellHelper.AddStatCurse(Caster, m, StatType.Dex);
        SpellHelper.AddStatCurse(Caster, m, StatType.Int);
        SpellHelper.DisableSkillCheck = false;

        if (Caster.Player && m.Player /*&& Caster != m */ && !UnderEffect(m)) // On OSI you CAN curse yourself and get this effect.
        {
          TimeSpan duration = SpellHelper.GetDuration(Caster, m);
          m_UnderEffect.Add(m);
          Timer.DelayCall(duration, RemoveEffect, m);
          m.UpdateResistances();
        }

        m.Spell?.OnCasterHurt();

        m.Paralyzed = false;

        m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
        m.PlaySound(0x1E1);

        int percentage = (int)(SpellHelper.GetOffsetScalar(Caster, m, true) * 100);
        TimeSpan length = SpellHelper.GetDuration(Caster, m);

        string args = $"{percentage}\t{percentage}\t{percentage}\t{10}\t{10}\t{10}\t{10}";

        BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Curse, 1075835, 1075836, length, m, args));

        HarmfulSpell(m);
      }

      FinishSequence();
    }
  }
}
