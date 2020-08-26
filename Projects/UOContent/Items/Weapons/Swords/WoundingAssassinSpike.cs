namespace Server.Items
{
    public class WoundingAssassinSpike : AssassinSpike
    {
        [Constructible]
        public WoundingAssassinSpike() => WeaponAttributes.HitHarm = 15;

        public WoundingAssassinSpike(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073520; // wounding assassin spike

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
