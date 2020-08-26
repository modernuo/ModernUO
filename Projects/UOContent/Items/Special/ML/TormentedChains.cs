namespace Server.Items
{
    public class TormentedChains : Item
    {
        [Constructible]
        public TormentedChains() : base(Utility.Random(6663, 2)) => Weight = 1.0;

        public TormentedChains(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "chains of the tormented";

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
