using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class DragonFlameKey : PeerlessKey
{
    [Constructible]
    public DragonFlameKey()
        : base( 0x2002 )
    {
        Weight = 2.0;
        Hue = 42;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074343; // dragon flame key
}
