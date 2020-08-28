namespace Server.Items
{
    public class TrueLeafblade : Leafblade
    {
        [Constructible]
        public TrueLeafblade() => WeaponAttributes.ResistPoisonBonus = 5;

        public TrueLeafblade(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073521; // true leafblade

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
