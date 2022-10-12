using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseNecklace : BaseJewel
{
    public BaseNecklace(int itemID) : base(itemID, Layer.Neck)
    {
    }

    public override int BaseGemTypeNumber => 1044241; // star sapphire necklace
}

[SerializationGenerator(0, false)]
public partial class Necklace : BaseNecklace
{
    [Constructible]
    public Necklace() : base(0x1085) => Weight = 0.1;
}

[SerializationGenerator(0, false)]
public partial class GoldNecklace : BaseNecklace
{
    [Constructible]
    public GoldNecklace() : base(0x1088) => Weight = 0.1;
}

[SerializationGenerator(0, false)]
public partial class GoldBeadNecklace : BaseNecklace
{
    [Constructible]
    public GoldBeadNecklace() : base(0x1089) => Weight = 0.1;
}

[SerializationGenerator(0, false)]
public partial class SilverNecklace : BaseNecklace
{
    [Constructible]
    public SilverNecklace() : base(0x1F08) => Weight = 0.1;
}

[SerializationGenerator(0, false)]
public partial class SilverBeadNecklace : BaseNecklace
{
    [Constructible]
    public SilverBeadNecklace() : base(0x1F05) => Weight = 0.1;
}
