/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ProtocolChanges.cs                                              *
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
public enum ProtocolChanges
{
    None = 0x00000000,
    NewSpellbook = 0x00000001,
    DamagePacket = 0x00000002,
    Unpack = 0x00000004,
    BuffIcon = 0x00000008,
    NewHaven = 0x00000010,
    ContainerGridLines = 0x00000020,
    ExtendedSupportedFeatures = 0x00000040,
    StygianAbyss = 0x00000080,
    HighSeas = 0x00000100,
    NewCharacterList = 0x00000200,
    NewCharacterCreation = 0x00000400,
    ExtendedStatus = 0x00000800,
    NewMobileIncoming = 0x00001000,
    NewSecureTrading = 0x00002000,
    UltimaStore = 0x00004000,
    EndlessJourney = 0x00008000,

    Version400a = NewSpellbook,
    Version407a = Version400a | DamagePacket,
    Version500a = Version407a | Unpack,
    Version502b = Version500a | BuffIcon,
    Version6000 = Version502b | NewHaven,
    Version6017 = Version6000 | ContainerGridLines,
    Version60142 = Version6017 | ExtendedSupportedFeatures,
    Version7000 = Version60142 | StygianAbyss,
    Version7090 = Version7000 | HighSeas,
    Version70130 = Version7090 | NewCharacterList,
    Version70160 = Version70130 | NewCharacterCreation,
    Version70300 = Version70160 | ExtendedStatus,
    Version70331 = Version70300 | NewMobileIncoming,
    Version704565 = Version70331 | NewSecureTrading,
    Version70500 = Version704565 | UltimaStore,
    Version70610 = Version70500 | EndlessJourney
}
