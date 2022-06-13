using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class RuneBladeOfKnowledge : RuneBlade
    {
        [Constructible]
        public RuneBladeOfKnowledge() => Attributes.SpellDamage = 5;

        public override int LabelNumber => 1073539; // rune blade of knowledge
    }
}
