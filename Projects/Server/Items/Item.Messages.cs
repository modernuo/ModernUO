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
using Server.Buffers;
using Server.Network;

namespace Server;

public partial class Item
{
    public void PublicOverheadMessage(MessageType type, int hue, bool ascii, ReadOnlySpan<char> text)
    {
        if (m_Map == null)
        {
            return;
        }

        var worldLoc = GetWorldLocation();

        var buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text.Length)].InitializePacket();

        foreach (var state in m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange()))
        {
            var m = state.Mobile;

            if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
            {
                var length = OutgoingMessagePackets.CreateMessage(
                    buffer, Serial, m_ItemID, type, hue, 3, ascii, "ENU", Name, text
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length]; // Adjust to the actual size
                }

                state.Send(buffer);
            }
        }
    }

    public void PublicOverheadMessage(MessageType type, int hue, int number, ReadOnlySpan<char> args = default)
    {
        if (m_Map == null)
        {
            return;
        }

        var worldLoc = GetWorldLocation();

        var buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedLength(args.Length)].InitializePacket();

        foreach (var state in m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange()))
        {
            var m = state.Mobile;

            if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
            {
                var length = OutgoingMessagePackets.CreateMessageLocalized(
                    buffer, Serial, m_ItemID, type, hue, 3, number, Name, args
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length]; // Adjust to the actual size
                }

                state.Send(buffer);
            }
        }
    }

    public void SendLocalizedMessageTo(Mobile to, int number, ReadOnlySpan<char> args = default)
    {
        if (Deleted || !to.CanSee(this))
        {
            return;
        }

        to.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", args);
    }

    public void SendLocalizedMessageTo(Mobile to, int number, AffixType affixType, ReadOnlySpan<char> affix, ReadOnlySpan<char> args)
    {
        if (Deleted || !to.CanSee(this))
        {
            return;
        }

        to.NetState.SendMessageLocalizedAffix(
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
}
