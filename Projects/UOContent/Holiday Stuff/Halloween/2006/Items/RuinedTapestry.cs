namespace Server.Items
{
    public class RuinedTapestry : Item
    {
        [Constructible]
        public RuinedTapestry()
            : base(Utility.RandomBool() ? 0x4699 : 0x469A)
        {
        }

        public RuinedTapestry(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Ruined Tapestry ";

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
