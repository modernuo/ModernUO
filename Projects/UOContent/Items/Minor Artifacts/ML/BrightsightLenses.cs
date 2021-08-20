namespace Server.Items
{
    public class BrightsightLenses : ElvenGlasses
    {
        [Constructible]
        public BrightsightLenses()
        {
            Hue = 0x501;

            Attributes.NightSight = 1;
            Attributes.RegenMana = 3;

            ArmorAttributes.SelfRepair = 3;
        }

        public BrightsightLenses(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075039; // Brightsight Lenses

        public override int BasePhysicalResistance => 9;
        public override int BaseFireResistance => 29;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 7;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1)
            {
                _weaponAttributes.SelfRepair = 0;
                ArmorAttributes.SelfRepair = 3;
            }
        }
    }
}
