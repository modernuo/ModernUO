namespace Server.Items
{
    [Flippable(0x2B76, 0x316D)]
    public class HideGorget : BaseArmor
    {
        [Constructible]
        public HideGorget() : base(0x2B76) => Weight = 3.0;

        public HideGorget(Serial serial) : base(serial)
        {
        }

        public override Race RequiredRace => Race.Elf;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 35;
        public override int InitMaxHits => 45;

        public override int AosStrReq => 15;
        public override int OldStrReq => 15;

        public override int ArmorBase => 15;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.Half;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (Weight == 2.0)
            {
                Weight = 1.0;
            }
        }
    }
}
