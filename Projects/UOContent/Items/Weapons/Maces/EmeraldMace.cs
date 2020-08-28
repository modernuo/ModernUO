namespace Server.Items
{
    public class EmeraldMace : DiamondMace
    {
        [Constructible]
        public EmeraldMace() => WeaponAttributes.ResistPoisonBonus = 5;

        public EmeraldMace(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073530; // emerald mace

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
