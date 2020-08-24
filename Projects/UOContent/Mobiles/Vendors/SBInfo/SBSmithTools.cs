using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBSmithTools : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(IronIngot), 5, 16, 0x1BF2, 0));
                Add(new GenericBuyInfo(typeof(Tongs), 13, 14, 0xFBB, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(Tongs), 7);
                Add(typeof(IronIngot), 4);
            }
        }
    }
}
