namespace Server.Items
{
    public class MaceShieldGlasses : ElvenGlasses
    {
        [Constructible]
        public MaceShieldGlasses()
        {
            WeaponAttributes.HitLowerDefend = 30;
            Attributes.BonusStr = 10;
            Attributes.BonusDex = 5;

            Hue = 0x1DD;
        }

        public MaceShieldGlasses(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073381; // Mace And Shield Reading Glasses

        public override int BasePhysicalResistance => 25;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 10;
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
                Hue = 0x1DD;
            }
        }
    }
}
