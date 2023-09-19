using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBMapmaker : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(BlankMap), 5, 40, 0x14EC, 0));
                Add(new GenericBuyInfo(typeof(MapmakersPen), 8, 20, 0x0FBF, 0));
                Add(new GenericBuyInfo(typeof(BlankScroll), 12, 40, 0xEF3, 0));

                for (var i = 0; i < PresetMapEntry.Table.Length; ++i)
                {
                    Add(new PresetMapBuyInfo(PresetMapEntry.Table[i], Utility.RandomMinMax(7, 10), 20));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(BlankScroll), 3);
                Add(typeof(MapmakersPen), 4);
                Add(typeof(BlankMap), 2);
                Add(typeof(CityMap), 3);
                Add(typeof(LocalMap), 3);
                Add(typeof(WorldMap), 3);
                Add(typeof(PresetMapEntry), 3);
                // TODO: Buy back maps that the mapmaker sells!!!
            }
        }
    }
}
