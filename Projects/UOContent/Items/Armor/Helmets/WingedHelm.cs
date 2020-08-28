namespace Server.Items
{
    [Flippable(0x2B73, 0x316A)]
    public class WingedHelm : BaseArmor
    {
        [Constructible]
        public WingedHelm() : base(0x2B73) => Weight = 5.0;

        public WingedHelm(Serial serial) : base(serial)
        {
        }

        public override Race RequiredRace => Race.Elf;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 1;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 45;
        public override int InitMaxHits => 55;

        public override int AosStrReq => 25;
        public override int OldStrReq => 25;

        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
