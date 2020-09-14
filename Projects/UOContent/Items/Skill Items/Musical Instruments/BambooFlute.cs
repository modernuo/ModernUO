namespace Server.Items
{
    public class BambooFlute : BaseInstrument
    {
        [Constructible]
        public BambooFlute() : base(0x2805, 0x504, 0x503) => Weight = 2.0;

        public BambooFlute(Serial serial) : base(serial)
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

            if (Weight == 3.0)
            {
                Weight = 2.0;
            }
        }
    }
}
