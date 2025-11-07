using System.Collections.Generic;
using Server.Accounting;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Gumps
{
    public class ViewHousesGump : Gump
    {
        private const int White16 = 0x7FFF;
        private const int White = 0xFFFFFF;

        private readonly Mobile m_From;
        private readonly List<BaseHouse> m_List;
        private readonly BaseHouse m_Selection;

        public override bool Singleton => true;

        public ViewHousesGump(Mobile from, List<BaseHouse> list, BaseHouse sel) : base(50, 40)
        {
            m_From = from;
            m_List = list;
            m_Selection = sel;

            AddPage(0);

            AddBackground(0, 0, 240, 360, 5054);
            AddBlackAlpha(10, 10, 220, 340);

            if (sel?.Deleted != false)
            {
                m_Selection = null;

                AddHtml(35, 15, 120, 20, "House Type".Color(White));

                if (list.Count == 0)
                {
                    AddHtml(35, 40, 160, 40, "There were no houses found for that player.".Color(White));
                }

                AddImage(190, 17, 0x25EA);
                AddImage(207, 17, 0x25E6);

                var page = 0;

                for (var i = 0; i < list.Count; ++i)
                {
                    if (i % 15 == 0)
                    {
                        if (page > 0)
                        {
                            AddButton(207, 17, 0x15E1, 0x15E5, 0, GumpButtonType.Page, page + 1);
                        }

                        AddPage(++page);

                        if (page > 1)
                        {
                            AddButton(190, 17, 0x15E3, 0x15E7, 0, GumpButtonType.Page, page - 1);
                        }
                    }

                    var name = FindHouseName(list[i]);

                    AddHtml(15, 40 + i % 15 * 20, 20, 20, Html.Color($"{i + 1}.", White));

                    if (name.Number > 0)
                    {
                        AddHtmlLocalized(35, 40 + i % 15 * 20, 160, 20, name, White16);
                    }
                    else
                    {
                        AddHtml(35, 40 + i % 15 * 20, 160, 20, Html.Color(name, White));
                    }

                    AddButton(198, 39 + i % 15 * 20, 4005, 4007, i + 1);
                }
            }
            else
            {
                string location;
                var map = sel.Map;

                var houseName = sel.Sign == null ? "An Unnamed House" : sel.Sign.GetName();
                var owner = sel.Owner == null ? "nobody" : sel.Owner.Name;

                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;

                var valid = Sextant.Format(
                    sel.Location,
                    map,
                    ref xLong,
                    ref yLat,
                    ref xMins,
                    ref yMins,
                    ref xEast,
                    ref ySouth
                );

                if (valid)
                {
                    location = $"{yLat}° {yMins}'{(ySouth ? "S" : "N")}, {xLong}° {xMins}'{(xEast ? "E" : "W")}";
                }
                else
                {
                    location = "unknown";
                }

                AddHtml(10, 15, 220, 20, "House Properties".Center(White));

                AddHtml(15, 40, 210, 20, "Facet:".Color(White));
                AddHtml(15, 40, 210, 20, (map == null ? "(null)" : map.Name).Right(White));

                AddHtml(15, 60, 210, 20, "Location:".Color(White));
                AddHtml(15, 60, 210, 20, sel.Location.ToString().Right(White));

                AddHtml(15, 80, 210, 20, "Sextant:".Color(White));
                AddHtml(15, 80, 210, 20, location.Right(White));

                AddHtml(15, 100, 210, 20, "Owner:".Color(White));
                AddHtml(15, 100, 210, 20, owner.Right(White));

                AddHtml(15, 120, 210, 20, "Name:".Color(White));
                AddHtml(15, 120, 210, 20, houseName.Right(White));

                AddHtml(15, 140, 210, 20, "Friends:".Color(White));
                AddHtml(15, 140, 210, 20, sel.Friends.Count.ToString().Right(White));

                AddHtml(15, 160, 210, 20, "Co-Owners:".Color(White));
                AddHtml(15, 160, 210, 20, sel.CoOwners.Count.ToString().Right(White));

                AddHtml(15, 180, 210, 20, "Bans:".Color(White));
                AddHtml(15, 180, 210, 20, sel.Bans.Count.ToString().Right(White));

                AddHtml(15, 200, 210, 20, "Decays:".Color(White));
                AddHtml(15, 200, 210, 20, (sel.CanDecay ? "Yes" : "No").Right(White));

                AddHtml(15, 220, 210, 20, "Decay Level:".Color(White));
                AddHtml(15, 220, 210, 20, sel.DecayLevel.ToString().Right(White));

                AddButton(15, 245, 4005, 4007, 1);
                AddHtml(50, 245, 120, 20, "Go to house".Color(White));

                AddButton(15, 265, 4005, 4007, 2);
                AddHtml(50, 265, 120, 20, "Open house menu".Color(White));

                AddButton(15, 285, 4005, 4007, 3);
                AddHtml(50, 285, 120, 20, "Demolish house".Color(White));

                AddButton(15, 305, 4005, 4007, 4);
                AddHtml(50, 305, 120, 20, "Refresh house".Color(White));
            }
        }

        public static void Configure()
        {
            CommandSystem.Register("ViewHouses", AccessLevel.GameMaster, ViewHouses_OnCommand);
        }

        [Usage("ViewHouses"), Description(
             "Displays a menu listing all houses of a targeted player. The menu also contains specific house details, and options to: go to house, open house menu, and demolish house."
         )]
        public static void ViewHouses_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, ViewHouses_OnTarget);
        }

        public static void ViewHouses_OnTarget(Mobile from, object targeted)
        {
            if (targeted is Mobile mobile)
            {
                from.SendGump(new ViewHousesGump(from, GetHouses(mobile), null));
            }
        }

        public static List<BaseHouse> GetHouses(Mobile owner)
        {
            var list = new List<BaseHouse>();

            if (owner.Account is not Account acct)
            {
                list.AddRange(BaseHouse.GetHouses(owner));
            }
            else
            {
                for (var i = 0; i < acct.Length; ++i)
                {
                    var mob = acct[i];

                    if (mob != null)
                    {
                        list.AddRange(BaseHouse.GetHouses(mob));
                    }
                }
            }

            list.Sort(HouseComparer.Instance);

            return list;
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (m_Selection == null)
            {
                var v = info.ButtonID - 1;

                if (v >= 0 && v < m_List.Count)
                {
                    m_From.SendGump(new ViewHousesGump(m_From, m_List, m_List[v]));
                }
            }
            else if (!m_Selection.Deleted)
            {
                switch (info.ButtonID)
                {
                    case 0:
                        {
                            m_From.SendGump(new ViewHousesGump(m_From, m_List, null));
                            break;
                        }
                    case 1:
                        {
                            var map = m_Selection.Map;

                            if (map != null && map != Map.Internal)
                            {
                                m_From.MoveToWorld(m_Selection.BanLocation, map);
                            }

                            m_From.SendGump(new ViewHousesGump(m_From, m_List, m_Selection));

                            break;
                        }
                    case 2:
                        {
                            m_From.SendGump(new ViewHousesGump(m_From, m_List, m_Selection));

                            var sign = m_Selection.Sign;

                            if (sign?.Deleted == false)
                            {
                                sign.OnDoubleClick(m_From);
                            }

                            break;
                        }
                    case 3:
                        {
                            m_From.SendGump(new ViewHousesGump(m_From, m_List, m_Selection));
                            m_From.SendGump(new ConfirmDemolishHouseGump(m_Selection));

                            break;
                        }
                    case 4:
                        {
                            m_Selection.RefreshDecay();
                            m_From.SendGump(new ViewHousesGump(m_From, m_List, m_Selection));

                            break;
                        }
                }
            }
        }

        public static TextDefinition FindHouseName(BaseHouse house)
        {
            var multiID = house.ItemID;
            var entries = HousePlacementEntry.ClassicHouses;

            for (var i = 0; i < entries.Length; ++i)
            {
                if (entries[i].MultiID == multiID)
                {
                    return entries[i].Description;
                }
            }

            entries = HousePlacementEntry.TwoStoryFoundations;

            for (var i = 0; i < entries.Length; ++i)
            {
                if (entries[i].MultiID == multiID)
                {
                    return entries[i].Description;
                }
            }

            entries = HousePlacementEntry.ThreeStoryFoundations;

            for (var i = 0; i < entries.Length; ++i)
            {
                if (entries[i].MultiID == multiID)
                {
                    return entries[i].Description;
                }
            }

            return house.GetType().Name;
        }

        public void AddBlackAlpha(int x, int y, int width, int height)
        {
            AddImageTiled(x, y, width, height, 2624);
            AddAlphaRegion(x, y, width, height);
        }

        private class HouseComparer : IComparer<BaseHouse>
        {
            public static readonly IComparer<BaseHouse> Instance = new HouseComparer();

            public int Compare(BaseHouse x, BaseHouse y) => x?.BuiltOn.CompareTo(y?.BuiltOn) ?? 0;
        }
    }
}
