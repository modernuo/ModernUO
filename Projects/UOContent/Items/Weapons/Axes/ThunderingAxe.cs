namespace Server.Items
{
    public class ThunderingAxe : OrnateAxe
    {
        [Constructible]
        public ThunderingAxe() => WeaponAttributes.HitLightning = 10;

        public ThunderingAxe(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073547; // thundering axe

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
