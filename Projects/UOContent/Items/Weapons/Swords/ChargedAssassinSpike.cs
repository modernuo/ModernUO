namespace Server.Items
{
    public class ChargedAssassinSpike : AssassinSpike
    {
        [Constructible]
        public ChargedAssassinSpike() => WeaponAttributes.HitLightning = 10;

        public ChargedAssassinSpike(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073518; // charged assassin spike

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
