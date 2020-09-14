namespace Server.Items
{
    public class CloseHelm : BaseArmor
    {
        [Constructible]
        public CloseHelm() : base(0x1408) => Weight = 5.0;

        public CloseHelm(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 45;
        public override int InitMaxHits => 60;

        public override int AosStrReq => 55;
        public override int OldStrReq => 40;

        public override int ArmorBase => 30;

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

            if (Weight == 1.0)
            {
                Weight = 5.0;
            }
        }
    }
}
