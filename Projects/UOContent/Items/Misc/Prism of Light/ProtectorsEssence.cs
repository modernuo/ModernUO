namespace Server.Items
{
    public class ProtectorsEssence : Item
    {
        [Constructible]
        public ProtectorsEssence() : base(0x23F)
        {
        }

        public ProtectorsEssence(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073159; // Protector's Essence

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
