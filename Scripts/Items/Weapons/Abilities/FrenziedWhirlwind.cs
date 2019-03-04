using System;
using System.Collections.Generic;
using Server.Spells;

namespace Server.Items
{
  /// <summary>
  ///   A quick attack to all enemies in range of your weapon that causes damage over time. Requires Bushido or Ninjitsu skill.
  /// </summary>
  public class FrenziedWhirlwind : WeaponAbility
  {
    public override int BaseMana => 30;

    public static Dictionary<Mobile, FrenziedWirlwindTimer> Registry{ get; } = new Dictionary<Mobile, FrenziedWirlwindTimer>();

    public override bool CheckSkills(Mobile from)
    {
      if (GetSkill(from, SkillName.Ninjitsu) < 50.0 && GetSkill(from, SkillName.Bushido) < 50.0)
      {
        from.SendLocalizedMessage(1063347,
          "50"); // You need ~1_SKILL_REQUIREMENT~ Bushido or Ninjitsu skill to perform that attack!
        return false;
      }

      return base.CheckSkills(from);
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      if (!Validate(attacker)) //Mana check after check that there are targets
        return;

      ClearCurrentAbility(attacker);

      Map map = attacker.Map;

      if (!(map != null && attacker.Weapon is BaseWeapon weapon))
        return;

      List<Mobile> list = new List<Mobile>();

      foreach (Mobile m in attacker.GetMobilesInRange(1))
        list.Add(m);

      List<Mobile> targets = new List<Mobile>();

      for (int i = 0; i < list.Count; ++i)
      {
        Mobile m = list[i];

        if (m != defender && m != attacker && SpellHelper.ValidIndirectTarget(attacker, m))
        {
          if (m == null || m.Deleted || m.Map != attacker.Map || !m.Alive || !attacker.CanSee(m) ||
              !attacker.CanBeHarmful(m))
            continue;

          if (!attacker.InRange(m, weapon.MaxRange))
            continue;

          if (attacker.InLOS(m))
            targets.Add(m);
        }
      }

      if (targets.Count == 0 || !CheckMana(attacker, true))
        return;

      attacker.FixedEffect(0x3728, 10, 15);
      attacker.PlaySound(0x2A1);

      // 5-15 damage
      int amount = (int)(10.0 * ((Math.Max(attacker.Skills.Bushido.Value,
                                    attacker.Skills.Ninjitsu.Value) - 50.0) / 70.0 + 5));

      for (int i = 0; i < targets.Count; ++i)
      {
        Mobile m = targets[i];
        attacker.DoHarmful(m, true);

        if (Registry.TryGetValue(m, out FrenziedWirlwindTimer timer))
        {
          timer.Stop();
          Registry.Remove(m);
        }

        timer = new FrenziedWirlwindTimer(attacker, m, amount);
        timer.Start();
        Registry.Add(m, timer);
      }

      Timer.DelayCall(TimeSpan.FromSeconds(2.0), RepeatEffect, attacker);
    }

    private void RepeatEffect(Mobile attacker)
    {
      attacker.FixedEffect(0x3728, 10, 15);
      attacker.PlaySound(0x2A1);
    }

    public class FrenziedWirlwindTimer : Timer
    {
      private readonly double DamagePerTick;
      private Mobile m_Attacker;
      private double m_DamageRemaining;
      private double m_DamageToDo;
      private Mobile m_Defender;

      public FrenziedWirlwindTimer(Mobile attacker, Mobile defender, int totalDamage)
        : base(TimeSpan.Zero, TimeSpan.FromSeconds(0.25),
          12) // 3 seconds at .25 seconds apart = 12.  Confirm delay in between of .25 each.
      {
        m_Attacker = attacker;
        m_Defender = defender;

        m_DamageRemaining = totalDamage;
        DamagePerTick = (double)totalDamage / 12 + 0.01;

        Priority = TimerPriority.TwentyFiveMS;
      }

      protected override void OnTick()
      {
        if (!m_Defender.Alive || m_DamageRemaining <= 0)
        {
          Stop();
          Registry.Remove(m_Defender);
          return;
        }

        m_DamageRemaining -= DamagePerTick;
        m_DamageToDo += DamagePerTick;

        if (m_DamageRemaining <= 0 && m_DamageToDo < 1)
          m_DamageToDo = 1.0; //Confirm this 'round up' at the end

        int damage = (int)m_DamageToDo;

        if (damage > 0)
        {
          m_Defender.Damage(damage, m_Attacker);
          m_DamageToDo -= damage;
        }

        if (!m_Defender.Alive || m_DamageRemaining <= 0)
        {
          Stop();
          Registry.Remove(m_Defender);
        }
      }
    }
  }
}
