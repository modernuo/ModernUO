using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpeckledPoisonSac : TransientItem
{
    [Constructible]
    public SpeckledPoisonSac() : base(0x23A, TimeSpan.FromHours(1)) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073133; // Speckled Poison Sac
}
