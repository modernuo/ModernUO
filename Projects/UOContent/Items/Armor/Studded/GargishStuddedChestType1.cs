namespace Server.Items
{
    [TypeAlias("Server.Items.GargishStuddedChest")]
    public class GargishStuddedChestType1 : BaseArmor
    {
        [Constructible]
        public GargishStuddedChestType1() : base(0x286) => Weight = 15.0;
        public GargishStuddedChestType1(Serial serial) : base(serial)
        {
        }

        public override Race RequiredRace => Race.Gargoyle;
        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 6;

        public override int InitMinHits => 40;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 40;
        public override int OldStrReq => 40;

        public override int ArmorBase => 16;

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
            int version = reader.ReadInt();
        }
    }
}
