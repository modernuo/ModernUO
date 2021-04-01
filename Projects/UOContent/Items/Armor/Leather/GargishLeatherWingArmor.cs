namespace Server.Items
{
    [Flippable(0x457E, 0x457F)]
    public class GargishLeatherWingArmor : BaseArmor
    {
        [Constructible]
        public GargishLeatherWingArmor() : base(0x457E) => Weight = 2.0;
        public GargishLeatherWingArmor(Serial serial) : base(serial)
        {
        }

        public override Race RequiredRace => Race.Gargoyle;
        public override int PhysicalResistance => 0;
        public override int FireResistance => 0;
        public override int ColdResistance => 0;
        public override int PoisonResistance => 0;
        public override int EnergyResistance => 0;

        public override int AosStrReq => 10;
        public override int OldStrReq => 10;
        public override int ArmorBase => 0;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }
        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
