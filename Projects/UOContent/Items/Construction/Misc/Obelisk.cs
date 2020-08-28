namespace Server.Items
{
    public class Obelisk : Item
    {
        [Constructible]
        public Obelisk() : base(0x1184) => Movable = false;

        public Obelisk(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1016474; // an obelisk

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
