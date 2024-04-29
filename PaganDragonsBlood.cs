using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PaganDragonsBlood : BaseReagent
{
    [Constructible]
    public PaganDragonsBlood(int amount = 1) : base(0x4077, amount)
    {
    }
}
