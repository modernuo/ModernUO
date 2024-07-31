using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator( 1 )]
public partial class ParrotPerchAddon : BaseAddon
{
    [Constructible]
    public ParrotPerchAddon()
    {
        AddComponent( new AddonComponent( 0x2FF4 ), 0, 0, 0 );
    }

    [SerializableProperty( 0 )]
    public BaseCreature Parrot { get; set; }

    public override BaseAddonDeed Deed => new ParrotPerchDeed();

    private void MigrateFrom( V0Content content )
    {
    }
}

[SerializationGenerator( 0 )]
public partial class ParrotPerchDeed : BaseAddonDeed
{
    [Constructible]
    public ParrotPerchDeed()
    {
    }

    public override BaseAddon Addon => new ParrotPerchAddon();
    public override int LabelNumber => 1072617; // parrot perch
}
