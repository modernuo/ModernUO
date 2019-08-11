using System;

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

    public static void SendSeasonChange(NetState ns, byte season, bool playSound)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(3));
      w.Write((byte)0xBC); // Packet ID
      w.Write(season); // Length
      w.Write(playSound);

      _ = ns.Flush(3);
    }

    public static void SendMapChange(NetState ns, Map map)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(6);
      span[0] = 0xBF; // Extended Packet ID
      span[2] = 0x06; // Length
      span[4] = 0x08; // Command
      span[5] = (byte)(map?.MapID ?? 0);

      _ = ns.Flush(6);
    }
  }
}
