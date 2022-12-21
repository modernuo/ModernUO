using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Utilities;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class CustomHairstylist : BaseVendor
    {
        public static readonly object From = new();
        public static readonly object Vendor = new();
        public static readonly object Price = new();

        private static readonly HairstylistBuyInfo[] m_SellList =
        {
            new(
                1018357,
                50000,
                false,
                typeof(ChangeHairstyleGump),
                new[]
                    { From, Vendor, Price, false, ChangeHairstyleEntry.HairEntries }
            ),
            new(
                1018358,
                50000,
                true,
                typeof(ChangeHairstyleGump),
                new[]
                    { From, Vendor, Price, true, ChangeHairstyleEntry.BeardEntries }
            ),
            new(
                1018359,
                50,
                false,
                typeof(ChangeHairHueGump),
                new[]
                    { From, Vendor, Price, true, true, ChangeHairHueEntry.RegularEntries }
            ),
            new(
                1018360,
                500000,
                false,
                typeof(ChangeHairHueGump),
                new[]
                    { From, Vendor, Price, true, true, ChangeHairHueEntry.BrightEntries }
            ),
            new(
                1018361,
                30000,
                false,
                typeof(ChangeHairHueGump),
                new[]
                    { From, Vendor, Price, true, false, ChangeHairHueEntry.RegularEntries }
            ),
            new(
                1018362,
                30000,
                true,
                typeof(ChangeHairHueGump),
                new[]
                    { From, Vendor, Price, false, true, ChangeHairHueEntry.RegularEntries }
            ),
            new(
                1018363,
                500000,
                false,
                typeof(ChangeHairHueGump),
                new[]
                    { From, Vendor, Price, true, false, ChangeHairHueEntry.BrightEntries }
            ),
            new(
                1018364,
                500000,
                true,
                typeof(ChangeHairHueGump),
                new[]
                    { From, Vendor, Price, false, true, ChangeHairHueEntry.BrightEntries }
            )
        };

        [Constructible]
        public CustomHairstylist() : base("the hairstylist")
        {
        }

        protected override List<SBInfo> SBInfos { get; } = new();

        public override bool ClickTitle => false;

        public override bool IsActiveBuyer => false;
        public override bool IsActiveSeller => true;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals;

        public override bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list) => false;

        public override void VendorBuy(Mobile from)
        {
            from.SendGump(new HairstylistBuyGump(from, this, m_SellList));
        }

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
        public HairstylistBuyInfo(int title, int price, bool facialHair, Type gumpType, object[] args)
        {
            Title = title;
            Price = price;
            FacialHair = facialHair;
            GumpType = gumpType;
            GumpArgs = args;
        }

        public HairstylistBuyInfo(string title, int price, bool facialHair, Type gumpType, object[] args)
        {
            TitleString = title;
            Price = price;
            FacialHair = facialHair;
            GumpType = gumpType;
            GumpArgs = args;
        }

        public int Title { get; }

        public string TitleString { get; }

        public int Price { get; }

        public bool FacialHair { get; }

        public Type GumpType { get; }

        public object[] GumpArgs { get; }
    }

    public class HairstylistBuyGump : Gump
    {
        private readonly Mobile m_From;
        private readonly HairstylistBuyInfo[] m_SellList;
        private readonly Mobile m_Vendor;

        public HairstylistBuyGump(Mobile from, Mobile vendor, HairstylistBuyInfo[] sellList) : base(50, 50)
        {
            m_From = from;
            m_Vendor = vendor;
            m_SellList = sellList;

            from.CloseGump<HairstylistBuyGump>();
            from.CloseGump<ChangeHairHueGump>();
            from.CloseGump<ChangeHairstyleGump>();

            var isFemale = from.Female || from.Body.IsFemale;

            var balance = Banker.GetBalance(from);
            var canAfford = 0;

            for (var i = 0; i < sellList.Length; ++i)
            {
                if (balance >= sellList[i].Price && (!sellList[i].FacialHair || !isFemale))
                {
                    ++canAfford;
                }
            }

            AddPage(0);

            AddBackground(50, 10, 450, 100 + canAfford * 25, 2600);

            AddHtmlLocalized(100, 40, 350, 20, 1018356); // Choose your hairstyle change:

            var index = 0;

            for (var i = 0; i < sellList.Length; ++i)
            {
                if (balance >= sellList[i].Price && (!sellList[i].FacialHair || !isFemale))
                {
                    if (sellList[i].TitleString != null)
                    {
                        AddHtml(140, 75 + index * 25, 300, 20, sellList[i].TitleString);
                    }
                    else
                    {
                        AddHtmlLocalized(140, 75 + index * 25, 300, 20, sellList[i].Title);
                    }

                    AddButton(100, 75 + index++ * 25, 4005, 4007, 1 + i);
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var index = info.ButtonID - 1;

            if (index >= 0 && index < m_SellList.Length)
            {
                var buyInfo = m_SellList[index];

                var balance = Banker.GetBalance(m_From);

                var isFemale = m_From.Female || m_From.Body.IsFemale;

                if (buyInfo.FacialHair && isFemale)
                {
                    m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1010639, m_From.NetState);
                }
                else if (balance >= buyInfo.Price)
                {
                    try
                    {
                        var origArgs = buyInfo.GumpArgs;
                        var args = new object[origArgs.Length];

                        for (var i = 0; i < args.Length; ++i)
                        {
                            if (origArgs[i] == CustomHairstylist.Price)
                            {
                                args[i] = m_SellList[index].Price;
                            }
                            else if (origArgs[i] == CustomHairstylist.From)
                            {
                                args[i] = m_From;
                            }
                            else if (origArgs[i] == CustomHairstylist.Vendor)
                            {
                                args[i] = m_Vendor;
                            }
                            else
                            {
                                args[i] = origArgs[i];
                            }
                        }

                        var g = buyInfo.GumpType.CreateInstance<Gump>(args);

                        m_From.SendGump(g);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else
                {
                    m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042293, m_From.NetState);
                }
            }
        }
    }

    public class ChangeHairHueEntry
    {
        public static readonly ChangeHairHueEntry[] BrightEntries =
        {
            new("*****", 12, 10),
            new("*****", 32, 5),
            new("*****", 38, 8),
            new("*****", 54, 3),
            new("*****", 62, 10),
            new("*****", 81, 2),
            new("*****", 89, 2),
            new("*****", 1153, 2)
        };

        public static readonly ChangeHairHueEntry[] RegularEntries =
        {
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
        };

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

    public class ChangeHairHueGump : Gump
    {
        private readonly ChangeHairHueEntry[] m_Entries;
        private readonly bool m_FacialHair;
        private readonly Mobile m_From;
        private readonly bool m_Hair;
        private readonly int m_Price;
        private readonly Mobile m_Vendor;

        public ChangeHairHueGump(
            Mobile from, Mobile vendor, int price, bool hair, bool facialHair,
            ChangeHairHueEntry[] entries
        ) : base(50, 50)
        {
            m_From = from;
            m_Vendor = vendor;
            m_Price = price;
            m_Hair = hair;
            m_FacialHair = facialHair;
            m_Entries = entries;

            from.CloseGump<HairstylistBuyGump>();
            from.CloseGump<ChangeHairHueGump>();
            from.CloseGump<ChangeHairstyleGump>();

            AddPage(0);

            AddBackground(100, 10, 350, 370, 2600);
            AddBackground(120, 54, 110, 270, 5100);

            AddHtmlLocalized(155, 25, 240, 30, 1011013); // <center>Hair Color Selection Menu</center>

            AddHtmlLocalized(150, 330, 220, 35, 1011014); // Dye my hair this color!
            AddButton(380, 330, 4005, 4007, 1);

            for (var i = 0; i < entries.Length; ++i)
            {
                var entry = entries[i];

                AddLabel(130, 59 + i * 22, entry.Hues[0] - 1, entry.Name);
                AddButton(207, 60 + i * 22, 5224, 5224, 0, GumpButtonType.Page, 1 + i);
            }

            for (var i = 0; i < entries.Length; ++i)
            {
                var entry = entries[i];
                var hues = entry.Hues;
                var name = entry.Name;

                AddPage(1 + i);

                for (var j = 0; j < hues.Length; ++j)
                {
                    AddLabel(278 + j / 16 * 80, 52 + j % 16 * 17, hues[j] - 1, name);
                    AddRadio(260 + j / 16 * 80, 52 + j % 16 * 17, 210, 211, false, j * entries.Length + i);
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                var switches = info.Switches;

                if (switches.Length > 0)
                {
                    var index = switches[0] % m_Entries.Length;
                    var offset = switches[0] / m_Entries.Length;

                    if (index >= 0 && index < m_Entries.Length)
                    {
                        if (offset >= 0 && offset < m_Entries[index].Hues.Length)
                        {
                            if (m_Hair && m_From.HairItemID > 0 || m_FacialHair && m_From.FacialHairItemID > 0)
                            {
                                if (!Banker.Withdraw(m_From, m_Price))
                                {
                                    m_Vendor.PrivateOverheadMessage(
                                        MessageType.Regular,
                                        0x3B2,
                                        1042293, // You cannot afford my services for that style.
                                        m_From.NetState
                                    );
                                    return;
                                }

                                var hue = m_Entries[index].Hues[offset];

                                if (m_Hair)
                                {
                                    m_From.HairHue = hue;
                                }

                                if (m_FacialHair)
                                {
                                    m_From.FacialHairHue = hue;
                                }
                            }
                            else
                            {
                                m_Vendor.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x3B2,
                                    502623, // You have no hair to dye and you cannot use this.
                                    m_From.NetState
                                );
                            }
                        }
                    }
                }
                else
                {
                    // You decide not to change your hairstyle.
                    m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, m_From.NetState);
                }
            }
            else
            {
                // You decide not to change your hairstyle.
                m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, m_From.NetState);
            }
        }
    }

    public class ChangeHairstyleEntry
    {
        public static readonly ChangeHairstyleEntry[] HairEntries =
        {
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
        };

        public static readonly ChangeHairstyleEntry[] BeardEntries =
        {
            new(50800, 120 - 187, 30 - 80, 0x2040),
            new(50904, 243 - 310, 33 - 80, 0x204B),
            new(50906, 120 - 187, 100 - 150, 0x204D),
            new(50801, 243 - 310, 95 - 150, 0x203E),
            new(50802, 120 - 187, 173 - 220, 0x203F),
            new(50905, 243 - 310, 165 - 220, 0x204C),
            new(50808, 120 - 187, 242 - 290, 0x2041),
            new(0, 0, 0, 0)
        };

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

    public class ChangeHairstyleGump : Gump
    {
        private readonly ChangeHairstyleEntry[] m_Entries;
        private readonly bool m_FacialHair;
        private readonly Mobile m_From;
        private readonly int m_Price;
        private readonly Mobile m_Vendor;

        public ChangeHairstyleGump(
            Mobile from, Mobile vendor, int price, bool facialHair, ChangeHairstyleEntry[] entries
        ) : base(50, 50)
        {
            m_From = from;
            m_Vendor = vendor;
            m_Price = price;
            m_FacialHair = facialHair;
            m_Entries = entries;

            from.CloseGump<HairstylistBuyGump>();
            from.CloseGump<ChangeHairHueGump>();
            from.CloseGump<ChangeHairstyleGump>();

            var tableWidth = m_FacialHair ? 2 : 3;
            var tableHeight = (entries.Length + tableWidth - (m_FacialHair ? 1 : 2)) / tableWidth;
            var offsetWidth = 123;
            var offsetHeight = m_FacialHair ? 70 : 65;

            AddPage(0);

            AddBackground(0, 0, 81 + tableWidth * offsetWidth, 105 + tableHeight * offsetHeight, 2600);

            AddButton(45, 45 + tableHeight * offsetHeight, 4005, 4007, 1);
            AddHtmlLocalized(77, 45 + tableHeight * offsetHeight, 90, 35, 1006044); // Ok

            AddButton(81 + tableWidth * offsetWidth - 180, 45 + tableHeight * offsetHeight, 4005, 4007, 0);
            AddHtmlLocalized(
                81 + tableWidth * offsetWidth - 148,
                45 + tableHeight * offsetHeight,
                90,
                35,
                1006045
            ); // Cancel

            if (!facialHair)
            {
                AddHtmlLocalized(50, 15, 350, 20, 1018353); // <center>New Hairstyle</center>
            }
            else
            {
                AddHtmlLocalized(55, 15, 200, 20, 1018354); // <center>New Beard</center>
            }

            for (var i = 0; i < entries.Length; ++i)
            {
                var xTable = i % tableWidth;
                var yTable = i / tableWidth;

                if (entries[i].GumpID != 0)
                {
                    AddRadio(40 + xTable * offsetWidth, 70 + yTable * offsetHeight, 208, 209, false, i);
                    AddBackground(87 + xTable * offsetWidth, 50 + yTable * offsetHeight, 50, 50, 2620);
                    AddImage(
                        87 + xTable * offsetWidth + entries[i].X,
                        50 + yTable * offsetHeight + entries[i].Y,
                        entries[i].GumpID
                    );
                }
                else if (!facialHair)
                {
                    AddRadio(40 + (xTable + 1) * offsetWidth, 240, 208, 209, false, i);
                    AddHtmlLocalized(60 + (xTable + 1) * offsetWidth, 240, 85, 35, 1011064); // Bald
                }
                else
                {
                    AddRadio(40 + xTable * offsetWidth, 70 + yTable * offsetHeight, 208, 209, false, i);
                    AddHtmlLocalized(60 + xTable * offsetWidth, 70 + yTable * offsetHeight, 85, 35, 1011064); // Bald
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_FacialHair && (m_From.Female || m_From.Body.IsFemale))
            {
                return;
            }

            if (m_From.Race == Race.Elf)
            {
                m_From.SendMessage("This isn't implemented for elves yet.  Sorry!");
                return;
            }

            if (info.ButtonID == 1)
            {
                var switches = info.Switches;

                if (switches.Length > 0)
                {
                    var index = switches[0];

                    if (index >= 0 && index < m_Entries.Length)
                    {
                        var entry = m_Entries[index];

                        (m_From as PlayerMobile)?.SetHairMods(-1, -1);

                        var hairID = m_From.HairItemID;
                        var facialHairID = m_From.FacialHairItemID;

                        if (entry.ItemID == 0)
                        {
                            if (m_FacialHair ? facialHairID == 0 : hairID == 0)
                            {
                                return;
                            }

                            if (Banker.Withdraw(m_From, m_Price))
                            {
                                if (m_FacialHair)
                                {
                                    m_From.FacialHairItemID = 0;
                                }
                                else
                                {
                                    m_From.HairItemID = 0;
                                }
                            }
                            else
                            {
                                m_Vendor.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x3B2,
                                    1042293, // You cannot afford my services for that style.
                                    m_From.NetState
                                );
                            }
                        }
                        else
                        {
                            if (m_FacialHair)
                            {
                                if (facialHairID > 0 && facialHairID == entry.ItemID)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                if (hairID > 0 && hairID == entry.ItemID)
                                {
                                    return;
                                }
                            }

                            if (Banker.Withdraw(m_From, m_Price))
                            {
                                if (m_FacialHair)
                                {
                                    m_From.FacialHairItemID = entry.ItemID;
                                }
                                else
                                {
                                    m_From.HairItemID = entry.ItemID;
                                }
                            }
                            else
                            {
                                m_Vendor.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x3B2,
                                    1042293,
                                    m_From.NetState
                                ); // You cannot afford my services for that style.
                            }
                        }
                    }
                }
                else
                {
                    // You decide not to change your hairstyle.
                    m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, m_From.NetState);
                }
            }
            else
            {
                // You decide not to change your hairstyle.
                m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, m_From.NetState);
            }
        }
    }
}
