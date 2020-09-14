namespace Server.Items
{
    public class NecromanticGlasses : ElvenGlasses
    {
        [Constructible]
        public NecromanticGlasses()
        {
            Attributes.LowerManaCost = 15;
            Attributes.LowerRegCost = 30;

            Hue = 0x22D;
        }

        public NecromanticGlasses(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073377; // Necromantic Reading Glasses

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 0;
        public override int BaseColdResistance => 0;
        public override int BasePoisonResistance => 0;
        public override int BaseEnergyResistance => 0;

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
                Hue = 0x22D;
            }
        }
    }
}
