/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingMessagePackets.Interpolated.cs                          *
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

namespace Server.Network;

public static partial class OutgoingMessagePackets
{
    public static void SendMessageLocalized(
        this NetState ns,
        Serial serial, int graphic, MessageType type, int hue, int font, int number,
        ReadOnlySpan<char> name,
        ref RawInterpolatedStringHandler args
    )
    {
        ns.SendMessageLocalized(serial, graphic, type, hue, font, number, name, args.Text);
        args.Clear();
    }

    public static void SendMessageLocalizedAffix(
        this NetState ns,
        Serial serial, int graphic, MessageType type, int hue, int font, int number,
        ReadOnlySpan<char> name, AffixType affixType, ReadOnlySpan<char> affix,
        ref RawInterpolatedStringHandler args
    )
    {
        ns.SendMessageLocalizedAffix(
            serial, graphic, type, hue, font, number, name, affixType, affix, args.Text
        );
        args.Clear();
    }

    public static void SendMessage(
        this NetState ns,
        Serial serial, int graphic, MessageType type, int hue, int font, bool ascii,
        string lang, ReadOnlySpan<char> name,
        ref RawInterpolatedStringHandler text
    )
    {
        ns.SendMessage(serial, graphic, type, hue, font, ascii, lang, name, text.Text);
        text.Clear();
    }
}
