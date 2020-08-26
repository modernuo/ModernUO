using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBSpearForkWeapon : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo(typeof(Pitchfork), 19, 20, 0xE87, 0));
        Add(new GenericBuyInfo(typeof(ShortSpear), 23, 20, 0x1403, 0));
        Add(new GenericBuyInfo(typeof(Spear), 31, 20, 0xF62, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(Spear), 15);
        Add(typeof(Pitchfork), 9);
        Add(typeof(ShortSpear), 11);
      }
    }
  }
}