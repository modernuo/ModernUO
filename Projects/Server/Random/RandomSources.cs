/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RandomSources.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Random;

public static class RandomSources
{
    private static IRandomSource _source;
    private static IRandomSource _secureSource;

    public static IRandomSource Source => _source ??= new Xoshiro256PlusPlus();
    public static IRandomSource SecureSource => _secureSource ??= new SecureRandom();

    public static void SetRng(IRandomSource newSource) => _source = newSource;

    public static void SetSecureRng(IRandomSource newSource) => _secureSource = newSource;
}
