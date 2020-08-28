namespace Server.Items
{
    public class HeavyOrnateAxe : OrnateAxe
    {
        [Constructible]
        public HeavyOrnateAxe() => Attributes.WeaponDamage = 8;

        public HeavyOrnateAxe(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073548; // heavy ornate axe

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
