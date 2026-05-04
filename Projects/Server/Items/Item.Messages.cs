/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Item.Messages.cs                                                *
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

public partial class Item
{
    private readonly struct PublicVisibilityFilter : IBroadcastFilter
    {
        private readonly Item _source;
        private readonly Point3D _worldLoc;

        public PublicVisibilityFilter(Item source, Point3D worldLoc)
        {
            _source = source;
            _worldLoc = worldLoc;
        }

        public bool Allow(NetState state)
        {
            var m = state.Mobile;
            return m.CanSee(_source) && m.InRange(_worldLoc, _source.GetUpdateRange(m));
        }
    }

    public void PublicOverheadMessage(MessageType type, int hue, bool ascii, ReadOnlySpan<char> text)
    {
        var worldLoc = GetWorldLocation();
        OutgoingMessagePackets.BroadcastMessage(
            m_Map, worldLoc, GetMaxUpdateRange(),
            Serial, m_ItemID, type, hue, 3, ascii, "ENU", Name, text,
            new PublicVisibilityFilter(this, worldLoc)
        );
    }

    public void PublicOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args = default)
    {
        var worldLoc = GetWorldLocation();
        OutgoingMessagePackets.BroadcastMessageLocalized(
            m_Map, worldLoc, GetMaxUpdateRange(),
            Serial, m_ItemID, type, hue, 3, number, Name, args,
            new PublicVisibilityFilter(this, worldLoc)
        );
    }

    public void SendLocalizedMessageTo(Mobile to, int number, ReadOnlySpan<char> args = default)
    {
        if (Deleted || !to.CanSee(this))
        {
            return;
        }

        to?.NetState?.SendMessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", args);
    }

    public void SendLocalizedMessageTo(Mobile to, int number, AffixType affixType, ReadOnlySpan<char> affix, ReadOnlySpan<char> args)
    {
        if (Deleted || !to.CanSee(this))
        {
            return;
        }

        to?.NetState?.SendMessageLocalizedAffix(
            Serial,
            ItemID,
            MessageType.Regular,
            0x3B2,
            3,
            number,
            "",
            affixType,
            affix,
            args
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendLocalizedMessageTo(Mobile to, int number, int hue, ReadOnlySpan<char> args = default)
        => to?.NetState?.SendMessageLocalized(Serial, ItemID, MessageType.Regular, hue, 3, number, "", args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendMessageTo(Mobile to, ReadOnlySpan<char> text, int hue = 0x3B2)
        => to?.NetState?.SendMessage(Serial, ItemID, MessageType.Regular, hue, 3, false, "ENU", "", text);

    // ---------- Interpolated handler overloads ----------

    public void PublicOverheadMessage(MessageType type, int hue, bool ascii, ref RawInterpolatedStringHandler text)
    {
        PublicOverheadMessage(type, hue, ascii, text.Text);
        text.Clear();
    }

    public void PublicOverheadMessage(MessageType type, int hue, int number, ref RawInterpolatedStringHandler args)
    {
        PublicOverheadMessage(type, hue, number, args.Text);
        args.Clear();
    }

    public void SendLocalizedMessageTo(Mobile to, int number, ref RawInterpolatedStringHandler args)
    {
        SendLocalizedMessageTo(to, number, args.Text);
        args.Clear();
    }

    public void SendLocalizedMessageTo(Mobile to, int number, AffixType affixType, ReadOnlySpan<char> affix, ref RawInterpolatedStringHandler args)
    {
        SendLocalizedMessageTo(to, number, affixType, affix, args.Text);
        args.Clear();
    }

    public void SendLocalizedMessageTo(Mobile to, int number, int hue, ref RawInterpolatedStringHandler args)
    {
        SendLocalizedMessageTo(to, number, hue, args.Text);
        args.Clear();
    }

    public void SendMessageTo(Mobile to, int hue, ref RawInterpolatedStringHandler text)
    {
        SendMessageTo(to, text.Text, hue);
        text.Clear();
    }
}
