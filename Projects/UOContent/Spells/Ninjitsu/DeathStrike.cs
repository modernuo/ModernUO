using System;
using System.Collections.Generic;
using Server.Items;
using Server.SkillHandlers;

namespace Server.Spells.Ninjitsu
{
  public class DeathStrike : NinjaMove
  {
    private static readonly Dictionary<Mobile, DeathStrikeInfo> m_Table = new Dictionary<Mobile, DeathStrikeInfo>();

    public override int BaseMana => 30;
    public override double RequiredSkill => 85.0;

    public override TextDefinition AbilityMessage =>
      new TextDefinition(1063091); // You prepare to hit your opponent with a Death Strike.

    public override double GetDamageScalar(Mobile attacker, Mobile defender) => 0.5;

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      if (!Validate(attacker) || !CheckMana(attacker, true))
        return;

      ClearCurrentMove(attacker);

      double ninjitsu = attacker.Skills.Ninjitsu.Value;

      double chance;

      // TODO: should be defined onHit method, what if the player hit and remove the weapon before process? ;)
      bool isRanged = attacker.Weapon is BaseRanged;

      if (ninjitsu < 100) // This formula is an approximation from OSI data.  TODO: find correct formula
        chance = 30 + (ninjitsu - 85) * 2.2;
      else
        chance = 63 + (ninjitsu - 100) * 1.1;

      if (chance / 100 < Utility.RandomDouble())
      {
        attacker.SendLocalizedMessage(1070779); // You missed your opponent with a Death Strike.
        return;
      }

      int damageBonus = 0;

      if (m_Table.TryGetValue(defender, out DeathStrikeInfo info))
      {
        defender.SendLocalizedMessage(1063092); // Your opponent lands another Death Strike!

        if (info.m_Steps > 0)
          damageBonus = attacker.Skills.Ninjitsu.Fixed / 150;

        info.m_Timer?.Stop();

        m_Table.Remove(defender);
      }
      else
      {
        defender.SendLocalizedMessage(1063093); // You have been hit by a Death Strike!  Move with caution!
      }

      attacker.SendLocalizedMessage(1063094); // You inflict a Death Strike upon your opponent!

      defender.FixedParticles(0x374A, 1, 17, 0x26BC, EffectLayer.Waist);
      attacker.PlaySound(attacker.Female ? 0x50D : 0x50E);

      info = new DeathStrikeInfo(defender, attacker, damageBonus, isRanged)
      {
        m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), ProcessDeathStrike, defender)
      };

      m_Table[defender] = info;

      CheckGain(attacker);
    }

    public static void AddStep(Mobile m)
    {
      if (m_Table.TryGetValue(m, out DeathStrikeInfo info) && ++info.m_Steps >= 5)
        ProcessDeathStrike(m);
    }

    private static void ProcessDeathStrike(Mobile defender)
    {
      if (!m_Table.TryGetValue(defender, out DeathStrikeInfo info))
        return;

      int damage;

      double ninjitsu = info.m_Attacker.Skills.Ninjitsu.Value;
      double stalkingBonus = Tracking.GetStalkingBonus(info.m_Attacker, info.m_Target);

      if (Core.ML)
      {
        double scalar = (info.m_Attacker.Skills.Hiding.Value +
                         info.m_Attacker.Skills.Stealth.Value) / 220;

        if (scalar > 1)
          scalar = 1;

        // New formula doesn't apply DamageBonus anymore, caps must be, directly, 60/30.
        if (info.m_Steps >= 5)
          damage = (int)Math.Floor(Math.Min(60, ninjitsu / 3 * (0.3 + 0.7 * scalar) + stalkingBonus));
        else
          damage = (int)Math.Floor(Math.Min(30, ninjitsu / 9 * (0.3 + 0.7 * scalar) + stalkingBonus));

        if (info.m_isRanged)
          damage /= 2;
      }
      else
      {
        int divisor = info.m_Steps >= 5 ? 30 : 80;
        double baseDamage = ninjitsu / divisor * 10;

        int maxDamage = info.m_Steps >= 5 ? 62 : 22;
        damage = Math.Clamp((int)(baseDamage + stalkingBonus), 0, maxDamage) + info.m_DamageBonus;
      }

      if (Core.ML)
        info.m_Target.Damage(damage, info.m_Attacker); // Damage is direct.
      else
        AOS.Damage(info.m_Target, info.m_Attacker, damage, true, 100, 0, 0, 0, 0, 0, 0, false, false,
          true); // Damage is physical.

      info.m_Timer?.Stop();

      m_Table.Remove(info.m_Target);
    }

    private class DeathStrikeInfo
    {
      public readonly Mobile m_Attacker;
      public readonly int m_DamageBonus;
      public readonly bool m_isRanged;
      public int m_Steps;
      public readonly Mobile m_Target;
      public Timer m_Timer;

      public DeathStrikeInfo(Mobile target, Mobile attacker, int damageBonus, bool isRanged)
      {
        m_Target = target;
        m_Attacker = attacker;
        m_DamageBonus = damageBonus;
        m_isRanged = isRanged;
      }
    }
  }
}
