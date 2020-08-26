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

    public class ThinBrickWall : BaseWall
    {
        [Constructible]
        public ThinBrickWall(ThinBrickWallTypes type) : base(0x0033 + (int)type)
        {
        }

        public ThinBrickWall(Serial serial) : base(serial)
        {
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
