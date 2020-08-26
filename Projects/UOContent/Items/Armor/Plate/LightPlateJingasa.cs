namespace Server.Items
{
    public class LightPlateJingasa : BaseArmor
    {
        [Constructible]
        public LightPlateJingasa() : base(0x2781) => Weight = 5.0;

        public LightPlateJingasa(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 7;
        public override int BaseFireResistance => 2;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 55;
        public override int InitMaxHits => 60;

        public override int AosStrReq => 55;
        public override int OldStrReq => 55;

        public override int ArmorBase => 4;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

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
