namespace Server.Items
{
    [Flippable(0x104F, 0x1050)]
    public class ClockParts : Item
    {
        [Constructible]
        public ClockParts(int amount = 1) : base(0x104F)
        {
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public ClockParts(Serial serial) : base(serial)
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
