using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(2, false)]
public abstract partial class BaseLeather : Item, ICommodity
{
    public BaseLeather(CraftResource resource, int amount = 1) : base(0x1081)
    {
        Stackable = true;
        Amount = amount;
        Hue = CraftResources.GetHue(resource);
        _resource = resource;
    }

    public override double DefaultWeight => 1.0;

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CraftResource Resource
    {
        get => _resource;
        set
        {
            if (_resource != value)
            {
                _resource = value;
                Hue = CraftResources.GetHue(value);

                InvalidateProperties();
                this.MarkDirty();
            }
        }
    }

    public override int LabelNumber
    {
        get
        {
            if (_resource >= CraftResource.SpinedLeather && _resource <= CraftResource.BarbedLeather)
            {
                return 1049684 + (_resource - CraftResource.SpinedLeather);
            }

            return 1047022;
        }
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;

    private void Deserialize(IGenericReader reader, int version)
    {
        _resource = (CraftResource)reader.ReadInt();
    }

    public override void AddNameProperty(IPropertyList list)
    {
        if (Amount > 1)
        {
            list.Add(1050039, $"{Amount}\t{1024199:#}"); // ~1_NUMBER~ ~2_ITEMNAME~
        }
        else
        {
            list.Add(1024199); // cut leather
        }
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
[Flippable(0x1081, 0x1082)]
public partial class Leather : BaseLeather
{
    [Constructible]
    public Leather(int amount = 1) : base(CraftResource.RegularLeather, amount)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(0x1081, 0x1082)]
public partial class SpinedLeather : BaseLeather
{
    [Constructible]
    public SpinedLeather(int amount = 1) : base(CraftResource.SpinedLeather, amount)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(0x1081, 0x1082)]
public partial class HornedLeather : BaseLeather
{
    [Constructible]
    public HornedLeather(int amount = 1) : base(CraftResource.HornedLeather, amount)
    {
    }
}

[SerializationGenerator(0, false)]
[Flippable(0x1081, 0x1082)]
public partial class BarbedLeather : BaseLeather
{
    [Constructible]
    public BarbedLeather(int amount = 1) : base(CraftResource.BarbedLeather, amount)
    {
    }
}
