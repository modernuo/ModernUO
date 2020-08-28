namespace Server.Items
{
    public class AncientWildStaff : WildStaff
    {
        [Constructible]
        public AncientWildStaff() => WeaponAttributes.ResistPoisonBonus = 5;

        public AncientWildStaff(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073550; // ancient wild staff

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
