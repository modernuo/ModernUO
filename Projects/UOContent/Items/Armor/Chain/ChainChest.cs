namespace Server.Items
{
    [Flippable(0x13bf, 0x13c4)]
    public class ChainChest : BaseArmor
    {
        [Constructible]
        public ChainChest() : base(0x13BF) => Weight = 7.0;

        public ChainChest(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 4;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 1;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 45;
        public override int InitMaxHits => 60;

        public override int AosStrReq => 60;
        public override int OldStrReq => 20;

        public override int OldDexBonus => -5;

        public override int ArmorBase => 28;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
