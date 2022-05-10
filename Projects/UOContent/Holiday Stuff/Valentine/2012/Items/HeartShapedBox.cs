using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x49CA, 0x49CB)]
    public partial class HeartShapedBox : BaseContainer
    {
        private static int m_DropSound;

        [Constructible]
        public HeartShapedBox() : base(0x49CA)
        {
        }

        public override int DefaultDropSound => m_DropSound;

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            PrepareSound(from);
            return base.OnDragDropInto(from, item, p);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            PrepareSound(from);
            return base.OnDragDrop(from, dropped);
        }

        private static void PrepareSound(Mobile from)
        {
            m_DropSound = from.Female ? 0x430 : 0x320;
        }
    }
}
