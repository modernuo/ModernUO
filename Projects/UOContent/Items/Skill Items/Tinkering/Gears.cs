namespace Server.Items
{
    [Flippable(0x1053, 0x1054)]
    public class Gears : Item
    {
        [Constructible]
        public Gears(int amount = 1) : base(0x1053)
        {
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public Gears(Serial serial) : base(serial)
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
