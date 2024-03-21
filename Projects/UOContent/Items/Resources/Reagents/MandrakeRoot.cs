using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MandrakeRoot : BaseReagent
{
    [Constructible]
    public MandrakeRoot(int amount = 1) : base(0xF86, amount)
    {
    }
}
