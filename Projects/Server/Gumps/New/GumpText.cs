/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpText.cs                                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Gumps;

public static class GumpText
{
    public static string Color(string? text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";
    public static string Center(string? text) => $"<CENTER>{text}</CENTER>";
    public static string Center(string? text, int color) => $"<BASEFONT COLOR=#{color:X6}><CENTER>{text}</CENTER></BASEFONT>";
}