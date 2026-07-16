/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IGenericWriter.cs                                               *
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
using System.Collections;
using System.IO;
using System.Net;

namespace Server;

public interface IGenericWriter
{
    long Position { get; }
    void Write(string value);
    void Write(long value);
    void Write(ulong value);
    void Write(int value);
    void Write(uint value);
    void Write(short value);
    void Write(ushort value);
    void Write(double value);
    void Write(float value);
    void Write(byte value);
    void Write(sbyte value);
    void Write(bool value);
    void Write(Serial serial);
    void Write(Type type);
    void Write(decimal value);
    void WriteEncodedInt(int value);
    void Write(DateTime value);
    void WriteDeltaTime(DateTime value);
    void Write(IPAddress value);
    void Write(TimeSpan value);
    void Write(Point3D value);
    void Write(Point2D value);
    void Write(Rectangle2D value);
    void Write(Rectangle3D value);
    void Write(Map value);
    void Write(Race value);
    void Write(byte[] bytes);
    void Write(byte[] bytes, int offset, int count);
    void Write(ReadOnlySpan<byte> bytes);
    void WriteEnum<T>(T value) where T : unmanaged, Enum;
    void Write(Guid guid);
    void Write(BitArray bitArray);
    void Write(TextDefinition def);

    long Seek(long offset, SeekOrigin origin);
}
