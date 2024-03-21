using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BlackPearl : BaseReagent
{
    [Constructible]
    public BlackPearl(int amount = 1) : base(0xF7A, amount)
    {
    }
}
