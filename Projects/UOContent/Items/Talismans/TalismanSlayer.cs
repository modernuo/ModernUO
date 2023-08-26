using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items;

public enum TalismanSlayerName
{
    None,
    Bear,
    Vermin,
    Bat,
    Mage,
    Beetle,
    Bird,
    Ice,
    Flame,
    Bovine
}

public static class TalismanSlayer
{
    private static Dictionary<TalismanSlayerName, Type[]> m_Table;

    public static void Initialize()
    {
        m_Table = new Dictionary<TalismanSlayerName, Type[]>
        {
            [TalismanSlayerName.Bear] = new[]
            {
                typeof(GrizzlyBear), typeof(BlackBear), typeof(BrownBear), typeof(PolarBear) // , typeof( Grobu )
            },
            [TalismanSlayerName.Vermin] = new[]
            {
                typeof(RatmanMage), typeof(RatmanMage), typeof(RatmanArcher), typeof(Barracoon), typeof(Ratman),
                typeof(SewerRat),
                typeof(Rat), typeof(GiantRat) // , typeof( Chiikkaha )
            },
            [TalismanSlayerName.Bat] = new[] { typeof(Mongbat), typeof(StrongMongbat), typeof(VampireBat) },
            [TalismanSlayerName.Mage] =
                new[]
                {
                    typeof(EvilMage), typeof(EvilMageLord), typeof(AncientLich), typeof(Lich), typeof(LichLord),
                    typeof(SkeletalMage), typeof(BoneMagi), typeof(OrcishMage), typeof(KhaldunZealot), typeof(JukaMage)
                },
            [TalismanSlayerName.Beetle] =
                new[]
                {
                    typeof(Beetle), typeof(RuneBeetle), typeof(FireBeetle), typeof(DeathwatchBeetle),
                    typeof(DeathwatchBeetleHatchling)
                },
            [TalismanSlayerName.Bird] = new[]
            {
                typeof(Bird), typeof(TropicalBird), typeof(Chicken), typeof(Crane), typeof(DesertOstard), typeof(Eagle),
                typeof(ForestOstard), typeof(FrenziedOstard),
                typeof(Phoenix), /*typeof( Pyre ), typeof( Swoop ), typeof( Saliva ),*/ typeof(Harpy),
                typeof(StoneHarpy) // ?????
            },
            [TalismanSlayerName.Ice] = new[]
            {
                typeof(ArcticOgreLord), typeof(IceElemental), typeof(SnowElemental), typeof(FrostOoze),
                typeof(IceFiend), /*typeof( UnfrozenMummy ),*/ typeof(FrostSpider), typeof(LadyOfTheSnow),
                typeof(FrostTroll),

                // TODO WinterReaper, check
                typeof(IceSnake), typeof(SnowLeopard), typeof(PolarBear), typeof(IceSerpent), typeof(GiantIceWorm)
            },
            [TalismanSlayerName.Flame] = new[]
            {
                typeof(FireBeetle), typeof(HellHound), typeof(LavaSerpent), typeof(FireElemental),
                typeof(PredatorHellCat),
                typeof(Phoenix), typeof(FireGargoyle), typeof(HellCat),
                /*typeof( Pyre ),*/ typeof(FireSteed), typeof(LavaLizard),

                // TODO check
                typeof(LavaSnake)
            },
            [TalismanSlayerName.Bovine] = new[]
            {
                typeof(Cow), typeof(Bull),
                typeof(Gaman) /*, typeof( MinotaurCaptain ), typeof( MinotaurScout ), typeof( Minotaur ) */
                // TODO TormentedMinotaur
            }
        };
    }

    public static bool Slays(TalismanSlayerName name, Mobile m)
    {
        if (m == null || !m_Table.TryGetValue(name, out var types) || types == null)
        {
            return false;
        }

        var type = m.GetType();

        for (var i = 0; i < types.Length; i++)
        {
            if (types[i].IsAssignableFrom(type))
            {
                return true;
            }
        }

        return false;
    }
}