using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(1, false)]
public abstract partial class BaseScales : Item, ICommodity
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CraftResource _resource;

    public BaseScales(CraftResource resource, int amount = 1) : base(0x26B4)
    {
        Stackable = true;
        Amount = amount;
        Hue = CraftResources.GetHue(resource);

        _resource = resource;
    }

    public override int LabelNumber => 1053139; // dragon scales

    public override double DefaultWeight => 0.1;

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;

    private void Deserialize(IGenericReader reader, int version)
    {
        _resource = (CraftResource)reader.ReadInt();
    }
}

[SerializationGenerator(0, false)]
public partial class RedScales : BaseScales
{
    [Constructible]
    public RedScales(int amount = 1) : base(CraftResource.RedScales, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class YellowScales : BaseScales
{
    [Constructible]
    public YellowScales(int amount = 1) : base(CraftResource.YellowScales, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BlackScales : BaseScales
{
    [Constructible]
    public BlackScales(int amount = 1) : base(CraftResource.BlackScales, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GreenScales : BaseScales
{
    [Constructible]
    public GreenScales(int amount = 1) : base(CraftResource.GreenScales, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class WhiteScales : BaseScales
{
    [Constructible]
    public WhiteScales(int amount = 1) : base(CraftResource.WhiteScales, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BlueScales : BaseScales
{
    [Constructible]
    public BlueScales(int amount = 1) : base(CraftResource.BlueScales, amount)
    {
    }

    public override int LabelNumber => 1053140; // sea serpent scales
}
