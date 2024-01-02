/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpInterpolationTextFormatters.cs                              *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Gumps.Components.Interpolation;

public static class GumpInterpolationTextFormatters
{
    public readonly struct None : IGumpInterpolationTextFormatter<None>
    {
        public string? Begin => null;
        public string? End => null;

        public static None Create() => default;
        public static None Create(int color) => default;
    }

    public readonly struct Centered : IGumpInterpolationTextFormatter<Centered>
    {
        public string? Begin { get; }
        public string? End { get; }

        public Centered()
        {
            Begin = "<CENTER>";
            End = "</CENTER>";
        }

        public Centered(int color)
        {
            Begin = $"<BASEFONT COLOR=#{color:X6}><CENTER>";
            End = "</CENTER></BASEFONT>";
        }

        public static Centered Create() => new();
        public static Centered Create(int color) => new(color);
    }

    public readonly struct Colored : IGumpInterpolationTextFormatter<Colored>
    {
        public string? Begin { get; }
        public string? End { get; }

        public Colored(int color)
        {
            Begin = $"<BASEFONT COLOR=#{color:X6}>";
            End = "</BASEFONT>";
        }

        public static Colored Create() => default;
        public static Colored Create(int color) => new(color);
    }
}
