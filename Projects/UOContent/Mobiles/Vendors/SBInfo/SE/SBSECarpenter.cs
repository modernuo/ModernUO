using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBSECarpenter : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo(typeof(Bokuto), 21, 20, 0x27A8, 0));
        Add(new GenericBuyInfo(typeof(Tetsubo), 43, 20, 0x27A6, 0));
        Add(new GenericBuyInfo(typeof(Fukiya), 20, 20, 0x27AA, 0));
        Add(new GenericBuyInfo(typeof(BambooFlute), 21, 20, 0x2805, 0));
        Add(new GenericBuyInfo(typeof(BambooFlute), 21, 20, 0x2805, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(Tetsubo), 21);
        Add(typeof(Fukiya), 10);
        Add(typeof(BambooFlute), 10);
        Add(typeof(Bokuto), 10);
      }
    }
  }
}