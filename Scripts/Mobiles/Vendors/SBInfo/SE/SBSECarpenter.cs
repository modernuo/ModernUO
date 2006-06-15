using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public class SBSECarpenter: SBInfo
	{
		private ArrayList m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBSECarpenter()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override ArrayList BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : ArrayList
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( typeof( Bokuto ), 21, 20, 0x27A8, 0 ) );
				Add( new GenericBuyInfo( typeof( Tetsubo ), 43, 20, 0x27A6, 0 ) );
				Add( new GenericBuyInfo( typeof( Fukiya ), 20, 20, 0x27AA, 0 ) );
				Add( new GenericBuyInfo( typeof( BambooFlute ), 21, 20, 0x2805, 0 ) );
				Add( new GenericBuyInfo( typeof( BambooFlute ), 21, 20, 0x2805, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
				Add( typeof( Tetsubo ), 21 );
				Add( typeof( Fukiya ), 10 );
				Add( typeof( BambooFlute ), 10 );
				Add( typeof( Bokuto ), 10 );
			}
		}
	}
}