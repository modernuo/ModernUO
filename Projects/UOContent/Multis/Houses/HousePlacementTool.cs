using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HousePlacementTool : Item
{
    [Constructible]
    public HousePlacementTool() : base(0x14F6) => LootType = LootType.Blessed;

    public override double DefaultWeight => 3.0;
    public override int LabelNumber => 1060651; // a house placement tool

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.SendGump(new HousePlacementCategoryGump());
    }
}

public class HousePlacementCategoryGump : StaticGump<HousePlacementCategoryGump>
{
    private const int LabelColor = 0x7FFF;

    public override bool Singleton => true;

    public HousePlacementCategoryGump() : base(50, 50)
    {
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 270, 145, 5054);

        builder.AddImageTiled(10, 10, 250, 125, 2624);
        builder.AddAlphaRegion(10, 10, 250, 125);

        builder.AddHtmlLocalized(10, 10, 250, 20, 1060239, LabelColor); // <CENTER>HOUSE PLACEMENT TOOL</CENTER>

        builder.AddButton(10, 110, 4017, 4019, 0);
        builder.AddHtmlLocalized(45, 110, 150, 20, 3000363, LabelColor); // Close

        builder.AddButton(10, 40, 4005, 4007, 1);
        builder.AddHtmlLocalized(45, 40, 200, 20, 1060390, LabelColor); // Classic Houses

        builder.AddButton(10, 60, 4005, 4007, 2);
        builder.AddHtmlLocalized(45, 60, 200, 20, 1060391, LabelColor); // 2-Story Customizable Houses

        builder.AddButton(10, 80, 4005, 4007, 3);
        builder.AddHtmlLocalized(45, 80, 200, 20, 1060392, LabelColor); // 3-Story Customizable Houses
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (!from.CheckAlive() || from.Backpack?.FindItemByType<HousePlacementTool>() == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1: // Classic Houses
                {
                    var entry = Core.EJ ? HousePlacementEntry.HousesEJ : HousePlacementEntry.ClassicHouses;
                    from.SendGump(new HousePlacementListGump(from, entry));

                    break;
                }
            case 2: // 2-Story Customizable Houses
                {
                    from.SendGump(new HousePlacementListGump(from, HousePlacementEntry.TwoStoryFoundations));
                    break;
                }
            case 3: // 3-Story Customizable Houses
                {
                    from.SendGump(new HousePlacementListGump(from, HousePlacementEntry.ThreeStoryFoundations));
                    break;
                }
        }
    }
}

public class HousePlacementListGump : DynamicGump
{
    private const int LabelColor = 0x7FFF;
    private const int LabelHue = 0x480;

    private readonly Mobile _from;
    private readonly HousePlacementEntry[] _entries;

    public override bool Singleton => true;

    public HousePlacementListGump(Mobile from, HousePlacementEntry[] entries) : base(50, 50)
    {
        _from = from;
        _entries = entries;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 520, 420, 5054);

        builder.AddImageTiled(10, 10, 500, 20, 2624);
        builder.AddAlphaRegion(10, 10, 500, 20);

        builder.AddHtmlLocalized(10, 10, 500, 20, 1060239, LabelColor); // <CENTER>HOUSE PLACEMENT TOOL</CENTER>

        builder.AddImageTiled(10, 40, 500, 20, 2624);
        builder.AddAlphaRegion(10, 40, 500, 20);

        builder.AddHtmlLocalized(50, 40, 225, 20, 1060235, LabelColor); // House Description
        builder.AddHtmlLocalized(275, 40, 75, 20, 1060236, LabelColor); // Storage
        builder.AddHtmlLocalized(350, 40, 75, 20, 1060237, LabelColor); // Lockdowns
        builder.AddHtmlLocalized(425, 40, 75, 20, 1060034, LabelColor); // Cost

        builder.AddImageTiled(10, 70, 500, 280, 2624);
        builder.AddAlphaRegion(10, 70, 500, 280);

        builder.AddImageTiled(10, 360, 500, 20, 2624);
        builder.AddAlphaRegion(10, 360, 500, 20);

        builder.AddHtmlLocalized(10, 360, 250, 20, 1060645, LabelColor); // Bank Balance:
        builder.AddLabel(250, 360, LabelHue, Banker.GetBalance(_from).ToString());

        builder.AddImageTiled(10, 390, 500, 20, 2624);
        builder.AddAlphaRegion(10, 390, 500, 20);

        builder.AddButton(10, 390, 4017, 4019, 0);
        builder.AddHtmlLocalized(50, 390, 100, 20, 3000363, LabelColor); // Close

        for (var i = 0; i < _entries.Length; ++i)
        {
            var page = Math.DivRem(i, 14, out var index) + 1;

            if (index == 0)
            {
                if (page > 1)
                {
                    builder.AddButton(450, 390, 4005, 4007, 0, GumpButtonType.Page, page);
                    builder.AddHtmlLocalized(400, 390, 100, 20, 3000406, LabelColor); // Next
                }

                builder.AddPage(page);

                if (page > 1)
                {
                    builder.AddButton(200, 390, 4014, 4016, 0, GumpButtonType.Page, page - 1);
                    builder.AddHtmlLocalized(250, 390, 100, 20, 3000405, LabelColor); // Previous
                }
            }

            var entry = _entries[i];

            var y = 70 + index * 20;

            builder.AddButton(10, y, 4005, 4007, 1 + i);
            builder.AddHtmlLocalized(50, y, 225, 20, entry.Description, LabelColor);
            builder.AddLabel(275, y, LabelHue, entry.Storage.ToString());
            builder.AddLabel(350, y, LabelHue, entry.Lockdowns.ToString());
            builder.AddLabel(425, y, LabelHue, entry.Cost.ToString());
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (!_from.CheckAlive() || _from.Backpack?.FindItemByType<HousePlacementTool>() == null)
        {
            return;
        }

        var index = info.ButtonID - 1;

        if (index < 0 || index >= _entries.Length)
        {
            _from.SendGump(new HousePlacementCategoryGump());
        }
        else if (_from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(_from))
        {
            _from.SendLocalizedMessage(501271); // You already own a house, you may not place another!
        }
        else
        {
            _from.Target = new NewHousePlacementTarget(_entries, _entries[index]);
        }
    }
}

public class NewHousePlacementTarget : MultiTarget
{
    private readonly HousePlacementEntry[] _entries;
    private readonly HousePlacementEntry _entry;

    private bool _placed;

    public NewHousePlacementTarget(HousePlacementEntry[] entries, HousePlacementEntry entry) : base(
        entry.MultiID,
        entry.Offset
    )
    {
        Range = 14;

        _entries = entries;
        _entry = entry;
    }

    protected override void OnTarget(Mobile from, object o)
    {
        if (o is not IPoint3D ip)
        {
            return;
        }

        if (!from.CheckAlive() || from.Backpack?.FindItemByType<HousePlacementTool>() == null)
        {
            return;
        }

        var p = ip switch
        {
            Item item => item.GetWorldTop(),
            Mobile m  => m.Location,
            _         => new Point3D(ip)
        };

        var reg = Region.Find(p, from.Map);

        if (from.AccessLevel >= AccessLevel.GameMaster || reg.AllowHousing(from, p))
        {
            _placed = _entry.OnPlacement(from, p);
        }
        else if (reg.IsPartOf<TempNoHousingRegion>())
        {
            // Lord British has decreed a 'no build' period, thus you cannot build this house at this time.
            from.SendLocalizedMessage(501270);
        }
        else if (reg.IsPartOf<TreasureRegion>() || reg.IsPartOf<HouseRegion>())
        {
            // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
            from.SendLocalizedMessage(1043287);
        }
        else if (reg.IsPartOf<HouseRaffleRegion>())
        {
            from.SendLocalizedMessage(1150493); // You must have a deed for this plot of land in order to build here.
        }
        else
        {
            from.SendLocalizedMessage(501265); // Housing can not be created in this area.
        }
    }

    protected override void OnTargetFinish(Mobile from)
    {
        if (!from.CheckAlive() || from.Backpack?.FindItemByType<HousePlacementTool>() == null)
        {
            return;
        }

        if (!_placed)
        {
            from.SendGump(new HousePlacementListGump(from, _entries));
        }
    }
}

public class HousePlacementEntry
{
    private static readonly Dictionary<Type, object> _table;
    private readonly int _lockdowns;
    private readonly int _newLockdowns;
    private readonly int _newStorage;
    private readonly int _storage;

    static HousePlacementEntry()
    {
        _table = new Dictionary<Type, object>();

        FillTable(Core.EJ ? HousesEJ : ClassicHouses);
        FillTable(TwoStoryFoundations);
        FillTable(ThreeStoryFoundations);
    }

    public HousePlacementEntry(
        Type type, int description, int storage, int lockdowns, int newStorage, int newLockdowns,
        int vendors, int cost, int xOffset, int yOffset, int zOffset, int multiID, Direction direction = Direction.South
    )
    {
        Type = type;
        Description = description;
        _storage = storage;
        _lockdowns = lockdowns;
        _newStorage = newStorage;
        _newLockdowns = newLockdowns;
        Vendors = vendors;
        Cost = cost;

        Offset = new Point3D(xOffset, yOffset, zOffset);

        MultiID = multiID;
        HouseDirection = direction;
    }

    public Type Type { get; }

    public int Description { get; }

    public int Storage => BaseHouse.NewVendorSystem ? _newStorage : _storage;
    public int Lockdowns => BaseHouse.NewVendorSystem ? _newLockdowns : _lockdowns;
    public int Vendors { get; }

    public int Cost { get; }

    public int MultiID { get; }

    public Point3D Offset { get; }

    public Direction HouseDirection { get; }

    public static HousePlacementEntry[] ClassicHouses { get; } =
    {
        new(typeof(SmallOldHouse), 1011303, 425, 212, 489, 244, 10, 37000, 0, 4, 0, 0x0064),
        new(typeof(SmallOldHouse), 1011304, 425, 212, 489, 244, 10, 37000, 0, 4, 0, 0x0066),
        new(typeof(SmallOldHouse), 1011305, 425, 212, 489, 244, 10, 36750, 0, 4, 0, 0x0068),
        new(typeof(SmallOldHouse), 1011306, 425, 212, 489, 244, 10, 35250, 0, 4, 0, 0x006A),
        new(typeof(SmallOldHouse), 1011307, 425, 212, 489, 244, 10, 36750, 0, 4, 0, 0x006C),
        new(typeof(SmallOldHouse), 1011308, 425, 212, 489, 244, 10, 36750, 0, 4, 0, 0x006E),
        new(typeof(SmallShop), 1011321, 425, 212, 489, 244, 10, 50500, -1, 4, 0, 0x00A0),
        new(typeof(SmallShop), 1011322, 425, 212, 489, 244, 10, 52500, 0, 4, 0, 0x00A2),
        new(typeof(SmallTower), 1011317, 580, 290, 667, 333, 14, 73500, 3, 4, 0, 0x0098),
        new(typeof(TwoStoryVilla), 1011319, 1100, 550, 1265, 632, 24, 113750, 3, 6, 0, 0x009E),
        new(typeof(SandStonePatio), 1011320, 850, 425, 1265, 632, 24, 76500, -1, 4, 0, 0x009C),
        new(typeof(LogCabin), 1011318, 1100, 550, 1265, 632, 24, 81750, 1, 6, 0, 0x009A),
        new(typeof(GuildHouse), 1011309, 1370, 685, 1576, 788, 28, 131500, -1, 7, 0, 0x0074),
        new(typeof(TwoStoryHouse), 1011310, 1370, 685, 1576, 788, 28, 162750, -3, 7, 0, 0x0076),
        new(typeof(TwoStoryHouse), 1011311, 1370, 685, 1576, 788, 28, 162000, -3, 7, 0, 0x0078),
        new(typeof(LargePatioHouse), 1011315, 1370, 685, 1576, 788, 28, 129250, -4, 7, 0, 0x008C),
        new(typeof(LargeMarbleHouse), 1011316, 1370, 685, 1576, 788, 28, 160500, -4, 7, 0, 0x0096),
        new(typeof(Tower), 1011312, 2119, 1059, 2437, 1218, 42, 366500, 0, 7, 0, 0x007A),
        new(typeof(Keep), 1011313, 2625, 1312, 3019, 1509, 52, 572750, 0, 11, 0, 0x007C),
        new(typeof(Castle), 1011314, 4076, 2038, 4688, 2344, 78, 865250, 0, 16, 0, 0x007E)
    };

    public static HousePlacementEntry[] HousesEJ { get; } =
    {
        new(typeof(SmallOldHouse), 1011303, 425, 212, 489, 244, 10, 36750, 0, 4, 0, 0x0064),
        new(typeof(SmallOldHouse), 1011304, 425, 212, 489, 244, 10, 36750, 0, 4, 0, 0x0066),
        new(typeof(SmallOldHouse), 1011305, 425, 212, 489, 244, 10, 36500, 0, 4, 0, 0x0068),
        new(typeof(SmallOldHouse), 1011306, 425, 212, 489, 244, 10, 35000, 0, 4, 0, 0x006A),
        new(typeof(SmallOldHouse), 1011307, 425, 212, 489, 244, 10, 36500, 0, 4, 0, 0x006C),
        new(typeof(SmallOldHouse), 1011308, 425, 212, 489, 244, 10, 36500, 0, 4, 0, 0x006E),
        new(typeof(SmallShop), 1011321, 425, 212, 489, 244, 10, 50250, -1, 4, 0, 0x00A0),
        new(typeof(SmallShop), 1011322, 425, 212, 489, 244, 10, 52250, 0, 4, 0, 0x00A2),
        new(typeof(SmallTower), 1011317, 580, 290, 667, 333, 14, 73250, 3, 4, 0, 0x0098),
        new(typeof(TwoStoryVilla), 1011319, 1100, 550, 1265, 632, 24, 113500, 3, 6, 0, 0x009E),
        new(typeof(SandStonePatio), 1011320, 850, 425, 1265, 632, 24, 76250, -1, 4, 0, 0x009C),
        new(typeof(LogCabin), 1011318, 1100, 550, 1265, 632, 24, 81250, 1, 6, 0, 0x009A),
        new(typeof(GuildHouse), 1011309, 1370, 685, 1576, 788, 28, 131250, -1, 7, 0, 0x0074),
        new(typeof(TwoStoryHouse), 1011310, 1370, 685, 1576, 788, 28, 162500, -3, 7, 0, 0x0076),
        new(typeof(TwoStoryHouse), 1011311, 1370, 685, 1576, 788, 28, 162750, -3, 7, 0, 0x0078),
        new(typeof(LargePatioHouse), 1011315, 1370, 685, 1576, 788, 28, 129000, -4, 7, 0, 0x008C),
        new(typeof(LargeMarbleHouse), 1011316, 1370, 685, 1576, 788, 28, 160250, -4, 7, 0, 0x0096),
        new(typeof(Tower), 1011312, 2119, 1059, 2437, 1218, 42, 366250, 0, 7, 0, 0x007A),
        new(typeof(Keep), 1011313, 2625, 1312, 3019, 1509, 52, 562500, 0, 11, 0, 0x007C),
        new(typeof(Castle), 1011314, 4076, 2038, 4688, 2344, 78, 865000, 0, 16, 0, 0x007E),

        new(typeof(TrinsicKeep), 1158748, 2625, 1312, 3019, 1509, 52, 29643750, 0, 11, 0, 0x147E),
        new(
            typeof(GothicRoseCastle),
            1158749,
            4076,
            2038,
            4688,
            2344,
            78,
            44808750,
            0,
            16,
            0,
            0x147F
        ),
        new(typeof(ElsaCastle), 1158750, 4076, 2038, 4688, 2344, 78, 45450000, 0, 16, 0, 0x1480),
        new(typeof(Spires), 1158761, 4076, 2038, 4688, 2344, 78, 47025000, 0, 16, 0, 0x1481),
        new(
            typeof(CastleOfOceania),
            1158760,
            4076,
            2038,
            4688,
            2344,
            78,
            48971250,
            0,
            16,
            0,
            0x1482
        ),
        new(typeof(FeudalCastle), 1158762, 4076, 2038, 4688, 2344, 78, 27337500, 0, 16, 0, 0x1483),
        new(typeof(RobinsNest), 1158850, 2625, 1312, 3019, 1509, 52, 25301250, 0, 11, 0, 0x1484),
        new(
            typeof(TraditionalKeep),
            1158851,
            2625,
            1312,
            3019,
            1509,
            52,
            26685000,
            0,
            11,
            0,
            0x1485
        ),
        new(typeof(VillaCrowley), 1158852, 2625, 1312, 3019, 1509, 52, 21813750, 0, 11, 0, 0x1486),
        new(typeof(DarkthornKeep), 1158853, 2625, 1312, 3019, 1509, 52, 27990000, 0, 11, 0, 0x1487),
        new(typeof(SandalwoodKeep), 1158854, 2625, 1312, 3019, 1509, 52, 23456250, 0, 11, 0, 0x1488),
        new(typeof(CasaMoga), 1158855, 2625, 1312, 3019, 1509, 52, 26313750, 0, 11, 0, 0x1489),

        new(typeof(RobinsRoost), 1158960, 4076, 2038, 4688, 2344, 78, 43863750, 0, 16, 0, 0x148A),
        new(typeof(Camelot), 1158961, 4076, 2038, 4688, 2344, 78, 47092500, 0, 16, 0, 0x148B),
        new(
            typeof(LacrimaeInCaelo),
            1158962,
            4076,
            2038,
            4688,
            2344,
            78,
            45315000,
            0,
            16,
            0,
            0x148C
        ),
        new(
            typeof(OkinawaSweetDreamCastle),
            1158963,
            4076,
            2038,
            4688,
            2344,
            78,
            40128750,
            0,
            16,
            0,
            0x148D
        ),
        new(
            typeof(TheSandstoneCastle),
            1158964,
            4076,
            2038,
            4688,
            2344,
            78,
            48690000,
            0,
            16,
            0,
            0x148E
        ),
        new(
            typeof(GrimswindSisters),
            1158965,
            4076,
            2038,
            4688,
            2344,
            78,
            42142500,
            0,
            16,
            0,
            0x148F
        )
    };

    public static HousePlacementEntry[] TwoStoryFoundations { get; } =
    {
        new(
            typeof(HouseFoundation),
            1060241,
            425,
            212,
            489,
            244,
            10,
            30500,
            0,
            4,
            0,
            0x13EC
        ), // 7x7 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060242,
            580,
            290,
            667,
            333,
            14,
            34500,
            0,
            5,
            0,
            0x13ED
        ), // 7x8 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060243,
            650,
            325,
            748,
            374,
            16,
            38500,
            0,
            5,
            0,
            0x13EE
        ), // 7x9 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060244,
            700,
            350,
            805,
            402,
            16,
            42500,
            0,
            6,
            0,
            0x13EF
        ), // 7x10 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060245,
            750,
            375,
            863,
            431,
            16,
            46500,
            0,
            6,
            0,
            0x13F0
        ), // 7x11 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060246,
            800,
            400,
            920,
            460,
            18,
            50500,
            0,
            7,
            0,
            0x13F1
        ), // 7x12 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060253,
            580,
            290,
            667,
            333,
            14,
            34500,
            0,
            4,
            0,
            0x13F8
        ), // 8x7 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060254,
            650,
            325,
            748,
            374,
            16,
            39000,
            0,
            5,
            0,
            0x13F9
        ), // 8x8 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060255,
            700,
            350,
            805,
            402,
            16,
            43500,
            0,
            5,
            0,
            0x13FA
        ), // 8x9 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060256,
            750,
            375,
            863,
            431,
            16,
            48000,
            0,
            6,
            0,
            0x13FB
        ), // 8x10 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060257,
            800,
            400,
            920,
            460,
            18,
            52500,
            0,
            6,
            0,
            0x13FC
        ), // 8x11 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060258,
            850,
            425,
            1265,
            632,
            24,
            57000,
            0,
            7,
            0,
            0x13FD
        ), // 8x12 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060259,
            1100,
            550,
            1265,
            632,
            24,
            61500,
            0,
            7,
            0,
            0x13FE
        ), // 8x13 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060265,
            650,
            325,
            748,
            374,
            16,
            38500,
            0,
            4,
            0,
            0x1404
        ), // 9x7 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060266,
            700,
            350,
            805,
            402,
            16,
            43500,
            0,
            5,
            0,
            0x1405
        ), // 9x8 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060267,
            750,
            375,
            863,
            431,
            16,
            48500,
            0,
            5,
            0,
            0x1406
        ), // 9x9 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060268,
            800,
            400,
            920,
            460,
            18,
            53500,
            0,
            6,
            0,
            0x1407
        ), // 9x10 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060269,
            850,
            425,
            1265,
            632,
            24,
            58500,
            0,
            6,
            0,
            0x1408
        ), // 9x11 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060270,
            1100,
            550,
            1265,
            632,
            24,
            63500,
            0,
            7,
            0,
            0x1409
        ), // 9x12 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060271,
            1100,
            550,
            1265,
            632,
            24,
            68500,
            0,
            7,
            0,
            0x140A
        ), // 9x13 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060277,
            700,
            350,
            805,
            402,
            16,
            42500,
            0,
            4,
            0,
            0x1410
        ), // 10x7 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060278,
            750,
            375,
            863,
            431,
            16,
            48000,
            0,
            5,
            0,
            0x1411
        ), // 10x8 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060279,
            800,
            400,
            920,
            460,
            18,
            53500,
            0,
            5,
            0,
            0x1412
        ), // 10x9 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060280,
            850,
            425,
            1265,
            632,
            24,
            59000,
            0,
            6,
            0,
            0x1413
        ), // 10x10 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060281,
            1100,
            550,
            1265,
            632,
            24,
            64500,
            0,
            6,
            0,
            0x1414
        ), // 10x11 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060282,
            1100,
            550,
            1265,
            632,
            24,
            70000,
            0,
            7,
            0,
            0x1415
        ), // 10x12 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060283,
            1150,
            575,
            1323,
            661,
            24,
            75500,
            0,
            7,
            0,
            0x1416
        ), // 10x13 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060289,
            750,
            375,
            863,
            431,
            16,
            46500,
            0,
            4,
            0,
            0x141C
        ), // 11x7 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060290,
            800,
            400,
            920,
            460,
            18,
            52500,
            0,
            5,
            0,
            0x141D
        ), // 11x8 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060291,
            850,
            425,
            1265,
            632,
            24,
            58500,
            0,
            5,
            0,
            0x141E
        ), // 11x9 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060292,
            1100,
            550,
            1265,
            632,
            24,
            64500,
            0,
            6,
            0,
            0x141F
        ), // 11x10 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060293,
            1100,
            550,
            1265,
            632,
            24,
            70500,
            0,
            6,
            0,
            0x1420
        ), // 11x11 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060294,
            1150,
            575,
            1323,
            661,
            24,
            76500,
            0,
            7,
            0,
            0x1421
        ), // 11x12 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060295,
            1200,
            600,
            1380,
            690,
            26,
            82500,
            0,
            7,
            0,
            0x1422
        ), // 11x13 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060301,
            800,
            400,
            920,
            460,
            18,
            50500,
            0,
            4,
            0,
            0x1428
        ), // 12x7 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060302,
            850,
            425,
            1265,
            632,
            24,
            57000,
            0,
            5,
            0,
            0x1429
        ), // 12x8 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060303,
            1100,
            550,
            1265,
            632,
            24,
            63500,
            0,
            5,
            0,
            0x142A
        ), // 12x9 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060304,
            1100,
            550,
            1265,
            632,
            24,
            70000,
            0,
            6,
            0,
            0x142B
        ), // 12x10 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060305,
            1150,
            575,
            1323,
            661,
            24,
            76500,
            0,
            6,
            0,
            0x142C
        ), // 12x11 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060306,
            1200,
            600,
            1380,
            690,
            26,
            83000,
            0,
            7,
            0,
            0x142D
        ), // 12x12 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060307,
            1250,
            625,
            1438,
            719,
            26,
            89500,
            0,
            7,
            0,
            0x142E
        ), // 12x13 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060314,
            1100,
            550,
            1265,
            632,
            24,
            61500,
            0,
            5,
            0,
            0x1435
        ), // 13x8 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060315,
            1100,
            550,
            1265,
            632,
            24,
            68500,
            0,
            5,
            0,
            0x1436
        ), // 13x9 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060316,
            1150,
            575,
            1323,
            661,
            24,
            75500,
            0,
            6,
            0,
            0x1437
        ), // 13x10 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060317,
            1200,
            600,
            1380,
            690,
            26,
            82500,
            0,
            6,
            0,
            0x1438
        ), // 13x11 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060318,
            1250,
            625,
            1438,
            719,
            26,
            89500,
            0,
            7,
            0,
            0x1439
        ), // 13x12 2-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060319,
            1300,
            650,
            1495,
            747,
            28,
            96500,
            0,
            7,
            0,
            0x143A
        ) // 13x13 2-Story Customizable House
    };

    public static HousePlacementEntry[] ThreeStoryFoundations { get; } =
    {
        new(
            typeof(HouseFoundation),
            1060272,
            1150,
            575,
            1323,
            661,
            24,
            73500,
            0,
            8,
            0,
            0x140B
        ), // 9x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060284,
            1200,
            600,
            1380,
            690,
            26,
            81000,
            0,
            8,
            0,
            0x1417
        ), // 10x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060285,
            1250,
            625,
            1438,
            719,
            26,
            86500,
            0,
            8,
            0,
            0x1418
        ), // 10x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060296,
            1250,
            625,
            1438,
            719,
            26,
            88500,
            0,
            8,
            0,
            0x1423
        ), // 11x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060297,
            1300,
            650,
            1495,
            747,
            28,
            94500,
            0,
            8,
            0,
            0x1424
        ), // 11x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060298,
            1350,
            675,
            1553,
            776,
            28,
            100500,
            0,
            9,
            0,
            0x1425
        ), // 11x16 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060308,
            1300,
            650,
            1495,
            747,
            28,
            96000,
            0,
            8,
            0,
            0x142F
        ), // 12x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060309,
            1350,
            675,
            1553,
            776,
            28,
            102500,
            0,
            8,
            0,
            0x1430
        ), // 12x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060310,
            1370,
            685,
            1576,
            788,
            28,
            109000,
            0,
            9,
            0,
            0x1431
        ), // 12x16 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060311,
            1370,
            685,
            1576,
            788,
            28,
            115500,
            0,
            9,
            0,
            0x1432
        ), // 12x17 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060320,
            1350,
            675,
            1553,
            776,
            28,
            103500,
            0,
            8,
            0,
            0x143B
        ), // 13x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060321,
            1370,
            685,
            1576,
            788,
            28,
            110500,
            0,
            8,
            0,
            0x143C
        ), // 13x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060322,
            1370,
            685,
            1576,
            788,
            28,
            117500,
            0,
            9,
            0,
            0x143D
        ), // 13x16 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060323,
            2119,
            1059,
            2437,
            1218,
            42,
            124500,
            0,
            9,
            0,
            0x143E
        ), // 13x17 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060324,
            2119,
            1059,
            2437,
            1218,
            42,
            131500,
            0,
            10,
            0,
            0x143F
        ), // 13x18 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060327,
            1150,
            575,
            1323,
            661,
            24,
            73500,
            0,
            5,
            0,
            0x1442
        ), // 14x9 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060328,
            1200,
            600,
            1380,
            690,
            26,
            81000,
            0,
            6,
            0,
            0x1443
        ), // 14x10 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060329,
            1250,
            625,
            1438,
            719,
            26,
            88500,
            0,
            6,
            0,
            0x1444
        ), // 14x11 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060330,
            1300,
            650,
            1495,
            747,
            28,
            96000,
            0,
            7,
            0,
            0x1445
        ), // 14x12 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060331,
            1350,
            675,
            1553,
            776,
            28,
            103500,
            0,
            7,
            0,
            0x1446
        ), // 14x13 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060332,
            1370,
            685,
            1576,
            788,
            28,
            111000,
            0,
            8,
            0,
            0x1447
        ), // 14x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060333,
            1370,
            685,
            1576,
            788,
            28,
            118500,
            0,
            8,
            0,
            0x1448
        ), // 14x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060334,
            2119,
            1059,
            2437,
            1218,
            42,
            126000,
            0,
            9,
            0,
            0x1449
        ), // 14x16 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060335,
            2119,
            1059,
            2437,
            1218,
            42,
            133500,
            0,
            9,
            0,
            0x144A
        ), // 14x17 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060336,
            2119,
            1059,
            2437,
            1218,
            42,
            141000,
            0,
            10,
            0,
            0x144B
        ), // 14x18 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060340,
            1250,
            625,
            1438,
            719,
            26,
            86500,
            0,
            6,
            0,
            0x144F
        ), // 15x10 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060341,
            1300,
            650,
            1495,
            747,
            28,
            94500,
            0,
            6,
            0,
            0x1450
        ), // 15x11 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060342,
            1350,
            675,
            1553,
            776,
            28,
            102500,
            0,
            7,
            0,
            0x1451
        ), // 15x12 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060343,
            1370,
            685,
            1576,
            788,
            28,
            110500,
            0,
            7,
            0,
            0x1452
        ), // 15x13 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060344,
            1370,
            685,
            1576,
            788,
            28,
            118500,
            0,
            8,
            0,
            0x1453
        ), // 15x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060345,
            2119,
            1059,
            2437,
            1218,
            42,
            126500,
            0,
            8,
            0,
            0x1454
        ), // 15x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060346,
            2119,
            1059,
            2437,
            1218,
            42,
            134500,
            0,
            9,
            0,
            0x1455
        ), // 15x16 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060347,
            2119,
            1059,
            2437,
            1218,
            42,
            142500,
            0,
            9,
            0,
            0x1456
        ), // 15x17 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060348,
            2119,
            1059,
            2437,
            1218,
            42,
            150500,
            0,
            10,
            0,
            0x1457
        ), // 15x18 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060353,
            1350,
            675,
            1553,
            776,
            28,
            100500,
            0,
            6,
            0,
            0x145C
        ), // 16x11 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060354,
            1370,
            685,
            1576,
            788,
            28,
            109000,
            0,
            7,
            0,
            0x145D
        ), // 16x12 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060355,
            1370,
            685,
            1576,
            788,
            28,
            117500,
            0,
            7,
            0,
            0x145E
        ), // 16x13 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060356,
            2119,
            1059,
            2437,
            1218,
            42,
            126000,
            0,
            8,
            0,
            0x145F
        ), // 16x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060357,
            2119,
            1059,
            2437,
            1218,
            42,
            134500,
            0,
            8,
            0,
            0x1460
        ), // 16x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060358,
            2119,
            1059,
            2437,
            1218,
            42,
            143000,
            0,
            9,
            0,
            0x1461
        ), // 16x16 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060359,
            2119,
            1059,
            2437,
            1218,
            42,
            151500,
            0,
            9,
            0,
            0x1462
        ), // 16x17 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060360,
            2119,
            1059,
            2437,
            1218,
            42,
            160000,
            0,
            10,
            0,
            0x1463
        ), // 16x18 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060366,
            1370,
            685,
            1576,
            788,
            28,
            115500,
            0,
            7,
            0,
            0x1469
        ), // 17x12 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060367,
            2119,
            1059,
            2437,
            1218,
            42,
            124500,
            0,
            7,
            0,
            0x146A
        ), // 17x13 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060368,
            2119,
            1059,
            2437,
            1218,
            42,
            133500,
            0,
            8,
            0,
            0x146B
        ), // 17x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060369,
            2119,
            1059,
            2437,
            1218,
            42,
            142500,
            0,
            8,
            0,
            0x146C
        ), // 17x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060370,
            2119,
            1059,
            2437,
            1218,
            42,
            151500,
            0,
            9,
            0,
            0x146D
        ), // 17x16 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060371,
            2119,
            1059,
            2437,
            1218,
            42,
            160500,
            0,
            9,
            0,
            0x146E
        ), // 17x17 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060372,
            2119,
            1059,
            2437,
            1218,
            42,
            169500,
            0,
            10,
            0,
            0x146F
        ), // 17x18 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060379,
            2119,
            1059,
            2437,
            1218,
            42,
            131500,
            0,
            7,
            0,
            0x1476
        ), // 18x13 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060380,
            2119,
            1059,
            2437,
            1218,
            42,
            141000,
            0,
            8,
            0,
            0x1477
        ), // 18x14 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060381,
            2119,
            1059,
            2437,
            1218,
            42,
            150500,
            0,
            8,
            0,
            0x1478
        ), // 18x15 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060382,
            2119,
            1059,
            2437,
            1218,
            42,
            160000,
            0,
            9,
            0,
            0x1479
        ), // 18x16 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060383,
            2119,
            1059,
            2437,
            1218,
            42,
            169500,
            0,
            9,
            0,
            0x147A
        ), // 18x17 3-Story Customizable House
        new(
            typeof(HouseFoundation),
            1060384,
            2119,
            1059,
            2437,
            1218,
            42,
            179000,
            0,
            10,
            0,
            0x147B
        ) // 18x18 3-Story Customizable House
    };

    public BaseHouse ConstructHouse(Mobile from)
    {
        try
        {
            object[] args;

            if (Type == typeof(HouseFoundation))
            {
                args = new object[] { from, MultiID, _storage, _lockdowns };
            }
            else if (Type == typeof(SmallOldHouse) || Type == typeof(SmallShop) || Type == typeof(TwoStoryHouse))
            {
                args = new object[] { from, MultiID };
            }
            else
            {
                args = new object[] { from };
            }

            return Type.CreateInstance<BaseHouse>(args);
        }
        catch
        {
            // ignored
        }

        return null;
    }

    public void PlacementWarning_Callback(Mobile from, bool okay, PreviewHouse prevHouse)
    {
        if (!from.CheckAlive() || from.Backpack?.FindItemByType<HousePlacementTool>() == null)
        {
            return;
        }

        if (!okay)
        {
            prevHouse.Delete();
            return;
        }

        if (prevHouse.Deleted)
        {
            from.SendGump(new HousePlacementTimeoutNoticeGump());
            return;
        }

        var center = prevHouse.Location;

        prevHouse.Delete();

        var res = HousePlacement.Check(from, MultiID, center, out var toMove, HouseDirection);

        switch (res)
        {
            case HousePlacementResult.Valid:
                {
                    if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
                    {
                        from.SendLocalizedMessage(501271); // You already own a house, you may not place another!
                    }
                    else
                    {
                        var house = ConstructHouse(from);

                        if (house == null)
                        {
                            return;
                        }

                        house.Price = Cost;

                        if (from.AccessLevel >= AccessLevel.GameMaster)
                        {
                            from.SendMessage(
                                $"{Cost} gold would have been withdrawn from your bank if you were not a GM."
                            );
                        }
                        else if (Banker.Withdraw(from, Cost))
                        {
                            // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                            from.SendLocalizedMessage(1060398, Cost.ToString());
                        }
                        else
                        {
                            house.RemoveKeys(from);
                            house.Delete();
                            // You do not have the funds available in your bank box to purchase this house.  Try placing a smaller house, or adding gold or checks to your bank box.
                            from.SendLocalizedMessage(1060646);
                            return;
                        }

                        house.MoveToWorld(center, from.Map);

                        for (var i = 0; i < toMove.Count; ++i)
                        {
                            object o = toMove[i];

                            if (o is Mobile mobile)
                            {
                                mobile.Location = house.BanLocation;
                            }
                            else if (o is Item item)
                            {
                                item.Location = house.BanLocation;
                            }
                        }
                    }

                    break;
                }
            case HousePlacementResult.BadItem:
            case HousePlacementResult.BadLand:
            case HousePlacementResult.BadStatic:
            case HousePlacementResult.BadRegionHidden:
            case HousePlacementResult.NoSurface:
                {
                    // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
                    from.SendLocalizedMessage(1043287);
                    break;
                }
            case HousePlacementResult.BadRegion:
                {
                    from.SendLocalizedMessage(501265); // Housing cannot be created in this area.
                    break;
                }
            case HousePlacementResult.BadRegionTemp:
                {
                    // Lord British has decreed a 'no build' period, thus you cannot build this house at this time.
                    from.SendLocalizedMessage(501270);
                    break;
                }
            case HousePlacementResult.BadRegionRaffle:
                {
                    // You must have a deed for this plot of land in order to build here.
                    from.SendLocalizedMessage(1150493);
                    break;
                }
            case HousePlacementResult.InvalidCastleKeep:
                {
                    from.SendLocalizedMessage(1061122); // Castles and keeps cannot be created here.
                    break;
                }
        }
    }

    public bool OnPlacement(Mobile from, Point3D p)
    {
        if (!from.CheckAlive() || from.Backpack?.FindItemByType<HousePlacementTool>() == null)
        {
            return false;
        }

        var center = new Point3D(p.X - Offset.X, p.Y - Offset.Y, p.Z - Offset.Z);
        var res = HousePlacement.Check(from, MultiID, center, out var toMove, HouseDirection);

        switch (res)
        {
            case HousePlacementResult.Valid:
                {
                    if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
                    {
                        from.SendLocalizedMessage(501271); // You already own a house, you may not place another!
                    }
                    else
                    {
                        from.SendLocalizedMessage(1011576); // This is a valid location.

                        var prev = new PreviewHouse(MultiID);

                        var mcl = prev.Components;

                        var banLoc = new Point3D(center.X + mcl.Min.X, center.Y + mcl.Max.Y + 1, center.Z);

                        for (var i = 0; i < mcl.List.Length; ++i)
                        {
                            var entry = mcl.List[i];

                            int itemID = entry.ItemId;

                            if (itemID >= 0xBA3 && itemID <= 0xC0E)
                            {
                                banLoc = new Point3D(center.X + entry.OffsetX, center.Y + entry.OffsetY, center.Z);
                                break;
                            }
                        }

                        for (var i = 0; i < toMove.Count; ++i)
                        {
                            object o = toMove[i];

                            if (o is Mobile mobile)
                            {
                                mobile.Location = banLoc;
                            }
                            else if (o is Item item)
                            {
                                item.Location = banLoc;
                            }
                        }

                        prev.MoveToWorld(center, from.Map);

                        from.SendGump(
                            new CondemnWarningGump(okay => PlacementWarning_Callback(from, okay, prev))
                        );

                        return true;
                    }

                    break;
                }
            case HousePlacementResult.BadItem:
            case HousePlacementResult.BadLand:
            case HousePlacementResult.BadStatic:
            case HousePlacementResult.BadRegionHidden:
            case HousePlacementResult.NoSurface:
                {
                    // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
                    from.SendLocalizedMessage(1043287);
                    break;
                }
            case HousePlacementResult.BadRegion:
                {
                    from.SendLocalizedMessage(501265); // Housing cannot be created in this area.
                    break;
                }
            case HousePlacementResult.BadRegionTemp:
                {
                    // Lord British has decreed a 'no build' period, thus you cannot build this house at this time.
                    from.SendLocalizedMessage(501270);
                    break;
                }
            case HousePlacementResult.BadRegionRaffle:
                {
                    // You must have a deed for this plot of land in order to build here.
                    from.SendLocalizedMessage(1150493);
                    break;
                }
            case HousePlacementResult.InvalidCastleKeep:
                {
                    from.SendLocalizedMessage(1061122); // Castles and keeps cannot be created here.
                    break;
                }
        }

        return false;
    }

    public static HousePlacementEntry Find(BaseHouse house)
    {
        _table.TryGetValue(house.GetType(), out var obj);

        if (obj is HousePlacementEntry entry)
        {
            return entry;
        }

        if (obj is List<HousePlacementEntry> list)
        {
            foreach (var hpe in list)
            {
                if (hpe.MultiID == house.ItemID)
                {
                    return hpe;
                }
            }

            return null;
        }

        if (obj is Dictionary<int, HousePlacementEntry> table)
        {
            return table[house.ItemID];
        }

        return null;
    }

    private static void FillTable(HousePlacementEntry[] entries)
    {
        for (var i = 0; i < entries.Length; ++i)
        {
            var e = entries[i];

            if (!_table.TryGetValue(e.Type, out var obj))
            {
                _table[e.Type] = e;
            }
            else if (obj is HousePlacementEntry entry)
            {
                var list = new List<HousePlacementEntry> { entry, e };

                _table[e.Type] = list;
            }
            else if (obj is List<HousePlacementEntry> list)
            {
                if (list.Count == 8)
                {
                    var table = new Dictionary<int, HousePlacementEntry>();

                    foreach (var t in list)
                    {
                        table[t.MultiID] = t;
                    }

                    table[e.MultiID] = e;

                    _table[e.Type] = table;
                }
                else
                {
                    list.Add(e);
                }
            }
            else if (obj is Dictionary<int, HousePlacementEntry> table)
            {
                table[e.MultiID] = e;
            }
        }
    }

    private class CondemnWarningGump : StaticWarningGump<CondemnWarningGump>
    {
        /*
         * You are about to place a new house.
         * Placing this house will <a href = "?ForceTopic97">condemn</a> any and all of your other houses that you may have.
         * All of your houses on <U>all shards</U> will be affected.<BR><BR>In addition, you will not be able to place
         * another house or have one transferred to you for one (1) real-life week.<BR><BR>
         * Once you accept these terms, these effects cannot be reversed.
         * Re-deeding or transferring your new house will <U>not</U> uncondemn your other house(s) nor will the one
         * week timer be removed.<BR><BR>If you are absolutely certain you wish to proceed, click the button next to OKAY below.
         * If you do not wish to trade for this house, click CANCEL.
         */

        public override int StaticLocalizedContent => 1049583;
        public override int Width => 420;
        public override int Height => 280;

        public CondemnWarningGump(Action<bool> callback) : base(callback)
        {
        }
    }

    private class HousePlacementTimeoutNoticeGump : StaticWarningGump<HousePlacementTimeoutNoticeGump>
    {
        // Too much time has passed and the test house you created has been deleted.  Please try again!
        public override int StaticLocalizedContent => 1060647;
        public override int Width => 320;
        public override int Height => 180;
    }
}
