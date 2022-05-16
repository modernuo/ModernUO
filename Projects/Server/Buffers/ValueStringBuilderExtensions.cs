/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ValueStringBuilderExtensions.cs                                 *
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

namespace Server.Buffers;

public static class ValueStringBuilderExtensions
{
    // Compiler generated
    public static void Append(
        this ref ValueStringBuilder stringBuilder,
        [InterpolatedStringHandlerArgument("stringBuilder")]
        ref ValueStringBuilder.AppendInterpolatedStringHandler handler)
    {
        // Reassign since the string builder stored on the interpolated handler is by-value
        stringBuilder = handler._stringBuilder;
    }
}
