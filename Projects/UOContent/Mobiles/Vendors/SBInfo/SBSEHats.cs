using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBSEHats : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo(typeof(Kasa), 31, 20, 0x2798, 0));
        Add(new GenericBuyInfo(typeof(LeatherJingasa), 11, 20, 0x2776, 0));
        Add(new GenericBuyInfo(typeof(ClothNinjaHood), 33, 20, 0x278F, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(Kasa), 15);
        Add(typeof(LeatherJingasa), 5);
        Add(typeof(ClothNinjaHood), 16);
      }
    }
  }
}