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

using System.Buffers;
using System.IO;
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

    public static partial class Packets
    {
        /**
         * Packet: 0x81
         * Size: Up to 425 bytes
         *
         * Displays the list of characters during the login process.
         */
        public static void SendChangeCharacter(NetState ns, IAccount a)
        {
            if (ns == null || a == null)
            {
                return;
            }

            var length = 5 + a.Length * 60;

            if (!ns.GetSendBuffer((uint)length, out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);

            writer.Write((byte)0x81); // Packet ID

            writer.Write((ushort)length);
            writer.Write((ushort)0); // Count

            int count = 0;

            for (var i = 0; i < a.Length; ++i)
            {
                var m = a[i];

                if (m == null)
                {
                    writer.Fill(60);
                }
                else
                {
                    var name = (m.RawName?.Trim()).DefaultIfNullOrEmpty("-no name-");

                    count++;
                    writer.WriteAsciiFixed(name, 30);
                    writer.Fill(30); // Password (empty)
                }
            }

            writer.Seek(3, SeekOrigin.Begin);
            writer.Write((byte)count);

            ns.WriteSendBuffer(buffer, (uint)length);
        }

        public static void SendClientVersionRequest(NetState ns)
        {
            if (ns != null && ns.GetSendBuffer(3, out var buffer))
            {
                var writer = new CircularBufferWriter(buffer);
                writer.Write((byte)0x81); // Packet ID
                writer.Write((ushort)3); // Length

                ns.WriteSendBuffer(buffer, 3);
            }
        }
    }
}
