namespace Server.Items
{
    public class Runesabre : RuneBlade
    {
        [Constructible]
        public Runesabre()
        {
            SkillBonuses.SetValues(0, SkillName.MagicResist, 5.0);
            WeaponAttributes.MageWeapon = -29;
        }

        public Runesabre(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073537; // runesabre

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
