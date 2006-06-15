
using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public class SBVeterinarian : SBInfo
	{
		private ArrayList m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBVeterinarian()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override ArrayList BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : ArrayList
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( typeof( Bandage ), 6, 20, 0xE21, 0 ) );
				Add( new AnimalBuyInfo( 1, typeof( PackHorse ), 616, 10, 291, 0 ) );
				Add( new AnimalBuyInfo( 1, typeof( PackLlama ), 523, 10, 292, 0 ) );
				Add( new AnimalBuyInfo( 1, typeof( Dog ), 158, 10, 217, 0 ) );
				Add( new AnimalBuyInfo( 1, typeof( Cat ), 131, 10, 201, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
				Add( typeof( Bandage ), 1 );
			}
		}
	}
}