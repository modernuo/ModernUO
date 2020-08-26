using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBKnifeWeapon : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo(typeof(ButcherKnife), 14, 20, 0x13F6, 0));
        Add(new GenericBuyInfo(typeof(Dagger), 21, 20, 0xF52, 0));
        Add(new GenericBuyInfo(typeof(Cleaver), 15, 20, 0xEC3, 0));
        Add(new GenericBuyInfo(typeof(SkinningKnife), 14, 20, 0xEC4, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(ButcherKnife), 7);
        Add(typeof(Cleaver), 7);
        Add(typeof(Dagger), 10);
        Add(typeof(SkinningKnife), 7);
      }
    }
  }
}