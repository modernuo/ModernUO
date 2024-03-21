using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpidersSilk : BaseReagent
{
    [Constructible]
    public SpidersSilk(int amount = 1) : base(0xF8D, amount)
    {
    }
}
