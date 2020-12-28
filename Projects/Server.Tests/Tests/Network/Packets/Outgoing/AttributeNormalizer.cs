namespace Server.Network
{
    public static class AttributeNormalizer
    {
        public static int Maximum { get; set; } = 100;

        public static bool Enabled { get; set; } = true;

        public static void Write(PacketWriter stream, int cur, int max)
        {
            if (Enabled && max != 0)
            {
                stream.Write((short)Maximum);
                stream.Write((short)(cur * Maximum / max));
            }
            else
            {
                stream.Write((short)max);
                stream.Write((short)cur);
            }
        }

        public static void WriteReverse(PacketWriter stream, int cur, int max)
        {
            if (Enabled && max != 0)
            {
                stream.Write((short)(cur * Maximum / max));
                stream.Write((short)Maximum);
            }
            else
            {
                stream.Write((short)cur);
                stream.Write((short)max);
            }
        }
    }
}
