using System;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Engines.Craft;

namespace Server.Factions
{
	public class FactionImbueGump : FactionGump
	{
		private Item m_Item;
		private Mobile m_Mobile;
		private Faction m_Faction;
		private CraftSystem m_CraftSystem;
		private BaseTool m_Tool;
		private object m_Notice;
		private int m_Quality;

		private FactionItemDefinition m_Definition;

		public FactionImbueGump( int quality, Item item, Mobile from, CraftSystem craftSystem, BaseTool tool, object notice, int availableSilver, Faction faction, FactionItemDefinition def ) : base( 100, 200 )			
		{	
			m_Item = item;
			m_Mobile = from;
			m_Faction = faction;
			m_CraftSystem = craftSystem;
			m_Tool = tool;
			m_Notice = notice;
			m_Quality = quality;
			m_Definition = def;

			AddPage( 0 );

			AddBackground( 0, 0, 320, 270, 5054 );
			AddBackground( 10, 10, 300, 250, 3000 );

			AddHtmlLocalized( 20, 20, 210, 25, 1011569, false, false ); // Imbue with Faction properties?


			AddHtmlLocalized( 20, 60, 170, 25, 1018302, false, false ); // Item quality: 
			AddHtmlLocalized( 175, 60, 100, 25, 1018305 - m_Quality, false, false ); //	Exceptional, Average, Low

			AddHtmlLocalized( 20, 80, 170, 25, 1011572, false, false ); // Item Cost : 
			AddLabel( 175, 80, 0x34, def.SilverCost.ToString( "N0" ) ); // NOTE: Added 'N0'

			AddHtmlLocalized( 20, 100, 170, 25, 1011573, false, false ); // Your Silver : 
			AddLabel( 175, 100, 0x34, availableSilver.ToString( "N0" ) ); // NOTE: Added 'N0'


			AddRadio( 20, 140, 210, 211, true, 1 );
			AddLabel( 55, 140, m_Faction.Definition.HuePrimary - 1, "*****" );
			AddHtmlLocalized( 150, 140, 150, 25, 1011570, false, false ); // Primary Color

			AddRadio( 20, 160, 210, 211, false, 2 );
			AddLabel( 55, 160, m_Faction.Definition.HueSecondary - 1, "*****" );
			AddHtmlLocalized( 150, 160, 150, 25, 1011571, false, false ); // Secondary Color


			AddHtmlLocalized( 55, 200, 200, 25, 1011011, false, false ); // CONTINUE
			AddButton( 20, 200, 4005, 4007, 1, GumpButtonType.Reply, 0 );

			AddHtmlLocalized( 55, 230, 200, 25, 1011012, false, false ); // CANCEL
			AddButton( 20, 230, 4005, 4007, 0, GumpButtonType.Reply, 0 );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 1 )
			{
				Container pack = m_Mobile.Backpack;

				if ( pack != null && m_Item.IsChildOf( pack ) )
				{
					if ( pack.ConsumeTotal( typeof( Silver ), m_Definition.SilverCost ) )
					{
						int hue;

						if ( m_Item is SpellScroll )
							hue = 0;
						else if ( info.IsSwitched( 1 ) )
							hue = m_Faction.Definition.HuePrimary;
						else
							hue = m_Faction.Definition.HueSecondary;

						FactionItem.Imbue( m_Item, m_Faction, true, hue );
					}
					else
					{
						m_Mobile.SendLocalizedMessage( 1042204 ); // You do not have enough silver.
					}
				}
			}

			if ( m_Tool != null && !m_Tool.Deleted && m_Tool.UsesRemaining > 0 )
				m_Mobile.SendGump( new CraftGump( m_Mobile, m_CraftSystem, m_Tool, m_Notice ) );
			else if ( m_Notice is string )
				m_Mobile.SendMessage( (string) m_Notice );
			else if ( m_Notice is int && ((int)m_Notice) > 0 )
				m_Mobile.SendLocalizedMessage( (int) m_Notice );
		}
	}
}