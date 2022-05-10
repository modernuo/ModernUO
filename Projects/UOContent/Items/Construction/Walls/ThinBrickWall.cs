using ModernUO.Serialization;

namespace Server.Items
{
    public enum ThinBrickWallTypes
    {
        Corner,
        SouthWall,
        EastWall,
        CornerPost,
        EastDoorFrame,
        SouthDoorFrame,
        WestDoorFrame,
        NorthDoorFrame,
        SouthWindow,
        EastWindow,
        CornerMedium,
        SouthWallMedium,
        EastWallMedium,
        CornerPostMedium,
        CornerShort,
        SouthWallShort,
        EastWallShort,
        CornerPostShort,
        CornerArch,
        SouthArch,
        WestArch,
        EastArch,
        NorthArch,
        SouthCenterArchTall,
        EastCenterArchTall,
        EastCornerArchTall,
        SouthCornerArchTall,
        SouthCornerArch,
        EastCornerArch,
        SouthCenterArch,
        EastCenterArch,
        CornerVVShort,
        SouthWallVVShort,
        EastWallVVShort,
        SouthWallVShort,
        EastWallVShort
    }

    [SerializationGenerator(0, false)]
    public partial class ThinBrickWall : BaseWall
    {
        [Constructible]
        public ThinBrickWall(ThinBrickWallTypes type) : base(0x0033 + (int)type)
        {
        }
    }
}
