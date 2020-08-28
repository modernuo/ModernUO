namespace Server.Items
{
    [Flippable(0x49CA, 0x49CB)]
    public class HeartShapedBox : BaseContainer
    {
        private static int m_DropSound;

        [Constructible]
        public HeartShapedBox()
            : base(0x49CA)
        {
        }

        public HeartShapedBox(Serial serial)
            : base(serial)
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
