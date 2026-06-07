using System;
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

    private enum Buttons
    {
        Close,
        PrevPage,
        NextPage,
        SortByName,
        SortByAccount,
        SortByAddress,
        Refresh,
        Delete // per-row delete buttons start here: (int)Buttons.Delete + row index
    }

    private const int RowsPerPage = 10;

    private readonly HouseRaffleStone _stone;
    // Cached, sorted snapshot of _stone.Entries. Page navigation, sort, and delete operate on
    // this snapshot so paginated indices stay stable even while other staff or players add or
    // remove entries on the live stone. Staff hits the Refresh button (or reopens the gump) to
    // resync. _count is the logical length; _entries backing array is reused across responses.
    private RaffleEntry[] _entries;
    private int _count;
    private SortMethod _sort;
    private int _page;

    public override bool Singleton => true;

    private HouseRaffleManagementGump(HouseRaffleStone stone, SortMethod sort = SortMethod.Default, int page = 0)
        : base(40, 40)
    {
        _stone = stone;
        _page = page;
        _sort = sort;

        _entries = _stone.Entries.ToArray();
        _count = _entries.Length;
        SortSnapshot();
    }

    public static void DisplayTo(Mobile from, HouseRaffleStone stone, SortMethod sort = SortMethod.Default, int page = 0)
    {
        if (from?.NetState != null && stone != null && !stone.Deleted)
        {
            from.SendGump(new HouseRaffleManagementGump(stone, sort, page));
        }
    }

    private void SortSnapshot()
    {
        if (_sort == SortMethod.Default || _count <= 1)
        {
            return;
        }

        var sortMethod = _sort switch
        {
            SortMethod.Name    => NameComparer.Instance,
            SortMethod.Account => AccountComparer.Instance,
            SortMethod.Address => AddressComparer.Instance
        };

        Array.Sort(_entries, 0, _count, sortMethod);
    }

    private void RefreshSnapshot()
    {
        var live = _stone.Entries;
        var liveCount = live.Count;

        if (_entries.Length < liveCount)
        {
            // Grow with doubling so subsequent inserts can fit without re-allocating each time.
            Array.Resize(ref _entries, Math.Max(_entries.Length * 2, liveCount));
        }

        live.CopyTo(_entries, 0);

        // Clear any references in trailing slots so we don't pin deleted entries.
        if (_count > liveCount)
        {
            Array.Clear(_entries, liveCount, _count - liveCount);
        }

        _count = liveCount;
        SortSnapshot();
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

        builder.AddButton(440, 33, 0xFA5, 0xFA7, (int)Buttons.SortByName);
        builder.AddHtml(474, 35, 120, 20, "Sort by name".Color(LabelColor));

        builder.AddButton(440, 53, 0xFA5, 0xFA7, (int)Buttons.SortByAccount);
        builder.AddHtml(474, 55, 120, 20, "Sort by account".Color(LabelColor));

        builder.AddButton(440, 73, 0xFA5, 0xFA7, (int)Buttons.SortByAddress);
        builder.AddHtml(474, 75, 120, 20, "Sort by address".Color(LabelColor));

        builder.AddImageTiled(13, 99, 592, 242, 9264);
        builder.AddImageTiled(14, 100, 590, 240, 9274);
        builder.AddAlphaRegion(14, 100, 590, 240);

        builder.AddHtml(14, 100, 590, 20, "Entries".Center(LabelColor));

        builder.AddButton(545, 104, 0x845, 0x846, (int)Buttons.Refresh);

        if (_page > 0)
        {
            builder.AddButton(567, 104, 0x15E3, 0x15E7, (int)Buttons.PrevPage);
        }
        else
        {
            builder.AddImage(567, 104, 0x25EA);
        }

        if ((_page + 1) * RowsPerPage < _count)
        {
            builder.AddButton(584, 104, 0x15E1, 0x15E5, (int)Buttons.NextPage);
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
        var pageStart = _page * RowsPerPage;
        var pageEnd = Math.Min(pageStart + RowsPerPage, _count);

        for (var i = pageStart; i < pageEnd; ++i, ++idx)
        {
            var entry = _entries[i];

            if (entry == null)
            {
                continue;
            }

            builder.AddButton(13, 138 + idx * 20, 4002, 4004, (int)Buttons.Delete + i);

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
        if (_stone.Deleted)
        {
            return;
        }

        switch ((Buttons)info.ButtonID)
        {
            case Buttons.Close:
                {
                    return;
                }
            case Buttons.PrevPage:
                {
                    if (_page > 0)
                    {
                        _page--;
                    }

                    break;
                }
            case Buttons.NextPage:
                {
                    if ((_page + 1) * RowsPerPage < _count)
                    {
                        _page++;
                    }

                    break;
                }
            case Buttons.SortByName:
                {
                    _sort = SortMethod.Name;
                    _page = 0;
                    SortSnapshot();

                    break;
                }
            case Buttons.SortByAccount:
                {
                    _sort = SortMethod.Account;
                    _page = 0;
                    SortSnapshot();

                    break;
                }
            case Buttons.SortByAddress:
                {
                    _sort = SortMethod.Address;
                    _page = 0;
                    SortSnapshot();

                    break;
                }
            case Buttons.Refresh:
                {
                    RefreshSnapshot();

                    var maxPage = _count == 0 ? 0 : (_count - 1) / RowsPerPage;
                    if (_page > maxPage)
                    {
                        _page = maxPage;
                    }

                    break;
                }
            default: // Per-row delete
                {
                    var deleteIndex = info.ButtonID - (int)Buttons.Delete;

                    if (deleteIndex < 0 || deleteIndex >= _count)
                    {
                        return;
                    }

                    var target = _entries[deleteIndex];

                    if (target != null)
                    {
                        _stone.Entries.Remove(target);
                    }

                    // Compact the snapshot in place — array stays sorted, no reallocation.
                    var tail = _count - deleteIndex - 1;
                    if (tail > 0)
                    {
                        Array.Copy(_entries, deleteIndex + 1, _entries, deleteIndex, tail);
                    }
                    _entries[--_count] = null;

                    if (_page > 0 && _page * RowsPerPage >= _count)
                    {
                        _page--;
                    }

                    break;
                }
        }

        sender.Mobile.SendGump(this);
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

            var result = x.From.RawName.InsensitiveCompare(y.From.RawName);

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
