namespace Server.Items
{
    public class IronwoodCrown : RavenHelm
    {
        [Constructible]
        public IronwoodCrown()
        {
            Hue = 0x1;

            ArmorAttributes.SelfRepair = 3;

            Attributes.BonusStr = 5;
            Attributes.BonusDex = 5;
            Attributes.BonusInt = 5;
        }

        public IronwoodCrown(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072924; // Ironwood Crown

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 7;
        public override int BaseEnergyResistance => 10;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
