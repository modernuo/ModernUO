namespace Server.Items
{
    public class HelmOfSwiftness : WingedHelm
    {
        [Constructible]
        public HelmOfSwiftness()
        {
            Hue = 0x592;

            Attributes.BonusInt = 5;
            Attributes.CastSpeed = 1;
            Attributes.CastRecovery = 2;
            ArmorAttributes.MageArmor = 1;
        }

        public HelmOfSwiftness(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075037; // Helm of Swiftness

        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 6;
        public override int BaseEnergyResistance => 8;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

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
