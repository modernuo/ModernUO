using System;
using System.Collections.Generic;

namespace Server.Spells.Bushido
{
  public class HonorableExecution : SamuraiMove
  {
    private static readonly Dictionary<Mobile, HonorableExecutionInfo> m_Table = new Dictionary<Mobile, HonorableExecutionInfo>();

    public override int BaseMana => 0;
    public override double RequiredSkill => 25.0;

    public override TextDefinition AbilityMessage =>
      new TextDefinition(1063122); // You better kill your enemy with your next hit or you'll be rather sorry...

    public override double GetDamageScalar(Mobile attacker, Mobile defender)
    {
      double bushido = attacker.Skills.Bushido.Value;

      // TODO: 20 -> Perfection
      return 1.0 + bushido * 20 / 10000;
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      if (!Validate(attacker) || !CheckMana(attacker, true))
        return;

      ClearCurrentMove(attacker);

      if (m_Table.TryGetValue(attacker, out HonorableExecutionInfo info))
      {
        info.Clear();
        info.m_Timer?.Stop();
      }

      if (!defender.Alive)
      {
        attacker.FixedParticles(0x373A, 1, 17, 0x7E2, EffectLayer.Waist);

        double bushido = attacker.Skills.Bushido.Value;

        attacker.Hits += 20 + (int)(bushido * bushido / 480.0);

        int swingBonus = Math.Max((int)(bushido * bushido / 720.0), 1);

        info = new HonorableExecutionInfo(attacker, swingBonus);
        info.m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(20.0), RemovePenalty, info.m_Mobile);

        m_Table[attacker] = info;
      }
      else
      {
        List<object> mods = new List<object>
        {
          new ResistanceMod(ResistanceType.Physical, -40),
          new ResistanceMod(ResistanceType.Fire, -40),
          new ResistanceMod(ResistanceType.Cold, -40),
          new ResistanceMod(ResistanceType.Poison, -40),
          new ResistanceMod(ResistanceType.Energy, -40)
        };

        double resSpells = attacker.Skills.MagicResist.Value;

        if (resSpells > 0.0)
          mods.Add(new DefaultSkillMod(SkillName.MagicResist, true, -resSpells));

        info = new HonorableExecutionInfo(attacker, mods);
        info.m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(7.0), RemovePenalty, info.m_Mobile);

        m_Table[attacker] = info;
      }

      CheckGain(attacker);
    }

    public static int GetSwingBonus(Mobile target) => m_Table.TryGetValue(target, out HonorableExecutionInfo info) ? info.m_SwingBonus : 0;

    public static bool IsUnderPenalty(Mobile target) => m_Table.TryGetValue(target, out HonorableExecutionInfo info) && info.m_Penalty;

    public static void RemovePenalty(Mobile target)
    {
      if (!m_Table.TryGetValue(target, out HonorableExecutionInfo info) || !info.m_Penalty)
        return;

      info.Clear();
      info.m_Timer?.Stop();
      m_Table.Remove(target);
    }

    private class HonorableExecutionInfo
    {
      public readonly Mobile m_Mobile;
      public readonly List<object> m_Mods;
      public readonly bool m_Penalty;
      public readonly int m_SwingBonus;
      public Timer m_Timer;

      public HonorableExecutionInfo(Mobile from, List<object> mods) : this(from, 0, mods, mods != null)
      {
      }

      public HonorableExecutionInfo(Mobile from, int swingBonus, List<object> mods = null, bool penalty = false)
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
