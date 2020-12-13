using System;
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

    public sealed class LoginConfirm : Packet
    {
        public LoginConfirm(Mobile m) : base(0x1B, 37)
        {
            Stream.Write(m.Serial);
            Stream.Write(0);
            Stream.Write((short)m.Body);
            Stream.Write((short)m.X);
            Stream.Write((short)m.Y);
            Stream.Write((short)m.Z);
            Stream.Write((byte)m.Direction);
            Stream.Write((byte)0);
            Stream.Write(-1);

            var map = m.Map;

            if (map == null || map == Map.Internal)
            {
                map = m.LogoutMap;
            }

            Stream.Write((short)0);
            Stream.Write((short)0);
            Stream.Write((short)(map?.Width ?? Map.Felucca.Width));
            Stream.Write((short)(map?.Height ?? Map.Felucca.Height));

            Stream.Fill();
        }
    }

    public sealed class LoginComplete : Packet
    {
        public LoginComplete() : base(0x55, 1)
        {
        }
    }

    public sealed class CharacterListUpdate : Packet
    {
        public CharacterListUpdate(IAccount a) : base(0x86)
        {
            EnsureCapacity(4 + a.Length * 60);

            var highSlot = -1;

            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                {
                    highSlot = i;
                }
            }

            var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            Stream.Write((byte)count);

            for (var i = 0; i < count; ++i)
            {
                var m = a[i];

                if (m != null)
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

    public sealed class CharacterList : Packet
    {
        public CharacterList(IAccount a, CityInfo[] info) : base(0xA9)
        {
            EnsureCapacity(11 + a.Length * 60 + info.Length * 89);

            var highSlot = -1;

            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                {
                    highSlot = i;
                }
            }

            var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            Stream.Write((byte)count);

            for (var i = 0; i < count; ++i)
            {
                if (a[i] != null)
                {
                    Stream.WriteAsciiFixed(a[i].Name, 30);
                    Stream.Fill(30); // password
                }
                else
                {
                    Stream.Fill(60);
                }
            }

            Stream.Write((byte)info.Length);

            for (var i = 0; i < info.Length; ++i)
            {
                var ci = info[i];

                Stream.Write((byte)i);
                Stream.WriteAsciiFixed(ci.City, 32);
                Stream.WriteAsciiFixed(ci.Building, 32);
                Stream.Write(ci.X);
                Stream.Write(ci.Y);
                Stream.Write(ci.Z);
                Stream.Write(ci.Map.MapID);
                Stream.Write(ci.Description);
                Stream.Write(0);
            }

            var flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

            if (count > 6)
            {
                flags |= CharacterListFlags.SeventhCharacterSlot |
                         CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
            }
            else if (count == 6)
            {
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            }
            else if (a.Limit == 1)
            {
                flags |= CharacterListFlags.SlotLimit &
                         CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character
            }

            Stream.Write((int)flags); // Additional Flags

            Stream.Write((short)-1);
        }
    }

    public sealed class CharacterListOld : Packet
    {
        public CharacterListOld(IAccount a, CityInfo[] info) : base(0xA9)
        {
            EnsureCapacity(9 + a.Length * 60 + info.Length * 63);

            var highSlot = -1;

            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                {
                    highSlot = i;
                }
            }

            var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            Stream.Write((byte)count);

            for (var i = 0; i < count; ++i)
            {
                var m = a[i];
                if (m != null)
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

            Stream.Write((byte)info.Length);

            for (var i = 0; i < info.Length; ++i)
            {
                var ci = info[i];

                Stream.Write((byte)i);
                Stream.WriteAsciiFixed(ci.City, 31);
                Stream.WriteAsciiFixed(ci.Building, 31);
            }

            var flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

            if (count > 6)
            {
                flags |= CharacterListFlags.SeventhCharacterSlot |
                         CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
            }
            else if (count == 6)
            {
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            }
            else if (a.Limit == 1)
            {
                flags |= CharacterListFlags.SlotLimit &
                         CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character
            }

            Stream.Write((int)flags); // Additional Flags
        }
    }

    public sealed class AccountLoginRej : Packet
    {
        public AccountLoginRej(ALRReason reason) : base(0x82, 2)
        {
            Stream.Write((byte)reason);
        }
    }

    public sealed class AccountLoginAck : Packet
    {
        public AccountLoginAck(ServerInfo[] info) : base(0xA8)
        {
            EnsureCapacity(6 + info.Length * 40);

            Stream.Write((byte)0x5D); // Unknown

            Stream.Write((ushort)info.Length);

            for (var i = 0; i < info.Length; ++i)
            {
                var si = info[i];

                Stream.Write((ushort)i);
                Stream.WriteAsciiFixed(si.Name, 32);
                Stream.Write((byte)si.FullPercent);
                Stream.Write((sbyte)si.TimeZone);
                // UO Doesn't support IPv6
                Stream.Write(si.RawAddress);
            }
        }
    }

    public sealed class PlayServerAck : Packet
    {
        public PlayServerAck(ServerInfo si, int authId) : base(0x8C, 11)
        {
            var addr = si.RawAddress;

            Stream.Write((byte)addr);
            Stream.Write((byte)(addr >> 8));
            Stream.Write((byte)(addr >> 16));
            Stream.Write((byte)(addr >> 24));

            Stream.Write((short)si.Address.Port);
            Stream.Write(authId);
        }
    }
}
