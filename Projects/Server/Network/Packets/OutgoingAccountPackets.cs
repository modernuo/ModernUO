/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingAccountPackets.cs                                       *
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
using System.IO;
using System.Buffers;
using System.Runtime.CompilerServices;
using Server.Accounting;

namespace Server.Network;

public enum ALRReason : byte
{
    Invalid = 0,
    InUse = 1,
    Blocked = 2,
    BadPass = 3,
    Idle = 254,
    BadComm = 255
}

public enum PMMessage : byte
{
    None = 0,
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

public static class OutgoingAccountPackets
{
    /**
         * Packet: 0x81
         * Length: Up to 425 bytes
         *
         * Displays the list of characters during the login process.
         * Note: Currently Unused
         */
    public static void SendChangeCharacter(this NetState ns, IAccount a)
    {
        if (ns == null || a == null)
        {
            return;
        }

        var length = 5 + a.Length * 60;
        var writer = new SpanWriter(stackalloc byte[length]);

        writer.Write((byte)0x81); // Packet ID
        writer.Write((ushort)length);
        writer.Write((ushort)0); // Count & Placeholder

        int count = 0;

        for (var i = 0; i < a.Length; ++i)
        {
            var m = a[i];

            if (m == null)
            {
                writer.Clear(60);
            }
            else
            {
                var name = (m.RawName?.Trim()).DefaultIfNullOrEmpty("-no name-");

                count++;
                writer.WriteAscii(name, 30);
                writer.Clear(30); // Password (empty)
            }
        }

        var position = writer.Position;
        writer.Seek(3, SeekOrigin.Begin);
        writer.Write((byte)count);
        writer.Seek(position, SeekOrigin.Begin);

        ns.Send(writer.Span);
    }

    /**
         * Packet: 0xBD
         * Length: 3 bytes
         *
         * Sends a requests for the client version
         */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendClientVersionRequest(this NetState ns) => ns?.Send(stackalloc byte[] { 0xBD, 0x00, 0x03 });

    /**
         * Packet: 0x85
         * Length: 2 bytes
         *
         * Sends the result of a deletion request
         */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendCharacterDeleteResult(this NetState ns, DeleteResultType res) =>
        ns?.Send(stackalloc byte[] { 0x85, (byte)res });

    /**
         * Packet: 0x53
         * Length: 2 bytes
         *
         * Sends a PopupMessage with a predetermined message
         */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendPopupMessage(this NetState ns, PMMessage msg) =>
        ns?.Send(stackalloc byte[] { 0x53, (byte)msg });

    /**
         * Packet: 0xB9
         * Length: 3 or 5 bytes
         *
         * Sends support features based on the client version
         */
    public static void SendSupportedFeature(this NetState ns)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var flags = ExpansionInfo.CoreExpansion.SupportedFeatures;

        if (ns.Account.Limit >= 6)
        {
            flags |= FeatureFlags.LiveAccount;

            if (ns.Account.Limit > 6)
            {
                flags |= FeatureFlags.SeventhCharacterSlot;
            }
            else
            {
                flags |= FeatureFlags.SixthCharacterSlot;
            }
        }

        if (ExpansionInfo.ForceOldAnimations)
        {
            flags &= ~FeatureFlags.LBR;
        }

        var length = ns.ExtendedSupportedFeatures ? 5 : 3;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xB9); // Packet ID

        if (ns.ExtendedSupportedFeatures)
        {
            writer.Write((uint)flags);
        }
        else
        {
            writer.Write((ushort)flags);
        }

        ns.Send(writer.Span);
    }

    /**
         * Packet: 0x1B
         * Length: 37 bytes
         *
         * Sends login confirmation
         */
    public static void SendLoginConfirmation(this NetState ns, Mobile m)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[37]);
        writer.Write((byte)0x1B); // PacketID
        writer.Write(m.Serial);
        writer.Write(0);
        writer.Write((short)m.Body);
        writer.Write((short)m.X);
        writer.Write((short)m.Y);
        writer.Write((short)m.Z);
        writer.Write((byte)m.Direction);
        writer.Write((byte)0);
        writer.Write(-1);

        writer.Write(0);

        var map = m.Map;

        if (map == null || map == Map.Internal)
        {
            map = m.LogoutMap;
        }

        writer.Write((short)(map?.Width ?? Map.Felucca.Width));
        writer.Write((short)(map?.Height ?? Map.Felucca.Height));
        writer.Clear(writer.Capacity - writer.Position); // Remaining is zero

        ns.Send(writer.Span);
    }

    /**
         * Packet: 0x55
         * Length: 1 byte
         *
         * Sends login completion
         */
    public static void SendLoginComplete(this NetState ns)
    {
        ns?.Send(stackalloc byte[] { 0x55 });
    }

    /**
         * Packet: 0x86
         * Length: Up to 424 bytes
         *
         * Sends updated character list
         */
    public static void SendCharacterListUpdate(this NetState ns, IAccount a)
    {
        if (ns == null || a == null)
        {
            return;
        }

        var highSlot = -1;

        for (var i = a.Length - 1; i >= 0; i--)
        {
            if (a[i] != null)
            {
                highSlot = i;
                break;
            }
        }

        var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);
        var length = 4 + count * 60;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0x86); // Packet ID
        writer.Write((ushort)length);

        writer.Write((byte)count);

        for (int i = 0; i < count; i++)
        {
            var m = a[i];

            if (m == null)
            {
                writer.Clear(60);
            }
            else
            {
                var name = (m.RawName?.Trim()).DefaultIfNullOrEmpty("-no name-");
                writer.WriteAscii(name, 30);
                writer.Clear(30); // password
            }
        }

        ns.Send(writer.Span);
    }

    /**
         * Packet: 0xA9
         * Length: 1410 or more bytes
         *
         * Sends list of characters and starting cities.
         */
    public static void SendCharacterList(this NetState ns)
    {
        var acct = ns?.Account;

        if (acct == null)
        {
            return;
        }

        var client70130 = ns.NewCharacterList;
        var textLength = client70130 ? 32 : 31;

        var cityInfo = ns.CityInfo;

        var highSlot = -1;

        for (var i = acct.Length - 1; i >= 0; i--)
        {
            if (acct[i] != null)
            {
                highSlot = i;
                break;
            }
        }

        var count = Math.Max(Math.Max(highSlot + 1, acct.Limit), 5);
        var length = (client70130 ?
            11 + (textLength * 2 + 25) * cityInfo.Length :
            9 + (textLength * 2 +  1) * cityInfo.Length) + count * 60;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xA9); // Packet ID
        writer.Write((ushort)length);
        writer.Write((byte)count);

        for (int i = 0; i < count; i++)
        {
            var m = acct[i];

            if (m == null)
            {
                writer.Clear(60);
            }
            else
            {
                var name = (m.RawName?.Trim()).DefaultIfNullOrEmpty("-no name-");
                writer.WriteAscii(name, 30);
                writer.Clear(30); // password
            }
        }

        writer.Write((byte)cityInfo.Length);

        for (int i = 0; i < cityInfo.Length; ++i)
        {
            var ci = cityInfo[i];

            writer.Write((byte)i);
            writer.WriteAscii(ci.City, textLength);
            writer.WriteAscii(ci.Building, textLength);
            if (client70130)
            {
                writer.Write(ci.X);
                writer.Write(ci.Y);
                writer.Write(ci.Z);
                writer.Write(ci.Map?.MapID ?? 0);
                writer.Write(ci.Description);
                writer.Write(0);
            }
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
        else if (acct.Limit == 1)
        {
            flags |= CharacterListFlags.SlotLimit &
                     CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character
        }

        writer.Write((int)flags);
        if (client70130)
        {
            writer.Write((short)-1);
        }

        ns.Send(writer.Span);
    }

    /**
         * Packet: 0x82
         * Length: 2 bytes
         *
         * Sends a reason for rejecting the login
         */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendAccountLoginRejected(this NetState ns, ALRReason reason) =>
        ns?.Send(stackalloc byte[] { 0x82, (byte)reason });

    /**
         * Packet: 0xA8
         * Length: 6 + 40 bytes per server listing
         *
         * Sends login acknowledge with server listing
         */
    public static void SendAccountLoginAck(this NetState ns)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var info = ns.ServerInfo;
        var length = 6 + 40 * info.Length;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xA8); // Packet ID
        writer.Write((ushort)length);
        writer.Write((byte)0x5D);
        writer.Write((ushort)info.Length);

        for (var i = 0; i < info.Length; ++i)
        {
            var si = info[i];

            writer.Write((ushort)i);
            writer.WriteAscii(si.Name, 32);
            writer.Write((byte)si.FullPercent);
            writer.Write((sbyte)si.TimeZone);
            // UO only supports IPv4
            writer.Write(si.RawAddress);
        }

        ns.Send(writer.Span);
    }

    /**
         * Packet: 0x8C
         * Length: 11 bytes
         *
         * Sends acknowledge play server
         */
    public static void SendPlayServerAck(this NetState ns, ServerInfo si, int authId)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[11]);
        writer.Write((byte)0x8C); // Packet ID

        writer.WriteLE(si.RawAddress);
        writer.Write((short)si.Address.Port);
        writer.Write(authId);

        ns.Send(writer.Span);
    }
}
