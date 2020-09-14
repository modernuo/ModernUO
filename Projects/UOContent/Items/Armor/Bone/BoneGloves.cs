namespace Server.Items
{
    [Flippable(0x1450, 0x1455)]
    public class BoneGloves : BaseArmor
    {
        [Constructible]
        public BoneGloves() : base(0x1450) => Weight = 2.0;

        public BoneGloves(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 25;
        public override int InitMaxHits => 30;

        public override int AosStrReq => 55;
        public override int OldStrReq => 40;

        public override int OldDexBonus => -1;

        public override int ArmorBase => 30;
        public override int RevertArmorBase => 2;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            if (Weight == 1.0)
            {
                Weight = 2.0;
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
