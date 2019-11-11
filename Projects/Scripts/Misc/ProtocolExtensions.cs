using System;
using Server.Network;

namespace Server.Misc
{
  public static class ProtocolExtensions
  {
    private static readonly PacketHandler[] m_Handlers = new PacketHandler[0x100];

    public static void Initialize()
    {
      PacketHandlers.Register(0xF0, 0, false, DecodeBundledPacket);
    }

    public static void Register(int packetID, bool ingame, OnPacketReceive onReceive)
    {
      m_Handlers[packetID] = new PacketHandler(packetID, 0, ingame, onReceive);
    }

    public static PacketHandler GetHandler(int packetID) => packetID >= 0 && packetID < m_Handlers.Length ? m_Handlers[packetID] : null;

    public static void DecodeBundledPacket(NetState state, PacketReader pvSrc)
    {
      int packetID = pvSrc.ReadByte();

      PacketHandler ph = GetHandler(packetID);
      if (ph == null)
        return;

      if (ph.Ingame)
      {
        if (state.Mobile == null)
        {
          Console.WriteLine(
            "Client: {0}: Sent ingame packet (0xF0x{1:X2}) before having been attached to a mobile", state, packetID);
          state.Dispose();
          return;
        }

        if (state.Mobile.Deleted)
        {
          state.Dispose();
          return;
        }
      }

      ph.OnReceive(state, pvSrc);
    }
  }
}
