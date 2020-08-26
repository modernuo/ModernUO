namespace Server.Items
{
    public class WalkersLeggings : LeatherNinjaPants
    {
        [Constructible]
        public WalkersLeggings() => LootType = LootType.Blessed;

        public WalkersLeggings(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1078222; // Walker's Leggings

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 3;
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
