using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
  public class MindRotSpell : NecromancerSpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Mind Rot", "Wis An Ben",
      203,
      9031,
      Reagent.BatWing,
      Reagent.PigIron,
      Reagent.DaemonBlood);

    private static readonly Dictionary<Mobile, MRBucket> m_Table = new Dictionary<Mobile, MRBucket>();

    public MindRotSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

    public override double RequiredSkill => 30.0;
    public override int RequiredMana => 17;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
    }

    public void Target(Mobile m)
    {
      if (m == null)
        Caster.SendLocalizedMessage(1060508); // You can't curse that.
      else if (HasMindRotScalar(m))
        Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
      else if (CheckHSequence(m))
      {
        SpellHelper.Turn(Caster, m);

        /* Attempts to place a curse on the Target that increases the mana cost of any spells they cast,
         * for a duration based off a comparison between the Caster's Spirit Speak skill and the Target's Resisting Spells skill.
         * The effect lasts for ((Spirit Speak skill level - target's Resist Magic skill level) / 50 ) + 20 seconds.
         */

        m.Spell?.OnCasterHurt();

        m.PlaySound(0x1FB);
        m.PlaySound(0x258);
        m.FixedParticles(0x373A, 1, 17, 9903, 15, 4, EffectLayer.Head);

        TimeSpan duration =
          TimeSpan.FromSeconds(
            ((GetDamageSkill(Caster) - GetResistSkill(m)) / 5.0 + 20.0) * (m.Player ? 1.0 : 2.0));
        m.CheckSkill(SkillName.MagicResist, 0.0, 120.0); // Skill check for gain

        SetMindRotScalar(Caster, m, m.Player ? 1.25 : 2.00, duration);

        HarmfulSpell(m);
      }

      FinishSequence();
    }

    public static void ClearMindRotScalar(Mobile m)
    {
      if (!m_Table.TryGetValue(m, out MRBucket tmpB))
        return;

      BuffInfo.RemoveBuff(m, BuffIcon.Mindrot);
      tmpB.m_MRExpireTimer.Stop();
      m_Table.Remove(m);
      m.SendLocalizedMessage(1060872); // Your mind feels normal again.
    }

    public static bool HasMindRotScalar(Mobile m) => m_Table.ContainsKey(m);

    public static bool GetMindRotScalar(Mobile m, ref double scalar)
    {
      if (m_Table.TryGetValue(m, out MRBucket tmpB))
      {
        scalar = tmpB.m_Scalar;
        return true;
      }

      return false;
    }

    public static void SetMindRotScalar(Mobile caster, Mobile target, double scalar, TimeSpan duration)
    {
      if (!m_Table.ContainsKey(target))
      {
        MRBucket tmpB = new MRBucket(scalar, new MRExpireTimer(caster, target, duration));
        m_Table.Add(target, tmpB);
        BuffInfo.AddBuff(target, new BuffInfo(BuffIcon.Mindrot, 1075665, duration, target));
        tmpB.m_MRExpireTimer.Start();
        target.SendLocalizedMessage(1074384);
      }
    }
  }

  public class MRExpireTimer : Timer
  {
    private readonly DateTime m_End;
    private readonly Mobile m_Target;

    public MRExpireTimer(Mobile caster, Mobile target, TimeSpan delay) : base(TimeSpan.FromSeconds(1.0),
      TimeSpan.FromSeconds(1.0))
    {
      m_Target = target;
      m_End = DateTime.UtcNow + delay;
      Priority = TimerPriority.TwoFiftyMS;
    }

    protected override void OnTick()
    {
      if (m_Target.Deleted || !m_Target.Alive || DateTime.UtcNow >= m_End)
      {
        MindRotSpell.ClearMindRotScalar(m_Target);
        Stop();
      }
    }
  }

  public class MRBucket
  {
    public MRExpireTimer m_MRExpireTimer;

    public double m_Scalar;

    public MRBucket(double theScalar, MRExpireTimer theTimer)
    {
      m_Scalar = theScalar;
      m_MRExpireTimer = theTimer;
    }
  }
}
