using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TormentedChains : Item
{
    [Constructible]
    public TormentedChains() : base(Utility.Random(6663, 2)) => Weight = 1.0;
}
