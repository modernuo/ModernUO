using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBSEFood : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo(typeof(Wasabi), 2, 20, 0x24E8, 0));
        Add(new GenericBuyInfo(typeof(Wasabi), 2, 20, 0x24E9, 0));
        Add(new GenericBuyInfo(typeof(BentoBox), 6, 20, 0x2836, 0));
        Add(new GenericBuyInfo(typeof(BentoBox), 6, 20, 0x2837, 0));
        Add(new GenericBuyInfo(typeof(GreenTeaBasket), 2, 20, 0x284B, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(Wasabi), 1);
        Add(typeof(BentoBox), 3);
        Add(typeof(GreenTeaBasket), 1);
      }
    }
  }
}