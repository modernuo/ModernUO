namespace Server.Items
{
    public class SkullPole : Item
    {
        [Constructible]
        public SkullPole() : base(0x2204) => Weight = 5;

        public SkullPole(Serial serial) : base(serial)
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
