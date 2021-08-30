using System.Collections.Generic;

namespace Server.Mobiles
{
    public class SBAnimalTrainer : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new AnimalBuyInfo(1, "a cat", typeof(Cat), 132, 10, 201, 0));
                Add(new AnimalBuyInfo(1, "a dog", typeof(Dog), 170, 10, 217, 0));
                Add(new AnimalBuyInfo(1, "a horse", typeof(Horse), 550, 10, 204, 0));
                Add(new AnimalBuyInfo(1, "a pack horse", typeof(PackHorse), 631, 10, 291, 0));
                Add(new AnimalBuyInfo(1, "a pack llama", typeof(PackLlama), 565, 10, 292, 0));
                Add(new AnimalBuyInfo(1, "a rabbit", typeof(Rabbit), 106, 10, 205, 0));

                if (!Core.AOS)
                {
                    Add(new AnimalBuyInfo(1, "an eagle", typeof(Eagle), 402, 10, 5, 0));
                    // Using itemID 211 (old value) instead of 167 for compatibility with CUO. Brown bear was not added to UOP
                    // If/when it is added, CUO will be able to show the 167 value.
                    Add(new AnimalBuyInfo(1, "a brown bear", typeof(BrownBear), 855, 10, 211, 0));
                    Add(new AnimalBuyInfo(1, "a grizzly bear", typeof(GrizzlyBear), 1767, 10, 212, 0));
                    Add(new AnimalBuyInfo(1, "a panther", typeof(Panther), 1271, 10, 214, 0));
                    Add(new AnimalBuyInfo(1, "a timber wolf", typeof(TimberWolf), 768, 10, 225, 0));
                    Add(new AnimalBuyInfo(1, "a rat", typeof(Rat), 107, 10, 238, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
        }
    }
}
