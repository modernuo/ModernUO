using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBVeterinarian : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Bandage), 6, 20, 0xE21, 0));
                Add(new AnimalBuyInfo(1, "a pack horse", typeof(PackHorse), 616, 10, 291, 0));
                Add(new AnimalBuyInfo(1, "a pack llama", typeof(PackLlama), 523, 10, 292, 0));
                Add(new AnimalBuyInfo(1, "a dog", typeof(Dog), 158, 10, 217, 0));
                Add(new AnimalBuyInfo(1, "a cat", typeof(Cat), 131, 10, 201, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(Bandage), 1);
            }
        }
    }
}
