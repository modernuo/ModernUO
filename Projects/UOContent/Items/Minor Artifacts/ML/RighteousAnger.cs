namespace Server.Items
{
    public class RighteousAnger : ElvenMachete
    {
        [Constructible]
        public RighteousAnger()
        {
            Hue = 0x284;

            Attributes.AttackChance = 15;
            Attributes.DefendChance = 5;
            Attributes.WeaponSpeed = 35;
            Attributes.WeaponDamage = 40;
        }

        public RighteousAnger(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075049; // Righteous Anger

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
