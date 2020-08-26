using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBMonk : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        if (Core.AOS) Add(new GenericBuyInfo(typeof(MonkRobe), 136, 20, 0x2687, 0x21E));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
    }
  }
}