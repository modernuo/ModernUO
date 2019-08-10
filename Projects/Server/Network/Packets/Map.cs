namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendMapPatches(NetState ns)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(33));
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

      _ = ns.Flush(33);
    }
  }
}
