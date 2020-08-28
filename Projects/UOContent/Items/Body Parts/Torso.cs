namespace Server.Items
{
    public class Torso : Item
    {
        [Constructible]
        public Torso() : base(0x1D9F) => Weight = 2.0;

        public Torso(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
