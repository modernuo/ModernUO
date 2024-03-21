using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GraveDust : BaseReagent
{
    [Constructible]
    public GraveDust(int amount = 1) : base(0xF8F, amount)
    {
    }
}
