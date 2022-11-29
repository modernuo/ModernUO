using System;
using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[Flippable(0x14ED, 0x14EE)]
[SerializationGenerator(0, false)]
public partial class SOS : Item
{
    // We can only speculate, but I lack of support for non-ascii arguments in localized html gumps is why this wasn't used.
    // By default let's use the old gump, because we can.
    private static bool UseNewMessages = false;

    private static int[] WaterTiles =
    {
        0x00A8, 0x00AB,
        0x0136, 0x0137
    };

    private static Rectangle2D[] BritRegions = { new(0, 0, 5120, 4096) };

    private static Rectangle2D[] IlshRegions =
        { new(1472, 272, 304, 240), new(1240, 1000, 312, 160) };

    private static Rectangle2D[] MalasRegions = { new(1376, 1520, 464, 280) };

    private static TextDefinition[] MessageEntries =
    {
        // Help! Ship going do...n heavy storms...precious cargo...st reach dest...current coordinates ~1_SEXTANT~...ve any survivors...ease!
        1153537,
        // ...was a cad and a boor, no matter what momma s...rew him overboard! Oh, Anna, 'twas so exciting! Unfort...y he grabbe...est, and all his riches went with him! ...sked the captain, and he says we're at ~1_SEXTANT~ ... so maybe ...
        1153538,
        // ...know that the wreck is near ~1_SEXTANT~ but have not found it. Could the message passed down in my family for generations be wrong? No... I swear on the soul of my grandfather, I will find...
        1153539,
        // ...Ar! ~1_SEXTANT~ and, a fair wind! No chance...storms, though--ar! Is that a sea serp...<br><br>uh oh.
        1153540,
        // WANTED: divers exp...d in shipwre...overy. Must have own vess...pply at ~1_SEXTANT~...good benefits, flexible hours...
        1153541,
        // ...isaster indeed. Capt. McQ...overboard, fear the worst. These are t...ast days of the good ship Norr...ast known position was ~1_SEXTANT~ but ...'re beyond salvag...
        1153542,
        // ...never expected an iceberg...silly woman on bow crushed instantly...send help to ~1_SEXTANT~...ey'll never forget the tragedy of the sinking of the...
        1153543,
        // ...so I threw the treasure overboard, before the curse could get me too. But I was too late. Now I am doomed to wander these seas, a ghost forever. Join me: seek ye at ~1_SEXTANT~ if thou wishest my company...
        1153544,
        // ...then the ship exploded. A dragon swooped over. The slime swallowed Bertie whole--he screamed, it was amazing. The sky glowed orange...A sextant reading put us at ~1_SEXTANT~. Norma was chattering about sailing over the edge of the world. I looked at my hands and saw through them..."
        1153545,
        // ...been inside this whale for three days now. I've run out of food I can pick out of his teeth. I took a sextant reading through the blowhole: ~1_SEXTANT~. I'll never see my treasure again...
        1153546,
        // ...grand adventure! Captain Quacklebush had me swab down the decks daily... ...pirates came, I was in the rigging practicing with my sextant. ~1_SEXTANT~ if I am not mistaken... ...scuttled the ship, and our precious cargo went with her and the screaming pirates, down to the bottom of the sea...
        1153547,
        // ...nobody knew I was a girl. They just assumed I was another sailor...then we met the undine. ~1_SEXTANT~. It demanded sacrifice...I was youngest, they figured...grabbed the captain's treasure, screamed "It'll go down with me!" ...they took me up on it.
        1153548,
        // ...trapped on a deserted island with a magic fountain supplying food, fresh water springs, gorgeous scenery, and my lovely young wife. I know the ship with all our life's earnings sank at ~1_SEXTANT ~ but I don't know what our coordinates are...someone has GOT to rescue me before Sunday's finals game or I'll go mad...
        1153549,
        // ...round Jhelom towne we did roam, drinking all night. Got into a fight...hoisted sail on the morn, all crew aboard but the cook...ull breach, last known coordinates ~1_SEXTANT~...be better off in Stone's jail cell!
        1153550
    };

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _targetMap;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _targetLocation;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _messageIndex;

    [Constructible]
    public SOS(Map map = null) : this(map, MessageInABottle.GetRandomLevel())
    {
    }

    [Constructible]
    public SOS(Map map, int level) : base(0x14EE)
    {
        Weight = 1.0;

        _level = level;
        MessageIndex = Utility.Random(MessageEntries.Length);
        TargetMap = map ?? Map.Trammel;
        TargetLocation = FindLocation(TargetMap);

        UpdateHue();
    }

    public override int LabelNumber
    {
        get
        {
            if (IsAncient)
            {
                return 1063450; // an ancient SOS
            }

            return 1041081; // a waterstained SOS
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsAncient => _level >= 4;

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(0)]
    public int Level
    {
        get => _level;
        set
        {
            _level = Math.Max(1, Math.Min(value, 4));
            UpdateHue();
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public void UpdateHue() => Hue = IsAncient ? 0x481 : 0;

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            var entry = MessageIndex >= 0 && MessageIndex < MessageEntries.Length ? MessageEntries[MessageIndex] : null;

            if (UseNewMessages || entry == null)
            {
                from.SendGump(new MessageGump(TargetMap, TargetLocation));
            }
            else
            {
                from.SendGump(new OldMessageGump(entry, TargetMap, TargetLocation, from.Language));
            }
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public static Point3D FindLocation(Map map)
    {
        if (map == null || map == Map.Internal)
        {
            return Point3D.Zero;
        }

        Rectangle2D[] regions;

        if (map == Map.Felucca || map == Map.Trammel)
        {
            regions = BritRegions;
        }
        else if (map == Map.Ilshenar)
        {
            regions = IlshRegions;
        }
        else if (map == Map.Malas)
        {
            regions = MalasRegions;
        }
        else
        {
            regions = new[] { new Rectangle2D(0, 0, map.Width, map.Height) };
        }

        if (regions.Length == 0)
        {
            return Point3D.Zero;
        }

        for (var i = 0; i < 50; ++i)
        {
            var reg = regions.RandomElement();
            var x = Utility.Random(reg.X, reg.Width);
            var y = Utility.Random(reg.Y, reg.Height);

            if (!ValidateDeepWater(map, x, y))
            {
                continue;
            }

            var valid = true;

            for (int j = 1, offset = 5; valid && j <= 5; ++j, offset += 5)
            {
                if (!ValidateDeepWater(map, x + offset, y + offset))
                {
                    valid = false;
                }
                else if (!ValidateDeepWater(map, x + offset, y - offset))
                {
                    valid = false;
                }
                else if (!ValidateDeepWater(map, x - offset, y + offset))
                {
                    valid = false;
                }
                else if (!ValidateDeepWater(map, x - offset, y - offset))
                {
                    valid = false;
                }
            }

            if (valid)
            {
                return new Point3D(x, y, 0);
            }
        }

        return Point3D.Zero;
    }

    private static bool ValidateDeepWater(Map map, int x, int y)
    {
        var tileID = map.Tiles.GetLandTile(x, y).ID;
        var water = false;

        for (var i = 0; !water && i < WaterTiles.Length; i += 2)
        {
            water = tileID >= WaterTiles[i] && tileID <= WaterTiles[i + 1];
        }

        return water;
    }

    private class MessageGump : Gump
    {
        public MessageGump(Map map, Point3D loc) : base(150, 50)
        {
            int xLong = 0, yLat = 0;
            int xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;
            string fmt;

            if (Sextant.Format(loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                fmt = $"{yLat}° {yMins}'{(ySouth ? "S" : "N")}, {xLong}°{xMins}'{(xEast ? "E" : "W")}";
            }
            else
            {
                fmt = "?????";
            }

            AddPage(0);

            AddBackground(0, 40, 350, 300, 2520);

            /* This is a message hastily scribbled by a passenger aboard a sinking ship.
             * While it is probably too late to save the passengers and crew,
             * perhaps some treasure went down with the ship!
             * The message gives the ship's last known sextant co-ordinates.
             */
            AddHtmlLocalized(30, 80, 285, 160, 1018326, true, true);

            AddHtml(35, 240, 230, 20, fmt);

            AddButton(35, 265, 4005, 4007, 0);
            AddHtmlLocalized(70, 265, 100, 20, 1011036); // OKAY
        }
    }

    private class OldMessageGump : Gump
    {
        private const int Width = 350;
        private const int Height = 300;

        public OldMessageGump(TextDefinition entry, Map map, Point3D loc, string lang) : base((640 - Width) / 2, (480 - Height) / 2)
        {
            int xLong = 0, yLat = 0;
            int xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;
            string fmt;

            if (Sextant.Format(loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                fmt = $"{yLat}°{yMins}'{(ySouth ? "S" : "N")},{xLong}°{xMins}'{(xEast ? "E" : "W")}";
            }
            else
            {
                fmt = "?????";
            }

            AddPage(0);
            AddBackground(0, 0, Width, Height, 2520);

            // Gumps do not support non-ascii string arguments. Probably why this was not used on OSI
            var message = entry.Number > 0 ? Localization.Format(entry.Number, lang, $"{fmt}") : string.Format(entry.String, fmt);
            AddHtml(38, 38, Width - 83, Height - 86, message);
        }
    }
}
