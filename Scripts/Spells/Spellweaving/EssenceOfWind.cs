using System;
using System.Collections.Generic;

namespace Server.Spells.Spellweaving
{
  public class EssenceOfWindSpell : ArcanistSpell
  {
    private static SpellInfo m_Info = new SpellInfo("Essence of Wind", "Anathrae", -1);

    private static Dictionary<Mobile, EssenceOfWindInfo> m_Table = new Dictionary<Mobile, EssenceOfWindInfo>();

    public EssenceOfWindSpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(3.0);

    public override double RequiredSkill => 52.0;
    public override int RequiredMana => 40;

    public override void OnCast()
    {
      if (CheckSequence())
      {
        Caster.PlaySound(0x5C6);

        int range = 5 + FocusLevel;
        int damage = 25 + FocusLevel;

        double skill = Caster.Skills[SkillName.Spellweaving].Value;

        TimeSpan duration = TimeSpan.FromSeconds((int)(skill / 24) + FocusLevel);

        int fcMalus = FocusLevel + 1;
        int ssiMalus = 2 * (FocusLevel + 1);

        List<Mobile> targets = new List<Mobile>();

        foreach (Mobile m in Caster.GetMobilesInRange(range))
          if (Caster != m && Caster.InLOS(m) && SpellHelper.ValidIndirectTarget(Caster, m) &&
              Caster.CanBeHarmful(m, false))
            targets.Add(m);

        for (int i = 0; i < targets.Count; i++)
        {
          Mobile m = targets[i];

          Caster.DoHarmful(m);

          SpellHelper.Damage(this, m, damage, 0, 0, 100, 0, 0);

          if (!CheckResisted(m)) //No message on resist
          {
            m_Table[m] = new EssenceOfWindInfo(m, fcMalus, ssiMalus, duration);

            BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.EssenceOfWind, 1075802, duration, m,
              $"{fcMalus.ToString()}\t{ssiMalus.ToString()}"));
          }
        }
      }

      FinishSequence();
    }

    public static int GetFCMalus(Mobile m)
    {
      if (m_Table.TryGetValue(m, out EssenceOfWindInfo info))
        return info.FCMalus;

      return 0;
    }

    public static int GetSSIMalus(Mobile m)
    {
      if (m_Table.TryGetValue(m, out EssenceOfWindInfo info))
        return info.SSIMalus;

      return 0;
    }

    public static bool IsDebuffed(Mobile m)
    {
      return m_Table.ContainsKey(m);
    }

    public static void StopDebuffing(Mobile m, bool message)
    {
      if (m_Table.TryGetValue(m, out EssenceOfWindInfo info))
        info.Timer.DoExpire(message);
    }

    private class EssenceOfWindInfo
    {
      public EssenceOfWindInfo(Mobile defender, int fcMalus, int ssiMalus, TimeSpan duration)
      {
        Defender = defender;
        FCMalus = fcMalus;
        SSIMalus = ssiMalus;

        Timer = new ExpireTimer(Defender, duration);
        Timer.Start();
      }

      public Mobile Defender{ get; }

      public int FCMalus{ get; }

      public int SSIMalus{ get; }

      public ExpireTimer Timer{ get; }
    }

    private class ExpireTimer : Timer
    {
      private Mobile m_Mobile;

      public ExpireTimer(Mobile m, TimeSpan delay) : base(delay)
      {
        m_Mobile = m;
      }

      protected override void OnTick()
      {
        DoExpire(true);
      }

      public void DoExpire(bool message)
      {
        Stop();
        /*
        if ( message )
        {
        }
        */
        m_Table.Remove(m_Mobile);

        BuffInfo.RemoveBuff(m_Mobile, BuffIcon.EssenceOfWind);
      }
    }
  }
}