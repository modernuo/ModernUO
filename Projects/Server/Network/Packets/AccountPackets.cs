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
using System.Buffers;
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

    public partial class Packets
    {
        /**
         * Packet: 0x81
         * Size: Up to 425 bytes
         *
         * Displays the list of characters during the login process.
         */
        public static void SendChangeCharacter(NetState ns, IAccount a)
        {
            Span<byte> data = stackalloc byte[5 + a.Length * 60];
            var pos = 0;
            data.Write(ref pos, (byte)0x81); // Packet ID
            data.Write(ref pos, (ushort)data.Length);

#if NO_LOCAL_INIT
            pos++; // Count
            data.Write(ref pos, (byte)0);
#else
            pos += 2;
#endif

            var count = 0;

            for (var i = 0; i < a.Length; ++i)
            {
                var m = a[i];

                if (m == null)
                {
#if NO_LOCAL_INIT
                data.Clear(ref pos, 60);
#else
                    pos += 60;
#endif
                }
                else
                {
                    var name = m.RawName?.Trim().IsNullOrDefault("-no name-");

                    count++;
                    data.WriteAsciiFixed(ref pos, name, 30);
#if NO_LOCAL_INIT
                    data.Clear(ref pos, 30); // Password (empty)
#else
                    pos += 30;
#endif
                }
            }

            data[3] = (byte)count;
            ns.Send(data);
        }
    }
}
