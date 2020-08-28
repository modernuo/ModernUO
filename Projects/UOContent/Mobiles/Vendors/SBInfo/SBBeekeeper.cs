using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBBeekeeper : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(JarHoney), 3, 20, 0x9EC, 0));
                Add(new GenericBuyInfo(typeof(Beeswax), 2, 20, 0x1422, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(JarHoney), 1);
                Add(typeof(Beeswax), 1);
            }
        }
    }
}
