using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class PaperSlidingDoor : BaseDoor
    {
        [Constructible]
        public PaperSlidingDoor(DoorFacing facing)
            : base(0x2A05 + (2 * (int)facing), 0x2A06 + (2 * (int)facing), 0x539, 0x539, new Point3D(0, 0, 0))
        {
        }
    }

    [SerializationGenerator(0, false)]
    public partial class ClothSlidingDoor : BaseDoor
    {
        [Constructible]
        public ClothSlidingDoor(DoorFacing facing)
            : base(0x2A0D + (2 * (int)facing), 0x2A0E + (2 * (int)facing), 0x539, 0x539, new Point3D(0, 0, 0))
        {
        }
    }

    [SerializationGenerator(0, false)]
    public partial class WoodenSlidingDoor : BaseDoor
    {
        [Constructible]
        public WoodenSlidingDoor(DoorFacing facing)
            : base(0x2A15 + (2 * (int)facing), 0x2A16 + (2 * (int)facing), 0x539, 0x539, new Point3D(0, 0, 0))
        {
        }
    }
}
