using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Fourth
{
  public class ManaDrainSpell : MagerySpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Mana Drain", "Ort Rel",
      215,
      9031,
      Reagent.BlackPearl,
      Reagent.MandrakeRoot,
      Reagent.SpidersSilk);

    private static readonly HashSet<Mobile> m_Table = new HashSet<Mobile>();

    public ManaDrainSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fourth;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
    }

    private void AosDelay_Callback(Mobile m, int mana)
    {
      if (m.Alive && !m.IsDeadBondedPet)
      {
        m.Mana += mana;

        m.FixedEffect(0x3779, 10, 25);
        m.PlaySound(0x28E);
      }

      m_Table.Remove(m);
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

        m.Spell?.OnCasterHurt();

        m.Paralyzed = false;

        if (Core.AOS)
        {
          int toDrain = Math.Clamp(40 + (int)(GetDamageSkill(Caster) - GetResistSkill(m)), 0, m.Mana);

          if (m_Table.Contains(m))
            toDrain = 0;

          m.FixedParticles(0x3789, 10, 25, 5032, EffectLayer.Head);
          m.PlaySound(0x1F8);

          if (toDrain > 0)
          {
            m.Mana -= toDrain;

            m_Table.Add(m);
            Timer.DelayCall(TimeSpan.FromSeconds(5.0), AosDelay_Callback, m, toDrain);
          }
        }
        else
        {
          if (CheckResisted(m))
            m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
          else if (m.Mana >= 100)
            m.Mana -= Utility.Random(1, 100);
          else
            m.Mana -= Utility.Random(1, m.Mana);

          m.FixedParticles(0x374A, 10, 15, 5032, EffectLayer.Head);
          m.PlaySound(0x1F8);
        }

        HarmfulSpell(m);
      }

      FinishSequence();
    }

    public override double GetResistPercent(Mobile target) => 99.0;
  }
}
