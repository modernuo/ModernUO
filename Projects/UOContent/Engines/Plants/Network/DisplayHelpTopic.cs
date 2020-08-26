namespace Server.Network
{
    public class DisplayHelpTopic : Packet
    {
        public DisplayHelpTopic(int topicID, bool display) : base(0xBF)
        {
            EnsureCapacity(11);

            Stream.Write((short)0x17);
            Stream.Write((byte)1);
            Stream.Write(topicID);
            Stream.Write(display);
        }
    }
}
