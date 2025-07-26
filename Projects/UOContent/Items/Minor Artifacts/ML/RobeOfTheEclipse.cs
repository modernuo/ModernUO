using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1F03, 0x1F04)]
[SerializationGenerator(0)]
public partial class RobeOfTheEclipse : BaseOuterTorso
{
    [Constructible]
    public RobeOfTheEclipse() : base(0x1F03, 0x486)
    {
        Attributes.Luck = 95;

        // TODO: Supports arcane?
    }

    public override double DefaultWeight => 3.0;

    public override int LabelNumber => 1075082; // Robe of the Eclipse
}
