/****************************************
 * NAME    : Thick Gray Stone Wall      *
 * SCRIPT  : ThickGrayStoneWall.cs      *
 * VERSION : v1.00                      *
 * CREATOR : Mans Sjoberg (Allmight)    *
 * CREATED : 10-07.2002                 *
 * **************************************/

using ModernUO.Serialization;

namespace Server.Items
{
    public enum ThickGrayStoneWallTypes
    {
        WestArch,
        NorthArch,
        SouthArchTop,
        EastArchTop,
        EastArch,
        SouthArch,
        Wall1,
        Wall2,
        Wall3,
        SouthWindow,
        Wall4,
        EastWindow,
        WestArch2,
        NorthArch2,
        SouthArchTop2,
        EastArchTop2,
        EastArch2,
        SouthArch2,
        SWArchEdge2,
        SouthWindow2,
        NEArchEdge2,
        EastWindow2
    }

    [SerializationGenerator(0, false)]
    public partial class ThickGrayStoneWall : BaseWall
    {
        [Constructible]
        public ThickGrayStoneWall(ThickGrayStoneWallTypes type) : base(0x007A + (int)type)
        {
        }
    }
}
