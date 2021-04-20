using System;
using Server.Commands;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class CategorizedAddGump : Gump
    {
        private static readonly int EntryHeight = PropsConfig.EntryHeight + 4;

        private static readonly int SetOffsetY = PropsConfig.SetOffsetY + (EntryHeight - 20) / 2 / 2;

        private static readonly int PrevOffsetY = PropsConfig.PrevOffsetY + (EntryHeight - 20) / 2 / 2;

        private static readonly int NextOffsetY = PropsConfig.NextOffsetY + (EntryHeight - 20) / 2 / 2;

        private static readonly int PrevLabelOffsetX = PrevWidth + 1;
        private static readonly int PrevLabelOffsetY = 0;

        private static readonly int NextLabelOffsetX = -29;
        private static readonly int NextLabelOffsetY = 0;

        private static readonly int EntryWidth = 180;
        private static readonly int EntryCount = 15;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;

        private readonly CAGCategory m_Category;

        private readonly Mobile m_Owner;
        private int m_Page;

        public CategorizedAddGump(Mobile owner) : this(owner, CAGCategory.Root)
        {
        }

        public CategorizedAddGump(Mobile owner, CAGCategory category, int page = 0) : base(GumpOffsetX, GumpOffsetY)
        {
            owner.CloseGump<WhoGump>();

            m_Owner = owner;
            m_Category = category;

            Initialize(page);
        }

        public void Initialize(int page)
        {
            m_Page = page;

            var nodes = m_Category.Nodes;

            var count = Math.Clamp(nodes.Length - page * EntryCount, 0, EntryCount);

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

            var x = BorderSize + OffsetSize;
            var y = BorderSize + OffsetSize;

            if (OldStyle)
            {
                AddImageTiled(x, y, TotalWidth - OffsetSize * 3 - SetWidth, EntryHeight, HeaderGumpID);
            }
            else
            {
                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);
            }

            if (m_Category.Parent != null)
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

            AddHtml(
                x + TextOffsetX,
                y + (EntryHeight - 20) / 2,
                emptyWidth - TextOffsetX,
                EntryHeight,
                $"<center>{m_Category.Title}</center>"
            );

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

            if ((page + 1) * EntryCount < nodes.Length)
            {
                AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 3, GumpButtonType.Reply, 1);

                if (NextLabel)
                {
                    AddLabel(x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next");
                }
            }

            for (int i = 0, index = page * EntryCount; i < EntryCount && index < nodes.Length; ++i, ++index)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                var node = nodes[index];

                AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
                AddLabelCropped(
                    x + TextOffsetX,
                    y + (EntryHeight - 20) / 2,
                    EntryWidth - TextOffsetX,
                    EntryHeight,
                    TextHue,
                    node.Title
                );

                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 4);

                if (node is CAGObject obj)
                {
                    var itemID = obj.ItemID ?? 0;
                    if (itemID == 0)
                    {
                        Console.WriteLine("Type {0} does not have a valid item id or shrink table entry.", obj.Type);
                    }

                    var bounds = ItemBounds.Table[itemID];

                    if (itemID != 1 && bounds.Height < EntryHeight * 2)
                    {
                        if (bounds.Height < EntryHeight)
                        {
                            AddItem(
                                x - OffsetSize - 22 - i % 2 * 44 - bounds.Width / 2 - bounds.X,
                                y + EntryHeight / 2 - bounds.Height / 2 - bounds.Y,
                                itemID
                            );
                        }
                        else
                        {
                            AddItem(
                                x - OffsetSize - 22 - i % 2 * 44 - bounds.Width / 2 - bounds.X,
                                y + EntryHeight - 1 - bounds.Height - bounds.Y,
                                itemID
                            );
                        }
                    }
                }
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var from = m_Owner;

            switch (info.ButtonID)
            {
                case 0: // Closed
                    {
                        return;
                    }
                case 1: // Up
                    {
                        if (m_Category.Parent != null)
                        {
                            var index = Array.IndexOf(m_Category.Parent.Nodes, m_Category) / EntryCount;

                            if (index < 0)
                            {
                                index = 0;
                            }

                            from.SendGump(new CategorizedAddGump(from, m_Category.Parent, index));
                        }

                        break;
                    }
                case 2: // Previous
                    {
                        if (m_Page > 0)
                        {
                            from.SendGump(new CategorizedAddGump(from, m_Category, m_Page - 1));
                        }

                        break;
                    }
                case 3: // Next
                    {
                        if ((m_Page + 1) * EntryCount < m_Category.Nodes.Length)
                        {
                            from.SendGump(new CategorizedAddGump(from, m_Category, m_Page + 1));
                        }

                        break;
                    }
                default:
                    {
                        var index = m_Page * EntryCount + (info.ButtonID - 4);

                        if (index >= 0 && index < m_Category.Nodes.Length)
                        {
                            m_Category.Nodes[index].OnClick(from, m_Page);
                        }

                        break;
                    }
            }
        }
    }
}
