namespace Server.Items
{
    public class AssassinsShortbow : MagicalShortbow
    {
        [Constructible]
        public AssassinsShortbow()
        {
            Attributes.AttackChance = 3;
            Attributes.WeaponDamage = 4;
        }

        public AssassinsShortbow(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073512; // assassin's shortbow

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
