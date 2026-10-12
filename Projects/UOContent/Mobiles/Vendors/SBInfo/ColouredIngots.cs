using System;
using Server.Items;

namespace Server.Mobiles
{
    /// <summary>
    /// What a smith's counter pays for a coloured bar: below iron's four on purpose, lower the rarer the metal, so that digging
    /// iron for the counter and working coloured metal into pieces stay the better trades and the counter is only the floor
    /// under a coloured bar nobody wants. Bought and consumed, never put up for sale again (see
    /// <see cref="GenericSellInfo.Add(Type, int, bool)"/>). Shared by every shopkeeper that buys iron bars for metalwork
    /// (<see cref="SBBlacksmith"/>, <see cref="SBSmithTools"/>), in every era.
    /// </summary>
    public static class ColouredIngots
    {
        public static readonly (Type Kind, int Price)[] Prices =
        {
            (typeof(DullCopperIngot), 3),
            (typeof(ShadowIronIngot), 3),
            (typeof(CopperIngot), 3),
            (typeof(BronzeIngot), 3),
            (typeof(GoldIngot), 2),
            (typeof(AgapiteIngot), 2),
            (typeof(VeriteIngot), 2),
            (typeof(ValoriteIngot), 2)
        };

        public static void AddTo(GenericSellInfo info)
        {
            if (info == null)
            {
                return;
            }

            for (var i = 0; i < Prices.Length; i++)
            {
                info.Add(Prices[i].Kind, Prices[i].Price, false);
            }
        }
    }
}
