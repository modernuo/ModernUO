
namespace Server.Items
{
    class GargishLeatherArms : BaseArmor
    {
        [Constructible]
        public GargishLeatherArms(int hue) : base(0x302) => Weight = 4.0;
   

        public GargishLeatherArms(Serial serial) : base(serial)
        {
        }

        public override Race RequiredRace => Race.Gargoyle;
        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 6;
        public override int BaseEnergyResistance => 6;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 25;
        public override int OldStrReq => 25;

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
