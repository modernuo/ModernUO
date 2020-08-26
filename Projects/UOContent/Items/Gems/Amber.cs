namespace Server.Items
{
    public class Amber : Item
    {
        [Constructible]
        public Amber(int amount = 1) : base(0xF25)
        {
            Stackable = true;
            Amount = amount;
        }

        public Amber(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;

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
