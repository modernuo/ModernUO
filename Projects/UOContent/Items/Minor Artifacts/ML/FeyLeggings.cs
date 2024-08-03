using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 1 )]
public partial class FeyLeggings : ChainLegs, ICanBeElfOrHuman
{
    [SerializableField( 0 )] private bool _elfOnly = true;

    [Constructible]
    public FeyLeggings()
    {
        Attributes.BonusHits = 6;
        Attributes.DefendChance = 20;

        ArmorAttributes.MageArmor = 1;
    }

    [Constructible]
    public FeyLeggings( bool human ) : this() => ElfOnly = !human;

    public override int LabelNumber => 1075041; // Fey Leggings

    public override int BasePhysicalResistance => 12;
    public override int BaseFireResistance => 8;
    public override int BaseColdResistance => 7;
    public override int BasePoisonResistance => 4;
    public override int BaseEnergyResistance => 19;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int RequiredRaces => _elfOnly ? Race.AllowElvesOnly : Race.AllowHumanOrElves;

    private void MigrateFrom(V0Content content)
    {
        ElfOnly = true;
    }
}
