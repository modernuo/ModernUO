namespace Server.Items
{
    public class BraceletOfHealth : GoldBracelet
    {
        [Constructible]
        public BraceletOfHealth()
        {
            Hue = 0x21;
            Attributes.BonusHits = 5;
            Attributes.RegenHits = 10;
        }

        public BraceletOfHealth(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061103; // Bracelet of Health
        public override int ArtifactRarity => 11;

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
