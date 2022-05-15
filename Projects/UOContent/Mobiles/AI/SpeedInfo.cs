using System;
using Server.Mobiles;

namespace Server;

public static class SpeedInfo
{
    public static double MinDelay { get; private set; }
    public static double MaxDelay { get; private set; }
    public static double MinMonsterDelay { get; private set; }
    public static double MaxMonsterDelay { get; private set; }

    // Determines the maximum dex for delay by dex
    public static int MaxDex { get; private set; }
    public static int MaxMonsterDex { get; private set; }

    public static void Configure()
    {
        /**
         * According to testing by ServUO devs, the settings on OSI should be the following.
         * Unfortunately they are horribly slow:
         * Non-Monsters:
         * MinDelay = 0.1
         * MaxDelay = 0.5
         * MaxDex = 190
         *
         * Monsters:
         * MinDelay = 0.4
         * MaxDelay = 0.8
         * MaxDex = 150
         */
        MinDelay = ServerConfiguration.GetSetting("movement.delay.npcMinDelay", 0.1);
        MaxDelay = ServerConfiguration.GetSetting("movement.delay.npcMaxDelay", 0.375);
        MaxDex = ServerConfiguration.GetSetting("movement.delay.maxDex", 150);

        MinMonsterDelay = ServerConfiguration.GetSetting("movement.delay.monsterMinDelay", 0.2);
        MaxMonsterDelay = ServerConfiguration.GetSetting("movement.delay.monsterMaxDelay", 0.325);
        MaxMonsterDex = ServerConfiguration.GetSetting("movement.delay.monsterMaxDex", 150);
    }

    public static void GetSpeeds(BaseCreature bc, out double activeSpeed, out double passiveSpeed)
    {
        if (!bc.ScaleSpeedByDex)
        {
            LegacySpeedInfo.GetSpeeds(bc.GetType(), out activeSpeed, out passiveSpeed);
            return;
        }

        var isMonster = bc.IsMonster;
        var maxDex = isMonster ? MaxMonsterDex : MaxDex;

        var dex = Math.Clamp(bc.Dex, 25, maxDex);

        double min = isMonster ? MinMonsterDelay : MinDelay;
        double max = isMonster ? MaxMonsterDelay : MaxDelay;

        activeSpeed = Math.Max(max - (max - min) * ((double)dex / maxDex), min);
        passiveSpeed = activeSpeed * 2;
    }
}
