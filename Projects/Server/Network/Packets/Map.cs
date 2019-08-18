using System;

namespace Server.Network
{
  public static partial class Packets
  {
    private static byte[] m_MapPatchesPacket;

    public static void SendMapPatches(NetState ns)
    {
      if (m_MapPatchesPacket == null)
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

        m_MapPatchesPacket = CreateStaticPacket(w.Span, true);
      }

      ns.Send(m_MapPatchesPacket);
    }

    public static byte[][][] m_SeasonChangePackets = {
      new byte[2][], new byte[2][], new byte[2][], new byte[2][], new byte[2][],
    };

    public static void SendSeasonChange(NetState ns, byte season, byte playSound)
    {
      byte[] packet = m_SeasonChangePackets[season][playSound];
      if (packet == null)
      {
        ReadOnlySpan<byte> input = stackalloc byte[]
        {
          0xBC, // Packet ID
          season,
          playSound
        };

        m_SeasonChangePackets[season][playSound] = packet = CreateStaticPacket(input, true);
      }

      ns.Send(packet);
    }

    public static void SendMapChange(NetState ns, Map map)
    {
      ReadOnlySpan<byte> span = stackalloc byte[6]
      {
        0xBF, // Extended Packet ID
        0x00,
        0x06, // Length
        0x00,
        0x08, // Command
        (byte)(map?.MapID ?? 0)
      };

      ns.SendCompressed(span);
    }
  }
}
