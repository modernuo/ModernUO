namespace Server.Items
{
    public class RingOfTheSavant : GoldRing
    {
        [Constructible]
        public RingOfTheSavant()
        {
            LootType = LootType.Blessed;

            Attributes.BonusInt = 3;
            Attributes.CastRecovery = 1;
            Attributes.CastSpeed = 1;
        }

        public RingOfTheSavant(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1077608; // Ring of the Savant

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
