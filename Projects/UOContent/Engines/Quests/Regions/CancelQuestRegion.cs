using System;
using Server.Regions;
using Server.Mobiles;

namespace Server.Engines.Quests;

public class CancelQuestRegion : BaseRegion
{
    public CancelQuestRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public CancelQuestRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public Type Quest { get; set; }

    public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
    {
        if (!base.OnMoveInto (m, d, newLocation, oldLocation))
        {
            return false;
        }

        if (m.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        if (Quest == null)
        {
            return true;
        }

        if (m is PlayerMobile player && player.Quest != null && player.Quest.GetType() == Quest)
        {
            if (!player.HasGump<QuestCancelGump>())
            {
                player.Quest.BeginCancelQuest();
            }

            return false;
        }

        return true;
    }
}
