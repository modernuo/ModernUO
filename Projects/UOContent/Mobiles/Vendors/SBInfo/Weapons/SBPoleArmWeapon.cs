using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBPoleArmWeapon : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo(typeof(Bardiche), 60, 20, 0xF4D, 0));
        Add(new GenericBuyInfo(typeof(Halberd), 42, 20, 0x143E, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(Bardiche), 30);
        Add(typeof(Halberd), 21);
      }
    }
  }
}