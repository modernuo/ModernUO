namespace Server.Items
{
    [Flippable(0x2B6F, 0x3166)]
    public class RoyalCirclet : BaseArmor
    {
        [Constructible]
        public RoyalCirclet() : base(0x2B6F) => Weight = 2.0;

        public RoyalCirclet(Serial serial) : base(serial)
        {
        }

        public override Race RequiredRace => Race.Elf;

        public override int BasePhysicalResistance => 1;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 35;

        public override int AosStrReq => 10;
        public override int OldStrReq => 10;

        public override int ArmorBase => 30;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

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
