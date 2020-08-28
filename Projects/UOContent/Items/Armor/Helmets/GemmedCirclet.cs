namespace Server.Items
{
    [Flippable(0x2B70, 0x3167)]
    public class GemmedCirclet : BaseArmor
    {
        [Constructible]
        public GemmedCirclet() : base(0x2B70) => Weight = 2.0;

        public GemmedCirclet(Serial serial) : base(serial)
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
