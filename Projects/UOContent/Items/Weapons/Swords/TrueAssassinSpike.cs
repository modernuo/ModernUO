namespace Server.Items
{
    public class TrueAssassinSpike : AssassinSpike
    {
        [Constructible]
        public TrueAssassinSpike()
        {
            Attributes.AttackChance = 4;
            Attributes.WeaponDamage = 4;
        }

        public TrueAssassinSpike(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073517; // true assassin spike

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
