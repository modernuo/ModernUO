using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xE41, 0xE40)]
[SerializationGenerator(0, false)]
public partial class TrashChest : Container
{
    [Constructible]
    public TrashChest() : base(0xE41) => Movable = false;

    public override int DefaultMaxWeight => 0; // A value of 0 signals unlimited weight

    public override bool IsDecoContainer => false;

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (base.OnDragDrop(from, dropped))
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.Random(1042891, 8));
            dropped.Delete();
            return true;
        }

        return false;
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (base.OnDragDropInto(from, item, p))
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.Random(1042891, 8));
            item.Delete();
            return true;
        }

        return false;
    }
}
