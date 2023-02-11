/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MessageType.cs                                                  *
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

namespace Server;

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
