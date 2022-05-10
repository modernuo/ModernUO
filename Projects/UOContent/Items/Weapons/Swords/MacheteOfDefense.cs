using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class MacheteOfDefense : ElvenMachete
    {
        [Constructible]
        public MacheteOfDefense() => Attributes.DefendChance = 5;

        public override int LabelNumber => 1073535; // machete of defense
    }
}
