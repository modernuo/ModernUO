namespace Server.Items
{
    public class TrueWarCleaver : WarCleaver
    {
        [Constructible]
        public TrueWarCleaver()
        {
            Attributes.WeaponDamage = 4;
            Attributes.RegenHits = 2;
        }

        public TrueWarCleaver(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073528; // true war cleaver

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
