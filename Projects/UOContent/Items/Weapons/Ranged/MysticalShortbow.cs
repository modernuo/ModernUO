namespace Server.Items
{
    public class MysticalShortbow : MagicalShortbow
    {
        [Constructible]
        public MysticalShortbow()
        {
            Attributes.SpellChanneling = 1;
            Attributes.CastSpeed = -1;
        }

        public MysticalShortbow(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073511; // mystical shortbow

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
