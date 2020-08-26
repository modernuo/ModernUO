namespace Server.Items
{
    [Flippable(0x105B, 0x105C)]
    public class Axle : Item
    {
        [Constructible]
        public Axle(int amount = 1) : base(0x105B)
        {
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public Axle(Serial serial) : base(serial)
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
