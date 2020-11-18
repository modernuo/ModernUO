namespace Server.Network
{
    public sealed class MapPatches : Packet
    {
        public MapPatches() : base(0xBF)
        {
            EnsureCapacity(9 + 4 * 8);

            Stream.Write((short)0x18);

            Stream.Write(4);

            Stream.Write(Map.Felucca.Tiles.Patch.StaticBlocks);
            Stream.Write(Map.Felucca.Tiles.Patch.LandBlocks);

            Stream.Write(Map.Trammel.Tiles.Patch.StaticBlocks);
            Stream.Write(Map.Trammel.Tiles.Patch.LandBlocks);

            Stream.Write(Map.Ilshenar.Tiles.Patch.StaticBlocks);
            Stream.Write(Map.Ilshenar.Tiles.Patch.LandBlocks);

            Stream.Write(Map.Malas.Tiles.Patch.StaticBlocks);
            Stream.Write(Map.Malas.Tiles.Patch.LandBlocks);
        }
    }

    public sealed class InvalidMapEnable : Packet
    {
        public InvalidMapEnable() : base(0xC6, 1)
        {
        }
    }

    public sealed class MapChange : Packet
    {
        public MapChange(Map map) : base(0xBF)
        {
            EnsureCapacity(6);

            Stream.Write((short)0x08);
            Stream.Write((byte)map.MapID);
        }
    }
}
