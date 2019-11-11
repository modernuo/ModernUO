using Server.Buffers;

namespace Server.Network
{
  public static partial class Packets
  {
    private static byte[] m_MapPatchesPacket;

    public static void SendMapPatches(NetState ns)
    {
      if (ns == null)
        return;

      if (m_MapPatchesPacket == null)
      {
        SpanWriter w = new SpanWriter(m_MapPatchesPacket = new byte[57]);
        w.Write((byte)0xBF); // Extended Packet ID
        w.Write((ushort)57); // Dynamic Length

        w.Write((short)0x18); // Map Patches
        w.Write(6); // Map count

        w.Write(Map.Felucca.Tiles.Patch.StaticBlocks);
        w.Write(Map.Felucca.Tiles.Patch.LandBlocks);

        w.Write(Map.Trammel.Tiles.Patch.StaticBlocks);
        w.Write(Map.Trammel.Tiles.Patch.LandBlocks);

        w.Write(Map.Ilshenar.Tiles.Patch.StaticBlocks);
        w.Write(Map.Ilshenar.Tiles.Patch.LandBlocks);

        w.Write(Map.Malas.Tiles.Patch.StaticBlocks);
        w.Write(Map.Malas.Tiles.Patch.LandBlocks);

        w.Write(Map.Tokuno.Tiles.Patch.StaticBlocks);
        w.Write(Map.Tokuno.Tiles.Patch.LandBlocks);

        w.Write(Map.TerMur.Tiles.Patch.StaticBlocks);
        w.Write(Map.TerMur.Tiles.Patch.LandBlocks);
      }

      ns.Send(m_MapPatchesPacket);
    }

    private static readonly byte[][][] _seasonChangePackets = {
      new byte[2][], new byte[2][], new byte[2][], new byte[2][], new byte[2][]
    };

    public static void SendSeasonChange(NetState ns, byte season, byte playSound)
    {
      ns?.Send(_seasonChangePackets[season][playSound] ??= new byte[]
      {
        0xBC, // Packet ID
        season,
        playSound
      });
    }

    public static void SendMapChange(NetState ns, Map map)
    {
      ns?.Send(stackalloc byte[6]
      {
        0xBF, // Extended Packet ID
        0x00,
        0x06, // Length
        0x00,
        0x08, // Command
        (byte)(map?.MapID ?? 0)
      });
    }
  }
}
