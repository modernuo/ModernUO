namespace Server.Items
{
    public class FrozenLongbow : ElvenCompositeLongbow
    {
        [Constructible]
        public FrozenLongbow()
        {
            Attributes.WeaponSpeed = -5;
            Attributes.DefendChance = 10;
        }

        public FrozenLongbow(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073507; // frozen longbow

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
