using System;
using System.Collections.Generic;

namespace Server.Engines.ConPVP
{
  public class LadderController : Item
  {
    [Constructible]
    public LadderController() : base(0x1B7A)
    {
      Visible = false;
      Movable = false;

      Ladder = new Ladder();

      Ladder.Instance ??= Ladder;
    }

    public LadderController(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.Administrator)]
    public Ladder Ladder { get; private set; }

    public override string DefaultName => "ladder controller";

    public override void Delete()
    {
      if (Ladder.Instance == Ladder)
        Ladder.Instance = null;

      base.Delete();
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1);

      Ladder.Serialize(writer);

      writer.Write(Ladder.Instance == Ladder);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
        case 0:
          {
            Ladder = new Ladder(reader);

            if (version < 1 || reader.ReadBool())
              Ladder.Instance = Ladder;

            break;
          }
      }
    }
  }

  public class Ladder
  {
    private static readonly int[] m_ShortLevels =
    {
      1,
      2,
      3, 3,
      4, 4,
      5, 5, 5,
      6, 6, 6,
      7, 7, 7, 7,
      8, 8, 8, 8,
      9, 9, 9, 9, 9
    };

    private static readonly int[] m_BaseXP =
    {
      0, 100, 200, 400, 600, 900, 1200, 1600, 2000, 2500
    };

    private static readonly int[] m_LossFactors =
    {
      10,
      11, 11,
      25, 25,
      43, 43,
      67, 67
    };

    private static readonly int[,] m_OffsetScalar =
    {
      /* { win, los } */
      /* -6 */ { 175, 25 },
      /* -5 */ { 165, 35 },
      /* -4 */ { 155, 45 },
      /* -3 */ { 145, 55 },
      /* -2 */ { 130, 70 },
      /* -1 */ { 115, 85 },
      /*  0 */ { 100, 100 },
      /* +1 */ { 90, 110 },
      /* +2 */ { 80, 120 },
      /* +3 */ { 70, 130 },
      /* +4 */ { 60, 140 },
      /* +5 */ { 50, 150 },
      /* +6 */ { 40, 160 }
    };

    public List<LadderEntry> Entries { get; } = new List<LadderEntry>();

    private readonly Dictionary<Mobile, LadderEntry> m_Table;

    public Ladder() => m_Table = new Dictionary<Mobile, LadderEntry>();

    public Ladder(IGenericReader reader)
    {
      int version = reader.ReadEncodedInt();

      switch (version)
      {
        case 1:
        case 0:
          {
            int count = reader.ReadEncodedInt();

            m_Table = new Dictionary<Mobile, LadderEntry>(count);
            Entries = new List<LadderEntry>(count);

            for (int i = 0; i < count; ++i)
            {
              LadderEntry entry = new LadderEntry(reader, this, version);

              if (entry.Mobile != null)
              {
                m_Table[entry.Mobile] = entry;
                entry.Index = Entries.Count;
                Entries.Add(entry);
              }
            }

            if (version == 0)
            {
              Entries.Sort();

              for (int i = 0; i < Entries.Count; ++i)
              {
                LadderEntry entry = Entries[i];

                entry.Index = i;
              }
            }

            break;
          }
      }
    }

    public static Ladder Instance { get; set; }

    public static int GetLevel(int xp)
    {
      if (xp >= 22500)
        return 50;
      if (xp >= 2500)
        return 10 + (xp - 2500) / 500;

      return m_ShortLevels[Math.Max(xp, 0) / 100];
    }

    public static void GetLevelInfo(int level, out int xpBase, out int xpAdvance)
    {
      if (level >= 10)
      {
        xpBase = 2500 + (level - 10) * 500;
        xpAdvance = 500;
      }
      else
      {
        xpBase = m_BaseXP[level - 1];
        xpAdvance = m_BaseXP[level] - xpBase;
      }
    }

    public static int GetLossFactor(int level)
    {
      if (level >= 10)
        return 100;

      return m_LossFactors[level - 1];
    }

    public static int GetOffsetScalar(int ourLevel, int theirLevel, bool win)
    {
      int x = ourLevel - theirLevel;

      if (x < -6 || x > +6)
        return 0;

      int y = win ? 0 : 1;

      return m_OffsetScalar[x + 6, y];
    }

    public static int GetExperienceGain(LadderEntry us, LadderEntry them, bool weWon)
    {
      if (us == null || them == null)
        return 0;

      int ourLevel = GetLevel(us.Experience);
      int theirLevel = GetLevel(them.Experience);

      int scalar = GetOffsetScalar(ourLevel, theirLevel, weWon);

      if (scalar == 0)
        return 0;

      int xp = 25 * scalar;

      if (!weWon)
        xp = xp * GetLossFactor(ourLevel) / 100;

      xp /= 100;

      if (xp <= 0)
        xp = 1;

      return xp * (weWon ? 1 : -1);
    }

    private int Swap(int idx, int newIdx)
    {
      LadderEntry hold = Entries[idx];

      Entries[idx] = Entries[newIdx];
      Entries[newIdx] = hold;

      Entries[idx].Index = idx;
      Entries[newIdx].Index = newIdx;

      return newIdx;
    }

    public void UpdateEntry(LadderEntry entry)
    {
      int index = entry.Index;

      if (index >= 0 && index < Entries.Count)
      {
        while (index - 1 >= 0 && entry.CompareTo(Entries[index - 1]) < 0)
          index = Swap(index, index - 1);

        while (index + 1 < Entries.Count && entry.CompareTo(Entries[index + 1]) > 0)
          index = Swap(index, index + 1);
      }
    }

    public LadderEntry Find(Mobile mob)
    {
      if (m_Table.TryGetValue(mob, out LadderEntry entry))
      {
        m_Table[mob] = entry = new LadderEntry(mob, this);
        entry.Index = Entries.Count;
        Entries.Add(entry);
      }

      return entry;
    }

    public LadderEntry FindNoCreate(Mobile mob)
    {
      m_Table.TryGetValue(mob, out LadderEntry entry);
      return entry;
    }

    public void Serialize(IGenericWriter writer)
    {
      writer.WriteEncodedInt(1); // version;

      writer.WriteEncodedInt(Entries.Count);

      for (int i = 0; i < Entries.Count; ++i)
        Entries[i].Serialize(writer);
    }
  }

  public class LadderEntry : IComparable<LadderEntry>
  {
    private int m_Experience;
    private readonly Ladder m_Ladder;

    public LadderEntry(Mobile mob, Ladder ladder)
    {
      m_Ladder = ladder;
      Mobile = mob;
    }

    public LadderEntry(IGenericReader reader, Ladder ladder, int version)
    {
      m_Ladder = ladder;

      switch (version)
      {
        case 1:
        case 0:
          {
            Mobile = reader.ReadMobile();
            m_Experience = reader.ReadEncodedInt();
            Wins = reader.ReadEncodedInt();
            Losses = reader.ReadEncodedInt();

            break;
          }
      }
    }

    public Mobile Mobile { get; }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Experience
    {
      get => m_Experience;
      set
      {
        m_Experience = value;
        m_Ladder.UpdateEntry(this);
      }
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Wins { get; set; }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Losses { get; set; }

    public int Index { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Rank => Index;

    public int CompareTo(LadderEntry l) => (l?.m_Experience ?? 0) - m_Experience;

    public void Serialize(IGenericWriter writer)
    {
      writer.Write(Mobile);
      writer.WriteEncodedInt(m_Experience);
      writer.WriteEncodedInt(Wins);
      writer.WriteEncodedInt(Losses);
    }
  }
}
