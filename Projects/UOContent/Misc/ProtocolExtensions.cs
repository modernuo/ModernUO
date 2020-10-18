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

        public static PacketHandler GetHandler(int packetID) =>
            packetID >= 0 && packetID < m_Handlers.Length ? m_Handlers[packetID] : null;

        public static void DecodeBundledPacket(NetState state, PacketReader reader)
        {
            int packetID = reader.ReadByte();

            var ph = GetHandler(packetID);

            if (ph != null)
            {
                if (ph.Ingame && state.Mobile == null)
                {
                    state.WriteConsole(
                        "Sent ingame packet (0xF0x{0:X2}) before having been attached to a mobile",
                        packetID
                    );
                    state.Dispose();
                }
                else if (ph.Ingame && state.Mobile.Deleted)
                {
                    state.Dispose();
                }
                else
                {
                    ph.OnReceive(state, reader);
                }
            }
        }
    }

    public abstract class ProtocolExtension : Packet
    {
        public ProtocolExtension(int packetID, int capacity) : base(0xF0)
        {
            EnsureCapacity(4 + capacity);

            Stream.Write((byte)packetID);
        }
    }
}
