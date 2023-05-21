using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(2, false)]
[Flippable(0x1bdd, 0x1be0)]
public partial class Log : Item, ICommodity, IAxe
{
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [SerializableField(0)]
    private CraftResource _resource;

    [Constructible]
    public Log(int amount = 1) : this(CraftResource.RegularWood, amount)
    {
    }

    [Constructible]
    public Log(CraftResource resource) : this(resource, 1)
    {
    }

    [Constructible]
    public Log(CraftResource resource, int amount) : base(0x1BDD)
    {
        Stackable = true;
        Weight = 2.0;
        Amount = amount;

        _resource = resource;
        Hue = CraftResources.GetHue(resource);
    }

    public virtual bool Axe(Mobile from, BaseAxe axe) => TryCreateBoards(from, 0, new Board());

    int ICommodity.DescriptionNumber => CraftResources.IsStandard(_resource)
        ? LabelNumber
        : 1075062 + ((int)_resource - (int)CraftResource.RegularWood);

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
        _resource = version switch
        {
            1 => (CraftResource)reader.ReadInt(),
            _ => CraftResource.RegularWood
        };
    }

    public virtual bool TryCreateBoards(Mobile from, double skill, Item item)
    {
        if (Deleted || !from.CanSee(this))
        {
            return false;
        }

        if (from.Skills.Carpentry.Value < skill &&
            from.Skills.Lumberjacking.Value < skill)
        {
            item.Delete();
            from.SendLocalizedMessage(1072652); // You cannot work this strange and unusual wood.
            return false;
        }

        ScissorHelper(from, item, 1, false);
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class HeartwoodLog : Log
{
    [Constructible]
    public HeartwoodLog(int amount = 1) : base(CraftResource.Heartwood, amount)
    {
    }

    public override bool Axe(Mobile from, BaseAxe axe) => TryCreateBoards(from, 100, new HeartwoodBoard());
}

[SerializationGenerator(0, false)]
public partial class BloodwoodLog : Log
{
    [Constructible]
    public BloodwoodLog(int amount = 1) : base(CraftResource.Bloodwood, amount)
    {
    }

    public override bool Axe(Mobile from, BaseAxe axe) => TryCreateBoards(from, 100, new BloodwoodBoard());
}

[SerializationGenerator(0, false)]
public partial class FrostwoodLog : Log
{
    [Constructible]
    public FrostwoodLog(int amount = 1) : base(CraftResource.Frostwood, amount)
    {
    }

    public override bool Axe(Mobile from, BaseAxe axe) => TryCreateBoards(from, 100, new FrostwoodBoard());
}

[SerializationGenerator(0, false)]
public partial class OakLog : Log
{
    [Constructible]
    public OakLog(int amount = 1) : base(CraftResource.OakWood, amount)
    {
    }

    public override bool Axe(Mobile from, BaseAxe axe) => TryCreateBoards(from, 65, new OakBoard());
}

[SerializationGenerator(0, false)]
public partial class AshLog : Log
{
    [Constructible]
    public AshLog(int amount = 1) : base(CraftResource.AshWood, amount)
    {
    }

    public override bool Axe(Mobile from, BaseAxe axe) => TryCreateBoards(from, 80, new AshBoard());
}

[SerializationGenerator(0, false)]
public partial class YewLog : Log
{
    [Constructible]
    public YewLog(int amount = 1) : base(CraftResource.YewWood, amount)
    {
    }

    public override bool Axe(Mobile from, BaseAxe axe) => TryCreateBoards(from, 95, new YewBoard());
}
