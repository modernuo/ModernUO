using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1BD7, 0x1BDA)]
[SerializationGenerator(4, false)]
public partial class Board : Item, ICommodity
{
    [InvalidateProperties]
    [SerializableField(0)]
    private CraftResource _resource;

    [Constructible]
    public Board(int amount = 1) : this(CraftResource.RegularWood, amount)
    {
    }

    [Constructible]
    public Board(CraftResource resource, int amount = 1) : base(0x1BD7)
    {
        Stackable = true;
        Amount = amount;

        _resource = resource;
        Hue = CraftResources.GetHue(resource);
    }

    int ICommodity.DescriptionNumber
    {
        get
        {
            if (_resource >= CraftResource.OakWood && _resource <= CraftResource.YewWood)
            {
                return 1075052 + ((int)_resource - (int)CraftResource.OakWood);
            }

            return _resource switch
            {
                CraftResource.Bloodwood => 1075055,
                CraftResource.Frostwood => 1075056,
                CraftResource.Heartwood => 1075062, // WHY Osi.  Why?
                _                       => LabelNumber
            };
        }
    }

    bool ICommodity.IsDeedable => true;

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

    private void Deserialize(IGenericReader reader, int version)
    {
        _resource = (CraftResource)reader.ReadInt();
    }
}

[SerializationGenerator(0, false)]
public partial class HeartwoodBoard : Board
{
    [Constructible]
    public HeartwoodBoard(int amount = 1) : base(CraftResource.Heartwood, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BloodwoodBoard : Board
{
    [Constructible]
    public BloodwoodBoard(int amount = 1) : base(CraftResource.Bloodwood, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class FrostwoodBoard : Board
{
    [Constructible]
    public FrostwoodBoard(int amount = 1) : base(CraftResource.Frostwood, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class OakBoard : Board
{
    [Constructible]
    public OakBoard(int amount = 1) : base(CraftResource.OakWood, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class AshBoard : Board
{
    [Constructible]
    public AshBoard(int amount = 1) : base(CraftResource.AshWood, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class YewBoard : Board
{
    [Constructible]
    public YewBoard(int amount = 1) : base(CraftResource.YewWood, amount)
    {
    }
}
