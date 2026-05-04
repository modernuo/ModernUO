/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingMessagePackets.Broadcast.cs                             *
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

public static partial class OutgoingMessagePackets
{
    public static void BroadcastMessageLocalized<TFilter>(
        Map map, Point3D location,
        Serial serial, int graphic, MessageType type, int hue, int font,
        int number, ReadOnlySpan<char> name, ReadOnlySpan<char> args,
        TFilter filter
    ) where TFilter : struct, IBroadcastFilter
    {
        if (map == null)
        {
            return;
        }

        var buffer = stackalloc byte[GetMaxMessageLocalizedLength(args.Length)].InitializePacket();

        foreach (var state in map.GetClientsInRange(location))
        {
            if (filter.Allow(state))
            {
                var length = CreateMessageLocalized(
                    buffer, serial, graphic, type, hue, font, number, name, args
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length];
                }

                state.Send(buffer);
            }
        }
    }

    public static void BroadcastMessageLocalized<TFilter>(
        Map map, Point3D location, int range,
        Serial serial, int graphic, MessageType type, int hue, int font,
        int number, ReadOnlySpan<char> name, ReadOnlySpan<char> args,
        TFilter filter
    ) where TFilter : struct, IBroadcastFilter
    {
        if (map == null)
        {
            return;
        }

        var buffer = stackalloc byte[GetMaxMessageLocalizedLength(args.Length)].InitializePacket();

        foreach (var state in map.GetClientsInRange(location, range))
        {
            if (filter.Allow(state))
            {
                var length = CreateMessageLocalized(
                    buffer, serial, graphic, type, hue, font, number, name, args
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length];
                }

                state.Send(buffer);
            }
        }
    }

    public static void BroadcastMessageLocalizedAffix<TFilter>(
        Map map, Point3D location,
        Serial serial, int graphic, MessageType type, int hue, int font,
        int number, ReadOnlySpan<char> name, AffixType affixType,
        ReadOnlySpan<char> affix, ReadOnlySpan<char> args,
        TFilter filter
    ) where TFilter : struct, IBroadcastFilter
    {
        if (map == null)
        {
            return;
        }

        var buffer = stackalloc byte[GetMaxMessageLocalizedAffixLength(affix.Length, args.Length)].InitializePacket();

        foreach (var state in map.GetClientsInRange(location))
        {
            if (filter.Allow(state))
            {
                var length = CreateMessageLocalizedAffix(
                    buffer, serial, graphic, type, hue, font, number, name, affixType, affix, args
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length];
                }

                state.Send(buffer);
            }
        }
    }

    public static void BroadcastMessage<TFilter>(
        Map map, Point3D location,
        Serial serial, int graphic, MessageType type, int hue, int font, bool ascii,
        string lang, ReadOnlySpan<char> name, ReadOnlySpan<char> text,
        TFilter filter
    ) where TFilter : struct, IBroadcastFilter
    {
        if (map == null)
        {
            return;
        }

        var buffer = stackalloc byte[GetMaxMessageLength(text.Length)].InitializePacket();

        foreach (var state in map.GetClientsInRange(location))
        {
            if (filter.Allow(state))
            {
                var length = CreateMessage(
                    buffer, serial, graphic, type, hue, font, ascii, lang, name, text
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length];
                }

                state.Send(buffer);
            }
        }
    }

    public static void BroadcastMessage<TFilter>(
        Map map, Point3D location, int range,
        Serial serial, int graphic, MessageType type, int hue, int font, bool ascii,
        string lang, ReadOnlySpan<char> name, ReadOnlySpan<char> text,
        TFilter filter
    ) where TFilter : struct, IBroadcastFilter
    {
        if (map == null)
        {
            return;
        }

        var buffer = stackalloc byte[GetMaxMessageLength(text.Length)].InitializePacket();

        foreach (var state in map.GetClientsInRange(location, range))
        {
            if (filter.Allow(state))
            {
                var length = CreateMessage(
                    buffer, serial, graphic, type, hue, font, ascii, lang, name, text
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length];
                }

                state.Send(buffer);
            }
        }
    }
}
