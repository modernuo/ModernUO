using System;
using System.Collections.Generic;

namespace Server.Items
{
  /// <summary>
  ///   Attack with increased damage with additional damage over time.
  /// </summary>
  public class TalonStrike : WeaponAbility
  {
    private static readonly HashSet<Mobile> m_Table = new HashSet<Mobile>();

    public override int BaseMana => 30;
    public override double DamageScalar => 1.2;

    public override bool CheckSkills(Mobile from)
    {
      if (GetSkill(from, SkillName.Ninjitsu) < 50.0)
      {
        from.SendLocalizedMessage(1063352,
          "50"); // You need ~1_SKILL_REQUIREMENT~ Ninjitsu skill to perform that attack!
        return false;
      }

      return base.CheckSkills(from);
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      if (m_Table.Contains(defender) || !Validate(attacker) || !CheckMana(attacker, true))
        return;

      ClearCurrentAbility(attacker);

      attacker.SendLocalizedMessage(1063358); // You deliver a talon strike!
      defender.SendLocalizedMessage(1063359); // Your attacker delivers a talon strike!

      defender.FixedParticles(0x373A, 1, 17, 0x26BC, 0x662, 0, EffectLayer.Waist);

      InternalTimer timer = new InternalTimer(defender,
        (int)(10.0 * (attacker.Skills.Ninjitsu.Value - 50.0) / 70.0 + 5)); // 5 - 15 damage

      timer.Start();

      m_Table.Add(defender);
    }

    private class InternalTimer : Timer
    {
      private readonly double DamagePerTick;
      private double m_DamageRemaining;
      private double m_DamageToDo;
      private readonly Mobile m_Defender;

      public InternalTimer(Mobile defender, int totalDamage)
        : base(TimeSpan.Zero, TimeSpan.FromSeconds(0.25),
          12) // 3 seconds at .25 seconds apart = 12.  Confirm delay inbetween of .25 each.
      {
        m_Defender = defender;
        m_DamageRemaining = totalDamage;
        Priority = TimerPriority.TwentyFiveMS;

        DamagePerTick = (double)totalDamage / 12 + .01;
      }

      protected override void OnTick()
      {
        if (!m_Defender.Alive || m_DamageRemaining <= 0)
        {
          Stop();
          m_Table.Remove(m_Defender);
          return;
        }

        m_DamageRemaining -= DamagePerTick;
        m_DamageToDo += DamagePerTick;

        if (m_DamageRemaining <= 0 && m_DamageToDo < 1)
          m_DamageToDo = 1.0; // Confirm this 'round up' at the end

        int damage = (int)m_DamageToDo;

        if (damage > 0)
        {
          // m_Defender.Damage( damage, m_Attacker, false );
          m_Defender.Hits -= damage; // Don't show damage, don't disrupt
          m_DamageToDo -= damage;
        }

        if (!m_Defender.Alive || m_DamageRemaining <= 0)
        {
          Stop();
          m_Table.Remove(m_Defender);
        }
      }
    }
  }
}
