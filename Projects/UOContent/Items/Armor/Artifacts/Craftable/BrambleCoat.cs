namespace Server.Items
{
    public class BrambleCoat : WoodlandChest
    {
        [Constructible]
        public BrambleCoat()
        {
            Hue = 0x1;

            ArmorAttributes.SelfRepair = 3;
            Attributes.BonusHits = 4;
            Attributes.Luck = 150;
            Attributes.ReflectPhysical = 25;
            Attributes.DefendChance = 15;
        }

        public BrambleCoat(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072925; // Bramble Coat

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 8;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 7;

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
