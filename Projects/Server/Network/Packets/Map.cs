using System;

namespace Server.Network
{
  public static partial class Packets
  {
    // TODO: Static Packet
    // TODO: Verify the size of this packet is correct through all eras
    public static WriteFixedPacketMethod MapPatches(out int length)
    {
      length = 33;

      static void write(Memory<byte> mem)
      {
        SpanWriter w = new SpanWriter(mem.Span, 33);
        w.Write((byte)0xBF); // Extended Packet ID
        w.Write((ushort)33); // Length

        w.Write((short)0x18);
        w.Write(4);

        w.Write(Map.Felucca.Tiles.Patch.StaticBlocks);
        w.Write(Map.Felucca.Tiles.Patch.LandBlocks);

        w.Write(Map.Trammel.Tiles.Patch.StaticBlocks);
        w.Write(Map.Trammel.Tiles.Patch.LandBlocks);

        w.Write(Map.Ilshenar.Tiles.Patch.StaticBlocks);
        w.Write(Map.Ilshenar.Tiles.Patch.LandBlocks);

        w.Write(Map.Malas.Tiles.Patch.StaticBlocks);
        w.Write(Map.Malas.Tiles.Patch.LandBlocks);
      }

      return write;
    }
  }
}
