using System.Collections.Generic;

namespace Server.Mobiles
{
  public class SBRancher : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new AnimalBuyInfo(1, typeof(PackHorse), 631, 10, 291, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
    }
  }
}