using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class CrystalCaveBarrier : Item
{
    [Constructible]
    public CrystalCaveBarrier() : base(0x3967) => Movable = false;

    public override bool OnMoveOver(Mobile m)
    {
        if (m.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        var mob = m;

        if (m is BaseCreature creature)
        {
            mob = creature.ControlMaster;
        }

        if (mob is not PlayerMobile pm)
        {
            return false;
        }

        var qs = pm.Quest;

        if (qs is DarkTidesQuest)
        {
            QuestObjective obj = qs.FindObjective<SpeakCavePasswordObjective>();

            if (obj?.Completed == true)
            {
                // With Horus' permission, you are able to pass through the barrier.
                m.SendLocalizedMessage(1060648);

                return true;
            }
        }

        // Without the permission of the guardian Horus, the magic of the barrier prevents your passage.
        m.SendLocalizedMessage(1060649, "", 0x66D);

        return false;
    }
}
