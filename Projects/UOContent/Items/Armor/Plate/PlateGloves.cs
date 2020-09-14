namespace Server.Items
{
    [Flippable(0x1414, 0x1418)]
    public class PlateGloves : BaseArmor
    {
        [Constructible]
        public PlateGloves() : base(0x1414) => Weight = 2.0;

        public PlateGloves(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 50;
        public override int InitMaxHits => 65;

        public override int AosStrReq => 70;
        public override int OldStrReq => 30;

        public override int OldDexBonus => -2;

        public override int ArmorBase => 40;

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
                Weight = 2.0;
            }
        }
    }
}
