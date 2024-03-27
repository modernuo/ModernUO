using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bloodmoss : BaseReagent
{
    [Constructible]
    public Bloodmoss(int amount = 1) : base(0xF7B, amount)
    {
    }
}
