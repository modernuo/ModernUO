namespace Server.Items
{
    public class AnthropomorphistGlasses : ElvenGlasses
    {
        [Constructible]
        public AnthropomorphistGlasses()
        {
            Attributes.BonusHits = 5;
            Attributes.RegenMana = 3;
            Attributes.ReflectPhysical = 20;

            Hue = 0x80;
        }

        public AnthropomorphistGlasses(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073379; // Anthropomorphist Reading Glasses

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 20;
        public override int BaseEnergyResistance => 20;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (version == 0 && Hue == 0)
            {
                Hue = 0x80;
            }
        }
    }
}
