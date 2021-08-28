namespace Server.Items
{
    public class CrimsonCincture : HalfApron
    {
        [Constructible]
        public CrimsonCincture()
        {
            Hue = 0x485;

            Attributes.BonusDex = 5;
            Attributes.BonusHits = 10;
            Attributes.RegenHits = 2;
        }

        public CrimsonCincture(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075043; // Crimson Cincture

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
