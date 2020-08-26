using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Spellweaving
{
  public class GiftOfRenewalSpell : ArcanistSpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Gift of Renewal", "Olorisstra",
      -1);

    private static readonly Dictionary<Mobile, GiftOfRenewalInfo> m_Table = new Dictionary<Mobile, GiftOfRenewalInfo>();

    public GiftOfRenewalSpell(Mobile caster, Item scroll = null)
      : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(3.0);

    public override double RequiredSkill => 0.0;
    public override int RequiredMana => 24;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Beneficial, 10);
    }

    public void Target(Mobile m)
    {
      if (m == null)
        return;

      if (!Caster.CanSee(m))
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      else if (m_Table.ContainsKey(m))
        Caster.SendLocalizedMessage(501775); // This spell is already in effect.
      else if (!Caster.CanBeginAction<GiftOfRenewalSpell>())
        Caster.SendLocalizedMessage(501789); // You must wait before trying again.
      else if (CheckBSequence(m))
      {
        SpellHelper.Turn(Caster, m);

        Caster.FixedEffect(0x374A, 10, 20);
        Caster.PlaySound(0x5C9);

        if (m.Poisoned)
        {
          m.CurePoison(m);
        }
        else
        {
          double skill = Caster.Skills.Spellweaving.Value;

          int hitsPerRound = 5 + (int)(skill / 24) + FocusLevel;
          TimeSpan duration = TimeSpan.FromSeconds(30 + FocusLevel * 10);

          GiftOfRenewalInfo info = new GiftOfRenewalInfo(Caster, m, hitsPerRound);

          Timer.DelayCall(duration,
            () =>
            {
              if (StopEffect(m))
              {
                m.PlaySound(0x455);
                m.SendLocalizedMessage(1075071); // The Gift of Renewal has faded.
              }
            });

          m_Table[m] = info;

          Caster.BeginAction<GiftOfRenewalSpell>();

          BuffInfo.AddBuff(m,
            new BuffInfo(BuffIcon.GiftOfRenewal, 1031602, 1075797, duration, m, hitsPerRound.ToString()));
        }
      }

      FinishSequence();
    }

    public static bool StopEffect(Mobile m)
    {
      if (!m_Table.TryGetValue(m, out GiftOfRenewalInfo info))
        return false;

      m_Table.Remove(m);

      info.m_Timer.Stop();
      BuffInfo.RemoveBuff(m, BuffIcon.GiftOfRenewal);

      Timer.DelayCall(TimeSpan.FromSeconds(60), info.m_Caster.EndAction<GiftOfRenewalSpell>);

      return true;
    }

    private class GiftOfRenewalInfo
    {
      public readonly Mobile m_Caster;
      public readonly int m_HitsPerRound;
      public readonly Mobile m_Mobile;
      public readonly InternalTimer m_Timer;

      public GiftOfRenewalInfo(Mobile caster, Mobile mobile, int hitsPerRound)
      {
        m_Caster = caster;
        m_Mobile = mobile;
        m_HitsPerRound = hitsPerRound;

        m_Timer = new InternalTimer(this);
        m_Timer.Start();
      }
    }

    private class InternalTimer : Timer
    {
      private readonly GiftOfRenewalInfo m_GiftInfo;

      public InternalTimer(GiftOfRenewalInfo info)
        : base(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0)) =>
        m_GiftInfo = info;

      protected override void OnTick()
      {
        Mobile m = m_GiftInfo.m_Mobile;

        if (!m_Table.ContainsKey(m))
        {
          Stop();
          return;
        }

        if (!m.Alive)
        {
          Stop();
          StopEffect(m);
          return;
        }

        if (m.Hits >= m.HitsMax)
          return;

        int toHeal = m_GiftInfo.m_HitsPerRound;

        SpellHelper.Heal(toHeal, m, m_GiftInfo.m_Caster);
        m.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
      }
    }
  }
}
