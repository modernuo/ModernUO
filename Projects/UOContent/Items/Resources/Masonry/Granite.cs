using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(2, false)]
public abstract partial class BaseGranite : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CraftResource _resource;

    public BaseGranite(CraftResource resource) : base(0x1779)
    {
        Hue = CraftResources.GetHue(resource);
        Stackable = Core.ML;

        _resource = resource;
    }

    public override double DefaultWeight => Core.ML ? 1.0 : 10.0;

    public override int LabelNumber => 1044607; // high quality granite

    private void Deserialize(IGenericReader reader, int version)
    {
        _resource = (CraftResource)reader.ReadInt();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!CraftResources.IsStandard(_resource))
        {
            var num = CraftResources.GetLocalizationNumber(_resource);

            if (num > 0)
            {
                list.Add(num);
            }
            else
            {
                list.Add(CraftResources.GetName(_resource));
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class Granite : BaseGranite
{
    [Constructible]
    public Granite() : base(CraftResource.Iron)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DullCopperGranite : BaseGranite
{
    [Constructible]
    public DullCopperGranite() : base(CraftResource.DullCopper)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ShadowIronGranite : BaseGranite
{
    [Constructible]
    public ShadowIronGranite() : base(CraftResource.ShadowIron)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CopperGranite : BaseGranite
{
    [Constructible]
    public CopperGranite() : base(CraftResource.Copper)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BronzeGranite : BaseGranite
{
    [Constructible]
    public BronzeGranite() : base(CraftResource.Bronze)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GoldGranite : BaseGranite
{
    [Constructible]
    public GoldGranite() : base(CraftResource.Gold)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class AgapiteGranite : BaseGranite
{
    [Constructible]
    public AgapiteGranite() : base(CraftResource.Agapite)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class VeriteGranite : BaseGranite
{
    [Constructible]
    public VeriteGranite() : base(CraftResource.Verite)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ValoriteGranite : BaseGranite
{
    [Constructible]
    public ValoriteGranite() : base(CraftResource.Valorite)
    {
    }
}
