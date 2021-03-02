using System;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class GoGump : Gump
    {
        private static LocationTree Felucca;
        private static LocationTree Trammel;
        private static LocationTree Ilshenar;
        private static LocationTree Malas;
        private static LocationTree Tokuno;
        private static LocationTree TerMur;

        private static readonly int PrevLabelOffsetX = PrevWidth + 1;
        private static readonly int PrevLabelOffsetY = 0;

        private static readonly int NextLabelOffsetX = -29;
        private static readonly int NextLabelOffsetY = 0;

        private static readonly int EntryWidth = 180;
        private static readonly int EntryCount = 15;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;

        private readonly GoCategory m_Node;
        private readonly int m_Page;

        private readonly LocationTree m_Tree;

        private GoGump(int page, Mobile from, LocationTree tree, GoCategory node) : base(50, 50)
        {
            from.CloseGump<GoGump>();

            if (node == tree.Root)
            {
                tree.LastBranch.Remove(from);
            }
            else
            {
                tree.LastBranch[from] = node;
            }

            m_Page = page;
            m_Tree = tree;
            m_Node = node;

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
            LocationTree tree;

            if (from.Map == Map.Ilshenar)
            {
                tree = Ilshenar ??= new LocationTree("ilshenar", Map.Ilshenar);
            }
            else if (from.Map == Map.Felucca)
            {
                tree = Felucca ??= new LocationTree("felucca", Map.Felucca);
            }
            else if (from.Map == Map.Trammel)
            {
                tree = Trammel ??= new LocationTree("trammel", Map.Trammel);
            }
            else if (from.Map == Map.Malas)
            {
                tree = Malas ??= new LocationTree("malas", Map.Malas);
            }
            else if (from.Map == Map.Tokuno)
            {
                tree = Tokuno ??= new LocationTree("tokuno", Map.Tokuno);
            }
            else
            {
                tree = TerMur ??= new LocationTree("termur", Map.TerMur);
            }

            if (!tree.LastBranch.TryGetValue(from, out var branch))
            {
                branch = tree.Root;
            }

            if (branch != null)
            {
                from.SendGump(new GoGump(0, from, tree, branch));
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var from = state.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        if (m_Node.Parent != null)
                        {
                            from.SendGump(new GoGump(0, from, m_Tree, m_Node.Parent));
                        }

                        break;
                    }
                case 2:
                    {
                        if (m_Page > 0)
                        {
                            from.SendGump(new GoGump(m_Page - 1, from, m_Tree, m_Node));
                        }

                        break;
                    }
                case 3:
                    {
                        if ((m_Page + 1) * EntryCount < m_Node.Categories.Length + m_Node.Locations.Length)
                        {
                            from.SendGump(new GoGump(m_Page + 1, from, m_Tree, m_Node));
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

                        if (index < m_Node.Categories.Length)
                        {
                            from.SendGump(new GoGump(0, from, m_Tree, m_Node.Categories[index]));
                        }
                        else
                        {
                            index -= m_Node.Categories.Length;
                            if (index < m_Node.Locations.Length)
                            {
                                from.MoveToWorld(m_Node.Locations[index].Location, m_Tree.Map);
                            }
                        }

                        break;
                    }
            }
        }
    }
}
