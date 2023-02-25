using System;
using Server.Regions;
using Server.Mobiles;

namespace Server.Engines.Quests;

public class QuestNoEntryRegion : BaseRegion
{
    public Type Quest { get; set;  }

    public Type MinObjective { get; set; }

    public Type MaxObjective { get; set; }

    public int Message { get; set; }

    public QuestNoEntryRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public QuestNoEntryRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

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

        if (m is BaseCreature { Controlled: false, Summoned: false })
        {
            return true;
        }

        if (Quest == null)
        {
            return true;
        }

        if (m is PlayerMobile player && player.Quest != null && player.Quest.GetType() == Quest
            && (MinObjective == null || player.Quest.FindObjective(MinObjective) != null)
            && (MaxObjective == null || player.Quest.FindObjective(MaxObjective) == null))
        {
            return true;
        }

        if (Message != 0)
        {
            m.SendLocalizedMessage(Message);
        }

        return false;
    }
}
