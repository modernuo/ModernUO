using System;
using Server;

namespace Server.Factions
{
	public class VendorDefinition
	{
		private Type m_Type;

		private int m_Price;
		private int m_Upkeep;
		private int m_Maximum;

		private int m_ItemID;

		private TextDefinition m_Header;
		private TextDefinition m_Label;

		public Type Type{ get{ return m_Type; } }

		public int Price{ get{ return m_Price; } }
		public int Upkeep{ get{ return m_Upkeep; } }
		public int Maximum{ get{ return m_Maximum; } }
		public int ItemID{ get{ return m_ItemID; } }

		public TextDefinition Header{ get{ return m_Header; } }
		public TextDefinition Label{ get{ return m_Label; } }

		public VendorDefinition( Type type, int itemID, int price, int upkeep, int maximum, TextDefinition header, TextDefinition label )
		{
			m_Type = type;

			m_Price = price;
			m_Upkeep = upkeep;
			m_Maximum = maximum;
			m_ItemID = itemID;

			m_Header = header;
			m_Label = label;
		}

		private static VendorDefinition[] m_Definitions = new VendorDefinition[]
			{
				new VendorDefinition( typeof( FactionBottleVendor ), 0xF0E,
					5000,
					1000,
					10,
					new TextDefinition( 1011549, "POTION BOTTLE VENDOR" ),
					new TextDefinition( 1011544, "Buy Potion Bottle Vendor" )
				),
				new VendorDefinition( typeof( FactionBoardVendor ), 0x1BD7,
					3000,
					500,
					10,
					new TextDefinition( 1011552, "WOOD VENDOR" ),
					new TextDefinition( 1011545, "Buy Wooden Board Vendor" )
				),
				new VendorDefinition( typeof( FactionOreVendor ), 0x19B8,
					3000,
					500,
					10,
					new TextDefinition( 1011553, "IRON ORE VENDOR" ),
					new TextDefinition( 1011546, "Buy Iron Ore Vendor" )
				),
				new VendorDefinition( typeof( FactionReagentVendor ), 0xF86,
					5000,
					1000,
					10,
					new TextDefinition( 1011554, "REAGENT VENDOR" ),
					new TextDefinition( 1011547, "Buy Reagent Vendor" )
				),
				new VendorDefinition( typeof( FactionHorseVendor ), 0x20DD,
					5000,
					1000,
					1,
					new TextDefinition( 1011556, "HORSE BREEDER" ),
					new TextDefinition( 1011555, "Buy Horse Breeder" )
				)
			};

		public static VendorDefinition[] Definitions{ get{ return m_Definitions; } }
	}
}