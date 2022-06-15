using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseEarrings : BaseJewel
{
    public BaseEarrings(int itemID) : base(itemID, Layer.Earrings)
    {
    }

    public override int BaseGemTypeNumber => 1044203; // star sapphire earrings
}

[SerializationGenerator(0, false)]
public partial class GoldEarrings : BaseEarrings
{
    [Constructible]
    public GoldEarrings() : base(0x1087) => Weight = 0.1;
}

[SerializationGenerator(0, false)]
public partial class SilverEarrings : BaseEarrings
{
    [Constructible]
    public SilverEarrings() : base(0x1F07) => Weight = 0.1;
}
