namespace Server.Network
{
    public delegate void OnEncodedPacketReceive(NetState state, IEntity ent, EncodedReader reader);

    public class EncodedPacketHandler
    {
        public EncodedPacketHandler(int packetID, bool ingame, OnEncodedPacketReceive onReceive)
        {
            PacketID = packetID;
            Ingame = ingame;
            OnReceive = onReceive;
        }

        public int PacketID { get; }

        public OnEncodedPacketReceive OnReceive { get; }

        public bool Ingame { get; }
    }
}
