namespace Server.Items
{
    [Flippable(0x1451, 0x1456)]
    public class BoneHelm : BaseArmor
    {
        [Constructible]
        public BoneHelm() : base(0x1451) => Weight = 3.0;

        public BoneHelm(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 25;
        public override int InitMaxHits => 30;

        public override int AosStrReq => 20;
        public override int OldStrReq => 40;

        public override int ArmorBase => 30;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            if (Weight == 1.0)
            {
                Weight = 3.0;
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
