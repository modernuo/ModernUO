/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpeechEventArgsExt.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System;

namespace Server;

public static class SpeechEventArgsExt
{
    public static int GetFirstKeyword(this SpeechEventArgs e, params ReadOnlySpan<int> keywords)
    {
        for (var i = 0; i < keywords.Length; i++)
        {
            var keyword = keywords[i];
            if (e.HasKeyword(keyword))
            {
                return keyword;
            }
        }

        return 0;
    }
}
