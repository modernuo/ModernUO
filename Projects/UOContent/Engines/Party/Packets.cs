using Server.Network;

namespace Server.Engines.PartySystem
{
    public sealed class PartyEmptyList : Packet
    {
        public PartyEmptyList(Mobile m) : base(0xBF)
        {
            EnsureCapacity(7);

            Stream.Write((short)0x0006);
            Stream.Write((byte)0x02);
            Stream.Write((byte)0);
            Stream.Write(m.Serial);
        }
    }

    public sealed class PartyMemberList : Packet
    {
        public PartyMemberList(Party p) : base(0xBF)
        {
            EnsureCapacity(7 + p.Count * 4);

            Stream.Write((short)0x0006);
            Stream.Write((byte)0x01);
            Stream.Write((byte)p.Count);

            for (int i = 0; i < p.Count; ++i)
                Stream.Write(p[i].Mobile.Serial);
        }
    }

    public sealed class PartyRemoveMember : Packet
    {
        public PartyRemoveMember(Mobile removed, Party p) : base(0xBF)
        {
            EnsureCapacity(11 + p.Count * 4);

            Stream.Write((short)0x0006);
            Stream.Write((byte)0x02);
            Stream.Write((byte)p.Count);

            Stream.Write(removed.Serial);

            for (int i = 0; i < p.Count; ++i)
                Stream.Write(p[i].Mobile.Serial);
        }
    }

    public sealed class PartyTextMessage : Packet
    {
        public PartyTextMessage(bool toAll, Mobile from, string text) : base(0xBF)
        {
            if (text == null)
                text = "";

            EnsureCapacity(12 + text.Length * 2);

            Stream.Write((short)0x0006);
            Stream.Write((byte)(toAll ? 0x04 : 0x03));
            Stream.Write(from.Serial);
            Stream.WriteBigUniNull(text);
        }
    }

    public sealed class PartyInvitation : Packet
    {
        public PartyInvitation(Mobile leader) : base(0xBF)
        {
            EnsureCapacity(10);

            Stream.Write((short)0x0006);
            Stream.Write((byte)0x07);
            Stream.Write(leader.Serial);
        }
    }
}
