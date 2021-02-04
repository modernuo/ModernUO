namespace Server.Items
{
    [Furniture]
    [Flippable(0xF65, 0xF67, 0xF69)]
    [TypeAlias("Server.Items.Easle")]
    public class Easel : Item
    {
        [Constructible]
        public Easel() : base(0xF65) => Weight = 25.0;

        public Easel(Serial serial) : base(serial)
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

            if (Weight == 10.0)
            {
                Weight = 25.0;
            }
        }
    }
}
