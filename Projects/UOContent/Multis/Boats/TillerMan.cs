using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TillerMan : Item
{
    [SerializableField(0, setter: "private")]
    private BaseBoat _boat;

    public TillerMan(BaseBoat boat) : base(0x3E4E)
    {
        _boat = boat;
        Movable = false;
    }

    public void SetFacing(Direction dir)
    {
        ItemID = dir switch
        {
            Direction.South => 0x3E4B,
            Direction.North => 0x3E4E,
            Direction.West  => 0x3E50,
            Direction.East  => 0x3E55,
            _               => ItemID
        };
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(_boat.Status);
    }

    public void Say(int number)
    {
        PublicOverheadMessage(MessageType.Regular, 0x3B2, number);
    }

    public void Say(int number, string args)
    {
        PublicOverheadMessage(MessageType.Regular, 0x3B2, number, args);
    }

    public override void AddNameProperty(IPropertyList list)
    {
        if (_boat?.ShipName != null)
        {
            list.Add(1042884, _boat.ShipName); // the tiller man of the ~1_SHIP_NAME~
        }
        else
        {
            base.AddNameProperty(list);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (_boat?.ShipName != null)
        {
            LabelTo(from, 1042884, _boat.ShipName); // the tiller man of the ~1_SHIP_NAME~
        }
        else
        {
            base.OnSingleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_boat?.Contains(from) == true)
        {
            _boat.BeginRename(from);
        }
        else
        {
            _boat?.BeginDryDock(from);
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is MapItem item && _boat?.CanCommand(from) == true && _boat.Contains(from))
        {
            _boat.AssociateMap(item);
        }

        return false;
    }

    public override void OnAfterDelete() => _boat?.Delete();

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_boat == null)
        {
            Timer.DelayCall(Delete);
        }
    }
}
