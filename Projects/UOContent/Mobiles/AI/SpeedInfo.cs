using System;
using Server.Mobiles;

namespace Server;

public static class SpeedInfo
{
    public const double MinDelay = 0.1;
    public const double MaxDelay = 0.5;
    public const double MinDelayMonster = 0.4;
    public const double MaxDelayMonster = 0.8;
    public const int MaxDex = 190;
    public const int MaxDexMonster = 150;


    public static void GetSpeeds(BaseCreature bc, ref double activeSpeed, ref double passiveSpeed)
    {
        var isMonster = bc.IsMonster;
        var monsterDelay = isMonster || bc.InActivePVPCombat();
        var maxDex = isMonster ? MaxDexMonster : MaxDex;

        var dex = Math.Min(maxDex, Math.Max(25, bc.Dex));

        double min = monsterDelay ? MinDelayMonster : MinDelay;
        double max = monsterDelay ? MaxDelayMonster : MaxDelay;

        if (bc.IsParagon)
        {
            min /= 2;
            max = min + 0.5;
        }

        activeSpeed = Math.Max(max - (max - min) * ((double)dex / maxDex), min);

        passiveSpeed = activeSpeed * 2;
    }
}
