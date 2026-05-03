using System.Collections.Generic;
using Server.Accounting;
using Server.Items;
using Server.Network;

namespace Server.Gumps;

public class HouseRaffleManagementGump : DynamicGump
{
    public enum SortMethod
    {
        Default,
        Name,
        Account,
        Address
    }

    public const int LabelColor = 0xFFFFFF;
    public const int HighlightColor = 0x11EE11;

    private readonly HouseRaffleStone _stone;
    private readonly List<RaffleEntry> _list;
    private SortMethod _sort;
    private int _page;

    public override bool Singleton => true;

    private HouseRaffleManagementGump(HouseRaffleStone stone, SortMethod sort = SortMethod.Default, int page = 0)
        : base(40, 40)
    {
        _stone = stone;
        _page = page;
        _sort = sort;

        _list = new List<RaffleEntry>(_stone.Entries);

        SortList();
    }

    public static void DisplayTo(Mobile from, HouseRaffleStone stone, SortMethod sort = SortMethod.Default, int page = 0)
    {
        if (from?.NetState == null || stone == null || stone.Deleted)
        {
            return;
        }

        from.SendGump(new HouseRaffleManagementGump(stone, sort, page));
    }

    private void SortList()
    {
        switch (_sort)
        {
            case SortMethod.Name:
                {
                    _list.Sort(NameComparer.Instance);

                    break;
                }
            case SortMethod.Account:
                {
                    _list.Sort(AccountComparer.Instance);

                    break;
                }
            case SortMethod.Address:
                {
                    _list.Sort(AddressComparer.Instance);

                    break;
                }
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 618, 354, 9270);
        builder.AddAlphaRegion(10, 10, 598, 334);

        builder.AddHtml(10, 10, 598, 20, "Raffle Management".Center(LabelColor));

        builder.AddHtml(45, 35, 100, 20, "Location:".Color(LabelColor));
        builder.AddHtml(145, 35, 250, 20, HouseRaffleStone.FormatLocation(_stone.PlotBounds, _stone.GetPlotCenter(), _stone.PlotFacet).Color(LabelColor));

        builder.AddHtml(45, 55, 100, 20, "Ticket Price:".Color(LabelColor));
        builder.AddHtml(145, 55, 250, 20, HouseRaffleStone.FormatPrice(_stone.TicketPrice).Color(LabelColor));

        builder.AddHtml(45, 75, 100, 20, "Total Entries:".Color(LabelColor));
        builder.AddHtml(145, 75, 250, 20, Html.Color($"{_stone.Entries.Count}", LabelColor));

        builder.AddButton(440, 33, 0xFA5, 0xFA7, 3);
        builder.AddHtml(474, 35, 120, 20, "Sort by name".Color(LabelColor));

        builder.AddButton(440, 53, 0xFA5, 0xFA7, 4);
        builder.AddHtml(474, 55, 120, 20, "Sort by account".Color(LabelColor));

        builder.AddButton(440, 73, 0xFA5, 0xFA7, 5);
        builder.AddHtml(474, 75, 120, 20, "Sort by address".Color(LabelColor));

        builder.AddImageTiled(13, 99, 592, 242, 9264);
        builder.AddImageTiled(14, 100, 590, 240, 9274);
        builder.AddAlphaRegion(14, 100, 590, 240);

        builder.AddHtml(14, 100, 590, 20, "Entries".Center(LabelColor));

        if (_page > 0)
        {
            builder.AddButton(567, 104, 0x15E3, 0x15E7, 1);
        }
        else
        {
            builder.AddImage(567, 104, 0x25EA);
        }

        if ((_page + 1) * 10 < _list.Count)
        {
            builder.AddButton(584, 104, 0x15E1, 0x15E5, 2);
        }
        else
        {
            builder.AddImage(584, 104, 0x25E6);
        }

        builder.AddHtml(14, 120, 30, 20, "DEL".Center(LabelColor));
        builder.AddHtml(47, 120, 250, 20, "Name".Color(LabelColor));
        builder.AddHtml(295, 120, 100, 20, "Address".Center(LabelColor));
        builder.AddHtml(395, 120, 150, 20, "Date".Center(LabelColor));
        builder.AddHtml(545, 120, 60, 20, "Num".Center(LabelColor));

        var idx = 0;
        var winner = _stone.Winner;

        for (var i = _page * 10; i >= 0 && i < _list.Count && i < (_page + 1) * 10; ++i, ++idx)
        {
            var entry = _list[i];

            if (entry == null)
            {
                continue;
            }

            builder.AddButton(13, 138 + idx * 20, 4002, 4004, 6 + i);

            var x = 45;
            var color = winner != null && entry.From == winner ? HighlightColor : LabelColor;

            if (entry.From != null)
            {
                if (entry.From.Account is Account acc)
                {
                    builder.AddHtml(x + 2, 140 + idx * 20, 250, 20, Html.Color($"{entry.From.RawName} ({acc})", color));
                }
                else
                {
                    builder.AddHtml(x + 2, 140 + idx * 20, 250, 20, entry.From.RawName.Color(color));
                }
            }

            x += 250;

            if (entry.Address != null)
            {
                builder.AddHtml(x, 140 + idx * 20, 100, 20, Html.Center($"{entry.Address}", color));
            }

            x += 100;

            builder.AddHtml(x, 140 + idx * 20, 150, 20, Html.Center($"{entry.Date}", color));
            x += 150;

            builder.AddHtml(x, 140 + idx * 20, 60, 20, "1".Center(color));
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        var buttonId = info.ButtonID;

        switch (buttonId)
        {
            case 1: // Previous
                {
                    if (_page > 0)
                    {
                        _page--;
                    }

                    DisplayTo(from, _stone, _sort, _page);

                    break;
                }
            case 2: // Next
                {
                    if ((_page + 1) * 10 < _stone.Entries.Count)
                    {
                        _page++;
                    }

                    DisplayTo(from, _stone, _sort, _page);

                    break;
                }
            case 3: // Sort by name
                {
                    DisplayTo(from, _stone, SortMethod.Name);

                    break;
                }
            case 4: // Sort by account
                {
                    DisplayTo(from, _stone, SortMethod.Account);

                    break;
                }
            case 5: // Sort by address
                {
                    DisplayTo(from, _stone, SortMethod.Address);

                    break;
                }
            default: // Delete
                {
                    buttonId -= 6;

                    if (buttonId >= 0 && buttonId < _list.Count)
                    {
                        _stone.Entries.Remove(_list[buttonId]);

                        if (_page > 0 && _page * 10 >= _list.Count - 1)
                        {
                            _page--;
                        }

                        DisplayTo(from, _stone, _sort, _page);
                    }

                    break;
                }
        }
    }

    private class NameComparer : IComparer<RaffleEntry>
    {
        public static readonly IComparer<RaffleEntry> Instance = new NameComparer();

        public int Compare(RaffleEntry x, RaffleEntry y)
        {
            var xIsNull = x?.From == null;
            var yIsNull = y?.From == null;

            if (xIsNull && yIsNull)
            {
                return 0;
            }

            if (xIsNull)
            {
                return -1;
            }

            if (yIsNull)
            {
                return 1;
            }

            var result = x.From.Name.InsensitiveCompare(y.From.Name);

            return result == 0 ? x.Date.CompareTo(y.Date) : result;
        }
    }

    private class AccountComparer : IComparer<RaffleEntry>
    {
        public static readonly IComparer<RaffleEntry> Instance = new AccountComparer();

        public int Compare(RaffleEntry x, RaffleEntry y)
        {
            var xIsNull = x?.From == null;
            var yIsNull = y?.From == null;

            if (xIsNull && yIsNull)
            {
                return 0;
            }

            if (xIsNull)
            {
                return -1;
            }

            if (yIsNull)
            {
                return 1;
            }

            var a = x.From.Account as Account;
            var b = y.From.Account as Account;

            if (a == null && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return -1;
            }

            if (b == null)
            {
                return 1;
            }

            var result = a.Username.InsensitiveCompare(b.Username);

            return result == 0 ? x.Date.CompareTo(y.Date) : result;
        }
    }

    private class AddressComparer : IComparer<RaffleEntry>
    {
        public static readonly IComparer<RaffleEntry> Instance = new AddressComparer();

        public int Compare(RaffleEntry x, RaffleEntry y)
        {
            var xIsNull = x?.Address == null;
            var yIsNull = y?.Address == null;

            if (xIsNull && yIsNull)
            {
                return 0;
            }

            if (xIsNull)
            {
                return -1;
            }

            if (yIsNull)
            {
                return 1;
            }

            var addressCompare = x.Address.ToUInt128().CompareTo(y.Address.ToUInt128());
            if (addressCompare != 0)
            {
                return addressCompare;
            }

            return x.Date.CompareTo(y.Date);
        }
    }
}
