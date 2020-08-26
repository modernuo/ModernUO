/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MessagePackets.cs - Created: 2020/05/26 - Updated: 2020/06/25   *
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

namespace Server.Network
{
    [Flags]
    public enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02
    }

    public sealed class MessageLocalized : Packet
    {
        private static readonly MessageLocalized[] m_Cache_IntLoc = new MessageLocalized[15000];
        private static readonly MessageLocalized[] m_Cache_CliLoc = new MessageLocalized[100000];
        private static readonly MessageLocalized[] m_Cache_CliLocCmp = new MessageLocalized[5000];

        public MessageLocalized(
            Serial serial, int graphic, MessageType type, int hue, int font, int number, string name,
            string args
        ) : base(0xC1)
        {
            name ??= "";
            args ??= "";

            if (hue == 0)
                hue = 0x3B2;

            EnsureCapacity(50 + args.Length * 2);

            Stream.Write(serial);
            Stream.Write((short)graphic);
            Stream.Write((byte)type);
            Stream.Write((short)hue);
            Stream.Write((short)font);
            Stream.Write(number);
            Stream.WriteAsciiFixed(name, 30);
            Stream.WriteLittleUniNull(args);
        }

        public static MessageLocalized InstantiateGeneric(int number)
        {
            MessageLocalized[] cache = null;
            var index = 0;

            if (number >= 3000000)
            {
                cache = m_Cache_IntLoc;
                index = number - 3000000;
            }
            else if (number >= 1000000)
            {
                cache = m_Cache_CliLoc;
                index = number - 1000000;
            }
            else if (number >= 500000)
            {
                cache = m_Cache_CliLocCmp;
                index = number - 500000;
            }

            MessageLocalized p;

            if (cache != null && index < cache.Length)
            {
                p = cache[index];

                if (p == null)
                {
                    cache[index] = p = new MessageLocalized(
                        Serial.MinusOne,
                        -1,
                        MessageType.Regular,
                        0x3B2,
                        3,
                        number,
                        "System",
                        ""
                    );
                    p.SetStatic();
                }
            }
            else
            {
                p = new MessageLocalized(Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, number, "System", "");
            }

            return p;
        }
    }

    public sealed class MessageLocalizedAffix : Packet
    {
        public MessageLocalizedAffix(
            Serial serial, int graphic, MessageType messageType, int hue, int font, int number,
            string name, AffixType affixType, string affix, string args
        ) : base(0xCC)
        {
            name ??= "";
            affix ??= "";
            args ??= "";

            if (hue == 0)
                hue = 0x3B2;

            EnsureCapacity(52 + affix.Length + args.Length * 2);

            Stream.Write(serial);
            Stream.Write((short)graphic);
            Stream.Write((byte)messageType);
            Stream.Write((short)hue);
            Stream.Write((short)font);
            Stream.Write(number);
            Stream.Write((byte)affixType);
            Stream.WriteAsciiFixed(name, 30);
            Stream.WriteAsciiNull(affix);
            Stream.WriteBigUniNull(args);
        }
    }

    public sealed class AsciiMessage : Packet
    {
        public AsciiMessage(
            Serial serial, int graphic, MessageType type, int hue, int font, string name, string text
        ) : base(0x1C)
        {
            name ??= "";
            text ??= "";

            if (hue == 0)
                hue = 0x3B2;

            EnsureCapacity(45 + text.Length);

            Stream.Write(serial);
            Stream.Write((short)graphic);
            Stream.Write((byte)type);
            Stream.Write((short)hue);
            Stream.Write((short)font);
            Stream.WriteAsciiFixed(name, 30);
            Stream.WriteAsciiNull(text);
        }
    }

    public sealed class UnicodeMessage : Packet
    {
        public UnicodeMessage(
            Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name,
            string text
        ) : base(0xAE)
        {
            if (string.IsNullOrEmpty(lang)) lang = "ENU";
            name ??= "";
            text ??= "";

            if (hue == 0)
                hue = 0x3B2;

            EnsureCapacity(50 + text.Length * 2);

            Stream.Write(serial);
            Stream.Write((short)graphic);
            Stream.Write((byte)type);
            Stream.Write((short)hue);
            Stream.Write((short)font);
            Stream.WriteAsciiFixed(lang, 4);
            Stream.WriteAsciiFixed(name, 30);
            Stream.WriteBigUniNull(text);
        }
    }

    public sealed class FollowMessage : Packet
    {
        public FollowMessage(Serial serial1, Serial serial2) : base(0x15, 9)
        {
            Stream.Write(serial1);
            Stream.Write(serial2);
        }
    }
}
