namespace Server.Items
{
    public class Beads : Item
    {
        [Constructible]
        public Beads() : base(0x108B) => Weight = 1.0;

        public Beads(Serial serial) : base(serial)
        {
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
