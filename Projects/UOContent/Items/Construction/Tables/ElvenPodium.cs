namespace Server.Items
{
    [Furniture, Flippable(0x2DDD, 0x2DDE)]
    public class ElvenPodium : Item
    {
        [Constructible]
        public ElvenPodium() : base(0x2DDD) => Weight = 2.0;

        public ElvenPodium(Serial serial) : base(serial)
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
