/****************************************
 * NAME    : White Stone Wall           *
 * SCRIPT  : WhiteStoneWall.cs          *
 * VERSION : v1.00                      *
 * CREATOR : Mans Sjoberg (Allmight)    *
 * CREATED : 10-07.2002                 *
 * **************************************/

namespace Server.Items
{
    public enum WhiteStoneWallTypes
    {
        EastWall,
        SouthWall,
        SECorner,
        NWCornerPost,
        EastArrowLoop,
        SouthArrowLoop,
        EastWindow,
        SouthWindow,
        SouthWallMedium,
        EastWallMedium,
        SECornerMedium,
        NWCornerPostMedium,
        SouthWallShort,
        EastWallShort,
        SECornerShort,
        NWCornerPostShort,
        NECornerPostShort,
        SWCornerPostShort,
        SouthWallVShort,
        EastWallVShort,
        SECornerVShort,
        NWCornerPostVShort,
        SECornerArch,
        SouthArch,
        WestArch,
        EastArch,
        NorthArch,
        EastBattlement,
        SECornerBattlement,
        SouthBattlement,
        NECornerBattlement,
        SWCornerBattlement,
        Column,
        SouthWallVVShort,
        EastWallVVShort
    }

    [Serializable(0, false)]
    public partial class WhiteStoneWall : BaseWall
    {
        [Constructible]
        public WhiteStoneWall(WhiteStoneWallTypes type) : base(0x0057 + (int)type)
        {
        }
    }
}
