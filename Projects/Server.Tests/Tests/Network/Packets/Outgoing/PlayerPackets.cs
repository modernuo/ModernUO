namespace Server.Network
{
    public sealed class StatLockInfo : Packet
    {
        public StatLockInfo(Mobile m) : base(0xBF)
        {
            EnsureCapacity(12);

            Stream.Write((short)0x19);
            Stream.Write((byte)2);
            Stream.Write(m.Serial);
            Stream.Write((byte)0);

            var lockBits = ((int)m.StrLock << 4) | ((int)m.DexLock << 2) | (int)m.IntLock;

            Stream.Write((byte)lockBits);
        }
    }

    public sealed class ChangeUpdateRange : Packet
    {
        private static readonly ChangeUpdateRange[] m_Cache = new ChangeUpdateRange[0x100];

        public ChangeUpdateRange(int range) : base(0xC8, 2)
        {
            Stream.Write((byte)range);
        }

        public static ChangeUpdateRange Instantiate(int range)
        {
            var idx = (byte)range;
            var p = m_Cache[idx];

            if (p == null)
            {
                m_Cache[idx] = p = new ChangeUpdateRange(range);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class DeathStatus : Packet
    {
        public static readonly Packet Dead = SetStatic(new DeathStatus(true));
        public static readonly Packet Alive = SetStatic(new DeathStatus(false));

        public DeathStatus(bool dead) : base(0x2C, 2)
        {
            Stream.Write((byte)(dead ? 0 : 2));
        }

        public static Packet Instantiate(bool dead) => dead ? Dead : Alive;
    }

    public sealed class ToggleSpecialAbility : Packet
    {
        public ToggleSpecialAbility(int abilityID, bool active) : base(0xBF)
        {
            EnsureCapacity(7);

            Stream.Write((short)0x25);

            Stream.Write((short)abilityID);
            Stream.Write(active);
        }
    }

    public sealed class DisplayProfile : Packet
    {
        public DisplayProfile(Serial m, string header, string body, string footer) : base(0xB8)
        {
            header ??= "";
            body ??= "";
            footer ??= "";

            EnsureCapacity(12 + header.Length + footer.Length * 2 + body.Length * 2);

            Stream.Write(m);
            Stream.WriteAsciiNull(header);
            Stream.WriteBigUniNull(footer);
            Stream.WriteBigUniNull(body);
        }
    }

    public sealed class LiftRej : Packet
    {
        public LiftRej(LRReason reason) : base(0x27, 2)
        {
            Stream.Write((byte)reason);
        }
    }

    public sealed class LogoutAck : Packet
    {
        public LogoutAck() : base(0xD1, 2)
        {
            Stream.Write((byte)0x01);
        }
    }

    public sealed class Weather : Packet
    {
        public Weather(int type, int density, int temp) : base(0x65, 4)
        {
            Stream.Write((byte)type);
            Stream.Write((byte)density);
            Stream.Write((byte)temp);
        }
    }
}
