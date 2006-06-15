using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public class SBKnifeWeapon: SBInfo
	{
		private ArrayList m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBKnifeWeapon()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override ArrayList BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : ArrayList
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( typeof( ButcherKnife ), 14, 20, 0x13F6, 0 ) );
				Add( new GenericBuyInfo( typeof( Dagger ), 21, 20, 0xF52, 0 ) );
				Add( new GenericBuyInfo( typeof( Cleaver ), 15, 20, 0xEC3, 0 ) );
				Add( new GenericBuyInfo( typeof( SkinningKnife ), 14, 20, 0xEC4, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
				Add( typeof( ButcherKnife ), 7 );
				Add( typeof( Cleaver ), 7 );
				Add( typeof( Dagger ), 10 );
				Add( typeof( SkinningKnife ), 7 );
			}
		}
	}
}
