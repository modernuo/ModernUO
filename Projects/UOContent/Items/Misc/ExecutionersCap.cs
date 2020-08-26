namespace Server.Items
{
    public class ExecutionersCap : Item
    {
        [Constructible]
        public ExecutionersCap() : base(0xF83) => Weight = 1.0;

        public ExecutionersCap(Serial serial) : base(serial)
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
