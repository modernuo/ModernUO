using System;
using System.Collections.Generic;
using Server.Utilities;

namespace Server.Spells
{
  public class SpellRegistry
  {
    private static readonly Type[] m_Types = new Type[700];
    private static int m_Count;

    private static readonly Dictionary<Type, int> m_IDsFromTypes = new Dictionary<Type, int>(m_Types.Length);

    private static readonly object[] m_Params = new object[2];

    private static readonly string[] m_CircleNames =
    {
      "First",
      "Second",
      "Third",
      "Fourth",
      "Fifth",
      "Sixth",
      "Seventh",
      "Eighth",
      "Necromancy",
      "Chivalry",
      "Bushido",
      "Ninjitsu",
      "Spellweaving"
    };

    public static Type[] Types
    {
      get
      {
        m_Count = -1;
        return m_Types;
      }
    }

    // What IS this used for anyways.
    public static int Count
    {
      get
      {
        if (m_Count == -1)
        {
          m_Count = 0;

          for (int i = 0; i < m_Types.Length; ++i)
            if (m_Types[i] != null)
              ++m_Count;
        }

        return m_Count;
      }
    }

    public static Dictionary<int, SpecialMove> SpecialMoves { get; } = new Dictionary<int, SpecialMove>();

    public static int GetRegistryNumber(ISpell s) => GetRegistryNumber(s.GetType());

    public static int GetRegistryNumber(SpecialMove s) => GetRegistryNumber(s.GetType());

    public static int GetRegistryNumber(Type type) => m_IDsFromTypes.TryGetValue(type, out int value) ? value : -1;

    public static void Register(int spellID, Type type)
    {
      if (spellID < 0 || spellID >= m_Types.Length)
        return;

      if (m_Types[spellID] == null)
        ++m_Count;

      m_Types[spellID] = type;

      if (!m_IDsFromTypes.ContainsKey(type))
        m_IDsFromTypes.Add(type, spellID);

      if (type.IsSubclassOf(typeof(SpecialMove)))
      {
        SpecialMove spm = null;

        try
        {
          spm = ActivatorUtil.CreateInstance(type) as SpecialMove;
        }
        catch
        {
          // ignored
        }

        if (spm != null)
          SpecialMoves.Add(spellID, spm);
      }
    }

    public static SpecialMove GetSpecialMove(int spellID)
    {
      if (spellID < 0 || spellID >= m_Types.Length)
        return null;

      Type t = m_Types[spellID];

      if (t == null || !t.IsSubclassOf(typeof(SpecialMove)))
        return null;

      SpecialMoves.TryGetValue(spellID, out SpecialMove move);
      return move;
    }

    public static Spell NewSpell(int spellID, Mobile caster, Item scroll)
    {
      if (spellID < 0 || spellID >= m_Types.Length)
        return null;

      Type t = m_Types[spellID];

      if (t?.IsSubclassOf(typeof(SpecialMove)) == false)
      {
        m_Params[0] = caster;
        m_Params[1] = scroll;

        try
        {
          return (Spell)ActivatorUtil.CreateInstance(t, m_Params);
        }
        catch
        {
          // ignored
        }
      }

      return null;
    }

    public static Spell NewSpell(string name, Mobile caster, Item scroll)
    {
      for (int i = 0; i < m_CircleNames.Length; ++i)
      {
        Type t = AssemblyHandler.FindFirstTypeForName($"Server.Spells.{m_CircleNames[i]}.{name}");

        if (t?.IsSubclassOf(typeof(SpecialMove)) == false)
        {
          m_Params[0] = caster;
          m_Params[1] = scroll;

          try
          {
            return (Spell)ActivatorUtil.CreateInstance(t, m_Params);
          }
          catch
          {
            // ignored
          }
        }
      }

      return null;
    }
  }
}
