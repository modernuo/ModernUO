namespace Server.Items
{
    public class TunicOfGuarding : LeatherChest
    {
        [Constructible]
        public TunicOfGuarding()
        {
            LootType = LootType.Blessed;

            Attributes.BonusHits = 2;
            Attributes.ReflectPhysical = 5;
        }

        public TunicOfGuarding(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1077693; // Tunic of Guarding

        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 5;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

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
