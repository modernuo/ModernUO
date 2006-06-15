using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public class SBRanger : SBInfo
	{
		private ArrayList m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBRanger()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override ArrayList BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : ArrayList
		{
			public InternalBuyInfo()
			{
				Add( new AnimalBuyInfo( 1, typeof( Cat ), 138, 20, 201, 0 ) );
				Add( new AnimalBuyInfo( 1, typeof( Dog ), 181, 20, 217, 0 ) );
				Add( new AnimalBuyInfo( 1, typeof( PackLlama ), 491, 20, 292, 0 ) );
				Add( new AnimalBuyInfo( 1, typeof( PackHorse ), 606, 20, 291, 0 ) );
				Add( new GenericBuyInfo( typeof( Bandage ), 5, 20, 0xE21, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
			}
		}
	}
}