/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BufferedFileWriter.cs                                           *
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

namespace Server
{
    public class BinaryFileWriter : BufferWriter
    {
        private readonly Stream m_File;
        private long m_Position;

        public BinaryFileWriter(string filename, bool prefixStr) : base(prefixStr) =>
            m_File = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);

        public BinaryFileWriter(Stream stream, bool prefixStr) : base(prefixStr)
        {
            m_File = stream;
            m_Position = m_File.Position;
        }

        public override long Position => m_Position + Index;


        protected override int BufferSize => 512;

        public override void Flush()
        {
            if (Index > 0)
            {
                m_Position += Index;

                m_File.Write(Buffer, 0, (int)Index);
                Index = 0;
            }
        }

        public override void Close()
        {
            if (Index > 0)
            {
                Flush();
            }

            m_File.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Flush();

            return m_Position = m_File.Seek(offset, origin);
        }
    }
}
