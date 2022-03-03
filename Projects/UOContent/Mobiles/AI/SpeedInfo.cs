using System;
using Server.Mobiles;

namespace Server;

public static class SpeedInfo
{
    public const double MinDelay = 0.1;
    public const double MaxDelay = 0.5;
    public const double MinDelayWild = 0.4;
    public const double MaxDelayWild = 0.8;

    public static void GetSpeeds(BaseCreature bc, ref double activeSpeed, ref double passiveSpeed)
    {
        var isMonster = bc.IsMonster;
        var wildDelay = isMonster || bc.InActivePVPCombat();
        var maxDex = isMonster ? 150 : 190;

        var dex = Math.Min(maxDex, Math.Max(25, bc.Dex));

        double min = wildDelay ? MinDelayWild : MinDelay;
        double max = wildDelay ? MaxDelayWild : MaxDelay;

        if (bc.IsParagon)
        {
            min /= 2;
            max = min + 0.5;
        }

        activeSpeed = Math.Max(max - (max - min) * ((double)dex / maxDex), min);

        passiveSpeed = activeSpeed * 2;
    }

    public static double TransformMoveDelay(BaseCreature bc, double delay)
    {
        double adjusted = bc.IsMonster ? MaxDelayWild : MaxDelay;

        if (!bc.IsDeadPet && (bc.ReduceSpeedWithDamage || bc.IsSubdued))
        {
            double offset = bc.Stam / (double)bc.StamMax;

            if (offset < 1.0)
            {
                delay = delay + (adjusted - delay) * (1.0 - offset);
            }
        }

        if (delay > adjusted)
        {
            delay = adjusted;
        }

        return delay;
    }
}
}
