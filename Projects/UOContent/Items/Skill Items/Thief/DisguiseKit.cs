using System;
using System.Collections.Generic;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Seventh;

namespace Server.Items
{
  public class DisguiseKit : Item
  {
    [Constructible]
    public DisguiseKit() : base(0xE05) => Weight = 1.0;

    public DisguiseKit(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1041078; // a disguise kit

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    public bool ValidateUse(Mobile from)
    {
      PlayerMobile pm = from as PlayerMobile;

      if (!IsChildOf(from.Backpack))
        from.SendLocalizedMessage(1042001);
      else if (pm == null || pm.NpcGuild != NpcGuild.ThievesGuild)
        from.SendLocalizedMessage(501702);
      else if (Stealing.SuspendOnMurder && pm.Kills > 0)
        from.SendLocalizedMessage(501703);
      else if (!from.CanBeginAction<IncognitoSpell>())
        from.SendLocalizedMessage(501704);
      else if (Sigil.ExistsOn(from))
        from.SendLocalizedMessage(1010465); // You cannot disguise yourself while holding a sigil
      else if (TransformationSpellHelper.UnderTransformation(from))
        from.SendLocalizedMessage(1061634);
      else if (from.BodyMod == 183 || from.BodyMod == 184)
        from.SendLocalizedMessage(1040002);
      else if (!from.CanBeginAction<PolymorphSpell>() || from.IsBodyMod)
        from.SendLocalizedMessage(501705);
      else
        return true;

      return false;
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (ValidateUse(from))
        from.SendGump(new DisguiseGump(from, this, true, false));
    }
  }

  public class DisguiseGump : Gump
  {
    private static readonly DisguiseEntry[] m_HairEntries =
    {
      new DisguiseEntry(8251, 50700, 0, 5, 1011052), // Short
      new DisguiseEntry(8261, 60710, 0, 3, 1011047), // Pageboy
      new DisguiseEntry(8252, 60708, 0, -5, 1011053), // Long
      new DisguiseEntry(8264, 60901, 0, 5, 1011048), // Receding
      new DisguiseEntry(8253, 60702, 0, -5, 1011054), // Ponytail
      new DisguiseEntry(8265, 60707, 0, -5, 1011049), // 2-tails
      new DisguiseEntry(8260, 50703, 0, 5, 1011055), // Mohawk
      new DisguiseEntry(8266, 60713, 0, 10, 1011050), // Topknot
      null,
      new DisguiseEntry(0, 0, 0, 0, 1011051) // None
    };

    private static readonly DisguiseEntry[] m_BeardEntries =
    {
      new DisguiseEntry(8269, 50906, 0, 0, 1011401), // Vandyke
      new DisguiseEntry(8257, 50808, 0, -2, 1011062), // Mustache
      new DisguiseEntry(8255, 50802, 0, 0, 1011060), // Short beard
      new DisguiseEntry(8268, 50905, 0, -10, 1011061), // Long beard
      new DisguiseEntry(8267, 50904, 0, 0, 1011060), // Short beard
      new DisguiseEntry(8254, 50801, 0, -10, 1011061), // Long beard
      null,
      new DisguiseEntry(0, 0, 0, 0, 1011051) // None
    };

    private readonly Mobile m_From;
    private readonly DisguiseKit m_Kit;
    private readonly bool m_Used;

    public DisguiseGump(Mobile from, DisguiseKit kit, bool startAtHair, bool used) : base(50, 50)
    {
      m_From = from;
      m_Kit = kit;
      m_Used = used;

      from.CloseGump<DisguiseGump>();

      AddPage(0);

      AddBackground(100, 10, 400, 385, 2600);

      // <center>THIEF DISGUISE KIT</center>
      AddHtmlLocalized(100, 25, 400, 35, 1011045);

      AddButton(140, 353, 4005, 4007, 0);
      AddHtmlLocalized(172, 355, 90, 35, 1011036); // OKAY

      AddButton(257, 353, 4005, 4007, 1);
      AddHtmlLocalized(289, 355, 90, 35, 1011046); // APPLY

      if (from.Female || from.Body.IsFemale)
      {
        DrawEntries(0, 1, -1, m_HairEntries, -1);
      }
      else if (startAtHair)
      {
        DrawEntries(0, 1, 2, m_HairEntries, 1011056);
        DrawEntries(1, 2, 1, m_BeardEntries, 1011059);
      }
      else
      {
        DrawEntries(1, 1, 2, m_BeardEntries, 1011059);
        DrawEntries(0, 2, 1, m_HairEntries, 1011056);
      }
    }

    private void DrawEntries(int index, int page, int nextPage, DisguiseEntry[] entries, int nextNumber)
    {
      AddPage(page);

      if (nextPage != -1)
      {
        AddButton(155, 320, 250 + index * 2, 251 + index * 2, 0, GumpButtonType.Page, nextPage);
        AddHtmlLocalized(180, 320, 150, 35, nextNumber);
      }

      for (int i = 0; i < entries.Length; ++i)
      {
        DisguiseEntry entry = entries[i];

        if (entry == null)
          continue;

        int x = i % 2 * 205;
        int y = i / 2 * 55;

        if (entry.m_GumpID != 0)
        {
          AddBackground(220 + x, 60 + y, 50, 50, 2620);
          AddImage(153 + x + entry.m_OffsetX, 15 + y + entry.m_OffsetY, entry.m_GumpID);
        }

        AddHtmlLocalized(140 + x, 72 + y, 80, 35, entry.m_Number);
        AddRadio(118 + x, 73 + y, 208, 209, false, i * 2 + index);
      }
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
      if (info.ButtonID == 0)
      {
        if (m_Used)
          m_From.SendLocalizedMessage(501706); // Disguises wear off after 2 hours.
        else
          m_From.SendLocalizedMessage(501707); // You're looking good.

        return;
      }

      int[] switches = info.Switches;

      if (switches.Length == 0)
        return;

      int switched = switches[0];
      int type = switched % 2;
      int index = switched / 2;

      bool hair = type == 0;

      DisguiseEntry[] entries = hair ? m_HairEntries : m_BeardEntries;

      if (index >= 0 && index < entries.Length)
      {
        DisguiseEntry entry = entries[index];

        if (entry == null)
          return;

        if (!m_Kit.ValidateUse(m_From))
          return;

        if (!hair && (m_From.Female || m_From.Body.IsFemale))
          return;

        m_From.NameMod = NameList.RandomName(m_From.Female ? "female" : "male");

        if (m_From is PlayerMobile pm)
        {
          if (hair)
            pm.SetHairMods(entry.m_ItemID, -2);
          else
            pm.SetHairMods(-2, entry.m_ItemID);
        }

        m_From.SendGump(new DisguiseGump(m_From, m_Kit, hair, true));

        DisguiseTimers.RemoveTimer(m_From);

        DisguiseTimers.CreateTimer(m_From, TimeSpan.FromHours(2.0));
        DisguiseTimers.StartTimer(m_From);
      }
    }

    private class DisguiseEntry
    {
      public readonly int m_GumpID;
      public readonly int m_ItemID;
      public readonly int m_Number;
      public readonly int m_OffsetX;
      public readonly int m_OffsetY;

      public DisguiseEntry(int itemID, int gumpID, int ox, int oy, int name)
      {
        m_ItemID = itemID;
        m_GumpID = gumpID;
        m_OffsetX = ox;
        m_OffsetY = oy;
        m_Number = name;
      }
    }
  }

  public class DisguiseTimers
  {
    public static Dictionary<Mobile, Timer> Timers { get; } = new Dictionary<Mobile, Timer>();

    public static void Initialize()
    {
      new DisguisePersistance();
    }

    public static void CreateTimer(Mobile m, TimeSpan delay)
    {
      if (m != null && !IsDisguised(m))
        Timers[m] = new InternalTimer(m, delay);
    }

    public static void StartTimer(Mobile m)
    {
      Timers.TryGetValue(m, out Timer t);
      t?.Start();
    }

    public static bool IsDisguised(Mobile m) => Timers.ContainsKey(m);

    public static void StopTimer(Mobile m)
    {
      if (!Timers.TryGetValue(m, out Timer t))
        return;

      TimeSpan ts = t.Next - DateTime.UtcNow;
      if (ts < TimeSpan.Zero)
        ts = TimeSpan.Zero;

      t.Delay = ts;
      t.Stop();
    }

    public static void RemoveTimer(Mobile m)
    {
      if (Timers.TryGetValue(m, out Timer t))
      {
        t.Stop();
        Timers.Remove(m);
      }
    }

    public static TimeSpan TimeRemaining(Mobile m) => Timers.TryGetValue(m, out Timer t) ? t.Next - DateTime.UtcNow : TimeSpan.Zero;

    private class InternalTimer : Timer
    {
      private readonly Mobile m_Player;

      public InternalTimer(Mobile m, TimeSpan delay) : base(delay)
      {
        m_Player = m;
        Priority = TimerPriority.OneMinute;
      }

      protected override void OnTick()
      {
        m_Player.NameMod = null;

        if (m_Player is PlayerMobile mobile)
          mobile.SetHairMods(-1, -1);

        RemoveTimer(m_Player);
      }
    }
  }
}
