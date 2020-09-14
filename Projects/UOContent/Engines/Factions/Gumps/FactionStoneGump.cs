using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
    public class FactionStoneGump : FactionGump
    {
        private readonly Faction m_Faction;
        private readonly PlayerMobile m_From;

        public FactionStoneGump(PlayerMobile from, Faction faction) : base(20, 30)
        {
            m_From = from;
            m_Faction = faction;

            AddPage(0);

            AddBackground(0, 0, 550, 440, 5054);
            AddBackground(10, 10, 530, 420, 3000);

            AddPage(1);

            AddHtmlText(20, 30, 510, 20, faction.Definition.Header, false, false);

            AddHtmlLocalized(20, 60, 100, 20, 1011429); // Led By :
            AddHtml(125, 60, 200, 20, faction.Commander != null ? faction.Commander.Name : "Nobody");

            AddHtmlLocalized(20, 80, 100, 20, 1011457); // Tithe rate :
            if (faction.Tithe >= 0 && faction.Tithe <= 100 && faction.Tithe % 10 == 0)
            {
                AddHtmlLocalized(125, 80, 350, 20, 1011480 + faction.Tithe / 10);
            }
            else
            {
                AddHtml(125, 80, 350, 20, $"{faction.Tithe}%");
            }

            AddHtmlLocalized(20, 100, 100, 20, 1011458); // Traps placed :
            AddHtml(125, 100, 50, 20, faction.Traps.Count.ToString());

            AddHtmlLocalized(55, 225, 200, 20, 1011428); // VOTE FOR LEADERSHIP
            AddButton(20, 225, 4005, 4007, ToButtonID(0, 0));

            AddHtmlLocalized(55, 150, 100, 20, 1011430); // CITY STATUS
            AddButton(20, 150, 4005, 4007, 0, GumpButtonType.Page, 2);

            AddHtmlLocalized(55, 175, 100, 20, 1011444); // STATISTICS
            AddButton(20, 175, 4005, 4007, 0, GumpButtonType.Page, 4);

            var isMerchantQualified = MerchantTitles.HasMerchantQualifications(from);

            var pl = PlayerState.Find(from);

            if (pl != null && pl.MerchantTitle != MerchantTitle.None)
            {
                AddHtmlLocalized(55, 200, 250, 20, 1011460); // UNDECLARE FACTION MERCHANT
                AddButton(20, 200, 4005, 4007, ToButtonID(1, 0));
            }
            else if (isMerchantQualified)
            {
                AddHtmlLocalized(55, 200, 250, 20, 1011459); // DECLARE FACTION MERCHANT
                AddButton(20, 200, 4005, 4007, 0, GumpButtonType.Page, 5);
            }
            else
            {
                AddHtmlLocalized(55, 200, 250, 20, 1011467); // MERCHANT OPTIONS
                AddImage(20, 200, 4020);
            }

            AddHtmlLocalized(55, 250, 300, 20, 1011461); // COMMANDER OPTIONS
            if (faction.IsCommander(from))
            {
                AddButton(20, 250, 4005, 4007, 0, GumpButtonType.Page, 6);
            }
            else
            {
                AddImage(20, 250, 4020);
            }

            AddHtmlLocalized(55, 275, 300, 20, 1011426); // LEAVE THIS FACTION
            AddButton(20, 275, 4005, 4007, ToButtonID(0, 1));

            AddHtmlLocalized(55, 300, 200, 20, 1011441); // EXIT
            AddButton(20, 300, 4005, 4007, 0);

            AddPage(2);

            AddHtmlLocalized(20, 30, 250, 20, 1011430); // CITY STATUS

            var towns = Town.Towns;

            for (var i = 0; i < towns.Count; ++i)
            {
                var town = towns[i];

                AddHtmlText(40, 55 + i * 30, 150, 20, town.Definition.TownName, false, false);

                if (town.Owner == null)
                {
                    AddHtmlLocalized(200, 55 + i * 30, 150, 20, 1011462); // : Neutral
                }
                else
                {
                    AddHtmlLocalized(200, 55 + i * 30, 150, 20, town.Owner.Definition.OwnerLabel);

                    BaseMonolith monolith = town.Monolith;

                    AddImage(20, 60 + i * 30, monolith?.Sigil?.IsPurifying == true ? 0x938 : 0x939);
                }
            }

            AddImage(20, 300, 2361);
            AddHtmlLocalized(45, 295, 300, 20, 1011491); // sigil may be recaptured

            AddImage(20, 320, 2360);
            AddHtmlLocalized(45, 315, 300, 20, 1011492); // sigil may not be recaptured

            AddHtmlLocalized(55, 350, 100, 20, 1011447); // BACK
            AddButton(20, 350, 4005, 4007, 0, GumpButtonType.Page, 1);

            AddPage(4);

            AddHtmlLocalized(20, 30, 150, 20, 1011444); // STATISTICS

            AddHtmlLocalized(20, 100, 100, 20, 1011445); // Name :
            AddHtml(120, 100, 150, 20, from.Name);

            AddHtmlLocalized(20, 130, 100, 20, 1018064); // score :
            AddHtml(120, 130, 100, 20, (pl?.KillPoints ?? 0).ToString());

            AddHtmlLocalized(20, 160, 100, 20, 1011446); // Rank :
            AddHtml(120, 160, 100, 20, (pl?.Rank.Rank ?? 0).ToString());

            AddHtmlLocalized(55, 250, 100, 20, 1011447); // BACK
            AddButton(20, 250, 4005, 4007, 0, GumpButtonType.Page, 1);

            if ((pl == null || pl.MerchantTitle == MerchantTitle.None) && isMerchantQualified)
            {
                AddPage(5);

                AddHtmlLocalized(20, 30, 250, 20, 1011467); // MERCHANT OPTIONS

                AddHtmlLocalized(20, 80, 300, 20, 1011473); // Select the title you wish to display

                var infos = MerchantTitles.Info;

                for (var i = 0; i < infos.Length; ++i)
                {
                    var info = infos[i];

                    if (MerchantTitles.IsQualified(from, info))
                    {
                        AddButton(20, 100 + i * 30, 4005, 4007, ToButtonID(1, i + 1));
                    }
                    else
                    {
                        AddImage(20, 100 + i * 30, 4020);
                    }

                    AddHtmlText(55, 100 + i * 30, 200, 20, info.Label, false, false);
                }

                AddHtmlLocalized(55, 340, 100, 20, 1011447); // BACK
                AddButton(20, 340, 4005, 4007, 0, GumpButtonType.Page, 1);
            }

            if (faction.IsCommander(from))
            {
                AddPage(6);

                AddHtmlLocalized(20, 30, 200, 20, 1011461); // COMMANDER OPTIONS

                AddHtmlLocalized(20, 70, 120, 20, 1011457); // Tithe rate :
                if (faction.Tithe >= 0 && faction.Tithe <= 100 && faction.Tithe % 10 == 0)
                {
                    AddHtmlLocalized(140, 70, 250, 20, 1011480 + faction.Tithe / 10);
                }
                else
                {
                    AddHtml(140, 70, 250, 20, $"{faction.Tithe}%");
                }

                AddHtmlLocalized(20, 100, 120, 20, 1011474);              // Silver available :
                AddHtml(140, 100, 50, 20, faction.Silver.ToString("N0")); // NOTE: Added 'N0' formatting

                AddHtmlLocalized(55, 130, 200, 20, 1011478); // CHANGE TITHE RATE
                AddButton(20, 130, 4005, 4007, 0, GumpButtonType.Page, 8);

                AddHtmlLocalized(55, 160, 200, 20, 1018301); // TRANSFER SILVER
                if (faction.Silver >= 10000)
                {
                    AddButton(20, 160, 4005, 4007, 0, GumpButtonType.Page, 7);
                }
                else
                {
                    AddImage(20, 160, 4020);
                }

                AddHtmlLocalized(55, 310, 100, 20, 1011447); // BACK
                AddButton(20, 310, 4005, 4007, 0, GumpButtonType.Page, 1);

                if (faction.Silver >= 10000)
                {
                    AddPage(7);

                    AddHtmlLocalized(20, 30, 250, 20, 1011476); // TOWN FINANCE

                    AddHtmlLocalized(20, 50, 400, 20, 1011477); // Select a town to transfer 10000 silver to

                    for (var i = 0; i < towns.Count; ++i)
                    {
                        var town = towns[i];

                        AddHtmlText(55, 75 + i * 30, 200, 20, town.Definition.TownName, false, false);

                        if (town.Owner == faction)
                        {
                            AddButton(20, 75 + i * 30, 4005, 4007, ToButtonID(2, i));
                        }
                        else
                        {
                            AddImage(20, 75 + i * 30, 4020);
                        }
                    }

                    AddHtmlLocalized(55, 310, 100, 20, 1011447); // BACK
                    AddButton(20, 310, 4005, 4007, 0, GumpButtonType.Page, 1);
                }

                AddPage(8);

                AddHtmlLocalized(20, 30, 400, 20, 1011479); // Select the % for the new tithe rate

                var y = 55;

                for (var i = 0; i <= 10; ++i)
                {
                    if (i == 5)
                    {
                        y += 5;
                    }

                    AddHtmlLocalized(55, y, 300, 20, 1011480 + i);
                    AddButton(20, y, 4005, 4007, ToButtonID(3, i));

                    y += 20;

                    if (i == 5)
                    {
                        y += 5;
                    }
                }

                AddHtmlLocalized(55, 310, 300, 20, 1011447); // BACK
                AddButton(20, 310, 4005, 4007, 0, GumpButtonType.Page, 1);
            }
        }

        public override int ButtonTypes => 4;

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!FromButtonID(info.ButtonID, out var type, out var index))
            {
                return;
            }

            switch (type)
            {
                case 0: // general
                    {
                        switch (index)
                        {
                            case 0: // vote
                                {
                                    m_From.SendGump(new ElectionGump(m_From, m_Faction.Election));
                                    break;
                                }
                            case 1: // leave
                                {
                                    m_From.SendGump(new LeaveFactionGump(m_From, m_Faction));
                                    break;
                                }
                        }

                        break;
                    }
                case 1: // merchant title
                    {
                        if (index >= 0 && index <= MerchantTitles.Info.Length)
                        {
                            var pl = PlayerState.Find(m_From);

                            var newTitle = (MerchantTitle)index;
                            var mti = MerchantTitles.GetInfo(newTitle);

                            if (mti == null)
                            {
                                m_From.SendLocalizedMessage(1010120); // Your merchant title has been removed

                                if (pl != null)
                                {
                                    pl.MerchantTitle = newTitle;
                                }
                            }
                            else if (MerchantTitles.IsQualified(m_From, mti))
                            {
                                m_From.SendLocalizedMessage(mti.Assigned);

                                if (pl != null)
                                {
                                    pl.MerchantTitle = newTitle;
                                }
                            }
                        }

                        break;
                    }
                case 2: // transfer silver
                    {
                        if (!m_Faction.IsCommander(m_From))
                        {
                            return;
                        }

                        var towns = Town.Towns;

                        if (index >= 0 && index < towns.Count)
                        {
                            var town = towns[index];

                            if (town.Owner == m_Faction)
                            {
                                if (m_Faction.Silver >= 10000)
                                {
                                    m_Faction.Silver -= 10000;
                                    town.Silver += 10000;

                                    // 10k in silver has been received by:
                                    m_From.SendLocalizedMessage(1042726, true, $" {town.Definition.FriendlyName}");
                                }
                            }
                        }

                        break;
                    }
                case 3: // change tithe
                    {
                        if (!m_Faction.IsCommander(m_From))
                        {
                            return;
                        }

                        if (index >= 0 && index <= 10)
                        {
                            m_Faction.Tithe = index * 10;
                        }

                        break;
                    }
            }
        }
    }
}
