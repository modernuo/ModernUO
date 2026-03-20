using System;
using Server.Engines.Craft;
using Server.Engines.Craft.T2A;
using Server.Systems.FeatureFlags;

namespace Server.SkillHandlers;

public static class Cartography
{
    public static void Initialize()
    {
        SkillInfo.Table[(int)SkillName.Cartography].Callback = OnUse;
    }

    public static TimeSpan OnUse(Mobile m)
    {
        if (!ContentFeatureFlags.T2ACraftMenus)
        {
            m.SendLocalizedMessage(1046444); // Use a mapmaker's pen to draw maps.
            return TimeSpan.Zero;
        }

        T2ACraftSystem.ShowMenu(m, DefCartography.CraftSystem, null);
        return TimeSpan.FromSeconds(1.0);
    }
}
