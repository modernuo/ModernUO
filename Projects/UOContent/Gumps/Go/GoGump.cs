using System;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps;

public class GoGump : Gump
{
    private const int PrevLabelOffsetX = PrevWidth + 1;
    private const int PrevLabelOffsetY = 0;

    private const int NextLabelOffsetX = -29;
    private const int NextLabelOffsetY = 0;

    private const int EntryWidth = 180;
    private const int EntryCount = 15;

    private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

    private const int BackWidth = BorderSize + TotalWidth + BorderSize;

    private readonly GoCategory _node;
    private readonly int _page;

    private readonly LocationTree _tree;

    public override bool Singleton => true;

    private GoGump(int page, Mobile from, LocationTree tree, GoCategory node) : base(50, 50)
    {
        if (node == tree.Root)
        {
            tree.LastBranch.Remove(from);
        }
        else
        {
            tree.LastBranch[from] = node;
        }

        _page = page;
        _tree = tree;
        _node = node;

        var x = BorderSize + OffsetSize;
        var y = BorderSize + OffsetSize;

        var count = Math.Clamp(node.Categories.Length + node.Locations.Length - page * EntryCount, 0, EntryCount);

        var totalHeight = OffsetSize + (EntryHeight + OffsetSize) * (count + 1);

        AddPage(0);

        AddBackground(0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID);
        AddImageTiled(
            BorderSize,
            BorderSize,
            TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0),
            totalHeight,
            OffsetGumpID
        );

        if (OldStyle)
        {
            AddImageTiled(x, y, TotalWidth - OffsetSize * 3 - SetWidth, EntryHeight, HeaderGumpID);
        }
        else
        {
            AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);
        }

        if (node.Parent != null)
        {
            AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1);

            if (PrevLabel)
            {
                AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
            }
        }

        x += PrevWidth + OffsetSize;

        var emptyWidth = TotalWidth - PrevWidth * 2 - NextWidth - OffsetSize * 5 -
                         (OldStyle ? SetWidth + OffsetSize : 0);

        if (!OldStyle)
        {
            AddImageTiled(
                x - (OldStyle ? OffsetSize : 0),
                y,
                emptyWidth + (OldStyle ? OffsetSize * 2 : 0),
                EntryHeight,
                EntryGumpID
            );
        }

        AddHtml(x + TextOffsetX, y, emptyWidth - TextOffsetX, EntryHeight, $"<center>{node.Name}</center>");

        x += emptyWidth + OffsetSize;

        if (OldStyle)
        {
            AddImageTiled(x, y, TotalWidth - OffsetSize * 3 - SetWidth, EntryHeight, HeaderGumpID);
        }
        else
        {
            AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);
        }

        if (page > 0)
        {
            AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 2);

            if (PrevLabel)
            {
                AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
            }
        }

        x += PrevWidth + OffsetSize;

        if (!OldStyle)
        {
            AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);
        }

        if ((page + 1) * EntryCount < node.Categories.Length + node.Locations.Length)
        {
            AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 3, GumpButtonType.Reply, 1);

            if (NextLabel)
            {
                AddLabel(x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next");
            }
        }

        var totalEntryCount = node.Categories.Length + node.Locations.Length;

        for (int i = 0, index = page * EntryCount; i < EntryCount && index < totalEntryCount; ++i, ++index)
        {
            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            var name = index >= node.Categories.Length
                ? node.Locations[index - node.Categories.Length].Name
                : node.Categories[index].Name;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, name);

            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, index + 4);
        }
    }

    public static void DisplayTo(Mobile from)
    {
        LocationTree tree = GoLocations.GetLocations(from.Map);

        if (!tree.LastBranch.TryGetValue(from, out var branch))
        {
            branch = tree.Root;
        }

        if (branch != null)
        {
            from.SendGump(new GoGump(0, from, tree, branch));
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        switch (info.ButtonID)
        {
            case 1:
                {
                    if (_node.Parent != null)
                    {
                        from.SendGump(new GoGump(0, from, _tree, _node.Parent));
                    }

                    break;
                }
            case 2:
                {
                    if (_page > 0)
                    {
                        from.SendGump(new GoGump(_page - 1, from, _tree, _node));
                    }

                    break;
                }
            case 3:
                {
                    if ((_page + 1) * EntryCount < _node.Categories.Length + _node.Locations.Length)
                    {
                        from.SendGump(new GoGump(_page + 1, from, _tree, _node));
                    }

                    break;
                }
            default:
                {
                    var index = info.ButtonID - 4;

                    if (index < 0)
                    {
                        break;
                    }

                    if (index < _node.Categories.Length)
                    {
                        from.SendGump(new GoGump(0, from, _tree, _node.Categories[index]));
                    }
                    else
                    {
                        index -= _node.Categories.Length;
                        if (index < _node.Locations.Length)
                        {
                            from.MoveToWorld(_node.Locations[index].Location, _tree.Map);
                        }
                    }

                    break;
                }
        }
    }
}
