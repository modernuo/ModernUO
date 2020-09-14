namespace Server.Items
{
    public class OrcHelm : BaseArmor
    {
        [Constructible]
        public OrcHelm() : base(0x1F0B)
        {
        }

        public OrcHelm(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 1;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 30;
        public override int OldStrReq => 10;

        public override int ArmorBase => 20;

        public override double DefaultWeight => 5;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.None;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (version == 0 && (Weight == 1 || Weight == 5))
            {
                Weight = -1;
            }
        }
    }
}
