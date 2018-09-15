using System;
using System.Collections;

namespace Server.Spells.Bushido
{
  public class Confidence : SamuraiSpell
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Confidence", null,
      -1,
      9002
    );

    private static Hashtable m_Table = new Hashtable();

    private static Hashtable m_RegenTable = new Hashtable();

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

    public static bool IsConfident(Mobile m)
    {
      return m_Table.Contains(m);
    }

    public static void BeginConfidence(Mobile m)
    {
      Timer t = (Timer)m_Table[m];

      t?.Stop();

      t = new InternalTimer(m);

      m_Table[m] = t;

      t.Start();
    }

    public static void EndConfidence(Mobile m)
    {
      Timer t = (Timer)m_Table[m];

      t?.Stop();

      m_Table.Remove(m);

      OnEffectEnd(m, typeof(Confidence));
    }

    public static bool IsRegenerating(Mobile m)
    {
      return m_RegenTable.Contains(m);
    }

    public static void BeginRegenerating(Mobile m)
    {
      Timer t = (Timer)m_RegenTable[m];

      t?.Stop();

      t = new RegenTimer(m);

      m_RegenTable[m] = t;

      t.Start();
    }

    public static void StopRegenerating(Mobile m)
    {
      Timer t = (Timer)m_RegenTable[m];

      t?.Stop();

      m_RegenTable.Remove(m);
    }

    private class InternalTimer : Timer
    {
      private Mobile m_Mobile;

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
      private int m_Hits;
      private Mobile m_Mobile;
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