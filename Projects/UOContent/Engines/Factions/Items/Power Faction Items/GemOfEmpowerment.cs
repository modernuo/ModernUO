using Server.Factions;

namespace Server
{
    public sealed class GemOfEmpowerment : PowerFactionItem
    {
        public GemOfEmpowerment()
            : base(7955) =>
            Hue = 1154;

        public GemOfEmpowerment(Serial serial)
            : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
