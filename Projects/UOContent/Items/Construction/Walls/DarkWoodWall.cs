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

    public class DarkWoodWall : BaseWall
    {
        [Constructible]
        public DarkWoodWall(DarkWoodWallTypes type) : base(0x0006 + (int)type)
        {
        }

        public DarkWoodWall(Serial serial) : base(serial)
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
