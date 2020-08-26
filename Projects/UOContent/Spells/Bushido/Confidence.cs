using System;
using System.Collections.Generic;

namespace Server.Spells.Bushido
{
  public class Confidence : SamuraiSpell
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Confidence", null,
      -1,
      9002);

    private static readonly Dictionary<Mobile, Timer> m_Table = new Dictionary<Mobile, Timer>();
    private static readonly Dictionary<Mobile, Timer> m_RegenTable = new Dictionary<Mobile, Timer>();

    public Confidence(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.25);

    public override double RequiredSkill => 25.0;
    public override int RequiredMana => 10;

    public override void OnBeginCast()
    {
      base.OnBeginCast();

      Caster.FixedEffect(0x37C4, 10, 7, 4, 3);
    }

    public override void OnCast()
    {
      if (CheckSequence())
      {
        Caster.SendLocalizedMessage(1063115); // You exude confidence.

        Caster.FixedParticles(0x375A, 1, 17, 0x7DA, 0x960, 0x3, EffectLayer.Waist);
        Caster.PlaySound(0x51A);

        OnCastSuccessful(Caster);

        BeginConfidence(Caster);
        BeginRegenerating(Caster);
      }

      FinishSequence();
    }

    public static bool IsConfident(Mobile m) => m_Table.ContainsKey(m);

    public static void BeginConfidence(Mobile m)
    {
      m_Table.TryGetValue(m, out Timer timer);
      timer?.Stop();
      m_Table[m] = timer = new InternalTimer(m);

      timer.Start();
    }

    public static void EndConfidence(Mobile m)
    {
      if (m_Table.TryGetValue(m, out Timer timer))
      {
        timer.Stop();
        m_Table.Remove(m);
      }

      OnEffectEnd(m, typeof(Confidence));
    }

    public static bool IsRegenerating(Mobile m) => m_RegenTable.ContainsKey(m);

    public static void BeginRegenerating(Mobile m)
    {
      m_RegenTable.TryGetValue(m, out Timer timer);
      timer?.Stop();

      m_RegenTable[m] = timer = new RegenTimer(m);

      timer.Start();
    }

    public static void StopRegenerating(Mobile m)
    {
      if (m_RegenTable.TryGetValue(m, out Timer timer))
      {
        timer.Stop();
        m_RegenTable.Remove(m);
      }
    }

    private class InternalTimer : Timer
    {
      private readonly Mobile m_Mobile;

      public InternalTimer(Mobile m) : base(TimeSpan.FromSeconds(15.0))
      {
        m_Mobile = m;
        Priority = TimerPriority.TwoFiftyMS;
      }

      protected override void OnTick()
      {
        EndConfidence(m_Mobile);
        m_Mobile.SendLocalizedMessage(1063116); // Your confidence wanes.
      }
    }

    private class RegenTimer : Timer
    {
      private readonly int m_Hits;
      private readonly Mobile m_Mobile;
      private int m_Ticks;

      public RegenTimer(Mobile m) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
      {
        m_Mobile = m;
        m_Hits = 15 + m.Skills.Bushido.Fixed * m.Skills.Bushido.Fixed / 57600;
        Priority = TimerPriority.TwoFiftyMS;
      }

      protected override void OnTick()
      {
        ++m_Ticks;

        if (m_Ticks >= 5)
        {
          m_Mobile.Hits += m_Hits - m_Hits * 4 / 5;
          StopRegenerating(m_Mobile);
        }

        m_Mobile.Hits += m_Hits / 5;
      }
    }
  }
}
