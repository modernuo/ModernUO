using Server.Network;

namespace Server.Items
{
    public class PowerCrystal : Item
    {
        [Constructible]
        public PowerCrystal() : base(0x1F1C) => Weight = 1.0;

        public PowerCrystal(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "power crystal";

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else
            {
                from.SendAsciiMessage("This looks like part of a larger contraption.");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
