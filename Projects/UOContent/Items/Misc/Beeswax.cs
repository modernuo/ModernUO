namespace Server.Items
{
    public class Beeswax : Item
    {
        [Constructible]
        public Beeswax(int amount = 1) : base(0x1422)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public Beeswax(Serial serial) : base(serial)
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
