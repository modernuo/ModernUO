using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Items;
using Server.Network;

namespace Server.Gumps;

public class HouseRaffleManagementGump : Gump
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
    private readonly List<RaffleEntry> _list;
    private readonly SortMethod _sort;

    private readonly HouseRaffleStone _stone;
    private int _page;

    public HouseRaffleManagementGump(HouseRaffleStone stone, SortMethod sort = SortMethod.Default, int page = 0)
        : base(40, 40)
    {
        _stone = stone;
        _page = page;

        _list = new List<RaffleEntry>(_stone.Entries);
        _sort = sort;

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

        AddPage(0);

        AddBackground(0, 0, 618, 354, 9270);
        AddAlphaRegion(10, 10, 598, 334);

        AddHtml(10, 10, 598, 20, Color(Center("Raffle Management"), LabelColor));

        AddHtml(45, 35, 100, 20, Color("Location:", LabelColor));
        AddHtml(145, 35, 250, 20, Color(_stone.FormatLocation(), LabelColor));

        AddHtml(45, 55, 100, 20, Color("Ticket Price:", LabelColor));
        AddHtml(145, 55, 250, 20, Color(_stone.FormatPrice(), LabelColor));

        AddHtml(45, 75, 100, 20, Color("Total Entries:", LabelColor));
        AddHtml(145, 75, 250, 20, Color(_stone.Entries.Count.ToString(), LabelColor));

        AddButton(440, 33, 0xFA5, 0xFA7, 3);
        AddHtml(474, 35, 120, 20, Color("Sort by name", LabelColor));

        AddButton(440, 53, 0xFA5, 0xFA7, 4);
        AddHtml(474, 55, 120, 20, Color("Sort by account", LabelColor));

        AddButton(440, 73, 0xFA5, 0xFA7, 5);
        AddHtml(474, 75, 120, 20, Color("Sort by address", LabelColor));

        AddImageTiled(13, 99, 592, 242, 9264);
        AddImageTiled(14, 100, 590, 240, 9274);
        AddAlphaRegion(14, 100, 590, 240);

        AddHtml(14, 100, 590, 20, Color(Center("Entries"), LabelColor));

        if (page > 0)
        {
            AddButton(567, 104, 0x15E3, 0x15E7, 1);
        }
        else
        {
            AddImage(567, 104, 0x25EA);
        }

        if ((page + 1) * 10 < _list.Count)
        {
            AddButton(584, 104, 0x15E1, 0x15E5, 2);
        }
        else
        {
            AddImage(584, 104, 0x25E6);
        }

        AddHtml(14, 120, 30, 20, Color(Center("DEL"), LabelColor));
        AddHtml(47, 120, 250, 20, Color("Name", LabelColor));
        AddHtml(295, 120, 100, 20, Color(Center("Address"), LabelColor));
        AddHtml(395, 120, 150, 20, Color(Center("Date"), LabelColor));
        AddHtml(545, 120, 60, 20, Color(Center("Num"), LabelColor));

        var idx = 0;
        var winner = _stone.Winner;

        for (var i = page * 10; i >= 0 && i < _list.Count && i < (page + 1) * 10; ++i, ++idx)
        {
            var entry = _list[i];

            if (entry == null)
            {
                continue;
            }

            AddButton(13, 138 + idx * 20, 4002, 4004, 6 + i);

            var x = 45;
            var color = winner != null && entry.From == winner ? HighlightColor : LabelColor;

            if (entry.From != null)
            {
                if (entry.From.Account is Account acc)
                {
                    AddHtml(x + 2, 140 + idx * 20, 250, 20, Color($"{entry.From.RawName} ({acc})", color));
                }
                else
                {
                    AddHtml(x + 2, 140 + idx * 20, 250, 20, Color(entry.From.RawName, color));
                }
            }

            x += 250;

            if (entry.Address != null)
            {
                AddHtml(x, 140 + idx * 20, 100, 20, Color(Center(entry.Address.ToString()), color));
            }

            x += 100;

            AddHtml(x, 140 + idx * 20, 150, 20, Color(Center(entry.Date.ToString()), color));
            x += 150;

            AddHtml(x, 140 + idx * 20, 60, 20, Color(Center("1"), color));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string Right(string text) => $"<DIV ALIGN=RIGHT>{text}</DIV>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string Center(string text) => $"<CENTER>{text}</CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

    public override void OnResponse(NetState sender, RelayInfo info)
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

                    from.SendGump(new HouseRaffleManagementGump(_stone, _sort, _page));

                    break;
                }
            case 2: // Next
                {
                    if ((_page + 1) * 10 < _stone.Entries.Count)
                    {
                        _page++;
                    }

                    from.SendGump(new HouseRaffleManagementGump(_stone, _sort, _page));

                    break;
                }
            case 3: // Sort by name
                {
                    from.SendGump(new HouseRaffleManagementGump(_stone, SortMethod.Name));

                    break;
                }
            case 4: // Sort by account
                {
                    from.SendGump(new HouseRaffleManagementGump(_stone, SortMethod.Account));

                    break;
                }
            case 5: // Sort by address
                {
                    from.SendGump(new HouseRaffleManagementGump(_stone, SortMethod.Address));

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

                        from.SendGump(new HouseRaffleManagementGump(_stone, _sort, _page));
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

            var a = x.Address.GetAddressBytes();
            var b = y.Address.GetAddressBytes();

            for (var i = 0; i < a.Length && i < b.Length; i++)
            {
                var compare = a[i].CompareTo(b[i]);

                if (compare != 0)
                {
                    return compare;
                }
            }

            return x.Date.CompareTo(y.Date);
        }
    }
}
