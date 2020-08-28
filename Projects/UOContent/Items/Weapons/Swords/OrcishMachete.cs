namespace Server.Items
{
    public class OrcishMachete : ElvenMachete
    {
        [Constructible]
        public OrcishMachete()
        {
            Attributes.BonusInt = -5;
            Attributes.WeaponDamage = 10;
        }

        public OrcishMachete(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073534; // Orcish Machete

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
