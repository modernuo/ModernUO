using System;
using System.Collections;

namespace Server.Engines.ConPVP
{
  public class LadderController : Item
  {
    private Ladder m_Ladder;

    [Constructible]
    public LadderController() : base(0x1B7A)
    {
      Visible = false;
      Movable = false;

      m_Ladder = new Ladder();

      if (Ladder.Instance == null)
        Ladder.Instance = m_Ladder;
    }

    public LadderController(Serial serial) : base(serial)
    {
    }

    //[CommandProperty( AccessLevel.GameMaster )]
    public Ladder Ladder
    {
      get => m_Ladder;
      set { }
    }

    public override string DefaultName => "ladder controller";

    public override void Delete()
    {
      if (Ladder.Instance == m_Ladder)
        Ladder.Instance = null;

      base.Delete();
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1);

      m_Ladder.Serialize(writer);

      writer.Write(Ladder.Instance == m_Ladder);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
        case 0:
        {
          m_Ladder = new Ladder(reader);

          if (version < 1 || reader.ReadBool())
            Ladder.Instance = m_Ladder;

          break;
        }
      }
    }
  }

  public class Ladder
  {
    private static int[] m_ShortLevels =
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

    private static int[] m_BaseXP =
    {
      0, 100, 200, 400, 600, 900, 1200, 1600, 2000, 2500
    };

    private static int[] m_LossFactors =
    {
      10,
      11, 11,
      25, 25,
      43, 43,
      67, 67
    };

    private static int[,] m_OffsetScalar =
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

    private ArrayList m_Entries;

    private Hashtable m_Table;

    public Ladder()
    {
      m_Table = new Hashtable();
      m_Entries = new ArrayList();
    }

    public Ladder(GenericReader reader)
    {
      int version = reader.ReadEncodedInt();

      switch (version)
      {
        case 1:
        case 0:
        {
          int count = reader.ReadEncodedInt();

          m_Table = new Hashtable(count);
          m_Entries = new ArrayList(count);

          for (int i = 0; i < count; ++i)
          {
            LadderEntry entry = new LadderEntry(reader, this, version);

            if (entry.Mobile != null)
            {
              m_Table[entry.Mobile] = entry;
              entry.Index = m_Entries.Count;
              m_Entries.Add(entry);
            }
          }

          if (version == 0)
          {
            m_Entries.Sort();

            for (int i = 0; i < m_Entries.Count; ++i)
            {
              LadderEntry entry = (LadderEntry)m_Entries[i];

              entry.Index = i;
            }
          }

          break;
        }
      }
    }

    public static Ladder Instance{ get; set; }

    public static int GetLevel(int xp)
    {
      if (xp >= 22500)
        return 50;
      if (xp >= 2500)
        return 10 + (xp - 2500) / 500;
      if (xp < 0)
        xp = 0;

      return m_ShortLevels[xp / 100];
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

    public ArrayList ToArrayList()
    {
      return m_Entries;
    }

    private int Swap(int idx, int newIdx)
    {
      object hold = m_Entries[idx];

      m_Entries[idx] = m_Entries[newIdx];
      m_Entries[newIdx] = hold;

      ((LadderEntry)m_Entries[idx]).Index = idx;
      ((LadderEntry)m_Entries[newIdx]).Index = newIdx;

      return newIdx;
    }

    public void UpdateEntry(LadderEntry entry)
    {
      int index = entry.Index;

      if (index >= 0 && index < m_Entries.Count)
      {
        // sanity

        int c;

        while (index - 1 >= 0 && (c = entry.CompareTo(m_Entries[index - 1])) < 0)
          index = Swap(index, index - 1);

        while (index + 1 < m_Entries.Count && (c = entry.CompareTo(m_Entries[index + 1])) > 0)
          index = Swap(index, index + 1);
      }
    }

    public LadderEntry Find(Mobile mob)
    {
      LadderEntry entry = (LadderEntry)m_Table[mob];

      if (entry == null)
      {
        m_Table[mob] = entry = new LadderEntry(mob, this);
        entry.Index = m_Entries.Count;
        m_Entries.Add(entry);
      }

      return entry;
    }

    public LadderEntry FindNoCreate(Mobile mob)
    {
      return m_Table[mob] as LadderEntry;
    }

    public void Serialize(GenericWriter writer)
    {
      writer.WriteEncodedInt(1); // version;

      writer.WriteEncodedInt(m_Entries.Count);

      for (int i = 0; i < m_Entries.Count; ++i)
        ((LadderEntry)m_Entries[i]).Serialize(writer);
    }
  }

  public class LadderEntry : IComparable
  {
    private int m_Experience;
    private Ladder m_Ladder;

    public LadderEntry(Mobile mob, Ladder ladder)
    {
      m_Ladder = ladder;
      Mobile = mob;
    }

    public LadderEntry(GenericReader reader, Ladder ladder, int version)
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

    public Mobile Mobile{ get; }

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
    public int Wins{ get; set; }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Losses{ get; set; }

    public int Index{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Rank => Index;

    public int CompareTo(object obj)
    {
      return ((LadderEntry)obj).m_Experience - m_Experience;
    }

    public void Serialize(GenericWriter writer)
    {
      writer.Write(Mobile);
      writer.WriteEncodedInt(m_Experience);
      writer.WriteEncodedInt(Wins);
      writer.WriteEncodedInt(Losses);
    }
  }
}