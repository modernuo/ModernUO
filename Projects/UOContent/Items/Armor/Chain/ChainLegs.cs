namespace Server.Items
{
    [Flippable(0x13be, 0x13c3)]
    public class ChainLegs : BaseArmor
    {
        [Constructible]
        public ChainLegs() : base(0x13BE) => Weight = 7.0;

        public ChainLegs(Serial serial) : base(serial)
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

        public override int OldDexBonus => -3;

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
