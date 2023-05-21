using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1F03, 0x1F04)]
[SerializationGenerator(0)]
public partial class RobeOfTheEquinox : BaseOuterTorso
{
    [Constructible]
    public RobeOfTheEquinox() : base(0x1F04, 0xD6)
    {
        Weight = 3.0;

        Attributes.Luck = 95;

        // TODO: Supports arcane?
        // TODO: Elves Only
    }

    public override int LabelNumber => 1075042; // Robe of the Equinox
}
