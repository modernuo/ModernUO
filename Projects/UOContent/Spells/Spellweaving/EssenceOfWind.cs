using System;
using System.Collections.Generic;

namespace Server.Spells.Spellweaving
{
  public class EssenceOfWindSpell : ArcanistSpell
  {
    private static readonly SpellInfo m_Info = new SpellInfo("Essence of Wind", "Anathrae", -1);

    private static readonly Dictionary<Mobile, EssenceOfWindInfo> m_Table = new Dictionary<Mobile, EssenceOfWindInfo>();

    public EssenceOfWindSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
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

        double skill = Caster.Skills.Spellweaving.Value;

        TimeSpan duration = TimeSpan.FromSeconds((int)(skill / 24) + FocusLevel);

        int fcMalus = FocusLevel + 1;
        int ssiMalus = 2 * (FocusLevel + 1);

        IPooledEnumerable<Mobile> eable = Caster.GetMobilesInRange(range);

        foreach (Mobile m in eable)
        {
          if (Caster == m || !Caster.InLOS(m) || !SpellHelper.ValidIndirectTarget(Caster, m) ||
              !Caster.CanBeHarmful(m, false))
            continue;

          Caster.DoHarmful(m);

          SpellHelper.Damage(this, m, damage, 0, 0, 100, 0, 0);

          if (CheckResisted(m))
            continue;

          m_Table[m] = new EssenceOfWindInfo(m, fcMalus, ssiMalus, duration);

          BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.EssenceOfWind, 1075802, duration, m,
            $"{fcMalus.ToString()}\t{ssiMalus.ToString()}"));
        }

        eable.Free();
      }

      FinishSequence();
    }

    public static int GetFCMalus(Mobile m) => m_Table.TryGetValue(m, out EssenceOfWindInfo info) ? info.FCMalus : 0;

    public static int GetSSIMalus(Mobile m) => m_Table.TryGetValue(m, out EssenceOfWindInfo info) ? info.SSIMalus : 0;

    public static bool IsDebuffed(Mobile m) => m_Table.ContainsKey(m);

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

      public Mobile Defender { get; }

      public int FCMalus { get; }

      public int SSIMalus { get; }

      public ExpireTimer Timer { get; }
    }

    private class ExpireTimer : Timer
    {
      private readonly Mobile m_Mobile;

      public ExpireTimer(Mobile m, TimeSpan delay) : base(delay) => m_Mobile = m;

      protected override void OnTick()
      {
        DoExpire(true);
      }

      public void DoExpire(bool message)
      {
        Stop();
        m_Table.Remove(m_Mobile);

        BuffInfo.RemoveBuff(m_Mobile, BuffIcon.EssenceOfWind);
      }
    }
  }
}
