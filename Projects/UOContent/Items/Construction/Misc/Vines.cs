namespace Server.Items
{
    public class Vines : Item
    {
        [Constructible]
        public Vines() : this(Utility.Random(8))
        {
        }

        [Constructible]
        public Vines(int v) : base(0xCEB)
        {
            if (v is < 0 or > 7)
            {
                v = 0;
            }

            ItemID += v;
            Weight = 1.0;
        }

        public Vines(Serial serial) : base(serial)
        {
        }

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

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
