namespace Server.Items
{
    [Flippable(0x2B69, 0x3160)]
    public class WoodlandGorget : BaseArmor
    {
        [Constructible]
        public WoodlandGorget() : base(0x2B69)
        {
        }

        public WoodlandGorget(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 50;
        public override int InitMaxHits => 65;

        public override int AosStrReq => 45;
        public override int OldStrReq => 45;

        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
        public override Race RequiredRace => Race.Elf;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            if (version == 0)
            {
                Weight = -1;
            }
        }
    }
}
