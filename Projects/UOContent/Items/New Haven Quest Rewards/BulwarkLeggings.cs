namespace Server.Items
{
    public class BulwarkLeggings : RingmailLegs
    {
        [Constructible]
        public BulwarkLeggings()
        {
            LootType = LootType.Blessed;

            Attributes.RegenStam = 1;
            Attributes.RegenMana = 1;
        }

        public BulwarkLeggings(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1077727; // Bulwark Leggings

        public override int BasePhysicalResistance => 9;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 5;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 3;

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
