using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class WhoGump : Gump
    {
        private static readonly int PrevLabelOffsetX = PrevWidth + 1;
        private static readonly int PrevLabelOffsetY = 0;

        private static readonly int NextLabelOffsetX = -29;
        private static readonly int NextLabelOffsetY = 0;

        private static readonly int EntryWidth = 180;
        private static readonly int EntryCount = 15;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;

        private readonly List<Mobile> m_Mobiles;

        private Mobile m_Owner;
        private int m_Page;

        public WhoGump(Mobile owner, string filter) : this(owner, BuildList(owner, filter))
        {
        }

        public WhoGump(Mobile owner, List<Mobile> list, int page = 0) : base(GumpOffsetX, GumpOffsetY)
        {
            owner.CloseGump<WhoGump>();

            m_Owner = owner;
            m_Mobiles = list;

            Initialize(page);
        }

        public static void Initialize()
        {
            CommandSystem.Register("Who", AccessLevel.Counselor, WhoList_OnCommand);
            CommandSystem.Register("WhoList", AccessLevel.Counselor, WhoList_OnCommand);
        }

        [Usage("WhoList [filter]"), Aliases("Who"),
         Description("Lists all connected clients. Optionally filters results by name.")]
        private static void WhoList_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new WhoGump(e.Mobile, e.ArgString));
        }

        public static List<Mobile> BuildList(Mobile owner, string rawFilter)
        {
            var filter = rawFilter.Trim().ToLower().DefaultIfNullOrEmpty(null);

            var list = new List<Mobile>();

            foreach (var ns in TcpServer.Instances)
            {
                var m = ns.Mobile;

                if (m != null && (m == owner || !m.Hidden || owner.AccessLevel >= m.AccessLevel ||
                                  m is PlayerMobile mobile && mobile.VisibilityList.Contains(owner)))
                {
                    if (filter != null && !m.Name.InsensitiveContains(filter))
                    {
                        continue;
                    }

                    list.Add(m);
                }
            }

            list.Sort(InternalComparer.Instance);

            return list;
        }

        public void Initialize(int page)
        {
            m_Page = page;

            var count = Math.Clamp(m_Mobiles.Count - page * EntryCount, 0, EntryCount);

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

            var emptyWidth = TotalWidth - PrevWidth - NextWidth - OffsetSize * 4 - (OldStyle ? SetWidth + OffsetSize : 0);

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

            AddLabel(
                x + TextOffsetX,
                y,
                TextHue,
                $"Page {page + 1} of {(m_Mobiles.Count + EntryCount - 1) / EntryCount} ({m_Mobiles.Count})"
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
                AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1);

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

            if ((page + 1) * EntryCount < m_Mobiles.Count)
            {
                AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 2, GumpButtonType.Reply, 1);

                if (NextLabel)
                {
                    AddLabel(x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next");
                }
            }

            for (int i = 0, index = page * EntryCount; i < EntryCount && index < m_Mobiles.Count; ++i, ++index)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                var m = m_Mobiles[index];

                AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
                AddLabelCropped(
                    x + TextOffsetX,
                    y,
                    EntryWidth - TextOffsetX,
                    EntryHeight,
                    GetHueFor(m),
                    m.Deleted ? "(deleted)" : m.Name
                );

                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                if (m.NetState != null && !m.Deleted)
                {
                    AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3);
                }
            }
        }

        private static int GetHueFor(Mobile m)
        {
            switch (m.AccessLevel)
            {
                case AccessLevel.Owner:
                case AccessLevel.Developer:
                case AccessLevel.Administrator: return 0x516;
                case AccessLevel.Seer:       return 0x144;
                case AccessLevel.GameMaster: return 0x21;
                case AccessLevel.Counselor:  return 0x2;
                default:
                    {
                        return m.Kills >= 5 ? 0x21 :
                            m.Criminal ? 0x3B1 : 0x58;
                    }
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
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
                        if (m_Page > 0)
                        {
                            from.SendGump(new WhoGump(from, m_Mobiles, m_Page - 1));
                        }

                        break;
                    }
                case 2: // Next
                    {
                        if ((m_Page + 1) * EntryCount < m_Mobiles.Count)
                        {
                            from.SendGump(new WhoGump(from, m_Mobiles, m_Page + 1));
                        }

                        break;
                    }
                default:
                    {
                        var index = m_Page * EntryCount + (info.ButtonID - 3);

                        if (index >= 0 && index < m_Mobiles.Count)
                        {
                            var m = m_Mobiles[index];

                            if (m.Deleted)
                            {
                                from.SendMessage("That player has deleted their character.");
                                from.SendGump(new WhoGump(from, m_Mobiles, m_Page));
                            }
                            else if (m.NetState == null)
                            {
                                from.SendMessage("That player is no longer online.");
                                from.SendGump(new WhoGump(from, m_Mobiles, m_Page));
                            }
                            else if (m == from || !m.Hidden || from.AccessLevel >= m.AccessLevel ||
                                     m is PlayerMobile mobile && mobile.VisibilityList.Contains(from))
                            {
                                from.SendGump(new ClientGump(from, m.NetState));
                            }
                            else
                            {
                                from.SendMessage("You cannot see them.");
                                from.SendGump(new WhoGump(from, m_Mobiles, m_Page));
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
                if (x == null)
                {
                    throw new ArgumentNullException(nameof(x));
                }

                if (y == null)
                {
                    throw new ArgumentNullException(nameof(y));
                }

                if (x.AccessLevel > y.AccessLevel)
                {
                    return -1;
                }

                return x.AccessLevel < y.AccessLevel ? 1 : x.Name.InsensitiveCompare(y.Name);
            }
        }
    }
}
