/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ClientVersionExtensions.cs                                      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;

namespace Server;

public static class ClientVersionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TypeName(this ClientType type) =>
        type switch
        {
            ClientType.UOTD => "UO:TD",
            ClientType.KR   => "UO:KR",
            ClientType.SA   => "UO:SA",
            _               => "classic",
        };
}
