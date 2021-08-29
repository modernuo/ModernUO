namespace Server.Items
{
    public class FeyLeggings : ChainLegs
    {
        [Constructible]
        public FeyLeggings()
        {
            Attributes.BonusHits = 6;
            Attributes.DefendChance = 20;

            ArmorAttributes.MageArmor = 1;
        }

        public FeyLeggings(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075041; // Fey Leggings

        public override int BasePhysicalResistance => 12;
        public override int BaseFireResistance => 8;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 4;
        public override int BaseEnergyResistance => 19;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override int RequiredRaces => Race.AllowElvesOnly;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
