using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1F03, 0x1F04)]
[SerializationGenerator(1)]
public partial class RobeOfTheEquinox : BaseOuterTorso, ICanBeElfOrHuman
{
    [Constructible]
    public RobeOfTheEquinox() : base(0x1F04, 0xD6)
    {
        Weight = 3.0;

        Attributes.Luck = 95;

        // TODO: Supports arcane?
    }

    [SerializableField(0)]
    private bool _elfOnly = true;
    public override int RequiredRaces => _elfOnly ? Race.AllowElvesOnly : Race.AllowHumanOrElves;

    public override int LabelNumber => 1075042; // Robe of the Equinox

    private void MigrateFrom(V0Content content)
    {
        ElfOnly = true;
    }
}
