using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class PeerlessKey : TransientItem
{
    [Constructible]
    public PeerlessKey(int itemID) : base(itemID, TimeSpan.FromDays( 7 ))
    {
    }

    public PeerlessKey(int itemID, TimeSpan expiry) : base(itemID, expiry)
    {
    }

    public override bool Nontransferable => false;

    public override bool DisplaySeconds => false;
}
