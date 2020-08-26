namespace Server.Items
{
    [Flippable(0x1051, 0x1052)]
    public class AxleGears : Item
    {
        [Constructible]
        public AxleGears(int amount = 1) : base(0x1051)
        {
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public AxleGears(Serial serial) : base(serial)
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
