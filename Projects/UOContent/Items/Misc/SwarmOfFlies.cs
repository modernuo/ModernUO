namespace Server.Items
{
    public class SwarmOfFlies : Item
    {
        [Constructible]
        public SwarmOfFlies() : base(0x91B)
        {
            Hue = 1;
            Movable = false;
        }

        public SwarmOfFlies(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a swarm of flies";

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
