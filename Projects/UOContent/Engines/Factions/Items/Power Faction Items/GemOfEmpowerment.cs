using ModernUO.Serialization;
using Server.Factions;

namespace Server;

[SerializationGenerator(0)]
public sealed partial class GemOfEmpowerment : PowerFactionItem
{
    public GemOfEmpowerment() : base(7955) => Hue = 1154;

    public override string DefaultName => "gem of empowerment";

    public override bool Use(Mobile from)
    {
        if (Faction.ClearSkillLoss(from))
        {
            from.LocalOverheadMessage(MessageType.Regular, 2219, false, "The gem shatters as you invoke its power.");
            from.PlaySound(909);

            from.FixedEffect(0x373A, 10, 30);
            from.PlaySound(0x209);

            return true;
        }

        return false;
    }
}
