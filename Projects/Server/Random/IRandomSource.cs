/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IRandomSource.cs                                                *
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

namespace Server.Random;

public interface IRandomSource
{
    int Next();
    int Next(int maxValue);
    int Next(int minValue, int count);
    uint Next(uint maxValue);
    uint Next(uint minValue, uint count);
    long Next(long maxValue);
    long Next(long minValue, long count);
    double NextDouble();
    void NextBytes(Span<byte> buffer);
    int NextInt();
    uint NextUInt();
    ulong NextULong();
    bool NextBool();
    byte NextByte();
    float NextFloat();
    float NextFloatNonZero();
    double NextDoubleNonZero();
    double NextDoubleHighRes();
}
