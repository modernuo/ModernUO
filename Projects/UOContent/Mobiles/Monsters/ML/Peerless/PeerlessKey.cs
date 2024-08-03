using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class PeerlessKey : Item
{
    [Constructible]
    public PeerlessKey( int itemID ) : base( itemID ) => LootType = LootType.Blessed;
}
