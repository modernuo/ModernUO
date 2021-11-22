namespace Server.Items
{
    public enum DarkWoodWallTypes
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
        EastWallMedium,
        SouthWallMedium,
        CornerPostMedium,
        CornerShort,
        EastWallShort,
        SouthWallShort,
        CornerPostShort,
        SouthWallVShort,
        EastWallVShort
    }

    [Serializable(0, false)]
    public partial class DarkWoodWall : BaseWall
    {
        [Constructible]
        public DarkWoodWall(DarkWoodWallTypes type) : base(0x0006 + (int)type)
        {
        }
    }
}
