using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Mobiles;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps;
public class WhoGump : DynamicGump
{
    private const int EntryWidth = 180;
    private const int EntryCount = 15;

    private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

    private const int BackWidth = BorderSize + TotalWidth + BorderSize;

    private readonly List<Mobile> _mobiles;
    private readonly int _page;

    public override bool Singleton => true;

    public WhoGump(Mobile owner, string filter) : this(BuildList(owner, filter))
    {
    }

    public WhoGump(List<Mobile> list, int page = 0) : base(GumpOffsetX, GumpOffsetY)
    {
        _mobiles = list;
        _page = page;
    }

    public static void Configure()
    {
        CommandSystem.Register("WhoList", AccessLevel.Counselor, WhoList_OnCommand);
    }

    [Usage("WhoList [filter]")]
    [Aliases("Who")]
    [Description("Lists all connected clients. Optionally filters results by name.")]
    private static void WhoList_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new WhoGump(e.Mobile, e.ArgString));
    }

    public static List<Mobile> BuildList(Mobile owner, string rawFilter)
    {
        var filter = rawFilter.AsSpan().Trim();

        var list = new List<Mobile>();

        foreach (var ns in NetState.Instances)
        {
            var m = ns.Mobile;

            if (m == null || m != owner && m.Hidden && owner.AccessLevel < m.AccessLevel &&
                (m is not PlayerMobile mobile || !mobile.VisibilityList.Contains(owner)))
            {
                continue;
            }

            if (filter.Length > 0 && !m.Name.AsSpan().InsensitiveContains(filter))
            {
                continue;
            }

            list.Add(m);
        }

        list.Sort(InternalComparer.Instance);

        return list;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var count = Math.Clamp(_mobiles.Count - _page * EntryCount, 0, EntryCount);

        builder.AddPage();

        builder.AddPropsFrame(TotalWidth, count + 1, out var x, out var y);

        const int emptyWidth = TotalWidth - PrevWidth - NextWidth - OffsetSize * 4;

        builder.AddImageTiled(x, y, emptyWidth, EntryHeight, EntryGumpID);

        builder.AddLabel(
            x + TextOffsetX,
            y,
            TextHue,
            $"Page {_page + 1} of {(_mobiles.Count + EntryCount - 1) / EntryCount} ({_mobiles.Count})"
        );

        x += emptyWidth + OffsetSize;

        builder.AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

        if (_page > 0)
        {
            builder.AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1);
        }

        x += PrevWidth + OffsetSize;

        builder.AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);

        if ((_page + 1) * EntryCount < _mobiles.Count)
        {
            builder.AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 2, GumpButtonType.Reply, 1);
        }

        for (int i = 0, index = _page * EntryCount; i < EntryCount && index < _mobiles.Count; ++i, ++index)
        {
            PropsLayout.NextRow(ref x, ref y);

            var m = _mobiles[index];

            builder.AddPropsEntryButton(
                ref x,
                ref y,
                EntryWidth,
                m.Deleted ? "(deleted)" : m.Name,
                m.NetState != null && !m.Deleted,
                i + 3,
                textHue: GetHueFor(m)
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetHueFor(Mobile m) =>
        m.AccessLevel switch
        {
            >= AccessLevel.Administrator => 0x516,
            AccessLevel.Seer             => 0x144,
            AccessLevel.GameMaster       => 0x21,
            AccessLevel.Counselor        => 0x2,
            _ when m.Murderer            => 0x21,
            _ when m.Criminal            => 0x3B1,
            _                            => 0x58
        };

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        switch (info.ButtonID)
        {
            case 0: // Closed
                {
                    return;
                }
            case 1: // Previous
                {
                    if (_page > 0)
                    {
                        from.SendGump(new WhoGump(_mobiles, _page - 1));
                    }

                    break;
                }
            case 2: // Next
                {
                    if ((_page + 1) * EntryCount < _mobiles.Count)
                    {
                        from.SendGump(new WhoGump(_mobiles, _page + 1));
                    }

                    break;
                }
            default:
                {
                    var index = _page * EntryCount + (info.ButtonID - 3);

                    if (index >= 0 && index < _mobiles.Count)
                    {
                        var m = _mobiles[index];

                        if (m.Deleted)
                        {
                            from.SendMessage("That player has deleted their character.");
                            from.SendGump(new WhoGump(_mobiles, _page));
                        }
                        else if (m.NetState == null)
                        {
                            from.SendMessage("That player is no longer online.");
                            from.SendGump(new WhoGump(_mobiles, _page));
                        }
                        else if (m == from || !m.Hidden || from.AccessLevel >= m.AccessLevel ||
                                 m is PlayerMobile mobile && mobile.VisibilityList.Contains(from))
                        {
                            from.SendGump(new ClientGump(from, m.NetState));
                        }
                        else
                        {
                            from.SendMessage("You cannot see them.");
                            from.SendGump(new WhoGump(_mobiles, _page));
                        }
                    }

                    break;
                }
        }
    }

    private class InternalComparer : IComparer<Mobile>
    {
        public static readonly IComparer<Mobile> Instance = new InternalComparer();

        public int Compare(Mobile x, Mobile y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            if (x.AccessLevel > y.AccessLevel)
            {
                return -1;
            }

            return x.AccessLevel < y.AccessLevel ? 1 : x.Name.InsensitiveCompare(y.Name);
        }
    }
}
