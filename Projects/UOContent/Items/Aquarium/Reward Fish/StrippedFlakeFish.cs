namespace Server.Items
{
    public class StrippedFlakeFish : BaseFish
    {
        [Constructible]
        public StrippedFlakeFish() : base(0x3B0A)
        {
        }

        public StrippedFlakeFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074595; // Stripped Flake Fish

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
