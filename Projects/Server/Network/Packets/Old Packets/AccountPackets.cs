/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AccountPackets.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Server.Accounting;

namespace Server.Network
{
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
