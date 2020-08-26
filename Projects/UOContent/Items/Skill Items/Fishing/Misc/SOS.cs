using System;
using Server.Gumps;

namespace Server.Items
{
  [Flippable(0x14ED, 0x14EE)]
  public class SOS : Item
  {
    public override int LabelNumber
    {
      get
      {
        if (IsAncient)
          return 1063450; // an ancient SOS

        return 1041081; // a waterstained SOS
      }
    }

    private int m_Level;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsAncient => m_Level >= 4;

    [CommandProperty(AccessLevel.GameMaster)]
    public int Level
    {
      get => m_Level;
      set
      {
        m_Level = Math.Max(1, Math.Min(value, 4));
        UpdateHue();
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Map TargetMap { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D TargetLocation { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MessageIndex { get; set; }

    public void UpdateHue()
    {
      if (IsAncient)
        Hue = 0x481;
      else
        Hue = 0;
    }

    [Constructible]
    public SOS(Map map = null) : this(map, MessageInABottle.GetRandomLevel())
    {
    }

    [Constructible]
    public SOS(Map map, int level) : base(0x14EE)
    {
      Weight = 1.0;

      m_Level = level;
      MessageIndex = Utility.Random(MessageEntry.Entries.Length);
      TargetMap = map ?? Map.Trammel;
      TargetLocation = FindLocation(TargetMap);

      UpdateHue();
    }

    public SOS(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(4); // version

      writer.Write(m_Level);

      writer.Write(TargetMap);
      writer.Write(TargetLocation);
      writer.Write(MessageIndex);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 4:
        case 3:
        case 2:
          {
            m_Level = reader.ReadInt();
            goto case 1;
          }
        case 1:
          {
            TargetMap = reader.ReadMap();
            TargetLocation = reader.ReadPoint3D();
            MessageIndex = reader.ReadInt();

            break;
          }
        case 0:
          {
            TargetMap = Map;

            if (TargetMap == null || TargetMap == Map.Internal)
              TargetMap = Map.Trammel;

            TargetLocation = FindLocation(TargetMap);
            MessageIndex = Utility.Random(MessageEntry.Entries.Length);

            break;
          }
      }

      if (version < 2)
        m_Level = MessageInABottle.GetRandomLevel();

      if (version < 3)
        UpdateHue();

      if (version < 4 && TargetMap == Map.Tokuno)
        TargetMap = Map.Trammel;
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (IsChildOf(from.Backpack))
      {
        MessageEntry entry;

        if (MessageIndex >= 0 && MessageIndex < MessageEntry.Entries.Length)
          entry = MessageEntry.Entries[MessageIndex];
        else
          entry = MessageEntry.Entries[MessageIndex = Utility.Random(MessageEntry.Entries.Length)];

        // from.CloseGump( typeof( MessageGump ) );
        from.SendGump(new MessageGump(entry, TargetMap, TargetLocation));
      }
      else
      {
        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
      }
    }

    private static readonly int[] m_WaterTiles =
    {
      0x00A8, 0x00AB,
      0x0136, 0x0137
    };

    private static readonly Rectangle2D[] m_BritRegions = { new Rectangle2D(0, 0, 5120, 4096) };

    private static readonly Rectangle2D[] m_IlshRegions =
      { new Rectangle2D(1472, 272, 304, 240), new Rectangle2D(1240, 1000, 312, 160) };

    private static readonly Rectangle2D[] m_MalasRegions = { new Rectangle2D(1376, 1520, 464, 280) };

    public static Point3D FindLocation(Map map)
    {
      if (map == null || map == Map.Internal)
        return Point3D.Zero;

      Rectangle2D[] regions;

      if (map == Map.Felucca || map == Map.Trammel)
        regions = m_BritRegions;
      else if (map == Map.Ilshenar)
        regions = m_IlshRegions;
      else if (map == Map.Malas)
        regions = m_MalasRegions;
      else
        regions = new[] { new Rectangle2D(0, 0, map.Width, map.Height) };

      if (regions.Length == 0)
        return Point3D.Zero;

      for (int i = 0; i < 50; ++i)
      {
        Rectangle2D reg = regions.RandomElement();
        int x = Utility.Random(reg.X, reg.Width);
        int y = Utility.Random(reg.Y, reg.Height);

        if (!ValidateDeepWater(map, x, y))
          continue;

        bool valid = true;

        for (int j = 1, offset = 5; valid && j <= 5; ++j, offset += 5)
          if (!ValidateDeepWater(map, x + offset, y + offset))
            valid = false;
          else if (!ValidateDeepWater(map, x + offset, y - offset))
            valid = false;
          else if (!ValidateDeepWater(map, x - offset, y + offset))
            valid = false;
          else if (!ValidateDeepWater(map, x - offset, y - offset))
            valid = false;

        if (valid)
          return new Point3D(x, y, 0);
      }

      return Point3D.Zero;
    }

    private static bool ValidateDeepWater(Map map, int x, int y)
    {
      int tileID = map.Tiles.GetLandTile(x, y).ID;
      bool water = false;

      for (int i = 0; !water && i < m_WaterTiles.Length; i += 2)
        water = tileID >= m_WaterTiles[i] && tileID <= m_WaterTiles[i + 1];

      return water;
    }

    private class MessageGump : Gump
    {
      public MessageGump(MessageEntry entry, Map map, Point3D loc) : base(150, 50)
      {
        int xLong = 0, yLat = 0;
        int xMins = 0, yMins = 0;
        bool xEast = false, ySouth = false;
        string fmt;

        if (Sextant.Format(loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
          fmt = $"{yLat}°{yMins}'{(ySouth ? "S" : "N")},{xLong}°{xMins}'{(xEast ? "E" : "W")}";
        else
          fmt = "?????";

        AddPage(0);

        AddBackground(0, 40, 350, 300, 2520);

        /* This is a message hastily scribbled by a passenger aboard a sinking ship.
         * While it is probably too late to save the passengers and crew,
         * perhaps some treasure went down with the ship!
         * The message gives the ship's last known sextant co-ordinates.
         */
        AddHtmlLocalized(30, 80, 285, 160, 1018326, true,
          true);

        AddHtml(35, 240, 230, 20, fmt);

        AddButton(35, 265, 4005, 4007, 0);
        AddHtmlLocalized(70, 265, 100, 20, 1011036); // OKAY
      }
    }

    private class MessageEntry
    {
      public MessageEntry(int width, int height, string message)
      {
        Width = width;
        Height = height;
        Message = message;
      }

      public int Width { get; }

      public int Height { get; }

      public string Message { get; }

      public static MessageEntry[] Entries { get; } =
      {
        new MessageEntry(280, 180,
          "...Ar! {0} and a fair wind! No chance... storms, though--ar! Is that a sea serp...<br><br>uh oh."),
        new MessageEntry(280, 215,
          "...been inside this whale for three days now. I've run out of food I can pick out of his teeth. I took a sextant reading through the blowhole: {0}. I'll never see my treasure again..."),
        new MessageEntry(280, 285,
          "...grand adventure! Captain Quacklebush had me swab down the decks daily...<br>  ...pirates came, I was in the rigging practicing with my sextant. {0} if I am not mistaken...<br>  ....scuttled the ship, and our precious cargo went with her and the screaming pirates, down to the bottom of the sea..."),
        new MessageEntry(280, 180,
          "Help! Ship going dow...n heavy storms...precious cargo...st reach dest...current coordinates {0}...ve any survivors... ease!"),
        new MessageEntry(280, 215,
          "...know that the wreck is near {0} but have not found it. Could the message passed down in my family for generations be wrong? No... I swear on the soul of my grandfather, I will find..."),
        new MessageEntry(280, 195,
          "...never expected an iceberg...silly woman on bow crushed instantly...send help to {0}...ey'll never forget the tragedy of the sinking of the Miniscule..."),
        new MessageEntry(280, 265,
          "...nobody knew I was a girl. They just assumed I was another sailor...then we met the undine. {0}. It was demanded sacrifice...I was youngset, they figured...<br>  ...grabbed the captain's treasure, screamed, 'It'll go down with me!'<br>  ...they took me up on it."),
        new MessageEntry(280, 230,
          "...so I threw the treasure overboard, before the curse could get me too. But I was too late. Now I am doomed to wander these seas, a ghost forever. Join me: seek ye at {0} if thou wishest my company..."),
        new MessageEntry(280, 285,
          "...then the ship exploded. A dragon swooped by. The slime swallowed Bertie whole--he screamed, it was amazing. The sky glowed orange. A sextant reading put us at {0}. Norma was chattering about sailing over the edge of the world. I looked at my hands and saw through them..."),
        new MessageEntry(280, 285,
          "...trapped on a deserted island, with a magic fountain supplying wood, fresh water springs, gorgeous scenery, and my lovely young wife. I know the ship with all our life's earnings sank at {0} but I don't know what our coordinates are... someone has GOT to rescue me before Sunday's finals game or I'll go mad..."),
        new MessageEntry(280, 160,
          "WANTED: divers exp...d in shipwre...overy. Must have own vess...pply at {0}<br>...good benefits, flexible hours..."),
        new MessageEntry(280, 250,
          "...was a cad and a boor, no matter what momma s...rew him overboard! Oh, Anna, 'twas so exciting!<br>  Unfort...y he grabbe...est, and all his riches went with him!<br>  ...sked the captain, and he says we're at {0}<br>...so maybe...")
      };
    }
  }
}
