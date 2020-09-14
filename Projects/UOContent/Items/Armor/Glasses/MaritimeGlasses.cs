namespace Server.Items
{
    public class MaritimeGlasses : ElvenGlasses
    {
        [Constructible]
        public MaritimeGlasses()
        {
            Attributes.Luck = 150;
            Attributes.NightSight = 1;
            Attributes.ReflectPhysical = 20;

            Hue = 0x581;
        }

        public MaritimeGlasses(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073364; // Maritime Reading Glasses

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 30;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 3;

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
                Hue = 0x581;
            }
        }
    }
}
