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

    /// <summary>
    ///     Asks the client for it's version
    /// </summary>
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
}
