namespace Server.Items
{
    public class AcidProofRope : Item
    {
        [Constructible]
        public AcidProofRope() : base(0x20D) => Hue = 0x3D1;

        public AcidProofRope(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074886; // Acid Proof Rope

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
