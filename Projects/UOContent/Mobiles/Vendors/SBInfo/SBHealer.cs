using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class SBHealer : SBInfo
  {
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
      public InternalBuyInfo()
      {
        Add(new GenericBuyInfo(typeof(Bandage), 5, 20, 0xE21, 0));
        Add(new GenericBuyInfo(typeof(LesserHealPotion), 15, 20, 0xF0C, 0));
        Add(new GenericBuyInfo(typeof(Ginseng), 3, 20, 0xF85, 0));
        Add(new GenericBuyInfo(typeof(Garlic), 3, 20, 0xF84, 0));
        Add(new GenericBuyInfo(typeof(RefreshPotion), 15, 20, 0xF0B, 0));
      }
    }

    public class InternalSellInfo : GenericSellInfo
    {
      public InternalSellInfo()
      {
        Add(typeof(Bandage), 1);
        Add(typeof(LesserHealPotion), 7);
        Add(typeof(RefreshPotion), 7);
        Add(typeof(Garlic), 2);
        Add(typeof(Ginseng), 2);
      }
    }
  }
}