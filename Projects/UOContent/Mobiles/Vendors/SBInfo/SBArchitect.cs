using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBArchitect : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo("1041280", typeof(InteriorDecorator), 10001, 20, 0xFC1, 0));
        if (Core.AOS)
          Add(new GenericBuyInfo("1060651", typeof(HousePlacementTool), 627, 20, 0x14F6, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(InteriorDecorator), 5000);

        if (Core.AOS)
          Add(typeof(HousePlacementTool), 301);
      }
    }
  }
}