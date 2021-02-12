namespace Server.Network
{
    public delegate void OnPacketReceive(NetState state, CircularBufferReader reader, ref int packetLength);

    public delegate bool ThrottlePacketCallback(int packetId, NetState state, out bool drop);

    public class PacketHandler
    {
        public PacketHandler(int packetID, int length, bool ingame, OnPacketReceive onReceive)
        {
            PacketID = packetID;
            Length = length;
            Ingame = ingame;
            OnReceive = onReceive;
        }

        public int PacketID { get; }

        public int Length { get; }

        public OnPacketReceive OnReceive { get; }

        public ThrottlePacketCallback ThrottleCallback { get; set; }

        public bool Ingame { get; }
    }
}
