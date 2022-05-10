using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SpellbladeOfDefense : ElvenSpellblade
    {
        [Constructible]
        public SpellbladeOfDefense() => Attributes.DefendChance = 5;

        public override int LabelNumber => 1073516; // spellblade of defense
    }
}
