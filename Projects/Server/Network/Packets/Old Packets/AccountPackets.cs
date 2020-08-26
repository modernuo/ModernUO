/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: AccountPackets.cs - Created: 2020/05/08 - Updated: 2020/06/25   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Server.Accounting;

namespace Server.Network
{
    public enum ALRReason : byte
    {
        Invalid = 0x00,
        InUse = 0x01,
        Blocked = 0x02,
        BadPass = 0x03,
        Idle = 0xFE,
        BadComm = 0xFF
    }

    public enum PMMessage : byte
    {
        CharNoExist = 1,
        CharExists = 2,
        CharInWorld = 5,
        LoginSyncError = 6,
        IdleWarning = 7
    }

    public enum DeleteResultType
    {
        PasswordInvalid,
        CharNotExist,
        CharBeingPlayed,
        CharTooYoung,
        CharQueued,
        BadRequest
    }

    public sealed class ChangeCharacter : Packet
    {
        public ChangeCharacter(IAccount a) : base(0x81)
        {
            EnsureCapacity(305);

            var count = 0;

            for (var i = 0; i < a.Length; ++i)
                if (a[i] != null)
                    ++count;

            Stream.Write((byte)count);
            Stream.Write((byte)0);

            for (var i = 0; i < a.Length; ++i)
                if (a[i] != null)
                {
                    var name = a[i].Name;

                    if (name == null)
                        name = "-null-";
                    else if ((name = name.Trim()).Length == 0)
                        name = "-empty-";

                    Stream.WriteAsciiFixed(name, 30);
                    Stream.Fill(30); // password
                }
                else
                {
                    Stream.Fill(60);
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

            flags |= Value;

            if (ns.Account.Limit >= 6)
            {
                flags |= FeatureFlags.LiveAccount;
                flags &= ~FeatureFlags.UOTD;

                if (ns.Account.Limit > 6)
                    flags |= FeatureFlags.SeventhCharacterSlot;
                else
                    flags |= FeatureFlags.SixthCharacterSlot;
            }

            if (ns.ExtendedSupportedFeatures)
                Stream.Write((uint)flags);
            else
                Stream.Write((ushort)flags);
        }

        public static FeatureFlags Value { get; set; }

        public static SupportedFeatures Instantiate(NetState ns) => new SupportedFeatures(ns);
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
                map = m.LogoutMap;

            Stream.Write((short)0);
            Stream.Write((short)0);
            Stream.Write((short)(map?.Width ?? Map.Felucca.Width));
            Stream.Write((short)(map?.Height ?? Map.Felucca.Height));

            Stream.Fill();
        }
    }

    public sealed class LoginComplete : Packet
    {
        public static readonly Packet Instance = SetStatic(new LoginComplete());

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
                if (a[i] != null)
                    highSlot = i;

            var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            Stream.Write((byte)count);

            for (var i = 0; i < count; ++i)
            {
                var m = a[i];

                if (m != null)
                {
                    Stream.WriteAsciiFixed(m.Name, 30);
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
                if (a[i] != null)
                    highSlot = i;

            var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            Stream.Write((byte)count);

            for (var i = 0; i < count; ++i)
                if (a[i] != null)
                {
                    Stream.WriteAsciiFixed(a[i].Name, 30);
                    Stream.Fill(30); // password
                }
                else
                {
                    Stream.Fill(60);
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
                flags |= CharacterListFlags.SeventhCharacterSlot |
                         CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
            else if (count == 6)
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            else if (a.Limit == 1)
                flags |= CharacterListFlags.SlotLimit &
                         CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

            Stream.Write((int)(flags | AdditionalFlags)); // Additional Flags

            Stream.Write((short)-1);
        }

        public static CharacterListFlags AdditionalFlags { get; set; }
    }

    public sealed class CharacterListOld : Packet
    {
        public CharacterListOld(IAccount a, CityInfo[] info) : base(0xA9)
        {
            EnsureCapacity(9 + a.Length * 60 + info.Length * 63);

            var highSlot = -1;

            for (var i = 0; i < a.Length; ++i)
                if (a[i] != null)
                    highSlot = i;

            var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            Stream.Write((byte)count);

            for (var i = 0; i < count; ++i)
                if (a[i] != null)
                {
                    Stream.WriteAsciiFixed(a[i].Name, 30);
                    Stream.Fill(30); // password
                }
                else
                {
                    Stream.Fill(60);
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
                flags |= CharacterListFlags.SeventhCharacterSlot |
                         CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
            else if (count == 6)
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            else if (a.Limit == 1)
                flags |= CharacterListFlags.SlotLimit &
                         CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

            Stream.Write((int)(flags | CharacterList.AdditionalFlags)); // Additional Flags
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
                Stream.Write(Utility.GetAddressValue(si.Address.Address));
            }
        }
    }

    public sealed class PlayServerAck : Packet
    {
        internal static int m_AuthID = -1;

        public PlayServerAck(ServerInfo si) : base(0x8C, 11)
        {
            var addr = Utility.GetAddressValue(si.Address.Address);

            Stream.Write((byte)addr);
            Stream.Write((byte)(addr >> 8));
            Stream.Write((byte)(addr >> 16));
            Stream.Write((byte)(addr >> 24));

            Stream.Write((short)si.Address.Port);
            Stream.Write(m_AuthID);
        }
    }
}
