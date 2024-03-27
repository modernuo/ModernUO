using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DaemonBlood : BaseReagent
{
    [Constructible]
    public DaemonBlood(int amount = 1) : base(0xF7D, amount)
    {
    }
}
