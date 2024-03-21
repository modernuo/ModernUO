using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Garlic : BaseReagent
{
    [Constructible]
    public Garlic(int amount = 1) : base(0xF84, amount)
    {
    }
}
