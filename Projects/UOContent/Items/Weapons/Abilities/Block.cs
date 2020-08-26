using System;
using System.Collections.Generic;

namespace Server.Items
{
  /// <summary>
  ///   Raises your defenses for a short time. Requires Bushido or Ninjitsu skill.
  /// </summary>
  public class Block : WeaponAbility
  {
    private static readonly Dictionary<Mobile, BlockInfo> m_Table = new Dictionary<Mobile, BlockInfo>();

    public override int BaseMana => 30;

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
      if (!Validate(attacker) || !CheckMana(attacker, true))
        return;

      ClearCurrentAbility(attacker);

      attacker.SendLocalizedMessage(1063345); // You block an attack!
      defender.SendLocalizedMessage(1063346); // Your attack was blocked!

      attacker.FixedParticles(0x37C4, 1, 16, 0x251D, 0x39D, 0x3, EffectLayer.RightHand);

      int bonus = (int)(10.0 * ((Math.Max(attacker.Skills.Bushido.Value,
                                   attacker.Skills.Ninjitsu.Value) - 50.0) / 70.0 + 5));

      BeginBlock(attacker, bonus);
    }

    public static bool GetBonus(Mobile targ, ref int bonus)
    {
      if (!m_Table.TryGetValue(targ, out BlockInfo info))
        return false;

      bonus = info.m_Bonus;
      return true;
    }

    public static void BeginBlock(Mobile m, int bonus)
    {
      EndBlock(m);
      m_Table[m] = new BlockInfo(m, bonus);
    }

    public static void EndBlock(Mobile m)
    {
      if (!m_Table.TryGetValue(m, out BlockInfo info))
        return;

      info.m_Timer?.Stop();
      m_Table.Remove(m);
    }

    private class BlockInfo
    {
      public readonly int m_Bonus;
      public readonly Timer m_Timer;

      public BlockInfo(Mobile target, int bonus)
      {
        m_Bonus = bonus;
        m_Timer = new InternalTimer(target);
      }
    }

    private class InternalTimer : Timer
    {
      private readonly Mobile m_Mobile;

      public InternalTimer(Mobile m) : base(TimeSpan.FromSeconds(6.0))
      {
        m_Mobile = m;
        Priority = TimerPriority.TwoFiftyMS;
      }

      protected override void OnTick()
      {
        EndBlock(m_Mobile);
      }
    }
  }
}
