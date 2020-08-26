namespace Server.Items
{
    public class CandelabraOfSouls : Item
    {
        [Constructible]
        public CandelabraOfSouls() : base(0xB26)
        {
        }

        public CandelabraOfSouls(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063478;

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
