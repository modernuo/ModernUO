using System;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps;

public class GoGump : DynamicGump
{
    private const int EntryWidth = 180;
    private const int EntryCount = 15;

    private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

    private GoCategory _node;
    private int _page;

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
    }

    public static void DisplayTo(Mobile from)
    {
        if (from?.NetState == null)
        {
            return;
        }

        var tree = GoLocations.GetLocations(from.Map);

        if (tree == null)
        {
            return;
        }

        if (!tree.LastBranch.TryGetValue(from, out var branch))
        {
            branch = tree.Root;
        }

        if (branch == null)
        {
            return;
        }

        from.SendGump(new GoGump(0, from, tree, branch));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var count = Math.Clamp(_node.Categories.Length + _node.Locations.Length - _page * EntryCount, 0, EntryCount);

        builder.AddPage();

        builder.AddPropsFrame(TotalWidth, count + 1, out var x, out var y);
        builder.AddPropsHeaderWithBack(
            TotalWidth, ref x, ref y, _node.Name,
            _node.Parent != null, 1,
            _page > 0, 2,
            (_page + 1) * EntryCount < _node.Categories.Length + _node.Locations.Length, 3,
            nextType: GumpButtonType.Reply, nextParam: 1
        );

        var totalEntryCount = _node.Categories.Length + _node.Locations.Length;

        for (int i = 0, index = _page * EntryCount; i < EntryCount && index < totalEntryCount; ++i, ++index)
        {
            PropsLayout.NextRow(ref x, ref y);

            var name = index >= _node.Categories.Length
                ? _node.Locations[index - _node.Categories.Length].Name
                : _node.Categories[index].Name;

            builder.AddPropsEntryButton(ref x, ref y, EntryWidth, name, true, index + 4);
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
                        _page = 0;
                        _node = _node.Parent;

                        if (_node == _tree.Root)
                        {
                            _tree.LastBranch.Remove(from);
                        }
                        else
                        {
                            _tree.LastBranch[from] = _node;
                        }

                        from.SendGump(this);
                    }

                    break;
                }
            case 2:
                {
                    if (_page > 0)
                    {
                        _page--;
                        from.SendGump(this);
                    }

                    break;
                }
            case 3:
                {
                    if ((_page + 1) * EntryCount < _node.Categories.Length + _node.Locations.Length)
                    {
                        _page++;
                        from.SendGump(this);
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
                        _page = 0;
                        _node = _node.Categories[index];
                        _tree.LastBranch[from] = _node;
                        from.SendGump(this);
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
