/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Mobile.Messages.cs                                              *
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
using Server.Network;

namespace Server;

public partial class Mobile
{
    // ---------- Say / Emote / Whisper / Yell ----------

    public void Say(bool ascii, ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Regular, SpeechHue, ascii, text);

    public void Say(ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Regular, SpeechHue, false, text);

    public void Say(int number, AffixType type, ReadOnlySpan<char> affix, ReadOnlySpan<char> args) =>
        PublicOverheadMessage(MessageType.Regular, SpeechHue, number, type, affix, args);

    public void Say(int number, ReadOnlySpan<char> args = default) =>
        PublicOverheadMessage(MessageType.Regular, SpeechHue, number, args);

    public void Emote(ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Emote, EmoteHue, false, text);

    public void Emote(int number, ReadOnlySpan<char> args = default) =>
        PublicOverheadMessage(MessageType.Emote, EmoteHue, number, args);

    public void Whisper(ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Whisper, WhisperHue, false, text);

    public void Whisper(int number, ReadOnlySpan<char> args = default) =>
        PublicOverheadMessage(MessageType.Whisper, WhisperHue, number, args);

    public void Yell(ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Yell, YellHue, false, text);

    public void Yell(int number, ReadOnlySpan<char> args = default) =>
        PublicOverheadMessage(MessageType.Yell, YellHue, number, args);

    // ---------- PublicOverheadMessage ----------

    public void PublicOverheadMessage(
        MessageType type, int hue, bool ascii, ReadOnlySpan<char> text, bool noLineOfSight = true,
        AccessLevel accessLevel = AccessLevel.Player
    )
    {
        if (m_Map == null)
        {
            return;
        }

        var buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text.Length)].InitializePacket();

        foreach (var state in m_Map.GetClientsInRange(m_Location))
        {
            if (
                state.Mobile.AccessLevel >= accessLevel &&
                state.Mobile.CanSee(this) &&
                (noLineOfSight || state.Mobile.InLOS(this))
            )
            {
                var length = OutgoingMessagePackets.CreateMessage(
                    buffer,
                    Serial,
                    Body,
                    type,
                    hue,
                    3,
                    ascii,
                    Language,
                    Name,
                    text
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length]; // Adjust to the actual size
                }

                state.Send(buffer);
            }
        }
    }

    public void PublicOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args = default, bool noLineOfSight = true)
    {
        if (m_Map == null)
        {
            return;
        }

        var buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedLength(args.Length)].InitializePacket();

        foreach (var state in m_Map.GetClientsInRange(m_Location))
        {
            if (state.Mobile.CanSee(this) && (noLineOfSight || state.Mobile.InLOS(this)))
            {
                var length = OutgoingMessagePackets.CreateMessageLocalized(
                    buffer,
                    Serial,
                    Body,
                    type,
                    hue,
                    3,
                    number,
                    Name,
                    args
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length]; // Adjust to the actual size
                }

                state.Send(buffer);
            }
        }
    }

    public void PublicOverheadMessage(
        MessageType type, int hue, int number, AffixType affixType, ReadOnlySpan<char> affix,
        ReadOnlySpan<char> args = default, bool noLineOfSight = false,
        AccessLevel accessLevel = AccessLevel.Player
    )
    {
        if (m_Map == null)
        {
            return;
        }

        var buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedAffixLength(affix.Length, args.Length)]
            .InitializePacket();

        foreach (var state in m_Map.GetClientsInRange(m_Location))
        {
            if (
                state.Mobile.AccessLevel >= accessLevel &&
                state.Mobile.CanSee(this) &&
                (noLineOfSight || state.Mobile.InLOS(this))
            )
            {
                var length = OutgoingMessagePackets.CreateMessageLocalizedAffix(
                    buffer,
                    Serial,
                    Body,
                    type,
                    hue,
                    3,
                    number,
                    Name,
                    affixType,
                    affix,
                    args
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length]; // Adjust to the actual size
                }

                state.Send(buffer);
            }
        }
    }

    // ---------- PrivateOverheadMessage ----------

    public void PrivateOverheadMessage(MessageType type, int hue, bool ascii, ReadOnlySpan<char> text, NetState state)
    {
        state.SendMessage(Serial, Body, type, hue, 3, ascii, m_Language, Name, text);
    }

    public void PrivateOverheadMessage(MessageType type, int hue, int number, NetState state) =>
        PrivateOverheadMessage(type, hue, number, default, state);

    public void PrivateOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args, NetState state) =>
        state.SendMessageLocalized(Serial, Body, type, hue, 3, number, Name, args);

    // ---------- LocalOverheadMessage ----------

    public void LocalOverheadMessage(MessageType type, int hue, bool ascii, ReadOnlySpan<char> text) =>
        m_NetState.SendMessage(Serial, Body, type, hue, 3, ascii, m_Language, Name, text);

    public void LocalOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args = default) =>
        m_NetState.SendMessageLocalized(Serial, Body, type, hue, 3, number, Name, args);

    // ---------- NonlocalOverheadMessage ----------

    public void NonlocalOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args = default)
    {
        if (m_Map == null)
        {
            return;
        }

        var buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedLength(args.Length)].InitializePacket();

        foreach (var state in m_Map.GetClientsInRange(m_Location))
        {
            if (state != m_NetState && state.Mobile.CanSee(this))
            {
                var length = OutgoingMessagePackets.CreateMessageLocalized(
                    buffer,
                    Serial,
                    Body,
                    type,
                    hue,
                    3,
                    number,
                    Name,
                    args
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length]; // Adjust to the actual size
                }

                state.Send(buffer);
            }
        }
    }

    public void NonlocalOverheadMessage(MessageType type, int hue, bool ascii, ReadOnlySpan<char> text)
    {
        if (m_Map == null)
        {
            return;
        }

        var buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text.Length)].InitializePacket();

        foreach (var state in m_Map.GetClientsInRange(m_Location))
        {
            if (state != m_NetState && state.Mobile.CanSee(this))
            {
                var length = OutgoingMessagePackets.CreateMessage(
                    buffer,
                    Serial,
                    Body,
                    type,
                    hue,
                    3,
                    ascii,
                    Language,
                    Name,
                    text
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length]; // Adjust to the actual size
                }

                state.Send(buffer);
            }
        }
    }

    // ---------- SendLocalizedMessage / SendMessage / SendAsciiMessage ----------

    public void SendLocalizedMessage(int number, ReadOnlySpan<char> args = default, int hue = 0x3B2) =>
        m_NetState.SendMessageLocalized(Serial.MinusOne, -1, MessageType.Regular, hue, 3, number, "System", args);

    public void SendLocalizedMessage(int number, bool append, ReadOnlySpan<char> affix, ReadOnlySpan<char> args = default, int hue = 0x3B2) =>
        m_NetState.SendMessageLocalizedAffix(
            Serial.MinusOne,
            -1,
            MessageType.Regular,
            hue,
            3,
            number,
            "System",
            (append ? AffixType.Append : AffixType.Prepend) | AffixType.System,
            affix,
            args
        );

    public void SendMessage(ReadOnlySpan<char> text) => SendMessage(0x3B2, text);

    public void SendMessage(int hue, ReadOnlySpan<char> text) =>
        m_NetState.SendMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, false, "ENU", "System", text);

    public void SendAsciiMessage(ReadOnlySpan<char> text) => SendAsciiMessage(0x3B2, text);

    public void SendAsciiMessage(int hue, ReadOnlySpan<char> text) =>
        m_NetState.SendMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, true, null, "System", text);
}
