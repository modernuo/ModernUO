using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class MysticalShortbow : MagicalShortbow
    {
        [Constructible]
        public MysticalShortbow()
        {
            Attributes.SpellChanneling = 1;
            Attributes.CastSpeed = -1;
        }

        public override int LabelNumber => 1073511; // mystical shortbow
    }
}
