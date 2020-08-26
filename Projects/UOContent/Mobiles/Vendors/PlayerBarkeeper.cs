using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Mobiles
{
  public class ChangeRumorMessagePrompt : Prompt
  {
    private readonly PlayerBarkeeper m_Barkeeper;
    private readonly int m_RumorIndex;

    public ChangeRumorMessagePrompt(PlayerBarkeeper barkeeper, int rumorIndex)
    {
      m_Barkeeper = barkeeper;
      m_RumorIndex = rumorIndex;
    }

    public override void OnCancel(Mobile from)
    {
      OnResponse(from, "");
    }

    public override void OnResponse(Mobile from, string text)
    {
      if (text.Length > 130)
        text = text.Substring(0, 130);

      m_Barkeeper.EndChangeRumor(from, m_RumorIndex, text);
    }
  }

  public class ChangeRumorKeywordPrompt : Prompt
  {
    private readonly PlayerBarkeeper m_Barkeeper;
    private readonly int m_RumorIndex;

    public ChangeRumorKeywordPrompt(PlayerBarkeeper barkeeper, int rumorIndex)
    {
      m_Barkeeper = barkeeper;
      m_RumorIndex = rumorIndex;
    }

    public override void OnCancel(Mobile from)
    {
      OnResponse(from, "");
    }

    public override void OnResponse(Mobile from, string text)
    {
      if (text.Length > 130)
        text = text.Substring(0, 130);

      m_Barkeeper.EndChangeKeyword(from, m_RumorIndex, text);
    }
  }

  public class ChangeTipMessagePrompt : Prompt
  {
    private readonly PlayerBarkeeper m_Barkeeper;

    public ChangeTipMessagePrompt(PlayerBarkeeper barkeeper) => m_Barkeeper = barkeeper;

    public override void OnCancel(Mobile from)
    {
      OnResponse(from, "");
    }

    public override void OnResponse(Mobile from, string text)
    {
      if (text.Length > 130)
        text = text.Substring(0, 130);

      m_Barkeeper.EndChangeTip(from, text);
    }
  }

  public class BarkeeperRumor
  {
    public BarkeeperRumor(string message, string keyword)
    {
      Message = message;
      Keyword = keyword;
    }

    public string Message { get; set; }

    public string Keyword { get; set; }

    public static BarkeeperRumor Deserialize(IGenericReader reader)
    {
      if (!reader.ReadBool())
        return null;

      return new BarkeeperRumor(reader.ReadString(), reader.ReadString());
    }

    public static void Serialize(IGenericWriter writer, BarkeeperRumor rumor)
    {
      if (rumor == null)
      {
        writer.Write(false);
      }
      else
      {
        writer.Write(true);
        writer.Write(rumor.Message);
        writer.Write(rumor.Keyword);
      }
    }
  }

  public class ManageBarkeeperEntry : ContextMenuEntry
  {
    private readonly PlayerBarkeeper m_Barkeeper;
    private readonly Mobile m_From;

    public ManageBarkeeperEntry(Mobile from, PlayerBarkeeper barkeeper) : base(6151, 12)
    {
      m_From = from;
      m_Barkeeper = barkeeper;
    }

    public override void OnClick()
    {
      m_Barkeeper.BeginManagement(m_From);
    }
  }

  public class PlayerBarkeeper : BaseVendor
  {
    private BaseHouse m_House;

    private Timer m_NewsTimer;

    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    public PlayerBarkeeper(Mobile owner, BaseHouse house) : base("the barkeeper")
    {
      Owner = owner;
      House = house;
      Rumors = new BarkeeperRumor[3];

      LoadSBInfo();
    }

    public PlayerBarkeeper(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Owner { get; set; }

    public BaseHouse House
    {
      get => m_House;
      set
      {
        m_House?.PlayerBarkeepers.Remove(this);

        value?.PlayerBarkeepers.Add(this);

        m_House = value;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public string TipMessage { get; set; }

    public override bool IsActiveBuyer => false;
    public override bool IsActiveSeller => m_SBInfos.Count > 0;

    public override bool DisallowAllMoves => true;
    public override bool NoHouseRestrictions => true;

    public BarkeeperRumor[] Rumors { get; private set; }

    public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.ThighBoots : VendorShoeType.Boots;
    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override bool GetGender() => false;

    public override void InitOutfit()
    {
      base.InitOutfit();

      AddItem(new HalfApron(Utility.RandomBrightHue()));

      Container pack = Backpack;

      pack?.Delete();
    }

    public override void InitBody()
    {
      base.InitBody();

      if (BodyValue == 0x340 || BodyValue == 0x402)
        Hue = 0;
      else
        Hue = 0x83F4; // hue is not random

      Container pack = Backpack;

      pack?.Delete();
    }

    public override bool HandlesOnSpeech(Mobile from) => InRange(from, 3) || base.HandlesOnSpeech(from);

    private void ShoutNews_Callback(TownCrierEntry tce, int index)
    {
      if (index < 0 || index >= tce.Lines.Length)
      {
        m_NewsTimer?.Stop();
        m_NewsTimer = null;
      }
      else
      {
        PublicOverheadMessage(MessageType.Regular, 0x3B2, false, tce.Lines[index]);
      }
    }

    public override void OnAfterDelete()
    {
      base.OnAfterDelete();

      House = null;
    }

    public override bool OnBeforeDeath()
    {
      if (!base.OnBeforeDeath())
        return false;

      Item shoes = FindItemOnLayer(Layer.Shoes);

      if (shoes is Sandals)
        shoes.Hue = 0;

      return true;
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
      base.OnSpeech(e);

      if (!e.Handled && InRange(e.Mobile, 3))
      {
        if (m_NewsTimer == null && e.HasKeyword(0x30)) // *news*
        {
          TownCrierEntry tce = GlobalTownCrierEntryList.Instance.GetRandomEntry();

          if (tce == null)
          {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005643); // I have no news at this time.
          }
          else
          {
            int index = 0;
            m_NewsTimer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(3.0),
              () => ShoutNews_Callback(tce, index));

            PublicOverheadMessage(MessageType.Regular, 0x3B2, 502978); // Some of the latest news!
          }
        }

        for (int i = 0; i < Rumors.Length; ++i)
        {
          BarkeeperRumor rumor = Rumors[i];

          string keyword = rumor?.Keyword;

          if (keyword == null || (keyword = keyword.Trim()).Length == 0)
            continue;

          if (Insensitive.Equals(keyword, e.Speech))
          {
            string message = rumor.Message;

            if (message == null || (message = message.Trim()).Length == 0)
              continue;

            PublicOverheadMessage(MessageType.Regular, 0x3B2, false, message);
          }
        }
      }
    }

    public override bool CheckGold(Mobile from, Item dropped)
    {
      if (!(dropped is Gold g))
        return false;

      if (g.Amount > 50)
      {
        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I cannot accept so large a tip!",
          from.NetState);
      }
      else
      {
        string tip = TipMessage;

        if (tip == null || (tip = tip.Trim()).Length == 0)
        {
          PrivateOverheadMessage(MessageType.Regular, 0x3B2, false,
            "It would not be fair of me to take your money and not offer you information in return.",
            from.NetState);
        }
        else
        {
          Direction = GetDirectionTo(from);
          PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, tip, from.NetState);

          g.Delete();
          return true;
        }
      }

      return false;
    }

    public bool IsOwner(Mobile from)
    {
      if (from?.Deleted != false || Deleted)
        return false;

      if (from.AccessLevel > AccessLevel.GameMaster)
        return true;

      return Owner == from;
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
      base.GetContextMenuEntries(from, list);

      if (IsOwner(from) && from.InLOS(this))
        list.Add(new ManageBarkeeperEntry(from, this));
    }

    public void BeginManagement(Mobile from)
    {
      if (!IsOwner(from))
        return;

      from.SendGump(new BarkeeperGump(from, this));
    }

    public void Dismiss()
    {
      Delete();
    }

    public void BeginChangeRumor(Mobile from, int index)
    {
      if (index < 0 || index >= Rumors.Length)
        return;

      from.Prompt = new ChangeRumorMessagePrompt(this, index);
      PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "Say what news you would like me to tell our guests.",
        from.NetState);
    }

    public void EndChangeRumor(Mobile from, int index, string text)
    {
      if (index < 0 || index >= Rumors.Length)
        return;

      if (Rumors[index] == null)
        Rumors[index] = new BarkeeperRumor(text, null);
      else
        Rumors[index].Message = text;

      from.Prompt = new ChangeRumorKeywordPrompt(this, index);
      PrivateOverheadMessage(MessageType.Regular, 0x3B2, false,
        "What keyword should a guest say to me to get this news?", from.NetState);
    }

    public void EndChangeKeyword(Mobile from, int index, string text)
    {
      if (index < 0 || index >= Rumors.Length)
        return;

      if (Rumors[index] == null)
        Rumors[index] = new BarkeeperRumor(null, text);
      else
        Rumors[index].Keyword = text;

      PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I'll pass on the message.", from.NetState);
    }

    public void RemoveRumor(Mobile from, int index)
    {
      if (index < 0 || index >= Rumors.Length)
        return;

      Rumors[index] = null;
    }

    public void BeginChangeTip(Mobile from)
    {
      from.Prompt = new ChangeTipMessagePrompt(this);
      PrivateOverheadMessage(MessageType.Regular, 0x3B2, false,
        "Say what you want me to tell guests when they give me a good tip.", from.NetState);
    }

    public void EndChangeTip(Mobile from, string text)
    {
      TipMessage = text;
      PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I'll say that to anyone who gives me a good tip.",
        from.NetState);
    }

    public void RemoveTip(Mobile from)
    {
      TipMessage = null;
    }

    public void BeginChangeTitle(Mobile from)
    {
      from.SendGump(new BarkeeperTitleGump(from, this));
    }

    public void EndChangeTitle(Mobile from, string title, bool vendor)
    {
      Title = title;

      LoadSBInfo();
    }

    public void CancelChangeTitle(Mobile from)
    {
      from.SendGump(new BarkeeperGump(from, this));
    }

    public void BeginChangeAppearance(Mobile from)
    {
      from.CloseGump<PlayerVendorCustomizeGump>();
      from.SendGump(new PlayerVendorCustomizeGump(this, from));
    }

    public void ChangeGender(Mobile from)
    {
      Female = !Female;

      if (Female)
      {
        Body = 401;
        Name = NameList.RandomName("female");

        FacialHairItemID = 0;
      }
      else
      {
        Body = 400;
        Name = NameList.RandomName("male");
      }
    }

    public override void InitSBInfo()
    {
      if (Title == "the waiter" || Title == "the barkeeper" || Title == "the baker" || Title == "the innkeeper" ||
          Title == "the chef")
      {
        if (m_SBInfos.Count == 0)
          m_SBInfos.Add(new SBPlayerBarkeeper());
      }
      else
      {
        m_SBInfos.Clear();
      }
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version;

      writer.Write(m_House);

      writer.Write(Owner);

      writer.WriteEncodedInt(Rumors.Length);

      for (int i = 0; i < Rumors.Length; ++i)
        BarkeeperRumor.Serialize(writer, Rumors[i]);

      writer.Write(TipMessage);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
          {
            House = (BaseHouse)reader.ReadItem();

            goto case 0;
          }
        case 0:
          {
            Owner = reader.ReadMobile();

            Rumors = new BarkeeperRumor[reader.ReadEncodedInt()];

            for (int i = 0; i < Rumors.Length; ++i)
              Rumors[i] = BarkeeperRumor.Deserialize(reader);

            TipMessage = reader.ReadString();

            break;
          }
      }

      if (version < 1)
        Timer.DelayCall(UpgradeFromVersion0);
    }

    private void UpgradeFromVersion0()
    {
      House = BaseHouse.FindHouseAt(this);
    }
  }

  public class BarkeeperTitleGump : Gump
  {
    private static readonly Entry[] m_Entries =
    {
      new Entry("Alchemist"),
      new Entry("Animal Tamer"),
      new Entry("Apothecary"),
      new Entry("Artist"),
      new Entry("Baker", true),
      new Entry("Bard"),
      new Entry("Barkeep", "the barkeeper", true),
      new Entry("Beggar"),
      new Entry("Blacksmith"),
      new Entry("Bounty Hunter"),
      new Entry("Brigand"),
      new Entry("Butler"),
      new Entry("Carpenter"),
      new Entry("Chef", true),
      new Entry("Commander"),
      new Entry("Curator"),
      new Entry("Drunkard"),
      new Entry("Farmer"),
      new Entry("Fisherman"),
      new Entry("Gambler"),
      new Entry("Gypsy"),
      new Entry("Herald"),
      new Entry("Herbalist"),
      new Entry("Hermit"),
      new Entry("Innkeeper", true),
      new Entry("Jailor"),
      new Entry("Jester"),
      new Entry("Librarian"),
      new Entry("Mage"),
      new Entry("Mercenary"),
      new Entry("Merchant"),
      new Entry("Messenger"),
      new Entry("Miner"),
      new Entry("Monk"),
      new Entry("Noble"),
      new Entry("Paladin"),
      new Entry("Peasant"),
      new Entry("Pirate"),
      new Entry("Prisoner"),
      new Entry("Prophet"),
      new Entry("Ranger"),
      new Entry("Sage"),
      new Entry("Sailor"),
      new Entry("Scholar"),
      new Entry("Scribe"),
      new Entry("Sentry"),
      new Entry("Servant"),
      new Entry("Shepherd"),
      new Entry("Soothsayer"),
      new Entry("Stoic"),
      new Entry("Storyteller"),
      new Entry("Tailor"),
      new Entry("Thief"),
      new Entry("Tinker"),
      new Entry("Town Crier"),
      new Entry("Treasure Hunter"),
      new Entry("Waiter", true),
      new Entry("Warrior"),
      new Entry("Watchman"),
      new Entry("No Title", null)
    };

    private readonly PlayerBarkeeper m_Barkeeper;
    private readonly Mobile m_From;

    public BarkeeperTitleGump(Mobile from, PlayerBarkeeper barkeeper) : base(0, 0)
    {
      m_From = from;
      m_Barkeeper = barkeeper;

      from.CloseGump<BarkeeperGump>();
      from.CloseGump<BarkeeperTitleGump>();

      Entry[] entries = m_Entries;

      RenderBackground();

      int pageCount = (entries.Length + 19) / 20;

      for (int i = 0; i < pageCount; ++i)
        RenderPage(entries, i);
    }

    private void RenderBackground()
    {
      AddPage(0);

      AddBackground(30, 40, 585, 410, 5054);

      AddImage(30, 40, 9251);
      AddImage(180, 40, 9251);
      AddImage(30, 40, 9253);
      AddImage(30, 130, 9253);
      AddImage(598, 40, 9255);
      AddImage(598, 130, 9255);
      AddImage(30, 433, 9257);
      AddImage(180, 433, 9257);
      AddImage(30, 40, 9250);
      AddImage(598, 40, 9252);
      AddImage(598, 433, 9258);
      AddImage(30, 433, 9256);

      AddItem(30, 40, 6816);
      AddItem(30, 125, 6817);
      AddItem(30, 233, 6817);
      AddItem(30, 341, 6817);
      AddItem(580, 40, 6814);
      AddItem(588, 125, 6815);
      AddItem(588, 233, 6815);
      AddItem(588, 341, 6815);

      AddImage(560, 20, 1417);
      AddItem(580, 44, 4033);

      AddBackground(183, 25, 280, 30, 5054);

      AddImage(180, 25, 10460);
      AddImage(434, 25, 10460);

      AddHtml(223, 32, 200, 40, "BARKEEP CUSTOMIZATION MENU");
      AddBackground(243, 433, 150, 30, 5054);

      AddImage(240, 433, 10460);
      AddImage(375, 433, 10460);

      AddImage(80, 398, 2151);
      AddItem(72, 406, 2543);

      AddHtml(110, 412, 180, 25, "sells food and drink");
    }

    private void RenderPage(Entry[] entries, int page)
    {
      AddPage(1 + page);

      AddHtml(430, 70, 180, 25, $"Page {page + 1} of {(entries.Length + 19) / 20}");

      for (int count = 0, i = page * 20; count < 20 && i < entries.Length; ++count, ++i)
      {
        Entry entry = entries[i];

        AddButton(80 + count / 10 * 260, 100 + count % 10 * 30, 4005, 4007, 2 + i);
        AddHtml(120 + count / 10 * 260, 100 + count % 10 * 30, entry.m_Vendor ? 148 : 180, 25, entry.m_Description,
          true);

        if (entry.m_Vendor)
        {
          AddImage(270 + count / 10 * 260, 98 + count % 10 * 30, 2151);
          AddItem(262 + count / 10 * 260, 106 + count % 10 * 30, 2543);
        }
      }

      AddButton(340, 400, 4005, 4007, 0, GumpButtonType.Page, 1 + (page + 1) % ((entries.Length + 19) / 20));
      AddHtml(380, 400, 180, 25, "More Job Titles");

      AddButton(338, 437, 4014, 4016, 1);
      AddHtml(290, 440, 35, 40, "Back");
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
      int buttonID = info.ButtonID;

      if (buttonID > 0)
      {
        --buttonID;

        if (buttonID > 0)
        {
          --buttonID;

          if (buttonID >= 0 && buttonID < m_Entries.Length)
            m_Barkeeper.EndChangeTitle(m_From, m_Entries[buttonID].m_Title, m_Entries[buttonID].m_Vendor);
        }
        else
        {
          m_Barkeeper.CancelChangeTitle(m_From);
        }
      }
    }

    private class Entry
    {
      public readonly string m_Description;
      public readonly string m_Title;
      public readonly bool m_Vendor;

      public Entry(string desc, bool vendor = false) : this(desc, $"the {desc.ToLower()}", vendor)
      {
      }

      public Entry(string desc, string title, bool vendor = false)
      {
        m_Description = desc;
        m_Title = title;
        m_Vendor = vendor;
      }
    }
  }

  public class BarkeeperGump : Gump
  {
    private readonly PlayerBarkeeper m_Barkeeper;
    private readonly Mobile m_From;

    public BarkeeperGump(Mobile from, PlayerBarkeeper barkeeper) : base(0, 0)
    {
      m_From = from;
      m_Barkeeper = barkeeper;

      from.CloseGump<BarkeeperGump>();
      from.CloseGump<BarkeeperTitleGump>();

      RenderBackground();
      RenderCategories();
      RenderMessageManagement();
      RenderDismissConfirmation();
      RenderMessageManagement_Message_AddOrChange();
      RenderMessageManagement_Message_Remove();
      RenderMessageManagement_Tip_AddOrChange();
      RenderMessageManagement_Tip_Remove();
      RenderAppearanceCategories();
    }

    public void RenderBackground()
    {
      AddPage(0);

      AddBackground(30, 40, 585, 410, 5054);

      AddImage(30, 40, 9251);
      AddImage(180, 40, 9251);
      AddImage(30, 40, 9253);
      AddImage(30, 130, 9253);
      AddImage(598, 40, 9255);
      AddImage(598, 130, 9255);
      AddImage(30, 433, 9257);
      AddImage(180, 433, 9257);
      AddImage(30, 40, 9250);
      AddImage(598, 40, 9252);
      AddImage(598, 433, 9258);
      AddImage(30, 433, 9256);

      AddItem(30, 40, 6816);
      AddItem(30, 125, 6817);
      AddItem(30, 233, 6817);
      AddItem(30, 341, 6817);
      AddItem(580, 40, 6814);
      AddItem(588, 125, 6815);
      AddItem(588, 233, 6815);
      AddItem(588, 341, 6815);

      AddBackground(183, 25, 280, 30, 5054);

      AddImage(180, 25, 10460);
      AddImage(434, 25, 10460);
      AddImage(560, 20, 1417);

      AddHtml(223, 32, 200, 40, "BARKEEP CUSTOMIZATION MENU");
      AddBackground(243, 433, 150, 30, 5054);

      AddImage(240, 433, 10460);
      AddImage(375, 433, 10460);
    }

    public void RenderCategories()
    {
      AddPage(1);

      AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 2);
      AddHtml(170, 120, 200, 40, "Message Control");

      AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 8);
      AddHtml(170, 200, 200, 40, "Customize your barkeep");

      AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 3);
      AddHtml(170, 280, 200, 40, "Dismiss your barkeep");

      AddButton(338, 437, 4014, 4016, 0);
      AddHtml(290, 440, 35, 40, "Back");

      AddItem(574, 43, 5360);
    }

    public void RenderMessageManagement()
    {
      AddPage(2);

      AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 4);
      AddHtml(170, 120, 380, 20, "Add or change a message and keyword");

      AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 5);
      AddHtml(170, 200, 380, 20, "Remove a message and keyword from your barkeep");

      AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 6);
      AddHtml(170, 280, 380, 20, "Add or change your barkeeper's tip message");

      AddButton(130, 360, 4005, 4007, 0, GumpButtonType.Page, 7);
      AddHtml(170, 360, 380, 20, "Delete your barkeepers tip message");

      AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
      AddHtml(290, 440, 35, 40, "Back");

      AddItem(580, 46, 4030);
    }

    public void RenderDismissConfirmation()
    {
      AddPage(3);

      AddHtml(170, 160, 380, 20, "Are you sure you want to dismiss your barkeeper?");

      AddButton(205, 280, 4005, 4007, GetButtonID(0, 0));
      AddHtml(240, 280, 100, 20, @"Yes");

      AddButton(395, 280, 4005, 4007, 0);
      AddHtml(430, 280, 100, 20, "No");

      AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
      AddHtml(290, 440, 35, 40, "Back");

      AddItem(574, 43, 5360);
      AddItem(584, 34, 6579);
    }

    public void RenderMessageManagement_Message_AddOrChange()
    {
      AddPage(4);

      AddHtml(250, 60, 500, 25, "Add or change a message");

      BarkeeperRumor[] rumors = m_Barkeeper.Rumors;

      for (int i = 0; i < rumors.Length; ++i)
      {
        BarkeeperRumor rumor = rumors[i];

        AddHtml(100, 70 + i * 120, 50, 20, "Message");
        AddHtml(100, 90 + i * 120, 450, 40, rumor == null ? "No current message" : rumor.Message, true);
        AddHtml(100, 130 + i * 120, 50, 20, "Keyword");
        AddHtml(100, 150 + i * 120, 450, 40, rumor == null ? "None" : rumor.Keyword, true);

        AddButton(60, 90 + i * 120, 4005, 4007, GetButtonID(1, i));
      }

      AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
      AddHtml(290, 440, 35, 40, "Back");

      AddItem(580, 46, 4030);
    }

    public void RenderMessageManagement_Message_Remove()
    {
      AddPage(5);

      AddHtml(190, 60, 500, 25, "Choose the message you would like to remove");

      BarkeeperRumor[] rumors = m_Barkeeper.Rumors;

      for (int i = 0; i < rumors.Length; ++i)
      {
        BarkeeperRumor rumor = rumors[i];

        AddHtml(100, 70 + i * 120, 50, 20, "Message");
        AddHtml(100, 90 + i * 120, 450, 40, rumor == null ? "No current message" : rumor.Message, true);
        AddHtml(100, 130 + i * 120, 50, 20, "Keyword");
        AddHtml(100, 150 + i * 120, 450, 40, rumor == null ? "None" : rumor.Keyword, true);

        AddButton(60, 90 + i * 120, 4005, 4007, GetButtonID(2, i));
      }

      AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
      AddHtml(290, 440, 35, 40, "Back");

      AddItem(580, 46, 4030);
    }

    private int GetButtonID(int type, int index) => 1 + index * 6 + type;

    private void RenderMessageManagement_Tip_AddOrChange()
    {
      AddPage(6);

      AddHtml(250, 95, 500, 20, "Change this tip message");
      AddHtml(100, 190, 50, 20, "Message");
      AddHtml(100, 210, 450, 40, m_Barkeeper.TipMessage ?? "No current message", true);

      AddButton(60, 210, 4005, 4007, GetButtonID(3, 0));

      AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
      AddHtml(290, 440, 35, 40, "Back");

      AddItem(580, 46, 4030);
    }

    private void RenderMessageManagement_Tip_Remove()
    {
      AddPage(7);

      AddHtml(250, 95, 500, 20, "Remove this tip message");
      AddHtml(100, 190, 50, 20, "Message");
      AddHtml(100, 210, 450, 40, m_Barkeeper.TipMessage ?? "No current message", true);

      AddButton(60, 210, 4005, 4007, GetButtonID(4, 0));

      AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
      AddHtml(290, 440, 35, 40, "Back");

      AddItem(580, 46, 4030);
    }

    private void RenderAppearanceCategories()
    {
      AddPage(8);

      AddButton(130, 120, 4005, 4007, GetButtonID(5, 0));
      AddHtml(170, 120, 120, 20, "Title");

      if (m_Barkeeper.BodyValue != 0x340 && m_Barkeeper.BodyValue != 0x402)
      {
        AddButton(130, 200, 4005, 4007, GetButtonID(5, 1));
        AddHtml(170, 200, 120, 20, "Appearance");

        AddButton(130, 280, 4005, 4007, GetButtonID(5, 2));
        AddHtml(170, 280, 120, 20, "Male / Female");

        AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
        AddHtml(290, 440, 35, 40, "Back");
      }

      AddItem(580, 44, 4033);
    }

    public override void OnResponse(NetState state, RelayInfo info)
    {
      if (!m_Barkeeper.IsOwner(m_From))
        return;

      int index = info.ButtonID - 1;

      if (index < 0)
        return;

      int type = index % 6;
      index /= 6;

      switch (type)
      {
        case 0: // Controls
          {
            switch (index)
            {
              case 0: // Dismiss
                {
                  m_Barkeeper.Dismiss();
                  break;
                }
            }

            break;
          }
        case 1: // Change message
          {
            m_Barkeeper.BeginChangeRumor(m_From, index);
            break;
          }
        case 2: // Remove message
          {
            m_Barkeeper.RemoveRumor(m_From, index);
            break;
          }
        case 3: // Change tip
          {
            m_Barkeeper.BeginChangeTip(m_From);
            break;
          }
        case 4: // Remove tip
          {
            m_Barkeeper.RemoveTip(m_From);
            break;
          }
        case 5: // Appearance category selection
          {
            switch (index)
            {
              case 0:
                m_Barkeeper.BeginChangeTitle(m_From);
                break;
              case 1:
                m_Barkeeper.BeginChangeAppearance(m_From);
                break;
              case 2:
                m_Barkeeper.ChangeGender(m_From);
                break;
            }

            break;
          }
      }
    }
  }
}
