using ModernUO.Serialization;

namespace Server.Items
{
    public enum ThinStoneWallTypes
    {
        Corner,
        EastWall,
        SouthWall,
        CornerPost,
        EastDoorFrame,
        SouthDoorFrame,
        NorthDoorFrame,
        WestDoorFrame,
        SouthWindow,
        EastWindow,
        CornerMedium,
        SouthWallMedium,
        EastWallMedium,
        CornerPostMedium,
        CornerArch,
        EastArch,
        SouthArch,
        NorthArch,
        WestArch,
        CornerShort,
        EastWallShort,
        SouthWallShort,
        CornerPostShort,
        SouthWallShort2,
        EastWallShort2
    }

    [SerializationGenerator(0, false)]
    public partial class ThinStoneWall : BaseWall
    {
        [Constructible]
        public ThinStoneWall(ThinStoneWallTypes type) : base(0x001A + (int)type)
        {
        }
    }
}
