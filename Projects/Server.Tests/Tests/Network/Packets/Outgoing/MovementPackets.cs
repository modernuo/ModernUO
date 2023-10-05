namespace Server.Network
{
    public sealed class SpeedControl : Packet
    {
        public static readonly Packet WalkSpeed = SetStatic(new SpeedControl(2));
        public static readonly Packet MountSpeed = SetStatic(new SpeedControl(1));
        public static readonly Packet Disable = SetStatic(new SpeedControl(0));

        public SpeedControl(int speedControl) : base(0xBF)
        {
            EnsureCapacity(6);

            Stream.Write((short)0x26);
            Stream.Write((byte)speedControl);
        }
    }

    /// <summary>
    ///     Causes the client to walk in a given direction. It does not send a movement request.
    /// </summary>
    public sealed class MovePlayer : Packet
    {
        public MovePlayer(Direction d) : base(0x97, 2)
        {
            Stream.Write((byte)d);

            // @4C63B0
        }
    }

    public sealed class MovementRej : Packet
    {
        public MovementRej(int seq, Mobile m) : base(0x21, 8)
        {
            Stream.Write((byte)seq);
            Stream.Write((short)m.X);
            Stream.Write((short)m.Y);
            Stream.Write((byte)m.Direction);
            Stream.Write((sbyte)m.Z);
        }
    }

    public sealed class MovementAck : Packet
    {
        private static readonly MovementAck[] m_Cache = new MovementAck[8 * 256];

        private MovementAck(int seq, int noto) : base(0x22, 3)
        {
            Stream.Write((byte)seq);
            Stream.Write((byte)noto);
        }

        public static MovementAck Instantiate(int seq, Mobile m)
        {
            var noto = Notoriety.Compute(m, m);

            var p = m_Cache[noto * seq];

            if (p == null)
            {
                m_Cache[noto * seq] = p = new MovementAck(seq, noto);
                p.SetStatic();
            }

            return p;
        }
    }
}
