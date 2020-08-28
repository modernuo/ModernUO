namespace Server.Items
{
    public class AmeliasToolbox : TinkerTools
    {
        [Constructible]
        public AmeliasToolbox() : base(500)
        {
            LootType = LootType.Blessed;
            Hue = 1895; // TODO check
        }

        public AmeliasToolbox(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1077749; // Amelias Toolbox

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
