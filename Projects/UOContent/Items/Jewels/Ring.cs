using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseRing : BaseJewel
{
    public BaseRing(int itemID) : base(itemID, Layer.Ring)
    {
    }

    public override int BaseGemTypeNumber => 1044176; // star sapphire ring
}

[SerializationGenerator(0, false)]
public partial class GoldRing : BaseRing
{
    [Constructible]
    public GoldRing() : base(0x108a) => Weight = 0.1;
}

[SerializationGenerator(0, false)]
public partial class SilverRing : BaseRing
{
    [Constructible]
    public SilverRing() : base(0x1F09) => Weight = 0.1;
}
