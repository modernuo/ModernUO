namespace Server.Items
{
    public class LongbowOfMight : ElvenCompositeLongbow
    {
        [Constructible]
        public LongbowOfMight() => Attributes.WeaponDamage = 5;

        public LongbowOfMight(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073508; // longbow of might

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
