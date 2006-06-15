using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public class SBPoleArmWeapon: SBInfo
	{
		private ArrayList m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBPoleArmWeapon()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override ArrayList BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : ArrayList
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( typeof( Bardiche ), 60, 20, 0xF4D, 0 ) );
				Add( new GenericBuyInfo( typeof( Halberd ), 42, 20, 0x143E, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
				Add( typeof( Bardiche ), 30 );
				Add( typeof( Halberd ), 21 );
			}
		}
	}
}
