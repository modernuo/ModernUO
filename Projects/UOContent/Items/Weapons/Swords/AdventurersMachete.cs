using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class AdventurersMachete : ElvenMachete
    {
        [Constructible]
        public AdventurersMachete() => Attributes.Luck = 20;

        public override int LabelNumber => 1073533; // adventurer's machete
    }
}
