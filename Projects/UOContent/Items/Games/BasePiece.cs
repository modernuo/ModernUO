using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BasePiece : Item
{
    [SerializableField(0)]
    private BaseBoard _board;

    public BasePiece(int itemID, BaseBoard board) : base(itemID) => _board = board;

    public override bool IsVirtualItem => true;

    public override bool CanTarget => false;

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (Board == null || Parent == null)
        {
            Delete();
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (Board?.Deleted != false)
        {
            Delete();
        }
        else if (!IsChildOf(Board))
        {
            Board.DropItem(this);
        }
        else
        {
            base.OnSingleClick(from);
        }
    }

    public override bool OnDragLift(Mobile from)
    {
        if (Board?.Deleted != false)
        {
            Delete();
            return false;
        }

        if (!IsChildOf(Board))
        {
            Board.DropItem(this);
            return false;
        }

        return true;
    }

    public override bool DropToMobile(Mobile from, Mobile target, Point3D p) => false;

    public override bool DropToItem(Mobile from, Item target, Point3D p) =>
        target == Board && p.X != -1 && p.Y != -1 && base.DropToItem(from, target, p);

    public override bool DropToWorld(Mobile from, Point3D p) => false;

    public override int GetLiftSound(Mobile from) => -1;
}
