using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Factions
{
	public class SheriffGump : FactionGump
	{
		private PlayerMobile m_From;
		private Faction m_Faction;
		private Town m_Town;

		private void CenterItem( int itemID, int x, int y, int w, int h )
		{
			Rectangle2D rc = ItemBounds.Table[itemID];
			AddItem( x + ((w - rc.Width) / 2) - rc.X, y + ((h - rc.Height) / 2) - rc.Y, itemID );
		}

		public SheriffGump( PlayerMobile from, Faction faction, Town town ) : base( 50, 50 )
		{
			m_From = from;
			m_Faction = faction;
			m_Town = town;


			AddPage( 0 );

			AddBackground( 0, 0, 320, 410, 5054 );
			AddBackground( 10, 10, 300, 390, 3000 );

			#region General
			AddPage( 1 );

			AddHtmlLocalized( 20, 30, 260, 25, 1011431, false, false ); // Sheriff

			AddHtmlLocalized( 55, 90, 200, 25, 1011494, false, false ); // HIRE GUARDS
			AddButton( 20, 90, 4005, 4007, 0, GumpButtonType.Page, 3 );

			AddHtmlLocalized( 55, 120, 200, 25, 1011495, false, false ); // VIEW FINANCES
			AddButton( 20, 120, 4005, 4007, 0, GumpButtonType.Page, 2 );

			AddHtmlLocalized( 55, 360, 200, 25, 1011441, false, false ); // Exit
			AddButton( 20, 360, 4005, 4007, 0, GumpButtonType.Reply, 0 );
			#endregion

			#region Finances
			AddPage( 2 );

			int financeUpkeep = town.FinanceUpkeep;
			int sheriffUpkeep = town.SheriffUpkeep;
			int dailyIncome = town.DailyIncome;
			int netCashFlow = town.NetCashFlow;

			AddHtmlLocalized( 20, 30, 300, 25, 1011524, false, false ); // FINANCE STATEMENT
			
			AddHtmlLocalized( 20, 80, 300, 25, 1011538, false, false ); // Current total money for town : 
			AddLabel( 20, 100, 0x44, town.Silver.ToString( "N0" ) ); // NOTE: Added 'N0'

			AddHtmlLocalized( 20, 130, 300, 25, 1011520, false, false ); // Finance Minister Upkeep : 
			AddLabel( 20, 150, 0x44, financeUpkeep.ToString( "N0" ) ); // NOTE: Added 'N0'
	
			AddHtmlLocalized( 20, 180, 300, 25, 1011521, false, false ); // Sheriff Upkeep : 
			AddLabel( 20, 200, 0x44, sheriffUpkeep.ToString( "N0" ) ); // NOTE: Added 'N0'

			AddHtmlLocalized( 20, 230, 300, 25, 1011522, false, false ); // Town Income : 
			AddLabel( 20, 250, 0x44, dailyIncome.ToString( "N0" ) ); // NOTE: Added 'N0'

			AddHtmlLocalized( 20, 280, 300, 25, 1011523, false, false ); // Net Cash flow per day : 
			AddLabel( 20, 300, 0x44, netCashFlow.ToString( "N0" ) ); // NOTE: Added 'N0'

			AddHtmlLocalized( 55, 360, 200, 25, 1011067, false, false ); // Previous page
			AddButton( 20, 360, 4005, 4007, 0, GumpButtonType.Page, 1 );
			#endregion

			#region Hire Guards
			AddPage( 3 );

			AddHtmlLocalized( 20, 30, 300, 25, 1011494, false, false ); // HIRE GUARDS

			List<GuardList> guardLists = town.GuardLists;

			for ( int i = 0; i < guardLists.Count; ++i )
			{
				GuardList guardList = guardLists[i];
				int y = 90 + (i * 60);

				AddButton( 20, y, 4005, 4007, 0, GumpButtonType.Page, 4 + i );
				CenterItem( guardList.Definition.ItemID, 50, y - 20, 70, 60 );
				AddHtmlText( 120, y, 200, 25, guardList.Definition.Header, false, false );
			}

			AddHtmlLocalized( 55, 360, 200, 25, 1011067, false, false ); // Previous page
			AddButton( 20, 360, 4005, 4007, 0, GumpButtonType.Page, 1 );
			#endregion

			#region Guard Pages
			for ( int i = 0; i < guardLists.Count; ++i )
			{
				GuardList guardList = guardLists[i];

				AddPage( 4 + i );

				AddHtmlText( 90, 30, 300, 25, guardList.Definition.Header, false, false );
				CenterItem( guardList.Definition.ItemID, 10, 10, 80, 80 );

				AddHtmlLocalized( 20, 90, 200, 25, 1011514, false, false ); // You have : 
				AddLabel( 230, 90, 0x26, guardList.Guards.Count.ToString() );

				AddHtmlLocalized( 20, 120, 200, 25, 1011515, false, false ); // Maximum : 
				AddLabel( 230, 120, 0x12A, guardList.Definition.Maximum.ToString() );

				AddHtmlLocalized( 20, 150, 200, 25, 1011516, false, false ); // Cost : 
				AddLabel( 230, 150, 0x44, guardList.Definition.Price.ToString( "N0" ) ); // NOTE: Added 'N0'

				AddHtmlLocalized( 20, 180, 200, 25, 1011517, false, false ); // Daily Pay :
				AddLabel( 230, 180, 0x37, guardList.Definition.Upkeep.ToString( "N0" ) ); // NOTE: Added 'N0'

				AddHtmlLocalized( 20, 210, 200, 25, 1011518, false, false ); // Current Silver : 
				AddLabel( 230, 210, 0x44, town.Silver.ToString( "N0" ) ); // NOTE: Added 'N0'

				AddHtmlLocalized( 20, 240, 200, 25, 1011519, false, false ); // Current Payroll : 
				AddLabel( 230, 240, 0x44, sheriffUpkeep.ToString( "N0" ) ); // NOTE: Added 'N0'

				AddHtmlText( 55, 300, 200, 25, guardList.Definition.Label, false, false );
				AddButton( 20, 300, 4005, 4007, 1 + i, GumpButtonType.Reply, 0 );

				AddHtmlLocalized( 55, 360, 200, 25, 1011067, false, false ); // Previous page
				AddButton( 20, 360, 4005, 4007, 0, GumpButtonType.Page, 3 );
			}
			#endregion
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( !m_Town.IsSheriff( m_From ) || m_Town.Owner != m_Faction )
			{
				m_From.SendLocalizedMessage( 1010339 ); // You no longer control this city
				return;
			}

			int index = info.ButtonID - 1;

			if ( index >= 0 && index < m_Town.GuardLists.Count )
			{
				GuardList guardList = m_Town.GuardLists[index];
				Town town = Town.FromRegion( m_From.Region );

				if ( Town.FromRegion( m_From.Region ) != m_Town )
				{
					m_From.SendLocalizedMessage( 1010305 ); // You must be in your controlled city to buy Items
				}
				else if ( guardList.Guards.Count >= guardList.Definition.Maximum )
				{
					m_From.SendLocalizedMessage( 1010306 ); // You currently have too many of this enhancement type to place another
				}
				else if ( m_Town.Silver >= guardList.Definition.Price )
				{
					BaseFactionGuard guard = guardList.Construct();

					if ( guard != null )
					{
						guard.Faction = m_Faction;
						guard.Town = m_Town;

						m_Town.Silver -= guardList.Definition.Price;

						guard.MoveToWorld( m_From.Location, m_From.Map );
						guard.Home = guard.Location;
					}
				}
			}
		}
	}
}