using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBLeatherWorker : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Hides), 4, 999, 0x1078, 0));
                Add(new GenericBuyInfo(typeof(ThighBoots), 56, 10, 0x1711, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(Hides), 2);
                Add(typeof(ThighBoots), 7);
            }
        }
    }
}
