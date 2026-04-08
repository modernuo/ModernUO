using System;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps;

public class GoGump : Gump
{
    private const int EntryWidth = 180;
    private const int EntryCount = 15;

    private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

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

        var count = Math.Clamp(node.Categories.Length + node.Locations.Length - page * EntryCount, 0, EntryCount);

        AddPage(0);

        this.AddPropsFrame(TotalWidth, count + 1, out var x, out var y);
        this.AddPropsHeaderWithBack(
            TotalWidth, ref x, ref y, node.Name,
            node.Parent != null, 1,
            page > 0, 2,
            (page + 1) * EntryCount < node.Categories.Length + node.Locations.Length, 3,
            nextType: GumpButtonType.Reply, nextParam: 1
        );

        var totalEntryCount = node.Categories.Length + node.Locations.Length;

        for (int i = 0, index = page * EntryCount; i < EntryCount && index < totalEntryCount; ++i, ++index)
        {
            PropsLayout.NextRow(ref x, ref y);

            var name = index >= node.Categories.Length
                ? node.Locations[index - node.Categories.Length].Name
                : node.Categories[index].Name;

            this.AddPropsEntryButton(ref x, ref y, EntryWidth, name, true, index + 4);
        }
    }

    public static void DisplayTo(Mobile from)
    {
        var tree = GoLocations.GetLocations(from.Map);

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
