using ModernUO.Serialization;
using Server.Factions;
using Server.Gumps;
using Server.Multis;

namespace Server;

[SerializationGenerator(0)]
public sealed partial class UrnOfAscension : PowerFactionItem
{
    public UrnOfAscension() : base(9246)
    {
    }

    public override string DefaultName => "urn of ascension";

    public override bool Use(Mobile from)
    {
        var ourFaction = Faction.Find(from);

        var used = false;

        foreach (var mob in from.GetMobilesInRange(8))
        {
            if (mob.Player && !mob.Alive && from.InLOS(mob))
            {
                if (Faction.Find(mob) != ourFaction)
                {
                    continue;
                }

                var house = BaseHouse.FindHouseAt(mob);

                if (house?.IsFriend(from) != false || house.IsFriend(mob))
                {
                    Faction.ClearSkillLoss(mob);

                    mob.SendGump(new ResurrectGump(from));
                    used = true;
                }
            }
        }

        if (used)
        {
            from.LocalOverheadMessage(MessageType.Regular, 2219, false, "The urn shatters as you invoke its power.");
            from.PlaySound(64);

            Effects.PlaySound(from.Location, from.Map, 1481);
        }

        return used;
    }
}
