using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class TrueSpellblade : ElvenSpellblade
    {
        [Constructible]
        public TrueSpellblade()
        {
            Attributes.SpellChanneling = 1;
            Attributes.CastSpeed = -1;
        }

        public override int LabelNumber => 1073513; // true spellblade
    }
}
