namespace Server.Items
{
    public class ShipModelOfTheHMSCape : Item
    {
        [Constructible]
        public ShipModelOfTheHMSCape() : base(0x14F3) => Hue = 0x37B;

        public ShipModelOfTheHMSCape(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063476;

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
