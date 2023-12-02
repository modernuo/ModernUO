using ModernUO.Serialization;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Hold : Container
{
    [SerializableField(0, setter: "private")]
    private BaseBoat _boat;

    public Hold(BaseBoat boat) : base(0x3EAE)
    {
        _boat = boat;
        Movable = false;
    }

    public override bool IsDecoContainer => false;

    public void SetFacing(Direction dir)
    {
        ItemID = dir switch
        {
            Direction.East  => 0x3E65,
            Direction.West  => 0x3E93,
            Direction.North => 0x3EAE,
            Direction.South => 0x3EB9,
            _               => ItemID
        };
    }

    public override bool OnDragDrop(Mobile from, Item item) =>
        _boat?.Contains(from) == true && !_boat.IsMoving && base.OnDragDrop(from, item);

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p) =>
        _boat?.Contains(from) == true && !_boat.IsMoving && base.OnDragDropInto(from, item, p);

    public override bool CheckItemUse(Mobile from, Item item) =>
        (item == this || _boat?.Contains(from) == true && !_boat.IsMoving) && base.CheckItemUse(from, item);

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject) =>
        _boat?.Contains(from) == true && !_boat.IsMoving && base.CheckLift(from, item, ref reject);

    public override void OnAfterDelete()
    {
        _boat?.Delete();
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_boat?.Contains(from) != true)
        {
            _boat?.TillerMan?.Say(502490); // You must be on the ship to open the hold.
        }
        else if (_boat.IsMoving)
        {
            _boat.TillerMan?.Say(502491); // I can not open the hold while the ship is moving.
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_boat == null || Parent != null)
        {
            Timer.DelayCall(Delete);
        }
    }
}
