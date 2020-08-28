namespace Server.Items
{
    public class BolaBall : Item
    {
        [Constructible]
        public BolaBall(int amount = 1) : base(0xE73)
        {
            Weight = 4.0;
            Stackable = true;
            Amount = amount;
            Hue = 0x8AC;
        }

        public BolaBall(Serial serial) : base(serial)
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
