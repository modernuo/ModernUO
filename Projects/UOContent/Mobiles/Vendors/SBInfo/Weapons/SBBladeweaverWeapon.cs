using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles;

public class SBBladeweaverWeapon : SBInfo
{
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
        public InternalBuyInfo()
        {
            Add(new GenericBuyInfo(typeof(Boomerang), 250, 20, 0x8FF, 0));
            Add(new GenericBuyInfo(typeof(Cyclone), 350, 20, 0x901, 0));
            Add(new GenericBuyInfo(typeof(SoulGlaive), 500, 20, 0x090A, 0));
        }
    }

    public class InternalSellInfo : GenericSellInfo
    {
        public InternalSellInfo()
        {
            Add(typeof(Boomerang), 125);
            Add(typeof(Cyclone), 175);
            Add(typeof(SoulGlaive), 250);
        }
    }
}
