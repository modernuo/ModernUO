namespace Server.Items
{
    [Flippable(0x104D, 0x104E)]
    public class ClockFrame : Item
    {
        [Constructible]
        public ClockFrame(int amount = 1) : base(0x104D)
        {
            Stackable = true;
            Amount = amount;
            Weight = 2.0;
        }

        public ClockFrame(Serial serial) : base(serial)
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
