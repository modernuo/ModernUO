namespace Server.Network
{
    public sealed class GlobalLightLevel : Packet
    {
        private static readonly GlobalLightLevel[] m_Cache = new GlobalLightLevel[0x100];

        public GlobalLightLevel(int level) : base(0x4F, 2)
        {
            Stream.Write((sbyte)level);
        }

        public static GlobalLightLevel Instantiate(int level)
        {
            var lvl = (byte)level;
            var p = m_Cache[lvl];

            if (p == null)
            {
                m_Cache[lvl] = p = new GlobalLightLevel(level);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class PersonalLightLevel : Packet
    {
        public PersonalLightLevel(Serial mobile, int level = 0) : base(0x4E, 6)
        {
            Stream.Write(mobile);
            Stream.Write((sbyte)level);
        }
    }
}
