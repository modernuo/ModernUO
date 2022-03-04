using System;
using System.Collections.Generic;
using Server.Multis;

namespace Server.Items;

[Flippable(0xE80, 0x9A8)]
[Serializable(0, false)]
public partial class StrongBox : BaseContainer, IChoppable
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private Mobile _owner;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private BaseHouse _house;

    public StrongBox(Mobile owner, BaseHouse house) : base(0xE80)
    {
        _owner = owner;
        _house = house;

        MaxItems = 25;
    }

    public override double DefaultWeight => 100;
    public override int LabelNumber => 1023712;

    public override int DefaultMaxWeight => 0;

    public override bool Decays => _house == null || _owner?.Deleted != false || !_house.IsCoOwner(_owner);

    public override TimeSpan DecayTime => TimeSpan.FromMinutes(30.0);

    public void OnChop(Mobile from)
    {
        if (_house?.Deleted != false || _owner?.Deleted != false || from == _owner || _house.IsOwner(from))
        {
            Chop(from);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), Validate);
    }

    private void Validate()
    {
        if (_owner != null && _house?.IsCoOwner(_owner) == false)
        {
            Console.WriteLine("Warning: Destroying strongbox of {0}", _owner.Name);
            Destroy();
        }
    }

    public override void AddNameProperty(ObjectPropertyList list)
    {
        if (_owner != null)
        {
            list.Add(1042887, _owner.Name); // a strong box owned by ~1_OWNER_NAME~
        }
        else
        {
            base.AddNameProperty(list);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (_owner == null)
        {
            base.OnSingleClick(from);
            return;
        }

        LabelTo(from, 1042887, _owner.Name); // a strong box owned by ~1_OWNER_NAME~

        if (CheckContentDisplay(from))
        {
            LabelTo(from, "({0} items, {1} stones)", TotalItems, TotalWeight);
        }
    }

    public override bool IsAccessibleTo(Mobile m) =>
        _owner?.Deleted != false || _house?.Deleted != false ||
        m.AccessLevel >= AccessLevel.GameMaster ||
        m == _owner && _house.IsCoOwner(m) && base.IsAccessibleTo(m);

    private void Chop(Mobile from)
    {
        Effects.PlaySound(Location, Map, 0x3B3);
        from.SendLocalizedMessage(500461); // You destroy the item.
        Destroy();
    }

    public Container ConvertToStandardContainer()
    {
        Container metalBox = new MetalBox();
        var subItems = new List<Item>(Items);

        foreach (var subItem in subItems)
        {
            metalBox.AddItem(subItem);
        }

        Delete();

        return metalBox;
    }
}
