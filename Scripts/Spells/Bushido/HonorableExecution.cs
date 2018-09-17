using System;
using System.Collections;

namespace Server.Spells.Bushido
{
  public class HonorableExecution : SamuraiMove
  {
    private static Hashtable m_Table = new Hashtable();

    public override int BaseMana => 0;
    public override double RequiredSkill => 25.0;

    public override TextDefinition AbilityMessage =>
      new TextDefinition(1063122); // You better kill your enemy with your next hit or you'll be rather sorry...

    public override double GetDamageScalar(Mobile attacker, Mobile defender)
    {
      double bushido = attacker.Skills[SkillName.Bushido].Value;

      // TODO: 20 -> Perfection
      return 1.0 + bushido * 20 / 10000;
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      if (!Validate(attacker) || !CheckMana(attacker, true))
        return;

      ClearCurrentMove(attacker);

      if (m_Table[attacker] is HonorableExecutionInfo info)
      {
        info.Clear();

        info.m_Timer?.Stop();
      }

      if (!defender.Alive)
      {
        attacker.FixedParticles(0x373A, 1, 17, 0x7E2, EffectLayer.Waist);

        double bushido = attacker.Skills[SkillName.Bushido].Value;

        attacker.Hits += 20 + (int)(bushido * bushido / 480.0);

        int swingBonus = Math.Max(1, (int)(bushido * bushido / 720.0));

        info = new HonorableExecutionInfo(attacker, swingBonus);
        info.m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(20.0), new TimerStateCallback(EndEffect), info);

        m_Table[attacker] = info;
      }
      else
      {
        ArrayList mods = new ArrayList
        {
          new ResistanceMod(ResistanceType.Physical, -40),
          new ResistanceMod(ResistanceType.Fire, -40),
          new ResistanceMod(ResistanceType.Cold, -40),
          new ResistanceMod(ResistanceType.Poison, -40),
          new ResistanceMod(ResistanceType.Energy, -40)
        };
        
        double resSpells = attacker.Skills[SkillName.MagicResist].Value;

        if (resSpells > 0.0)
          mods.Add(new DefaultSkillMod(SkillName.MagicResist, true, -resSpells));

        info = new HonorableExecutionInfo(attacker, mods);
        info.m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(7.0), new TimerStateCallback(EndEffect), info);

        m_Table[attacker] = info;
      }

      CheckGain(attacker);
    }

    public static int GetSwingBonus(Mobile target)
    {
      if (!(m_Table[target] is HonorableExecutionInfo info))
        return 0;

      return info.m_SwingBonus;
    }

    public static bool IsUnderPenalty(Mobile target)
    {
      if (!(m_Table[target] is HonorableExecutionInfo info))
        return false;

      return info.m_Penalty;
    }

    public static void RemovePenalty(Mobile target)
    {
      if (!(m_Table[target] is HonorableExecutionInfo info) || !info.m_Penalty)
        return;

      info.Clear();

      info.m_Timer?.Stop();

      m_Table.Remove(target);
    }

    public void EndEffect(object state)
    {
      RemovePenalty(((HonorableExecutionInfo)state).m_Mobile);
    }

    private class HonorableExecutionInfo
    {
      public Mobile m_Mobile;
      public ArrayList m_Mods;
      public bool m_Penalty;
      public int m_SwingBonus;
      public Timer m_Timer;

      public HonorableExecutionInfo(Mobile from, ArrayList mods) : this(from, 0, mods, true)
      {
      }

      public HonorableExecutionInfo(Mobile from, int swingBonus, ArrayList mods = null, bool penalty = false)
      {
        m_Mobile = from;
        m_SwingBonus = swingBonus;
        m_Mods = mods;
        m_Penalty = penalty;

        Apply();
      }

      public void Apply()
      {
        if (m_Mods == null)
          return;

        for (int i = 0; i < m_Mods.Count; ++i)
        {
          object mod = m_Mods[i];

          if (mod is ResistanceMod resistanceMod)
            m_Mobile.AddResistanceMod(resistanceMod);
          else if (mod is SkillMod skillMod)
            m_Mobile.AddSkillMod(skillMod);
        }
      }

      public void Clear()
      {
        if (m_Mods == null)
          return;

        for (int i = 0; i < m_Mods.Count; ++i)
        {
          object mod = m_Mods[i];

          if (mod is ResistanceMod resistanceMod)
            m_Mobile.RemoveResistanceMod(resistanceMod);
          else if (mod is SkillMod skillMod)
            m_Mobile.RemoveSkillMod(skillMod);
        }
      }
    }
  }
}