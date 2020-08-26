namespace Server.Items
{
    public class IcyScimitar : RadiantScimitar
    {
        [Constructible]
        public IcyScimitar() => WeaponAttributes.HitHarm = 15;

        public IcyScimitar(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073543; // icy scimitar

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
