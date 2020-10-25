using Server.Accounting;
using Server.Network;

namespace Server.Tests.Network
{
    public sealed class ChangeCharacter : Packet
    {
        public ChangeCharacter(IAccount a) : base(0x81)
        {
            EnsureCapacity(305);

            var count = 0;

            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                {
                    ++count;
                }
            }

            Stream.Write((byte)count);
            Stream.Write((byte)0);

            for (var i = 0; i < a.Length; ++i)
            {
                var m = a[i];
                if (a[i] != null)
                {
                    var name = (m.RawName?.Trim()).DefaultIfNullOrEmpty("-no name-");

                    Stream.WriteAsciiFixed(name, 30);
                    Stream.Fill(30); // password
                }
                else
                {
                    Stream.Fill(60);
                }
            }
        }
    }

    public sealed class ClientVersionReq : Packet
    {
        public ClientVersionReq() : base(0xBD)
        {
            EnsureCapacity(3);
        }
    }

    public sealed class DeleteResult : Packet
    {
        public DeleteResult(DeleteResultType res) : base(0x85, 2)
        {
            Stream.Write((byte)res);
        }
    }

    public sealed class PopupMessage : Packet
    {
        public PopupMessage(PMMessage msg) : base(0x53, 2)
        {
            Stream.Write((byte)msg);
        }
    }

    public sealed class SupportedFeatures : Packet
    {
        public SupportedFeatures(NetState ns) : base(0xB9, ns.ExtendedSupportedFeatures ? 5 : 3)
        {
            var flags = ExpansionInfo.CoreExpansion.SupportedFeatures;

            if (ns.Account.Limit >= 6)
            {
                flags |= FeatureFlags.LiveAccount;
                flags &= ~FeatureFlags.UOTD;

                if (ns.Account.Limit > 6)
                {
                    flags |= FeatureFlags.SeventhCharacterSlot;
                }
                else
                {
                    flags |= FeatureFlags.SixthCharacterSlot;
                }
            }

            if (ns.ExtendedSupportedFeatures)
            {
                Stream.Write((uint)flags);
            }
            else
            {
                Stream.Write((ushort)flags);
            }
        }
    }
}
