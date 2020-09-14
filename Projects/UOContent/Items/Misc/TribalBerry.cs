namespace Server.Items
{
    public class TribalBerry : Item
    {
        [Constructible]
        public TribalBerry(int amount = 1) : base(0x9D0)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
            Hue = 6;
        }

        public TribalBerry(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1040001; // tribal berry

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Hue == 4)
            {
                Hue = 6;
            }
        }
    }
}
