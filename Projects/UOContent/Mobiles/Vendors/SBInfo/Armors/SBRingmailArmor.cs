using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBRingmailArmor : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(RingmailChest), 121, 20, 0x13ec, 0));
                Add(new GenericBuyInfo(typeof(RingmailLegs), 90, 20, 0x13F0, 0));
                Add(new GenericBuyInfo(typeof(RingmailArms), 85, 20, 0x13EE, 0));
                Add(new GenericBuyInfo(typeof(RingmailGloves), 93, 20, 0x13eb, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(RingmailArms), 42);
                Add(typeof(RingmailChest), 60);
                Add(typeof(RingmailGloves), 26);
                Add(typeof(RingmailLegs), 45);
            }
        }
    }
}
