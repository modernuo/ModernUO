using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Nightshade : BaseReagent
{
    [Constructible]
    public Nightshade(int amount = 1) : base(0xF88, amount)
    {
    }
}
