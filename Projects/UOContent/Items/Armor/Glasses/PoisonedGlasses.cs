namespace Server.Items
{
    public class PoisonedGlasses : ElvenGlasses
    {
        [Constructible]
        public PoisonedGlasses()
        {
            Attributes.BonusStam = 3;
            Attributes.RegenStam = 4;

            Hue = 0x113;
        }

        public PoisonedGlasses(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073376; // Poisoned Reading Glasses

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 30;
        public override int BaseEnergyResistance => 10;

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
                Hue = 0x113;
            }
        }
    }
}
