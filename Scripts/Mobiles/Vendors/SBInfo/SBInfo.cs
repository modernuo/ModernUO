using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public abstract class SBInfo
	{
		public SBInfo()
		{
		}

		public abstract IShopSellInfo SellInfo { get; }
		public abstract ArrayList BuyInfo { get; }
	}
}