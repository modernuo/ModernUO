using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseBracelet : BaseJewel
{
    public BaseBracelet(int itemID) : base(itemID, Layer.Bracelet)
    {
    }

    public override int BaseGemTypeNumber => 1044221; // star sapphire bracelet
}

[SerializationGenerator(0, false)]
public partial class GoldBracelet : BaseBracelet
{
    [Constructible]
    public GoldBracelet() : base(0x1086) => Weight = 0.1;
}

[SerializationGenerator(0, false)]
public partial class SilverBracelet : BaseBracelet
{
    [Constructible]
    public SilverBracelet() : base(0x1F06) => Weight = 0.1;
}
