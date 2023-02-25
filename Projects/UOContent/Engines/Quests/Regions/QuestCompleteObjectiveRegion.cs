using System;
using Server.Regions;
using Server.Mobiles;

namespace Server.Engines.Quests;

public class QuestCompleteObjectiveRegion : BaseRegion
{
    public QuestCompleteObjectiveRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public QuestCompleteObjectiveRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public Type Quest { get; set; }
    public Type Objective { get; set; }

    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);

        if (Quest != null && Objective != null)
        {
            if (m is PlayerMobile player && player?.Quest != null && player.Quest.GetType() == Quest)
            {
                QuestObjective obj = player.Quest.FindObjective(Objective);

                if (obj is { Completed: false })
                {
                    obj.Complete();
                }
            }
        }
    }
}
