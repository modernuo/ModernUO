using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBSEBowyer : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo(typeof(Yumi), 53, 20, 0x27A5, 0));
        Add(new GenericBuyInfo(typeof(Fukiya), 20, 20, 0x27AA, 0));
        Add(new GenericBuyInfo(typeof(Nunchaku), 35, 20, 0x27AE, 0));
        Add(new GenericBuyInfo(typeof(FukiyaDarts), 3, 20, 0x2806, 0));
        Add(new GenericBuyInfo(typeof(Bokuto), 21, 20, 0x27A8, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(Yumi), 26);
        Add(typeof(Fukiya), 10);
        Add(typeof(Nunchaku), 17);
        Add(typeof(FukiyaDarts), 1);
        Add(typeof(Bokuto), 10);
      }
    }
  }
}