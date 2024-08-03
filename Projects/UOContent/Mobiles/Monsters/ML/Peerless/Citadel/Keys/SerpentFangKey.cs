using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class SerpentFangKey : PeerlessKey
{
    [Constructible]
    public SerpentFangKey()
        : base( 0x2002 )
    {
        Weight = 2.0;
        Hue = 53;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074341; // serpent fang key
}
