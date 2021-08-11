using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Factions
{
    public class FinanceGump : FactionGump
    {
        private static readonly int[] m_PriceOffsets =
        {
            -30, -25, -20, -15, -10, -5,
            +50, +100, +150, +200, +250, +300
        };

        private readonly Faction m_Faction;
        private readonly PlayerMobile m_From;
        private readonly Town m_Town;

        public FinanceGump(PlayerMobile from, Faction faction, Town town) : base(50, 50)
        {
            m_From = from;
            m_Faction = faction;
            m_Town = town;

            AddPage(0);

            AddBackground(0, 0, 320, 410, 5054);
            AddBackground(10, 10, 300, 390, 3000);

            AddPage(1);

            AddHtmlLocalized(20, 30, 260, 25, 1011541); // FINANCE MINISTER

            AddHtmlLocalized(55, 90, 200, 25, 1011539); // CHANGE PRICES
            AddButton(20, 90, 4005, 4007, 0, GumpButtonType.Page, 2);

            AddHtmlLocalized(55, 120, 200, 25, 1011540); // BUY SHOPKEEPERS
            AddButton(20, 120, 4005, 4007, 0, GumpButtonType.Page, 3);

            AddHtmlLocalized(55, 150, 200, 25, 1011495); // VIEW FINANCES
            AddButton(20, 150, 4005, 4007, 0, GumpButtonType.Page, 4);

            AddHtmlLocalized(55, 360, 200, 25, 1011441); // EXIT
            AddButton(20, 360, 4005, 4007, 0);

            AddPage(2);

            AddHtmlLocalized(20, 30, 200, 25, 1011539); // CHANGE PRICES

            for (var i = 0; i < m_PriceOffsets.Length; ++i)
            {
                var ofs = m_PriceOffsets[i];

                var x = 20 + i / 6 * 150;
                var y = 90 + i % 6 * 30;

                AddRadio(x, y, 208, 209, town.Tax == ofs, i + 1);

                if (ofs < 0)
                {
                    AddLabel(x + 35, y, 0x26, $"- {-ofs}%");
                }
                else
                {
                    AddLabel(x + 35, y, 0x12A, $"+ {ofs}%");
                }
            }

            AddRadio(20, 270, 208, 209, town.Tax == 0, 0);
            AddHtmlLocalized(55, 270, 90, 25, 1011542); // normal

            AddHtmlLocalized(55, 330, 200, 25, 1011509); // Set Prices
            AddButton(20, 330, 4005, 4007, ToButtonID(0, 0));

            AddHtmlLocalized(55, 360, 200, 25, 1011067); // Previous page
            AddButton(20, 360, 4005, 4007, 0, GumpButtonType.Page, 1);

            AddPage(3);

            AddHtmlLocalized(20, 30, 200, 25, 1011540); // BUY SHOPKEEPERS

            var vendorLists = town.VendorLists;

            for (var i = 0; i < vendorLists.Count; ++i)
            {
                var list = vendorLists[i];

                AddButton(20, 90 + i * 40, 4005, 4007, 0, GumpButtonType.Page, 5 + i);
                AddItem(55, 90 + i * 40, list.Definition.ItemID);
                AddHtmlText(100, 90 + i * 40, 200, 25, list.Definition.Label, false, false);
            }

            AddHtmlLocalized(55, 360, 200, 25, 1011067); // Previous page
            AddButton(20, 360, 4005, 4007, 0, GumpButtonType.Page, 1);

            AddPage(4);

            var financeUpkeep = town.FinanceUpkeep;
            var sheriffUpkeep = town.SheriffUpkeep;
            var dailyIncome = town.DailyIncome;
            var netCashFlow = town.NetCashFlow;

            AddHtmlLocalized(20, 30, 300, 25, 1011524); // FINANCE STATEMENT

            AddHtmlLocalized(20, 80, 300, 25, 1011538); // Current total money for town :
            AddLabel(20, 100, 0x44, town.Silver.ToString());

            AddHtmlLocalized(20, 130, 300, 25, 1011520);           // Finance Minister Upkeep :
            AddLabel(20, 150, 0x44, financeUpkeep.ToString("N0")); // NOTE: Added 'N0'

            AddHtmlLocalized(20, 180, 300, 25, 1011521);           // Sheriff Upkeep :
            AddLabel(20, 200, 0x44, sheriffUpkeep.ToString("N0")); // NOTE: Added 'N0'

            AddHtmlLocalized(20, 230, 300, 25, 1011522);         // Town Income :
            AddLabel(20, 250, 0x44, dailyIncome.ToString("N0")); // NOTE: Added 'N0'

            AddHtmlLocalized(20, 280, 300, 25, 1011523);         // Net Cash flow per day :
            AddLabel(20, 300, 0x44, netCashFlow.ToString("N0")); // NOTE: Added 'N0'

            AddHtmlLocalized(55, 360, 200, 25, 1011067); // Previous page
            AddButton(20, 360, 4005, 4007, 0, GumpButtonType.Page, 1);

            for (var i = 0; i < vendorLists.Count; ++i)
            {
                var vendorList = vendorLists[i];

                AddPage(5 + i);

                AddHtmlText(60, 30, 300, 25, vendorList.Definition.Header, false, false);
                AddItem(20, 30, vendorList.Definition.ItemID);

                AddHtmlLocalized(20, 90, 200, 25, 1011514); // You have :
                AddLabel(230, 90, 0x26, vendorList.Vendors.Count.ToString());

                AddHtmlLocalized(20, 120, 200, 25, 1011515); // Maximum :
                AddLabel(230, 120, 0x256, vendorList.Definition.Maximum.ToString());

                AddHtmlLocalized(20, 150, 200, 25, 1011516);                          // Cost :
                AddLabel(230, 150, 0x44, vendorList.Definition.Price.ToString("N0")); // NOTE: Added 'N0'

                AddHtmlLocalized(20, 180, 200, 25, 1011517);                           // Daily Pay :
                AddLabel(230, 180, 0x37, vendorList.Definition.Upkeep.ToString("N0")); // NOTE: Added 'N0'

                AddHtmlLocalized(20, 210, 200, 25, 1011518);          // Current Silver :
                AddLabel(230, 210, 0x44, town.Silver.ToString("N0")); // NOTE: Added 'N0'

                AddHtmlLocalized(20, 240, 200, 25, 1011519);            // Current Payroll :
                AddLabel(230, 240, 0x44, financeUpkeep.ToString("N0")); // NOTE: Added 'N0'

                AddHtmlText(55, 300, 200, 25, vendorList.Definition.Label, false, false);
                if (town.Silver >= vendorList.Definition.Price)
                {
                    AddButton(20, 300, 4005, 4007, ToButtonID(1, i));
                }
                else
                {
                    AddImage(20, 300, 4020);
                }

                AddHtmlLocalized(55, 360, 200, 25, 1011067); // Previous page
                AddButton(20, 360, 4005, 4007, 0, GumpButtonType.Page, 3);
            }
        }

        public override int ButtonTypes => 2;

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!m_Town.IsFinance(m_From) || m_Town.Owner != m_Faction)
            {
                m_From.SendLocalizedMessage(1010339); // You no longer control this city
                return;
            }

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
                            case 0: // set price
                                {
                                    var switches = info.Switches;

                                    if (switches.Length == 0)
                                    {
                                        break;
                                    }

                                    var opt = switches[0];
                                    var newTax = 0;

                                    if (opt >= 1 && opt <= m_PriceOffsets.Length)
                                    {
                                        newTax = m_PriceOffsets[opt - 1];
                                    }

                                    if (m_Town.Tax == newTax)
                                    {
                                        break;
                                    }

                                    if (m_From.AccessLevel == AccessLevel.Player && !m_Town.TaxChangeReady)
                                    {
                                        var remaining = Core.Now - (m_Town.LastTaxChange + Town.TaxChangePeriod);

                                        if (remaining.TotalMinutes < 4)
                                        {
                                            // You must wait a short while before changing prices again.
                                            m_From.SendLocalizedMessage(1042165);
                                        }
                                        else if (remaining.TotalMinutes < 10)
                                        {
                                            // You must wait several minutes before changing prices again.
                                            m_From.SendLocalizedMessage(1042166);
                                        }
                                        else if (remaining.TotalHours < 1)
                                        {
                                            // You must wait up to an hour before changing prices again.
                                            m_From.SendLocalizedMessage(1042167);
                                        }
                                        else if (remaining.TotalHours < 4)
                                        {
                                            // You must wait a few hours before changing prices again.
                                            m_From.SendLocalizedMessage(1042168);
                                        }
                                        else
                                        {
                                            // You must wait several hours before changing prices again.
                                            m_From.SendLocalizedMessage(1042169);
                                        }
                                    }
                                    else
                                    {
                                        m_Town.Tax = newTax;

                                        if (m_From.AccessLevel == AccessLevel.Player)
                                        {
                                            m_Town.LastTaxChange = Core.Now;
                                        }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 1: // make vendor
                    {
                        var vendorLists = m_Town.VendorLists;

                        if (index >= 0 && index < vendorLists.Count)
                        {
                            var vendorList = vendorLists[index];

                            if (Town.FromRegion(m_From.Region) != m_Town)
                            {
                                // You must be in your controlled city to buy Items
                                m_From.SendLocalizedMessage(1010305);
                            }
                            else if (vendorList.Vendors.Count >= vendorList.Definition.Maximum)
                            {
                                // You currently have too many of this enhancement type to place another
                                m_From.SendLocalizedMessage(1010306);
                            }
                            else if (BaseBoat.FindBoatAt(m_From.Location, m_From.Map) != null)
                            {
                                m_From.SendMessage("You cannot place a vendor here");
                            }
                            else if (m_Town.Silver >= vendorList.Definition.Price)
                            {
                                var vendor = vendorList.Construct(m_Town, m_Faction);

                                if (vendor != null)
                                {
                                    m_Town.Silver -= vendorList.Definition.Price;

                                    vendor.MoveToWorld(m_From.Location, m_From.Map);
                                    vendor.Home = vendor.Location;
                                }
                            }
                        }

                        break;
                    }
            }
        }
    }
}
