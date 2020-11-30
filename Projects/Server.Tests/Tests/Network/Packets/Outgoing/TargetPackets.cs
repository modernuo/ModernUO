using System.IO;
using Server.Targeting;

namespace Server.Network
{
    public sealed class MultiTargetReqHS : Packet
    {
        public MultiTargetReqHS(MultiTarget t) : base(0x99, 30)
        {
            Stream.Write(t.AllowGround);
            Stream.Write(t.TargetID);
            Stream.Write((byte)t.Flags);

            Stream.Fill();

            Stream.Seek(18, SeekOrigin.Begin);
            Stream.Write((short)t.MultiID);
            Stream.Write((short)t.Offset.X);
            Stream.Write((short)t.Offset.Y);
            Stream.Write((short)t.Offset.Z);

            // DWORD Hue
        }
    }

    public sealed class MultiTargetReq : Packet
    {
        public MultiTargetReq(MultiTarget t) : base(0x99, 26)
        {
            Stream.Write(t.AllowGround);
            Stream.Write(t.TargetID);
            Stream.Write((byte)t.Flags);

            Stream.Fill();

            Stream.Seek(18, SeekOrigin.Begin);
            Stream.Write((short)t.MultiID);
            Stream.Write((short)t.Offset.X);
            Stream.Write((short)t.Offset.Y);
            Stream.Write((short)t.Offset.Z);
        }
    }

    public sealed class CancelTarget : Packet
    {
        public static readonly Packet Instance = SetStatic(new CancelTarget());

        public CancelTarget() : base(0x6C, 19)
        {
            Stream.Write((byte)0);
            Stream.Write(0);
            Stream.Write((byte)3);
            Stream.Fill();
        }
    }

    public sealed class TargetReq : Packet
    {
        public TargetReq(Target t) : base(0x6C, 19)
        {
            Stream.Write(t.AllowGround);
            Stream.Write(t.TargetID);
            Stream.Write((byte)t.Flags);
            Stream.Fill();
        }
    }
}
