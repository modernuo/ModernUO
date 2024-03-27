using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DragonsBlood : BaseReagent
{
    [Constructible]
    public DragonsBlood(int amount = 1) : base(0x4077, amount)
    {
    }

    public override bool IsDeedable => Core.ML;
}
