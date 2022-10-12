/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingMessagePackets.cs                                       *
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

namespace Server.Network;

[Flags]
public enum MessageType
{
    Regular = 0x00,
    System = 0x01,
    Emote = 0x02,
    Label = 0x06,
    Focus = 0x07,
    Whisper = 0x08,
    Yell = 0x09,
    Spell = 0x0A,

    Guild = 0x0D,
    Alliance = 0x0E,
    Command = 0x0F,

    Encoded = 0xC0
}

public static class IncomingMessagePackets
{
    private static readonly KeywordList m_KeywordList = new();

    public static unsafe void Configure()
    {
        IncomingPackets.Register(0x03, 0, true, &AsciiSpeech);
        IncomingPackets.Register(0xAD, 0, true, &UnicodeSpeech);
    }

    public static void AsciiSpeech(NetState state, CircularBufferReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        var type = (MessageType)reader.ReadByte();
        int hue = reader.ReadInt16();
        reader.ReadInt16(); // font
        var text = reader.ReadAsciiSafe().Trim();

        if (text.Length is <= 0 or > 128)
        {
            return;
        }

        if (!Enum.IsDefined(typeof(MessageType), type))
        {
            type = MessageType.Regular;
        }

        from.DoSpeech(text, Array.Empty<int>(), type, Utility.ClipDyedHue(hue));
    }

    public static void UnicodeSpeech(NetState state, CircularBufferReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        var type = (MessageType)reader.ReadByte();
        int hue = reader.ReadInt16();
        reader.ReadInt16(); // font
        var lang = reader.ReadAscii(4);
        string text;

        var isEncoded = (type & MessageType.Encoded) != 0;
        int[] keywords;

        if (isEncoded)
        {
            int value = reader.ReadInt16();
            var count = (value & 0xFFF0) >> 4;
            var hold = value & 0xF;

            if (count is < 0 or > 50)
            {
                return;
            }

            var keyList = m_KeywordList;

            for (var i = 0; i < count; ++i)
            {
                int speechID;

                if ((i & 1) == 0)
                {
                    hold <<= 8;
                    hold |= reader.ReadByte();
                    speechID = hold;
                    hold = 0;
                }
                else
                {
                    value = reader.ReadInt16();
                    speechID = (value & 0xFFF0) >> 4;
                    hold = value & 0xF;
                }

                if (!keyList.Contains(speechID))
                {
                    keyList.Add(speechID);
                }
            }

            text = reader.ReadUTF8Safe();

            keywords = keyList.ToArray();
        }
        else
        {
            text = reader.ReadBigUniSafe();

            keywords = Array.Empty<int>();
        }

        text = text.Trim();

        if (text.Length is <= 0 or > 128)
        {
            return;
        }

        type &= ~MessageType.Encoded;

        if (!Enum.IsDefined(typeof(MessageType), type))
        {
            type = MessageType.Regular;
        }

        from.Language = lang;
        from.DoSpeech(text, keywords, type, Utility.ClipDyedHue(hue));
    }
}
