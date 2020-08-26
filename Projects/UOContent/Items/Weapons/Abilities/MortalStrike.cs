using System;
using System.Collections.Generic;

namespace Server.Items
{
  /// <summary>
  ///   The assassin's friend.
  ///   A successful Mortal Strike will render its victim unable to heal any damage for several seconds.
  ///   Use a gruesome follow-up to finish off your foe.
  /// </summary>
  public class MortalStrike : WeaponAbility
  {
    public static readonly TimeSpan PlayerDuration = TimeSpan.FromSeconds(6.0);
    public static readonly TimeSpan NPCDuration = TimeSpan.FromSeconds(12.0);

    private static readonly Dictionary<Mobile, InternalTimer> m_Table = new Dictionary<Mobile, InternalTimer>();

    public override int BaseMana => 30;

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      if (!Validate(attacker) || !CheckMana(attacker, true)) return;

      ClearCurrentAbility(attacker);

      attacker.SendLocalizedMessage(1060086); // You deliver a mortal wound!
      defender.SendLocalizedMessage(1060087); // You have been mortally wounded!

      defender.PlaySound(0x1E1);
      defender.FixedParticles(0x37B9, 244, 25, 9944, 31, 0, EffectLayer.Waist);

      // Do not reset timer if one is already in place.
      if (!IsWounded(defender))
        BeginWound(defender, defender.Player ? PlayerDuration : NPCDuration);
    }

    public static bool IsWounded(Mobile m) => m_Table.ContainsKey(m);

    public static void BeginWound(Mobile m, TimeSpan duration)
    {
      if (m_Table.TryGetValue(m, out InternalTimer timer))
        timer?.Stop();

      m_Table[m] = timer = new InternalTimer(m, duration);
      timer.Start();

      m.YellowHealthbar = true;
    }

    public static void EndWound(Mobile m)
    {
      if (m_Table.TryGetValue(m, out InternalTimer timer))
      {
        timer.Stop();
        m_Table.Remove(m);
      }

      m.YellowHealthbar = false;
      m.SendLocalizedMessage(1060208); // You are no longer mortally wounded.
    }

    private class InternalTimer : Timer
    {
      private readonly Mobile m_Mobile;

      public InternalTimer(Mobile m, TimeSpan duration) : base(duration)
      {
        m_Mobile = m;
        Priority = TimerPriority.TwoFiftyMS;
      }

      protected override void OnTick()
      {
        EndWound(m_Mobile);
      }
    }
  }
}
