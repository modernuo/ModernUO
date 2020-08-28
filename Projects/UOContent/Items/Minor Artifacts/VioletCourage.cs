namespace Server.Items
{
    public class VioletCourage : FemalePlateChest
    {
        [Constructible]
        public VioletCourage()
        {
            Hue = 0x486;
            Attributes.Luck = 95;
            Attributes.DefendChance = 15;
            ArmorAttributes.LowerStatReq = 100;
            ArmorAttributes.MageArmor = 1;
        }

        public VioletCourage(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063471;

        public override int BasePhysicalResistance => 14;
        public override int BaseFireResistance => 12;
        public override int BaseColdResistance => 12;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 9;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

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
