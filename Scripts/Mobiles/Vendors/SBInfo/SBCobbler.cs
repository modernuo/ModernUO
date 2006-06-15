using System; 
using System.Collections; 
using Server.Items; 

namespace Server.Mobiles 
{ 
	public class SBCobbler : SBInfo 
	{ 
		private ArrayList m_BuyInfo = new InternalBuyInfo(); 
		private IShopSellInfo m_SellInfo = new InternalSellInfo(); 

		public SBCobbler() 
		{ 
		} 

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } } 
		public override ArrayList BuyInfo { get { return m_BuyInfo; } } 

		public class InternalBuyInfo : ArrayList 
		{ 
			public InternalBuyInfo() 
			{ 	
				Add( new GenericBuyInfo( typeof( ThighBoots ), 15, 20, 0x1711, Utility.RandomNeutralHue() ) ); 
				Add( new GenericBuyInfo( typeof( Shoes ), 8, 20, 0x170f, Utility.RandomNeutralHue() ) ); 
				Add( new GenericBuyInfo( typeof( Boots ), 10, 20, 0x170b, Utility.RandomNeutralHue() ) );
				Add( new GenericBuyInfo( typeof( Sandals ), 5, 20, 0x170d, Utility.RandomNeutralHue() ) ); 
 
			} 
		} 

		public class InternalSellInfo : GenericSellInfo 
		{ 
			public InternalSellInfo() 
			{ 
				Add( typeof( Shoes ), 4 ); 
				Add( typeof( Boots ), 5 ); 
				Add( typeof( ThighBoots ), 7 ); 
				Add( typeof( Sandals ), 2 ); 
			} 
		} 
	} 
}