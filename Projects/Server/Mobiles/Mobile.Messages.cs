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
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Network;

namespace Server;

public partial class Mobile
{
    // ---------- Broadcast filter structs ----------

    private readonly struct PublicVisibilityFilter : IBroadcastFilter
    {
        private readonly Mobile _source;
        private readonly bool _noLineOfSight;
        private readonly AccessLevel _accessLevel;

        public PublicVisibilityFilter(Mobile source, bool noLineOfSight, AccessLevel accessLevel)
        {
            _source = source;
            _noLineOfSight = noLineOfSight;
            _accessLevel = accessLevel;
        }

        public bool Allow(NetState state) =>
            state.Mobile.AccessLevel >= _accessLevel &&
            state.Mobile.CanSee(_source) &&
            (_noLineOfSight || state.Mobile.InLOS(_source));
    }

    private readonly struct LocalizedVisibilityFilter : IBroadcastFilter
    {
        private readonly Mobile _source;
        private readonly bool _noLineOfSight;

        public LocalizedVisibilityFilter(Mobile source, bool noLineOfSight)
        {
            _source = source;
            _noLineOfSight = noLineOfSight;
        }

        public bool Allow(NetState state) => state.Mobile.CanSee(_source) && (_noLineOfSight || state.Mobile.InLOS(_source));
    }

    private readonly struct NonlocalVisibilityFilter : IBroadcastFilter
    {
        private readonly Mobile _source;
        private readonly NetState _exclude;

        public NonlocalVisibilityFilter(Mobile source, NetState exclude)
        {
            _source = source;
            _exclude = exclude;
        }

        public bool Allow(NetState state) => state != _exclude && state.Mobile.CanSee(_source);
    }

    // ---------- Say / Emote / Whisper / Yell ----------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Say(bool ascii, ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Regular, SpeechHue, ascii, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Say(ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Regular, SpeechHue, false, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Say(int number, AffixType type, ReadOnlySpan<char> affix, ReadOnlySpan<char> args) =>
        PublicOverheadMessage(MessageType.Regular, SpeechHue, number, type, affix, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Say(int number, ReadOnlySpan<char> args = default) =>
        PublicOverheadMessage(MessageType.Regular, SpeechHue, number, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emote(ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Emote, EmoteHue, false, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emote(int number, ReadOnlySpan<char> args = default) =>
        PublicOverheadMessage(MessageType.Emote, EmoteHue, number, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Whisper(ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Whisper, WhisperHue, false, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Whisper(int number, ReadOnlySpan<char> args = default) =>
        PublicOverheadMessage(MessageType.Whisper, WhisperHue, number, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Yell(ReadOnlySpan<char> text) =>
        PublicOverheadMessage(MessageType.Yell, YellHue, false, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Yell(int number, ReadOnlySpan<char> args = default) =>
        PublicOverheadMessage(MessageType.Yell, YellHue, number, args);

    // ---------- PublicOverheadMessage ----------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PublicOverheadMessage(
        MessageType type, int hue, bool ascii, ReadOnlySpan<char> text, bool noLineOfSight = true,
        AccessLevel accessLevel = AccessLevel.Player
    ) => OutgoingMessagePackets.BroadcastMessage(
        m_Map, m_Location,
        Serial, Body, type, hue, 3, ascii, Language, Name, text,
        new PublicVisibilityFilter(this, noLineOfSight, accessLevel)
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PublicOverheadMessage(
        MessageType type, int hue, int number, ReadOnlySpan<char> args = default, bool noLineOfSight = true
    ) => OutgoingMessagePackets.BroadcastMessageLocalized(
        m_Map, m_Location,
        Serial, Body, type, hue, 3, number, Name, args,
        new LocalizedVisibilityFilter(this, noLineOfSight));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PublicOverheadMessage(
        MessageType type, int hue, int number, AffixType affixType, ReadOnlySpan<char> affix,
        ReadOnlySpan<char> args = default, bool noLineOfSight = false,
        AccessLevel accessLevel = AccessLevel.Player
    ) => OutgoingMessagePackets.BroadcastMessageLocalizedAffix(
        m_Map, m_Location,
        Serial, Body, type, hue, 3, number, Name, affixType, affix, args,
        new PublicVisibilityFilter(this, noLineOfSight, accessLevel)
    );

    // ---------- PrivateOverheadMessage ----------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrivateOverheadMessage(MessageType type, int hue, bool ascii, ReadOnlySpan<char> text, NetState state) =>
        state.SendMessage(Serial, Body, type, hue, 3, ascii, m_Language, Name, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrivateOverheadMessage(MessageType type, int hue, int number, NetState state) =>
        PrivateOverheadMessage(type, hue, number, default, state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrivateOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args, NetState state) =>
        state.SendMessageLocalized(Serial, Body, type, hue, 3, number, Name, args);

    // ---------- LocalOverheadMessage ----------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LocalOverheadMessage(MessageType type, int hue, bool ascii, ReadOnlySpan<char> text) =>
        m_NetState.SendMessage(Serial, Body, type, hue, 3, ascii, m_Language, Name, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LocalOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args = default) =>
        m_NetState.SendMessageLocalized(Serial, Body, type, hue, 3, number, Name, args);

    // ---------- NonlocalOverheadMessage ----------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NonlocalOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args = default) =>
        OutgoingMessagePackets.BroadcastMessageLocalized(
            m_Map, m_Location,
            Serial, Body, type, hue, 3, number, Name, args,
            new NonlocalVisibilityFilter(this, m_NetState)
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NonlocalOverheadMessage(MessageType type, int hue, bool ascii, ReadOnlySpan<char> text) =>
        OutgoingMessagePackets.BroadcastMessage(
            m_Map, m_Location,
            Serial, Body, type, hue, 3, ascii, Language, Name, text,
            new NonlocalVisibilityFilter(this, m_NetState)
        );

    // ---------- SendLocalizedMessage / SendMessage / SendAsciiMessage ----------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendLocalizedMessage(int number, ReadOnlySpan<char> args = default, int hue = 0x3B2) =>
        m_NetState.SendMessageLocalized(Serial.MinusOne, -1, MessageType.Regular, hue, 3, number, "System", args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendLocalizedMessage(
        int number, bool append, ReadOnlySpan<char> affix, ReadOnlySpan<char> args = default, int hue = 0x3B2
    ) => m_NetState.SendMessageLocalizedAffix(
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendMessage(ReadOnlySpan<char> text) => SendMessage(0x3B2, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendMessage(int hue, ReadOnlySpan<char> text) =>
        m_NetState.SendMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, false, "ENU", "System", text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendAsciiMessage(ReadOnlySpan<char> text) => SendAsciiMessage(0x3B2, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendAsciiMessage(int hue, ReadOnlySpan<char> text) =>
        m_NetState.SendMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, true, null, "System", text);

    // ---------- Interpolated handler overloads ----------

    public void Say(bool ascii, ref RawInterpolatedStringHandler text)
    {
        PublicOverheadMessage(MessageType.Regular, SpeechHue, ascii, text.Text);
        text.Clear();
    }

    public void Say(ref RawInterpolatedStringHandler text)
    {
        PublicOverheadMessage(MessageType.Regular, SpeechHue, false, text.Text);
        text.Clear();
    }

    public void Say(int number, AffixType type, ReadOnlySpan<char> affix, ref RawInterpolatedStringHandler args)
    {
        PublicOverheadMessage(MessageType.Regular, SpeechHue, number, type, affix, args.Text);
        args.Clear();
    }

    public void Say(int number, ref RawInterpolatedStringHandler args)
    {
        PublicOverheadMessage(MessageType.Regular, SpeechHue, number, args.Text);
        args.Clear();
    }

    public void Emote(ref RawInterpolatedStringHandler text)
    {
        PublicOverheadMessage(MessageType.Emote, EmoteHue, false, text.Text);
        text.Clear();
    }

    public void Emote(int number, ref RawInterpolatedStringHandler args)
    {
        PublicOverheadMessage(MessageType.Emote, EmoteHue, number, args.Text);
        args.Clear();
    }

    public void Whisper(ref RawInterpolatedStringHandler text)
    {
        PublicOverheadMessage(MessageType.Whisper, WhisperHue, false, text.Text);
        text.Clear();
    }

    public void Whisper(int number, ref RawInterpolatedStringHandler args)
    {
        PublicOverheadMessage(MessageType.Whisper, WhisperHue, number, args.Text);
        args.Clear();
    }

    public void Yell(ref RawInterpolatedStringHandler text)
    {
        PublicOverheadMessage(MessageType.Yell, YellHue, false, text.Text);
        text.Clear();
    }

    public void Yell(int number, ref RawInterpolatedStringHandler args)
    {
        PublicOverheadMessage(MessageType.Yell, YellHue, number, args.Text);
        args.Clear();
    }

    public void PublicOverheadMessage(
        MessageType type, int hue, bool ascii, ref RawInterpolatedStringHandler text, bool noLineOfSight = true,
        AccessLevel accessLevel = AccessLevel.Player
    )
    {
        PublicOverheadMessage(type, hue, ascii, text.Text, noLineOfSight, accessLevel);
        text.Clear();
    }

    public void PublicOverheadMessage(MessageType type, int hue, int number, ref RawInterpolatedStringHandler args, bool noLineOfSight = true)
    {
        PublicOverheadMessage(type, hue, number, args.Text, noLineOfSight);
        args.Clear();
    }

    public void PublicOverheadMessage(
        MessageType type, int hue, int number, AffixType affixType, ReadOnlySpan<char> affix,
        ref RawInterpolatedStringHandler args, bool noLineOfSight = false,
        AccessLevel accessLevel = AccessLevel.Player
    )
    {
        PublicOverheadMessage(type, hue, number, affixType, affix, args.Text, noLineOfSight, accessLevel);
        args.Clear();
    }

    public void PrivateOverheadMessage(
        MessageType type, int hue, bool ascii, ref RawInterpolatedStringHandler text, NetState state
    )
    {
        PrivateOverheadMessage(type, hue, ascii, text.Text, state);
        text.Clear();
    }

    public void PrivateOverheadMessage(
        MessageType type, int hue, int number, ref RawInterpolatedStringHandler args, NetState state
    )
    {
        PrivateOverheadMessage(type, hue, number, args.Text, state);
        args.Clear();
    }

    public void LocalOverheadMessage(MessageType type, int hue, bool ascii, ref RawInterpolatedStringHandler text)
    {
        LocalOverheadMessage(type, hue, ascii, text.Text);
        text.Clear();
    }

    public void LocalOverheadMessage(MessageType type, int hue, int number, ref RawInterpolatedStringHandler args)
    {
        LocalOverheadMessage(type, hue, number, args.Text);
        args.Clear();
    }

    public void NonlocalOverheadMessage(MessageType type, int hue, int number, ref RawInterpolatedStringHandler args)
    {
        NonlocalOverheadMessage(type, hue, number, args.Text);
        args.Clear();
    }

    public void NonlocalOverheadMessage(MessageType type, int hue, bool ascii, ref RawInterpolatedStringHandler text)
    {
        NonlocalOverheadMessage(type, hue, ascii, text.Text);
        text.Clear();
    }

    public void SendLocalizedMessage(int number, ref RawInterpolatedStringHandler args, int hue = 0x3B2)
    {
        SendLocalizedMessage(number, args.Text, hue);
        args.Clear();
    }

    public void SendLocalizedMessage(
        int number, bool append, ReadOnlySpan<char> affix, ref RawInterpolatedStringHandler args, int hue = 0x3B2
    )
    {
        SendLocalizedMessage(number, append, affix, args.Text, hue);
        args.Clear();
    }

    public void SendMessage(ref RawInterpolatedStringHandler text)
    {
        SendMessage(0x3B2, text.Text);
        text.Clear();
    }

    public void SendMessage(int hue, ref RawInterpolatedStringHandler text)
    {
        SendMessage(hue, text.Text);
        text.Clear();
    }

    public void SendAsciiMessage(ref RawInterpolatedStringHandler text)
    {
        SendAsciiMessage(0x3B2, text.Text);
        text.Clear();
    }

    public void SendAsciiMessage(int hue, ref RawInterpolatedStringHandler text)
    {
        SendAsciiMessage(hue, text.Text);
        text.Clear();
    }
}
