using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Factions
{
    public class SheriffGump : FactionGump
    {
        private readonly Faction m_Faction;
        private readonly PlayerMobile m_From;
        private readonly Town m_Town;

        public SheriffGump(PlayerMobile from, Faction faction, Town town) : base(50, 50)
        {
            m_From = from;
            m_Faction = faction;
            m_Town = town;

            AddPage(0);

            AddBackground(0, 0, 320, 410, 5054);
            AddBackground(10, 10, 300, 390, 3000);

            AddPage(1);

            AddHtmlLocalized(20, 30, 260, 25, 1011431); // Sheriff

            AddHtmlLocalized(55, 90, 200, 25, 1011494); // HIRE GUARDS
            AddButton(20, 90, 4005, 4007, 0, GumpButtonType.Page, 3);

            AddHtmlLocalized(55, 120, 200, 25, 1011495); // VIEW FINANCES
            AddButton(20, 120, 4005, 4007, 0, GumpButtonType.Page, 2);

            AddHtmlLocalized(55, 360, 200, 25, 1011441); // Exit
            AddButton(20, 360, 4005, 4007, 0);

            AddPage(2);

            var financeUpkeep = town.FinanceUpkeep;
            var sheriffUpkeep = town.SheriffUpkeep;
            var dailyIncome = town.DailyIncome;
            var netCashFlow = town.NetCashFlow;

            AddHtmlLocalized(20, 30, 300, 25, 1011524); // FINANCE STATEMENT

            AddHtmlLocalized(20, 80, 300, 25, 1011538);          // Current total money for town :
            AddLabel(20, 100, 0x44, town.Silver.ToString("N0")); // NOTE: Added 'N0'

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

            AddPage(3);

            AddHtmlLocalized(20, 30, 300, 25, 1011494); // HIRE GUARDS

            var guardLists = town.GuardLists;

            for (var i = 0; i < guardLists.Count; ++i)
            {
                var guardList = guardLists[i];
                var y = 90 + i * 60;

                AddButton(20, y, 4005, 4007, 0, GumpButtonType.Page, 4 + i);
                CenterItem(guardList.Definition.ItemID, 50, y - 20, 70, 60);
                AddHtmlText(120, y, 200, 25, guardList.Definition.Header, false, false);
            }

            AddHtmlLocalized(55, 360, 200, 25, 1011067); // Previous page
            AddButton(20, 360, 4005, 4007, 0, GumpButtonType.Page, 1);

            for (var i = 0; i < guardLists.Count; ++i)
            {
                var guardList = guardLists[i];

                AddPage(4 + i);

                AddHtmlText(90, 30, 300, 25, guardList.Definition.Header, false, false);
                CenterItem(guardList.Definition.ItemID, 10, 10, 80, 80);

                AddHtmlLocalized(20, 90, 200, 25, 1011514); // You have :
                AddLabel(230, 90, 0x26, guardList.Guards.Count.ToString());

                AddHtmlLocalized(20, 120, 200, 25, 1011515); // Maximum :
                AddLabel(230, 120, 0x12A, guardList.Definition.Maximum.ToString());

                AddHtmlLocalized(20, 150, 200, 25, 1011516);                         // Cost :
                AddLabel(230, 150, 0x44, guardList.Definition.Price.ToString("N0")); // NOTE: Added 'N0'

                AddHtmlLocalized(20, 180, 200, 25, 1011517);                          // Daily Pay :
                AddLabel(230, 180, 0x37, guardList.Definition.Upkeep.ToString("N0")); // NOTE: Added 'N0'

                AddHtmlLocalized(20, 210, 200, 25, 1011518);          // Current Silver :
                AddLabel(230, 210, 0x44, town.Silver.ToString("N0")); // NOTE: Added 'N0'

                AddHtmlLocalized(20, 240, 200, 25, 1011519);            // Current Payroll :
                AddLabel(230, 240, 0x44, sheriffUpkeep.ToString("N0")); // NOTE: Added 'N0'

                AddHtmlText(55, 300, 200, 25, guardList.Definition.Label, false, false);
                AddButton(20, 300, 4005, 4007, 1 + i);

                AddHtmlLocalized(55, 360, 200, 25, 1011067); // Previous page
                AddButton(20, 360, 4005, 4007, 0, GumpButtonType.Page, 3);
            }
        }

        private void CenterItem(int itemID, int x, int y, int w, int h)
        {
            var rc = ItemBounds.Table[itemID];
            AddItem(x + (w - rc.Width) / 2 - rc.X, y + (h - rc.Height) / 2 - rc.Y, itemID);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!m_Town.IsSheriff(m_From) || m_Town.Owner != m_Faction)
            {
                m_From.SendLocalizedMessage(1010339); // You no longer control this city
                return;
            }

            var index = info.ButtonID - 1;

            if (index >= 0 && index < m_Town.GuardLists.Count)
            {
                var guardList = m_Town.GuardLists[index];

                if (Town.FromRegion(m_From.Region) != m_Town)
                {
                    m_From.SendLocalizedMessage(1010305); // You must be in your controlled city to buy Items
                }
                else if (guardList.Guards.Count >= guardList.Definition.Maximum)
                {
                    // You currently have too many of this enhancement type to place another
                    m_From.SendLocalizedMessage(1010306);
                }
                else if (BaseBoat.FindBoatAt(m_From.Location, m_From.Map) != null)
                {
                    m_From.SendMessage("You cannot place a guard here");
                }
                else if (m_Town.Silver >= guardList.Definition.Price)
                {
                    var guard = guardList.Construct();

                    if (guard != null)
                    {
                        guard.Faction = m_Faction;
                        guard.Town = m_Town;

                        m_Town.Silver -= guardList.Definition.Price;

                        guard.MoveToWorld(m_From.Location, m_From.Map);
                        guard.Home = guard.Location;
                    }
                }
            }
        }
    }
}
