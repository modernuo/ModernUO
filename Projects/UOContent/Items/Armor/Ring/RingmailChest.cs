namespace Server.Items
{
    [Flippable(0x13ec, 0x13ed)]
    public class RingmailChest : BaseArmor
    {
        [Constructible]
        public RingmailChest() : base(0x13EC) => Weight = 15.0;

        public RingmailChest(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 1;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 40;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 40;
        public override int OldStrReq => 20;

        public override int OldDexBonus => -2;

        public override int ArmorBase => 22;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Ringmail;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (Weight == 1.0)
            {
                Weight = 15.0;
            }
        }
    }
}
