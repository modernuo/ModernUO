/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IGenericBufferWriter.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.IO;
using System.Text;

namespace System.Buffers
{
    // Cannot be implemented directly. Used as a guide to build writers with standard API
    public interface IGenericBufferWriter
    {
        int Length { get; set; }
        int Position { get; }

        void Write(bool value);
        void Write(byte value);
        void Write(sbyte value);
        void Write(short value);
        void Write(ushort value);
        void Write(int value);
        void Write(uint value);
        void Write(long value);
        void Write(ulong value);

        void Write(ReadOnlySpan<byte> buffer);

        void WriteAscii(string value);
        void WriteAscii(string value, int size);
        void WriteAsciiNull(string value);

        void WriteBigUni(string value);
        void WriteBigUni(string value, int size);
        void WriteBigUniNull(string value);

        void WriteLittleUni(string value);
        void WriteLittleUniNull(string value);
        void WriteLittleUni(string value, int size);

        void WriteUTF8(string value);
        void WriteUTF8Null(string value);

        void WriteString(string value, Encoding encoding, int fixedLength = -1);

        void CopyTo(Span<byte> destination);
        void Fill(int count);

        public int Seek(int offset, SeekOrigin origin);
    }
}
