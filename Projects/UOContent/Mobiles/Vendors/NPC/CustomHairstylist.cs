using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class CustomHairstylist : BaseVendor
{
    private static readonly HairstylistBuyInfo[] _sellList =
    [
        new(
            1018357, // New Hair (50000 gold)
            50000,
            false,
            (from, vendor, price) =>
                new ChangeHairstyleGump(from, vendor, price, false, ChangeHairstyleEntry.HairEntries)
        ),
        new(
            1018358, // New Beard (50000 gold)
            50000,
            true,
            (from, vendor, price) =>
                new ChangeHairstyleGump(from, vendor, price, true, ChangeHairstyleEntry.BeardEntries)
        ),
        new(
            1018359, // Normal Hair Dye (50 gold)
            50,
            false,
            (_, vendor, price) =>
                new ChangeHairHueGump(vendor, price, true, true, ChangeHairHueEntry.RegularEntries)
        ),
        new(
            1018360, // Bright Hair Dye (500000 gold)
            500000,
            false,
            (_, vendor, price) =>
                new ChangeHairHueGump(vendor, price, true, true, ChangeHairHueEntry.BrightEntries)
        ),
        new(
            1018361, // Hair Only Dye (30000 gold)
            30000,
            false,
            (_, vendor, price) =>
                new ChangeHairHueGump(vendor, price, true, false, ChangeHairHueEntry.RegularEntries)
        ),
        new(
            1018362, // Beard Only Dye (30000 gold)
            30000,
            true,
            (_, vendor, price) =>
                new ChangeHairHueGump(vendor, price, false, true, ChangeHairHueEntry.RegularEntries)
        ),
        new(
            1018363, // Bright Hair Only Dye (500000 gold)
            500000,
            false,
            (_, vendor, price) =>
                new ChangeHairHueGump(vendor, price, true, false, ChangeHairHueEntry.BrightEntries)
        ),
        new(
            1018364, // Bright Beard Only Dye (500000 gold)
            500000,
            true,
            (_, vendor, price) =>
                new ChangeHairHueGump(vendor, price, false, true, ChangeHairHueEntry.BrightEntries)
        )
    ];

    [Constructible]
    public CustomHairstylist() : base("the hairstylist")
    {
    }

    protected override List<SBInfo> SBInfos { get; } = [];

    public override bool ClickTitle => false;
    public override bool IsActiveBuyer => false;
    public override bool IsActiveSeller => true;

    public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals;

    public override bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list) => false;

    public override void VendorBuy(Mobile from) => from.SendGump(new HairstylistBuyGump(from, this, _sellList));

    public override int GetHairHue() => Utility.RandomBrightHue();

    public override void InitOutfit()
    {
        base.InitOutfit();

        AddItem(new Robe(Utility.RandomPinkHue()));
    }

    public override void InitSBInfo()
    {
    }
}

public class HairstylistBuyInfo
{
    public HairstylistBuyInfo(
        TextDefinition title, int price, bool facialHair, Func<Mobile, Mobile, int, BaseGump> gumpFunction
    )
    {
        Title = title;
        Price = price;
        FacialHair = facialHair;
        GumpFactoryFn = gumpFunction;
    }

    public TextDefinition Title { get; }

    public int Price { get; }

    public bool FacialHair { get; }

    public Func<Mobile, Mobile, int, BaseGump> GumpFactoryFn { get; }
}

public class HairstylistBuyGump : DynamicGump
{
    private readonly Mobile _from;
    private readonly HairstylistBuyInfo[] _sellList;
    private readonly Mobile _vendor;
    public override bool Singleton => true;

    public HairstylistBuyGump(Mobile from, Mobile vendor, HairstylistBuyInfo[] sellList) : base(50, 50)
    {
        _from = from;
        _vendor = vendor;
        _sellList = sellList;

        var gumps = from.GetGumps();

        gumps.Close<ChangeHairHueGump>();
        gumps.Close<ChangeHairstyleGump>();
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var isFemale = _from.Female || _from.Body.IsFemale;

        var balance = Banker.GetBalance(_from);
        var canAfford = 0;

        for (var i = 0; i < _sellList.Length; ++i)
        {
            var buyInfo = _sellList[i];
            if (balance >= buyInfo.Price && (!buyInfo.FacialHair || !isFemale))
            {
                ++canAfford;
            }
        }

        builder.AddPage();
        builder.AddBackground(50, 10, 450, 100 + canAfford * 25, 2600);
        builder.AddHtmlLocalized(100, 40, 350, 20, 1018356); // Choose your hairstyle change:

        var index = 0;

        for (var i = 0; i < _sellList.Length; ++i)
        {
            var buyInfo = _sellList[i];
            if (balance >= buyInfo.Price && (!buyInfo.FacialHair || !isFemale))
            {
                buyInfo.Title.AddHtmlText(ref builder, 140, 75 + index * 25, 300, 20);
                builder.AddButton(100, 75 + index++ * 25, 4005, 4007, 1 + i);
            }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;

        if (index < 0 || index >= _sellList.Length)
        {
            return;
        }

        var balance = Banker.GetBalance(_from);
        var isFemale = _from.Female || _from.Body.IsFemale;

        var buyInfo = _sellList[index];
        if (buyInfo.FacialHair && isFemale)
        {
            _vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1010639, _from.NetState);
        }
        else if (balance >= buyInfo.Price)
        {
            _from.SendGump(buyInfo.GumpFactoryFn(_from, _vendor, buyInfo.Price));
        }
        else
        {
            _vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042293, _from.NetState);
        }
    }
}

public class ChangeHairHueEntry
{
    public static readonly ChangeHairHueEntry[] BrightEntries =
    [
        new("*****", 12, 10),
        new("*****", 32, 5),
        new("*****", 38, 8),
        new("*****", 54, 3),
        new("*****", 62, 10),
        new("*****", 81, 2),
        new("*****", 89, 2),
        new("*****", 1153, 2)
    ];

    public static readonly ChangeHairHueEntry[] RegularEntries =
    [
        new("*****", 1602, 26),
        new("*****", 1628, 27),
        new("*****", 1502, 32),
        new("*****", 1302, 32),
        new("*****", 1402, 32),
        new("*****", 1202, 24),
        new("*****", 2402, 29),
        new("*****", 2213, 6),
        new("*****", 1102, 8),
        new("*****", 1110, 8),
        new("*****", 1118, 16),
        new("*****", 1134, 16)
    ];

    public ChangeHairHueEntry(string name, int[] hues)
    {
        Name = name;
        Hues = hues;
    }

    public ChangeHairHueEntry(string name, int start, int count)
    {
        Name = name;

        Hues = new int[count];

        for (var i = 0; i < count; ++i)
        {
            Hues[i] = start + i;
        }
    }

    public string Name { get; }

    public int[] Hues { get; }
}

public class ChangeHairHueGump : DynamicGump
{
    private readonly ChangeHairHueEntry[] _entries;
    private readonly bool _facialHair;
    private readonly bool _hair;
    private readonly int _price;
    private readonly Mobile _vendor;

    public override bool Singleton => true;

    public ChangeHairHueGump(
        Mobile vendor, int price, bool hair, bool facialHair, ChangeHairHueEntry[] entries
    ) : base(50, 50)
    {
        _vendor = vendor;
        _price = price;
        _hair = hair;
        _facialHair = facialHair;
        _entries = entries;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(100, 10, 350, 370, 2600);
        builder.AddBackground(120, 54, 110, 270, 5100);

        builder.AddHtmlLocalized(155, 25, 240, 30, 1011013); // <center>Hair Color Selection Menu</center>

        builder.AddHtmlLocalized(150, 330, 220, 35, 1011014); // Dye my hair this color!
        builder.AddButton(380, 330, 4005, 4007, 1);

        for (var i = 0; i < _entries.Length; ++i)
        {
            var entry = _entries[i];

            builder.AddLabel(130, 59 + i * 22, entry.Hues[0] - 1, entry.Name);
            builder.AddButton(207, 60 + i * 22, 5224, 5224, 0, GumpButtonType.Page, 1 + i);
        }

        for (var i = 0; i < _entries.Length; ++i)
        {
            var entry = _entries[i];
            var hues = entry.Hues;
            var name = entry.Name;

            builder.AddPage(1 + i);

            for (var j = 0; j < hues.Length; ++j)
            {
                var page = Math.DivRem(j, 16, out var index);
                builder.AddLabel(278 + page * 80, 52 + index * 17, hues[j] - 1, name);
                builder.AddRadio(260 + page * 80, 52 + index * 17, 210, 211, false, j * _entries.Length + i);
            }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        var switches = info.Switches;
        if (info.ButtonID != 1 || switches.Length <= 0)
        {
            // You decide not to change your hairstyle.
            _vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, from.NetState);
            return;
        }

        var offset = Math.DivRem(switches[0], _entries.Length, out var index);

        if (index < 0 || index >= _entries.Length || offset < 0 || offset >= _entries[index].Hues.Length)
        {
            return;
        }

        if ((!_hair || from.HairItemID <= 0) && (!_facialHair || from.FacialHairItemID <= 0))
        {
            // You have no hair to dye and you cannot use this.
            _vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502623, sender);
            return;
        }

        if (!Banker.Withdraw(from, _price))
        {
            // You cannot afford my services for that style.
            _vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042293, sender);
            return;
        }

        var hue = _entries[index].Hues[offset];

        if (_hair)
        {
            from.HairHue = hue;
        }

        if (_facialHair)
        {
            from.FacialHairHue = hue;
        }
    }
}

public class ChangeHairstyleEntry
{
    public static readonly ChangeHairstyleEntry[] HairEntries =
    [
        new(50700, 70 - 137, 20 - 60, 0x203B),
        new(60710, 193 - 260, 18 - 60, 0x2045),
        new(50703, 316 - 383, 25 - 60, 0x2044),
        new(60708, 70 - 137, 75 - 125, 0x203C),
        new(60900, 193 - 260, 85 - 125, 0x2047),
        new(60713, 320 - 383, 85 - 125, 0x204A),
        new(60702, 70 - 137, 140 - 190, 0x203D),
        new(60707, 193 - 260, 140 - 190, 0x2049),
        new(60901, 315 - 383, 150 - 190, 0x2048),
        new(0, 0, 0, 0)
    ];

    public static readonly ChangeHairstyleEntry[] BeardEntries =
    [
        new(50800, 120 - 187, 30 - 80, 0x2040),
        new(50904, 243 - 310, 33 - 80, 0x204B),
        new(50906, 120 - 187, 100 - 150, 0x204D),
        new(50801, 243 - 310, 95 - 150, 0x203E),
        new(50802, 120 - 187, 173 - 220, 0x203F),
        new(50905, 243 - 310, 165 - 220, 0x204C),
        new(50808, 120 - 187, 242 - 290, 0x2041),
        new(0, 0, 0, 0)
    ];

    public ChangeHairstyleEntry(int gumpID, int x, int y, int itemID)
    {
        GumpID = gumpID;
        X = x;
        Y = y;
        ItemID = itemID;
    }

    public int ItemID { get; }

    public int GumpID { get; }

    public int X { get; }

    public int Y { get; }
}

public class ChangeHairstyleGump : DynamicGump
{
    private readonly ChangeHairstyleEntry[] _entries;
    private readonly bool _facialHair;
    private readonly Mobile _from;
    private readonly int _price;
    private readonly Mobile _vendor;

    public override bool Singleton => true;

    public ChangeHairstyleGump(Mobile from, Mobile vendor, int price, bool facialHair, ChangeHairstyleEntry[] entries)
        : base(50, 50)
    {
        _from = from;
        _vendor = vendor;
        _price = price;
        _facialHair = facialHair;
        _entries = entries;

        var gumps = from.GetGumps();

        gumps.Close<HairstylistBuyGump>();
        gumps.Close<ChangeHairHueGump>();
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var tableWidth = _facialHair ? 2 : 3;
        var tableHeight = (_entries.Length + tableWidth - (_facialHair ? 1 : 2)) / tableWidth;
        const int offsetWidth = 123;
        var offsetHeight = _facialHair ? 70 : 65;
        var tableWidthOffset = 81 + tableWidth * offsetWidth;
        var tableHeightOffset = 45 + tableHeight * offsetHeight;

        builder.AddPage();

        builder.AddBackground(0, 0, tableWidthOffset, 60 + tableHeightOffset, 2600);

        builder.AddButton(45, tableHeightOffset, 4005, 4007, 1);
        builder.AddHtmlLocalized(77, tableHeightOffset, 90, 35, 1006044); // Ok

        builder.AddButton(tableWidthOffset - 180, tableHeightOffset, 4005, 4007, 0);
        // Cancel
        builder.AddHtmlLocalized(tableWidthOffset - 148, tableHeightOffset, 90, 35, 1006045);

        if (!_facialHair)
        {
            builder.AddHtmlLocalized(50, 15, 350, 20, 1018353); // <center>New Hairstyle</center>
        }
        else
        {
            builder.AddHtmlLocalized(55, 15, 200, 20, 1018354); // <center>New Beard</center>
        }

        for (var i = 0; i < _entries.Length; ++i)
        {
            var yTable = Math.DivRem(i, tableWidth, out var xTable);
            var xOffset = xTable * offsetWidth;
            var yOffset = yTable * offsetHeight;
            var entry = _entries[i];

            if (entry.GumpID != 0)
            {
                builder.AddRadio(40 + xOffset, 70 + yOffset, 208, 209, false, i);
                builder.AddBackground(87 + xOffset, 50 + yOffset, 50, 50, 2620);
                builder.AddImage(87 + xOffset + entry.X, 50 + yOffset + entry.Y, entry.GumpID);
            }
            else if (!_facialHair)
            {
                builder.AddRadio(40 + (xTable + 1) * offsetWidth, 240, 208, 209, false, i);
                builder. AddHtmlLocalized(60 + (xTable + 1) * offsetWidth, 240, 85, 35, 1011064); // Bald
            }
            else
            {
                builder.AddRadio(40 + xOffset, 70 + yOffset, 208, 209, false, i);
                builder.AddHtmlLocalized(60 + xOffset, 70 + yOffset, 85, 35, 1011064); // Bald
            }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_facialHair && (_from.Female || _from.Body.IsFemale))
        {
            return;
        }

        if (_from.Race == Race.Elf)
        {
            _from.SendMessage("This isn't implemented for elves yet.  Sorry!");
            return;
        }

        var switches = info.Switches;

        if (info.ButtonID != 1 || switches.Length <= 0)
        {
            // You decide not to change your hairstyle.
            _vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, _from.NetState);
            return;
        }

        var index = switches[0];

        if (index < 0 || index >= _entries.Length)
        {
            return;
        }

        var entry = _entries[index];

        (_from as PlayerMobile)?.SetHairMods(-1, -1);

        if ((_facialHair ? _from.FacialHairItemID : _from.HairItemID) == entry.ItemID)
        {
            return;
        }

        if (!Banker.Withdraw(_from, _price))
        {
            // You cannot afford my services for that style.
            _vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042293, _from.NetState);
        }
        else if (_facialHair)
        {
            _from.FacialHairItemID = entry.ItemID;
        }
        else
        {
            _from.HairItemID = entry.ItemID;
        }
    }
}
